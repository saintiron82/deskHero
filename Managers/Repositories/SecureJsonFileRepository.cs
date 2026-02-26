using System;
using System.IO;
using System.Text;
using System.Text.Json;
using DeskWarrior.Security;

namespace DeskWarrior.Managers.Repositories
{
    /// <summary>
    /// 암호화된 JSON 파일 저장소
    /// RELEASE: AES-256-GCM 암호화 + HMAC 무결성 검증
    /// DEBUG: 평문 JSON (개발 편의)
    /// </summary>
    public abstract class SecureJsonFileRepository<T> : IRepository<T> where T : class, new()
    {
        protected readonly string FilePath;
        protected readonly JsonSerializerOptions ReadOptions;
        protected readonly JsonSerializerOptions WriteOptions;
        private bool _isDirty;

        public bool IsDirty => _isDirty;

        protected SecureJsonFileRepository(string filePath)
        {
            FilePath = filePath;
            ReadOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            WriteOptions = new JsonSerializerOptions { WriteIndented = true };
        }

        public virtual T Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    var defaultData = new T();
                    OnLoaded(defaultData);
                    return defaultData;
                }

                var fileBytes = File.ReadAllBytes(FilePath);
                T? data = null;

                if (SecurityConfig.SecurityEnabled)
                {
                    // RELEASE: 암호화된 파일 처리
                    data = LoadEncrypted(fileBytes);
                }
                else
                {
                    // DEBUG: 평문 JSON 처리
                    data = LoadPlainJson(fileBytes);
                }

                if (data != null)
                {
                    OnLoaded(data);
                    return data;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureRepo] Load failed: {ex.Message}");
            }

            // 로드 실패 시 기본값 반환 (변조 감지 포함)
            var defaultData2 = new T();
            OnLoaded(defaultData2);
            OnIntegrityViolation();
            return defaultData2;
        }

        public virtual void Save(T data)
        {
            if (!_isDirty) return;
            ForceSave(data);
        }

        public void ForceSave(T data)
        {
            try
            {
                OnSaving(data);
                var json = JsonSerializer.Serialize(data, WriteOptions);

                if (SecurityConfig.SecurityEnabled)
                {
                    // RELEASE: 암호화하여 저장
                    SaveEncrypted(json);
                }
                else
                {
                    // DEBUG: 평문으로 저장
                    File.WriteAllText(FilePath, json);
                }

                _isDirty = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureRepo] Save failed: {ex.Message}");
            }
        }

        public void MarkDirty() => _isDirty = true;
        public void ClearDirty() => _isDirty = false;

        #region Private Methods

        private T? LoadEncrypted(byte[] fileBytes)
        {
            // 평문 JSON인 경우 마이그레이션
            if (DataProtection.IsPlainJson(fileBytes))
            {
                System.Diagnostics.Debug.WriteLine("[SecureRepo] Migrating plain JSON to encrypted format");
                var plainData = LoadPlainJson(fileBytes);
                if (plainData != null)
                {
                    // 다음 저장 시 암호화되도록 설정
                    _isDirty = true;
                }
                return plainData;
            }

            // 암호화된 패키지 복호화
            if (DataProtection.IsEncryptedPackage(fileBytes))
            {
                var encryptKey = KeyManager.DeriveUserKey();
                var hmacKey = KeyManager.DeriveHmacKey();

                var decrypted = DataProtection.DecryptWithHeader(fileBytes, encryptKey, hmacKey);
                if (decrypted != null)
                {
                    var json = Encoding.UTF8.GetString(decrypted);
                    return JsonSerializer.Deserialize<T>(json, ReadOptions);
                }
            }

            return null;
        }

        private T? LoadPlainJson(byte[] fileBytes)
        {
            var json = Encoding.UTF8.GetString(fileBytes);
            return JsonSerializer.Deserialize<T>(json, ReadOptions);
        }

        private void SaveEncrypted(string json)
        {
            var plaintext = Encoding.UTF8.GetBytes(json);
            var encryptKey = KeyManager.DeriveUserKey();
            var hmacKey = KeyManager.DeriveHmacKey();

            var encrypted = DataProtection.EncryptWithHeader(plaintext, encryptKey, hmacKey);

            // 디렉토리 생성
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(FilePath, encrypted);
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// 로드 후 처리 (마이그레이션 등)
        /// </summary>
        protected virtual void OnLoaded(T data) { }

        /// <summary>
        /// 저장 전 처리
        /// </summary>
        protected virtual void OnSaving(T data) { }

        /// <summary>
        /// 무결성 검증 실패 시 처리
        /// 기본 동작: 로그만 출력, 상속 클래스에서 오버라이드 가능
        /// </summary>
        protected virtual void OnIntegrityViolation()
        {
            System.Diagnostics.Debug.WriteLine($"[SecureRepo] Integrity violation detected for: {FilePath}");
        }

        #endregion
    }
}
