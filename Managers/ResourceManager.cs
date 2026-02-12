using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DeskWarrior.Helpers;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 테이블 기반 리소스 경로 관리 (CLAUDE.md 원칙 준수)
    /// 모든 리소스 경로는 config/ResourcePaths.json에서 로드됩니다.
    /// </summary>
    public class ResourceManager
    {
        #region Singleton

        private static ResourceManager? _instance;
        private static readonly object _lock = new();

        public static ResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ResourceManager();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private ResourceTable? _resourceTable;
        private string? _baseDirectory;
        private bool _isInitialized;

        #endregion

        #region Constructor

        private ResourceManager()
        {
            // Singleton: private constructor
        }

        #endregion

        #region Properties

        /// <summary>
        /// ResourceManager 초기화 여부
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 로드된 리소스 테이블
        /// </summary>
        public ResourceTable? ResourceTable => _resourceTable;

        #endregion

        #region Public Methods - Initialization

        /// <summary>
        /// 리소스 테이블 로드 (초기화)
        /// </summary>
        /// <param name="configPath">config/ResourcePaths.json 경로 (null이면 자동 탐색)</param>
        public void LoadResourceTable(string? configPath = null)
        {
            try
            {
                _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // config 경로 결정
                string tablePath;
                if (!string.IsNullOrEmpty(configPath))
                {
                    tablePath = configPath;
                }
                else
                {
                    tablePath = Path.Combine(_baseDirectory, "config", "ResourcePaths.json");
                }

                if (!File.Exists(tablePath))
                {
                    var ex = new FileNotFoundException($"ResourcePaths.json not found", tablePath);
                    Logger.LogError($"[ResourceManager] ResourcePaths.json not found: {tablePath}", ex);
                    throw ex;
                }

                // JSON 로드
                var json = File.ReadAllText(tablePath);
                _resourceTable = JsonSerializer.Deserialize<ResourceTable>(json);

                if (_resourceTable == null)
                {
                    throw new InvalidDataException("Failed to deserialize ResourcePaths.json");
                }

                _isInitialized = true;
                Logger.Log($"[ResourceManager] Loaded ResourcePaths.json (version: {_resourceTable.Version})");
                Logger.Log($"[ResourceManager] Batches: {_resourceTable.Batches.Count}, Special Monsters: {_resourceTable.SpecialMonsters.Count}, UI: {_resourceTable.UI.Count}");
            }
            catch (Exception ex)
            {
                Logger.LogError("[ResourceManager] Failed to load ResourcePaths.json", ex);
                throw;
            }
        }

        #endregion

        #region Public Methods - Batch Resources

        /// <summary>
        /// 배치 데이터 JSON 파일 경로 가져오기
        /// </summary>
        /// <param name="batchId">배치 ID</param>
        /// <returns>절대 경로</returns>
        public string GetBatchDataPath(int batchId)
        {
            EnsureInitialized();

            var batchKey = $"batch_{batchId:D2}";
            if (!_resourceTable!.Batches.TryGetValue(batchKey, out var batchResource))
            {
                throw new KeyNotFoundException($"Batch resource not found: {batchKey}");
            }

            return GetFullPath(batchResource.DataFile);
        }

        /// <summary>
        /// 일반 몬스터 스프라이트 경로 가져오기
        /// </summary>
        /// <param name="batchId">배치 ID</param>
        /// <param name="species">몬스터 종족</param>
        /// <param name="element">속성 (옵션)</param>
        /// <returns>상대 경로 (Assets/Images 기준)</returns>
        public string GetMonsterSpritePath(int batchId, string species, string? element = null)
        {
            EnsureInitialized();

            var batchKey = $"batch_{batchId:D2}";
            if (!_resourceTable!.Batches.TryGetValue(batchKey, out var batchResource))
            {
                throw new KeyNotFoundException($"Batch resource not found: {batchKey}");
            }

            var fileName = string.IsNullOrEmpty(element)
                ? $"monster_{species}.png"
                : $"monster_{species}_{element}.png";

            return Path.Combine(batchResource.MonsterFolder, fileName);
        }

        /// <summary>
        /// 보스 스프라이트 경로 가져오기
        /// </summary>
        /// <param name="batchId">배치 ID</param>
        /// <param name="species">보스 종족</param>
        /// <param name="element">속성 (옵션)</param>
        /// <returns>상대 경로 (Assets/Images 기준)</returns>
        public string GetBossSpritePath(int batchId, string species, string? element = null)
        {
            EnsureInitialized();

            var batchKey = $"batch_{batchId:D2}";
            if (!_resourceTable!.Batches.TryGetValue(batchKey, out var batchResource))
            {
                throw new KeyNotFoundException($"Batch resource not found: {batchKey}");
            }

            var fileName = string.IsNullOrEmpty(element)
                ? $"boss_{species}.png"
                : $"boss_{species}_{element}.png";

            return Path.Combine(batchResource.BossFolder, fileName);
        }

        /// <summary>
        /// 배치 리소스 존재 여부 확인
        /// </summary>
        /// <param name="batchId">배치 ID</param>
        /// <returns>존재 여부</returns>
        public bool HasBatchResource(int batchId)
        {
            if (!_isInitialized || _resourceTable == null) return false;
            var batchKey = $"batch_{batchId:D2}";
            return _resourceTable.Batches.ContainsKey(batchKey);
        }

        #endregion

        #region Public Methods - Special Monster Resources

        /// <summary>
        /// 특수 몬스터 스프라이트 경로 가져오기
        /// </summary>
        /// <param name="specialId">특수 몬스터 ID (예: "golden_goblin")</param>
        /// <returns>상대 경로 (Assets/Images 기준)</returns>
        public string GetSpecialMonsterSprite(string specialId)
        {
            EnsureInitialized();

            if (!_resourceTable!.SpecialMonsters.TryGetValue(specialId, out var resource))
            {
                throw new KeyNotFoundException($"Special monster resource not found: {specialId}");
            }

            return resource.Sprite;
        }

        /// <summary>
        /// 특수 몬스터 설정 파일 경로 가져오기
        /// </summary>
        /// <param name="specialId">특수 몬스터 ID</param>
        /// <returns>절대 경로</returns>
        public string GetSpecialMonsterConfigPath(string specialId)
        {
            EnsureInitialized();

            if (!_resourceTable!.SpecialMonsters.TryGetValue(specialId, out var resource))
            {
                throw new KeyNotFoundException($"Special monster resource not found: {specialId}");
            }

            return GetFullPath(resource.Config);
        }

        #endregion

        #region Public Methods - UI Resources

        /// <summary>
        /// UI 이미지 경로 가져오기 (상대 경로)
        /// </summary>
        /// <param name="uiElement">UI 요소 키 (예: "crystal", "gold")</param>
        /// <returns>상대 경로 (Assets/Images 기준)</returns>
        public string GetUIImagePath(string uiElement)
        {
            EnsureInitialized();

            if (!_resourceTable!.UI.TryGetValue(uiElement, out var path))
            {
                throw new KeyNotFoundException($"UI resource not found: {uiElement}");
            }

            return path;
        }

        /// <summary>
        /// UI 이미지 URI 가져오기 (WPF pack:// 형식)
        /// </summary>
        /// <param name="uiElement">UI 요소 키</param>
        /// <returns>pack://application:,,,/ URI</returns>
        public Uri GetUIImageUri(string uiElement)
        {
            var relativePath = GetUIImagePath(uiElement);
            return new Uri($"pack://application:,,,/{relativePath}");
        }

        #endregion

        #region Public Methods - Sound Resources

        /// <summary>
        /// 사운드 폴더 경로 가져오기
        /// </summary>
        /// <param name="category">카테고리 키 (예: "custom_folder", "default_folder")</param>
        /// <returns>절대 경로</returns>
        public string GetSoundFolder(string category)
        {
            EnsureInitialized();

            if (!_resourceTable!.Sounds.TryGetValue(category, out var path))
            {
                throw new KeyNotFoundException($"Sound folder not found: {category}");
            }

            return GetFullPath(path);
        }

        #endregion

        #region Public Methods - General

        /// <summary>
        /// 상대 경로를 절대 경로로 변환
        /// </summary>
        /// <param name="relativePath">상대 경로</param>
        /// <returns>절대 경로</returns>
        public string GetFullPath(string relativePath)
        {
            EnsureInitialized();

            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            return Path.Combine(_baseDirectory!, relativePath);
        }

        /// <summary>
        /// 기본 경로 가져오기
        /// </summary>
        /// <param name="key">기본 경로 키 (예: "config", "assets")</param>
        /// <returns>절대 경로</returns>
        public string GetBasePath(string key)
        {
            EnsureInitialized();

            if (!_resourceTable!.BasePaths.TryGetValue(key, out var path))
            {
                throw new KeyNotFoundException($"Base path not found: {key}");
            }

            return GetFullPath(path);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 초기화 확인 (초기화 안 되어 있으면 예외 발생)
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isInitialized || _resourceTable == null)
            {
                throw new InvalidOperationException(
                    "ResourceManager is not initialized. Call LoadResourceTable() first.");
            }
        }

        #endregion

        #region Public Methods - Utility

        /// <summary>
        /// 모든 배치 ID 목록 가져오기
        /// </summary>
        /// <returns>배치 ID 목록</returns>
        public IEnumerable<int> GetAllBatchIds()
        {
            EnsureInitialized();
            return _resourceTable!.Batches.Values.Select(b => b.Id).OrderBy(id => id);
        }

        /// <summary>
        /// 모든 특수 몬스터 ID 목록 가져오기
        /// </summary>
        /// <returns>특수 몬스터 ID 목록</returns>
        public IEnumerable<string> GetAllSpecialMonsterIds()
        {
            EnsureInitialized();
            return _resourceTable!.SpecialMonsters.Keys;
        }

        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public string GetDebugInfo()
        {
            if (!_isInitialized || _resourceTable == null)
            {
                return "ResourceManager: Not initialized";
            }

            return $@"ResourceManager Debug Info
Version: {_resourceTable.Version}
Base Directory: {_baseDirectory}
Batches: {string.Join(", ", GetAllBatchIds())}
Special Monsters: {string.Join(", ", GetAllSpecialMonsterIds())}
UI Elements: {_resourceTable.UI.Count}
Sound Folders: {_resourceTable.Sounds.Count}
";
        }

        #endregion
    }
}
