using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DeskWarrior.Security
{
    /// <summary>
    /// 데이터 암호화/복호화 유틸리티
    /// AES-256-GCM 사용 (인증된 암호화)
    /// </summary>
    public static class DataProtection
    {
        /// <summary>
        /// 데이터 암호화 (AES-256-GCM)
        /// </summary>
        /// <param name="plaintext">평문 데이터</param>
        /// <param name="key">256-bit 키</param>
        /// <returns>(암호문, nonce, tag) 튜플</returns>
        public static (byte[] ciphertext, byte[] nonce, byte[] tag) Encrypt(byte[] plaintext, byte[] key)
        {
            var nonce = RandomNumberGenerator.GetBytes(SecurityConfig.NONCE_SIZE);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[SecurityConfig.TAG_SIZE];

            using var aes = new AesGcm(key, SecurityConfig.TAG_SIZE);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            return (ciphertext, nonce, tag);
        }

        /// <summary>
        /// 데이터 복호화 (AES-256-GCM)
        /// </summary>
        /// <param name="ciphertext">암호문</param>
        /// <param name="key">256-bit 키</param>
        /// <param name="nonce">12-byte nonce</param>
        /// <param name="tag">16-byte 인증 태그</param>
        /// <returns>복호화된 평문</returns>
        public static byte[] Decrypt(byte[] ciphertext, byte[] key, byte[] nonce, byte[] tag)
        {
            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(key, SecurityConfig.TAG_SIZE);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return plaintext;
        }

        /// <summary>
        /// 전체 암호화 패키지 생성
        /// 포맷: [MAGIC:4][VER:1][NONCE:12][HMAC:32][TAG:16][CIPHERTEXT:N]
        /// </summary>
        public static byte[] EncryptWithHeader(byte[] plaintext, byte[] encryptKey, byte[] hmacKey)
        {
            var (ciphertext, nonce, tag) = Encrypt(plaintext, encryptKey);
            var hmac = IntegrityValidator.ComputeHmac(ciphertext, hmacKey);

            using var ms = new MemoryStream();
            // 헤더 작성
            ms.Write(Encoding.ASCII.GetBytes(SecurityConfig.MAGIC)); // 4 bytes
            ms.WriteByte(SecurityConfig.VERSION);                     // 1 byte
            ms.Write(nonce);                                          // 12 bytes
            ms.Write(hmac);                                           // 32 bytes
            ms.Write(tag);                                            // 16 bytes
            ms.Write(ciphertext);                                     // N bytes

            return ms.ToArray();
        }

        /// <summary>
        /// 암호화 패키지 복호화
        /// </summary>
        /// <param name="encryptedPackage">암호화된 전체 패키지</param>
        /// <param name="encryptKey">복호화 키</param>
        /// <param name="hmacKey">HMAC 검증 키</param>
        /// <returns>복호화된 평문 (실패 시 null)</returns>
        public static byte[]? DecryptWithHeader(byte[] encryptedPackage, byte[] encryptKey, byte[] hmacKey)
        {
            const int HEADER_SIZE = 4 + 1 + SecurityConfig.NONCE_SIZE + SecurityConfig.HMAC_SIZE + SecurityConfig.TAG_SIZE;

            if (encryptedPackage.Length < HEADER_SIZE)
                return null;

            using var ms = new MemoryStream(encryptedPackage);
            using var reader = new BinaryReader(ms);

            // 매직 넘버 검증
            var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != SecurityConfig.MAGIC)
                return null;

            // 버전 검증
            var version = reader.ReadByte();
            if (version != SecurityConfig.VERSION)
                return null;

            // 헤더 파싱
            var nonce = reader.ReadBytes(SecurityConfig.NONCE_SIZE);
            var storedHmac = reader.ReadBytes(SecurityConfig.HMAC_SIZE);
            var tag = reader.ReadBytes(SecurityConfig.TAG_SIZE);

            // 나머지가 암호문
            var ciphertext = reader.ReadBytes((int)(ms.Length - ms.Position));

            // HMAC 무결성 검증
            if (!IntegrityValidator.ValidateHmac(ciphertext, storedHmac, hmacKey))
                return null;

            try
            {
                return Decrypt(ciphertext, encryptKey, nonce, tag);
            }
            catch (CryptographicException)
            {
                // 복호화 실패 (키 불일치 또는 데이터 변조)
                return null;
            }
        }

        /// <summary>
        /// 바이트 배열이 암호화된 패키지인지 확인
        /// </summary>
        public static bool IsEncryptedPackage(byte[] data)
        {
            if (data.Length < 5) return false;

            var magic = Encoding.ASCII.GetString(data, 0, 4);
            return magic == SecurityConfig.MAGIC;
        }

        /// <summary>
        /// 바이트 배열이 평문 JSON인지 확인
        /// </summary>
        public static bool IsPlainJson(byte[] data)
        {
            if (data.Length == 0) return false;

            // UTF-8 BOM 스킵
            int start = 0;
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
                start = 3;

            // 공백 스킵
            while (start < data.Length && (data[start] == ' ' || data[start] == '\t' || data[start] == '\n' || data[start] == '\r'))
                start++;

            if (start >= data.Length) return false;

            // JSON은 { 또는 [ 로 시작
            return data[start] == '{' || data[start] == '[';
        }
    }
}
