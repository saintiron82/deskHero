using System;
using System.Security.Cryptography;
using System.Text;

namespace DeskWarrior.Security
{
    /// <summary>
    /// 암호화 키 관리자
    /// 머신 + 사용자 기반 키 파생으로 다른 환경에서 복호화 방지
    /// </summary>
    public static class KeyManager
    {
        // 난독화 후에는 이 값들이 보호됨
        private static readonly byte[] BaseKeySalt = new byte[]
        {
            0x44, 0x65, 0x73, 0x6B, 0x57, 0x61, 0x72, 0x72,
            0x69, 0x6F, 0x72, 0x53, 0x65, 0x63, 0x75, 0x72,
            0x69, 0x74, 0x79, 0x4B, 0x65, 0x79, 0x32, 0x30,
            0x32, 0x35, 0x21, 0x40, 0x23, 0x24, 0x25, 0x5E
        }; // "DeskWarriorSecurityKey2025!@#$%^"

        private static readonly byte[] ConfigKeySalt = new byte[]
        {
            0x43, 0x6F, 0x6E, 0x66, 0x69, 0x67, 0x50, 0x72,
            0x6F, 0x74, 0x65, 0x63, 0x74, 0x69, 0x6F, 0x6E,
            0x4B, 0x65, 0x79, 0x46, 0x6F, 0x72, 0x47, 0x61,
            0x6D, 0x65, 0x44, 0x61, 0x74, 0x61, 0x21, 0x21
        }; // "ConfigProtectionKeyForGameData!!"

        private static byte[]? _cachedUserKey;
        private static byte[]? _cachedConfigKey;

        /// <summary>
        /// 사용자 데이터 암호화용 키 파생
        /// 머신명 + 사용자명 기반으로 다른 PC에서 복호화 불가
        /// </summary>
        public static byte[] DeriveUserKey()
        {
            if (_cachedUserKey != null) return _cachedUserKey;

            var machineIdentifier = GetMachineIdentifier();
            using var kdf = new Rfc2898DeriveBytes(
                Encoding.UTF8.GetBytes(machineIdentifier),
                BaseKeySalt,
                SecurityConfig.KEY_ITERATIONS,
                HashAlgorithmName.SHA256);

            _cachedUserKey = kdf.GetBytes(SecurityConfig.KEY_SIZE);
            return _cachedUserKey;
        }

        /// <summary>
        /// Config 파일 암호화용 키 파생
        /// 모든 머신에서 동일한 키 (배포된 설정 파일용)
        /// </summary>
        public static byte[] DeriveConfigKey()
        {
            if (_cachedConfigKey != null) return _cachedConfigKey;

            using var kdf = new Rfc2898DeriveBytes(
                ConfigKeySalt,
                BaseKeySalt,
                SecurityConfig.KEY_ITERATIONS,
                HashAlgorithmName.SHA256);

            _cachedConfigKey = kdf.GetBytes(SecurityConfig.KEY_SIZE);
            return _cachedConfigKey;
        }

        /// <summary>
        /// HMAC 키 파생 (무결성 검증용)
        /// </summary>
        public static byte[] DeriveHmacKey()
        {
            var baseKey = DeriveUserKey();
            using var kdf = new Rfc2898DeriveBytes(
                baseKey,
                Encoding.UTF8.GetBytes("HMAC_INTEGRITY"),
                10000,
                HashAlgorithmName.SHA256);

            return kdf.GetBytes(SecurityConfig.KEY_SIZE);
        }

        /// <summary>
        /// 머신 고유 식별자 생성
        /// </summary>
        private static string GetMachineIdentifier()
        {
            var machineName = Environment.MachineName;
            var userName = Environment.UserName;
            var osVersion = Environment.OSVersion.VersionString;

            return $"{machineName}:{userName}:{osVersion}";
        }

        /// <summary>
        /// 키 캐시 초기화 (테스트용)
        /// </summary>
        internal static void ClearCache()
        {
            _cachedUserKey = null;
            _cachedConfigKey = null;
        }
    }
}
