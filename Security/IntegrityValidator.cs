using System;
using System.Security.Cryptography;

namespace DeskWarrior.Security
{
    /// <summary>
    /// 데이터 무결성 검증 유틸리티
    /// HMAC-SHA256 기반
    /// </summary>
    public static class IntegrityValidator
    {
        /// <summary>
        /// HMAC-SHA256 계산
        /// </summary>
        /// <param name="data">데이터</param>
        /// <param name="key">HMAC 키</param>
        /// <returns>32-byte HMAC 값</returns>
        public static byte[] ComputeHmac(byte[] data, byte[] key)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(data);
        }

        /// <summary>
        /// HMAC 검증 (타이밍 공격 방지)
        /// </summary>
        /// <param name="data">원본 데이터</param>
        /// <param name="expectedHmac">기대 HMAC 값</param>
        /// <param name="key">HMAC 키</param>
        /// <returns>검증 성공 여부</returns>
        public static bool ValidateHmac(byte[] data, byte[] expectedHmac, byte[] key)
        {
            var computed = ComputeHmac(data, key);
            return CryptographicOperations.FixedTimeEquals(computed, expectedHmac);
        }

        /// <summary>
        /// SHA256 해시 계산
        /// </summary>
        public static byte[] ComputeSha256(byte[] data)
        {
            return SHA256.HashData(data);
        }

        /// <summary>
        /// 파일 SHA256 해시 계산
        /// </summary>
        public static byte[] ComputeFileSha256(string filePath)
        {
            using var fs = System.IO.File.OpenRead(filePath);
            return SHA256.HashData(fs);
        }
    }
}
