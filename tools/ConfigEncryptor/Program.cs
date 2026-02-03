using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DeskWarrior.Tools.ConfigEncryptor;

/// <summary>
/// Config 파일 암호화 도구
/// 빌드 시 config/*.json 파일들을 암호화하여 Resources/EncryptedConfig/*.enc로 저장
/// </summary>
class Program
{
    // SecurityConfig와 동일한 상수 (독립 실행 도구이므로 복제)
    const string MAGIC = "DWCF"; // DeskWarrior Config File
    const byte VERSION = 1;
    const int NONCE_SIZE = 12;
    const int TAG_SIZE = 16;
    const int KEY_SIZE = 32;
    const int KEY_ITERATIONS = 100000;

    // KeyManager.ConfigKeySalt와 동일
    static readonly byte[] ConfigKeySalt = new byte[]
    {
        0x43, 0x6F, 0x6E, 0x66, 0x69, 0x67, 0x50, 0x72,
        0x6F, 0x74, 0x65, 0x63, 0x74, 0x69, 0x6F, 0x6E,
        0x4B, 0x65, 0x79, 0x46, 0x6F, 0x72, 0x47, 0x61,
        0x6D, 0x65, 0x44, 0x61, 0x74, 0x61, 0x21, 0x21
    };

    static readonly byte[] BaseKeySalt = new byte[]
    {
        0x44, 0x65, 0x73, 0x6B, 0x57, 0x61, 0x72, 0x72,
        0x69, 0x6F, 0x72, 0x53, 0x65, 0x63, 0x75, 0x72,
        0x69, 0x74, 0x79, 0x4B, 0x65, 0x79, 0x32, 0x30,
        0x32, 0x35, 0x21, 0x40, 0x23, 0x24, 0x25, 0x5E
    };

    static int Main(string[] args)
    {
        try
        {
            // 기본 경로 설정
            string projectRoot = args.Length > 0 ? args[0] : FindProjectRoot();
            string configDir = Path.Combine(projectRoot, "config");
            string outputDir = Path.Combine(projectRoot, "Resources", "EncryptedConfig");

            Console.WriteLine($"[ConfigEncryptor] Project root: {projectRoot}");
            Console.WriteLine($"[ConfigEncryptor] Config dir: {configDir}");
            Console.WriteLine($"[ConfigEncryptor] Output dir: {outputDir}");

            if (!Directory.Exists(configDir))
            {
                Console.WriteLine($"[ConfigEncryptor] ERROR: Config directory not found: {configDir}");
                return 1;
            }

            // 출력 디렉토리 생성
            Directory.CreateDirectory(outputDir);

            // Config 키 파생
            byte[] configKey = DeriveConfigKey();
            Console.WriteLine("[ConfigEncryptor] Config key derived");

            int successCount = 0;
            int errorCount = 0;

            // config 디렉토리의 모든 JSON 파일 암호화
            foreach (var jsonFile in Directory.GetFiles(configDir, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    // 상대 경로 유지
                    string relativePath = Path.GetRelativePath(configDir, jsonFile);
                    string outputPath = Path.Combine(outputDir, Path.ChangeExtension(relativePath, ".enc"));

                    // 출력 서브디렉토리 생성
                    string? outputSubDir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(outputSubDir))
                    {
                        Directory.CreateDirectory(outputSubDir);
                    }

                    // 파일 암호화
                    EncryptFile(jsonFile, outputPath, configKey);
                    Console.WriteLine($"[ConfigEncryptor] Encrypted: {relativePath} -> {Path.ChangeExtension(relativePath, ".enc")}");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ConfigEncryptor] ERROR encrypting {jsonFile}: {ex.Message}");
                    errorCount++;
                }
            }

            Console.WriteLine($"[ConfigEncryptor] Completed: {successCount} files encrypted, {errorCount} errors");
            return errorCount > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ConfigEncryptor] FATAL ERROR: {ex.Message}");
            return 1;
        }
    }

    static string FindProjectRoot()
    {
        // 현재 디렉토리에서 위로 올라가며 DeskWarrior.csproj 찾기
        string? current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "DeskWarrior.csproj")))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }

        // 못 찾으면 현재 디렉토리 사용
        return Directory.GetCurrentDirectory();
    }

    static byte[] DeriveConfigKey()
    {
        using var kdf = new Rfc2898DeriveBytes(
            ConfigKeySalt,
            BaseKeySalt,
            KEY_ITERATIONS,
            HashAlgorithmName.SHA256);

        return kdf.GetBytes(KEY_SIZE);
    }

    static void EncryptFile(string inputPath, string outputPath, byte[] key)
    {
        // 파일 읽기
        byte[] plaintext = File.ReadAllBytes(inputPath);

        // AES-GCM 암호화
        byte[] nonce = RandomNumberGenerator.GetBytes(NONCE_SIZE);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TAG_SIZE];

        using var aes = new AesGcm(key, TAG_SIZE);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // 파일 포맷: [MAGIC:4][VER:1][NONCE:12][TAG:16][CIPHERTEXT:N]
        using var fs = File.Create(outputPath);
        fs.Write(Encoding.ASCII.GetBytes(MAGIC)); // 4 bytes
        fs.WriteByte(VERSION);                     // 1 byte
        fs.Write(nonce);                           // 12 bytes
        fs.Write(tag);                             // 16 bytes
        fs.Write(ciphertext);                      // N bytes
    }
}
