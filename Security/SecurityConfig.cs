namespace DeskWarrior.Security
{
    /// <summary>
    /// 보안 기능 설정 - DEBUG/RELEASE 모드 분리
    /// </summary>
    public static class SecurityConfig
    {
#if DEBUG
        /// <summary>
        /// DEBUG 빌드: 보안 기능 비활성화 (개발 편의)
        /// </summary>
        public static bool SecurityEnabled => false;
#else
        /// <summary>
        /// RELEASE 빌드: 보안 기능 활성화
        /// </summary>
        public static bool SecurityEnabled => true;
#endif

        /// <summary>
        /// 암호화 파일 매직 넘버 (DeskWarrior Secure Version)
        /// </summary>
        public const string MAGIC = "DWSV";

        /// <summary>
        /// 현재 암호화 포맷 버전
        /// </summary>
        public const byte VERSION = 1;

        /// <summary>
        /// AES-GCM Nonce 크기 (12 bytes)
        /// </summary>
        public const int NONCE_SIZE = 12;

        /// <summary>
        /// AES-GCM Tag 크기 (16 bytes)
        /// </summary>
        public const int TAG_SIZE = 16;

        /// <summary>
        /// HMAC-SHA256 크기 (32 bytes)
        /// </summary>
        public const int HMAC_SIZE = 32;

        /// <summary>
        /// 키 파생 반복 횟수
        /// </summary>
        public const int KEY_ITERATIONS = 100000;

        /// <summary>
        /// AES 키 크기 (256-bit = 32 bytes)
        /// </summary>
        public const int KEY_SIZE = 32;
    }
}
