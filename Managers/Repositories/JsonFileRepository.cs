using System;
using System.IO;
using System.Text.Json;

namespace DeskWarrior.Managers.Repositories
{
    /// <summary>
    /// JSON 파일 기반 저장소 기본 클래스
    /// </summary>
    public abstract class JsonFileRepository<T> : IRepository<T> where T : class, new()
    {
        protected readonly string FilePath;
        protected readonly JsonSerializerOptions ReadOptions;
        protected readonly JsonSerializerOptions WriteOptions;
        private bool _isDirty;

        public bool IsDirty => _isDirty;

        protected JsonFileRepository(string filePath)
        {
            FilePath = filePath;
            ReadOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            WriteOptions = new JsonSerializerOptions { WriteIndented = true };
        }

        public virtual T Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var data = JsonSerializer.Deserialize<T>(json, ReadOptions);
                    if (data != null)
                    {
                        OnLoaded(data);
                        return data;
                    }
                }
            }
            catch
            {
                // 로드 실패 시 기본값 반환
            }

            var defaultData = new T();
            OnLoaded(defaultData);
            return defaultData;
        }

        public virtual void Save(T data)
        {
            if (!_isDirty) return;

            try
            {
                OnSaving(data);
                var json = JsonSerializer.Serialize(data, WriteOptions);
                File.WriteAllText(FilePath, json);
                _isDirty = false;
            }
            catch
            {
                // 저장 실패 시 무시
            }
        }

        /// <summary>
        /// 강제 저장 (Dirty 플래그 무시)
        /// </summary>
        public void ForceSave(T data)
        {
            try
            {
                OnSaving(data);
                var json = JsonSerializer.Serialize(data, WriteOptions);
                File.WriteAllText(FilePath, json);
                _isDirty = false;
            }
            catch
            {
                // 저장 실패 시 무시
            }
        }

        public void MarkDirty() => _isDirty = true;
        public void ClearDirty() => _isDirty = false;

        /// <summary>
        /// 로드 후 처리 (마이그레이션 등)
        /// </summary>
        protected virtual void OnLoaded(T data) { }

        /// <summary>
        /// 저장 전 처리
        /// </summary>
        protected virtual void OnSaving(T data) { }
    }
}
