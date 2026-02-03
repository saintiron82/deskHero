using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DeskWarrior.Security
{
    /// <summary>
    /// 암호화된 Config 파일 로더
    /// RELEASE: 임베딩된 암호화 리소스에서 로드
    /// DEBUG: 외부 JSON 파일에서 직접 로드
    /// </summary>
    public static class SecureConfigLoader
    {
        private const string CONFIG_MAGIC = "DWCF";
        private const int CONFIG_HEADER_SIZE = 4 + 1 + SecurityConfig.NONCE_SIZE + SecurityConfig.TAG_SIZE;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Config 파일 로드
        /// </summary>
        /// <typeparam name="T">Config 타입</typeparam>
        /// <param name="configName">Config 파일명 (확장자 제외)</param>
        /// <returns>역직렬화된 Config 객체</returns>
        public static T Load<T>(string configName) where T : class, new()
        {
            try
            {
                if (SecurityConfig.SecurityEnabled)
                {
                    return LoadEncrypted<T>(configName);
                }
                else
                {
                    return LoadPlainJson<T>(configName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureConfigLoader] Failed to load {configName}: {ex.Message}");
                return new T();
            }
        }

        /// <summary>
        /// 서브 디렉토리의 Config 파일 로드
        /// </summary>
        public static T Load<T>(string subDir, string configName) where T : class, new()
        {
            try
            {
                if (SecurityConfig.SecurityEnabled)
                {
                    return LoadEncrypted<T>(subDir, configName);
                }
                else
                {
                    return LoadPlainJson<T>(subDir, configName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureConfigLoader] Failed to load {subDir}/{configName}: {ex.Message}");
                return new T();
            }
        }

        #region DEBUG Mode - Plain JSON

        private static T LoadPlainJson<T>(string configName) where T : class, new()
        {
            var configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "config",
                configName + ".json");

            if (!File.Exists(configPath))
            {
                System.Diagnostics.Debug.WriteLine($"[SecureConfigLoader] Config not found: {configPath}");
                return new T();
            }

            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? new T();
        }

        private static T LoadPlainJson<T>(string subDir, string configName) where T : class, new()
        {
            var configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "config",
                subDir,
                configName + ".json");

            if (!File.Exists(configPath))
            {
                System.Diagnostics.Debug.WriteLine($"[SecureConfigLoader] Config not found: {configPath}");
                return new T();
            }

            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? new T();
        }

        #endregion

        #region RELEASE Mode - Encrypted Resources

        private static T LoadEncrypted<T>(string configName) where T : class, new()
        {
            // 먼저 임베딩된 리소스에서 시도
            var resourceName = $"DeskWarrior.Resources.EncryptedConfig.{configName}.enc";
            var encrypted = LoadEmbeddedResource(resourceName);

            if (encrypted == null)
            {
                // 리소스가 없으면 외부 파일에서 시도 (마이그레이션 기간)
                var encPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources",
                    "EncryptedConfig",
                    configName + ".enc");

                if (File.Exists(encPath))
                {
                    encrypted = File.ReadAllBytes(encPath);
                }
                else
                {
                    // 암호화 파일도 없으면 평문 시도 (마이그레이션)
                    return LoadPlainJson<T>(configName);
                }
            }

            return DecryptConfig<T>(encrypted);
        }

        private static T LoadEncrypted<T>(string subDir, string configName) where T : class, new()
        {
            // 리소스 이름에서 / -> . 변환
            var resourceName = $"DeskWarrior.Resources.EncryptedConfig.{subDir}.{configName}.enc";
            var encrypted = LoadEmbeddedResource(resourceName);

            if (encrypted == null)
            {
                var encPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources",
                    "EncryptedConfig",
                    subDir,
                    configName + ".enc");

                if (File.Exists(encPath))
                {
                    encrypted = File.ReadAllBytes(encPath);
                }
                else
                {
                    return LoadPlainJson<T>(subDir, configName);
                }
            }

            return DecryptConfig<T>(encrypted);
        }

        private static byte[]? LoadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null) return null;

            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        private static T DecryptConfig<T>(byte[] encrypted) where T : class, new()
        {
            if (encrypted.Length < CONFIG_HEADER_SIZE)
                throw new InvalidDataException("Encrypted config too small");

            using var ms = new MemoryStream(encrypted);
            using var reader = new BinaryReader(ms);

            // 헤더 검증
            var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != CONFIG_MAGIC)
                throw new InvalidDataException("Invalid config magic number");

            var version = reader.ReadByte();
            if (version != SecurityConfig.VERSION)
                throw new InvalidDataException($"Unsupported config version: {version}");

            // 데이터 파싱
            var nonce = reader.ReadBytes(SecurityConfig.NONCE_SIZE);
            var tag = reader.ReadBytes(SecurityConfig.TAG_SIZE);
            var ciphertext = reader.ReadBytes((int)(ms.Length - ms.Position));

            // 복호화
            var key = KeyManager.DeriveConfigKey();
            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(key, SecurityConfig.TAG_SIZE);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            // 역직렬화
            var json = Encoding.UTF8.GetString(plaintext);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? new T();
        }

        #endregion
    }
}
