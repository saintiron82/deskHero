using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 몬스터 로컬라이제이션 데이터
    /// </summary>
    public class MonsterLocalizationEntry
    {
        [JsonPropertyName("ko-KR")]
        public MonsterLocalizationText KoKR { get; set; } = new();

        [JsonPropertyName("en-US")]
        public MonsterLocalizationText EnUS { get; set; } = new();
    }

    /// <summary>
    /// 로컬라이제이션 텍스트 (name + description)
    /// </summary>
    public class MonsterLocalizationText
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 몬스터 로컬라이제이션 관리자
    /// batch_XX_localization.json 파일들을 로드하고 관리
    /// </summary>
    public class MonsterLocalizationManager
    {
        private readonly string _configPath;
        private readonly Dictionary<string, MonsterLocalizationEntry> _localizationData = new();
        private string _currentLanguage = "ko-KR";

        public MonsterLocalizationManager(string? configBasePath = null)
        {
            _configPath = configBasePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "monsters");
        }

        /// <summary>
        /// 현재 언어 설정
        /// </summary>
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set => _currentLanguage = value;
        }

        /// <summary>
        /// 특정 배치의 로컬라이제이션 파일 로드
        /// </summary>
        /// <param name="batchId">배치 ID (예: 1, 2, 3...)</param>
        public void LoadBatchLocalization(int batchId)
        {
            var fileName = $"batch_{batchId:D2}_localization.json";
            var filePath = Path.Combine(_configPath, fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[Warning] Localization file not found: {fileName}");
                return;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, MonsterLocalizationEntry>>(json);

                if (data != null)
                {
                    foreach (var (key, entry) in data)
                    {
                        _localizationData[key] = entry;
                    }
                    Console.WriteLine($"[Info] Loaded localization: {fileName} ({data.Count} entries)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to load {fileName}: {ex.Message}");
            }
        }

        /// <summary>
        /// 여러 배치의 로컬라이제이션 로드
        /// </summary>
        public void LoadBatchLocalizations(IEnumerable<int> batchIds)
        {
            foreach (var batchId in batchIds)
            {
                LoadBatchLocalization(batchId);
            }
        }

        /// <summary>
        /// 로컬라이제이션 키로 이름 가져오기
        /// </summary>
        /// <param name="key">로컬라이제이션 키 (예: "monster.slime.normal")</param>
        /// <param name="languageCode">언어 코드 (기본값: 현재 언어)</param>
        /// <returns>몬스터 이름</returns>
        public string GetName(string key, string? languageCode = null)
        {
            languageCode ??= _currentLanguage;

            if (!_localizationData.TryGetValue(key, out var entry))
            {
                Console.WriteLine($"[Warning] Localization key not found: {key}");
                return $"[{key}]";
            }

            return languageCode switch
            {
                "ko-KR" => entry.KoKR.Name,
                "en-US" => entry.EnUS.Name,
                _ => entry.EnUS.Name // fallback to English
            };
        }

        /// <summary>
        /// 로컬라이제이션 키로 설명 가져오기
        /// </summary>
        public string GetDescription(string key, string? languageCode = null)
        {
            languageCode ??= _currentLanguage;

            if (!_localizationData.TryGetValue(key, out var entry))
            {
                Console.WriteLine($"[Warning] Localization key not found: {key}");
                return string.Empty;
            }

            return languageCode switch
            {
                "ko-KR" => entry.KoKR.Description,
                "en-US" => entry.EnUS.Description,
                _ => entry.EnUS.Description // fallback to English
            };
        }

        /// <summary>
        /// 로컬라이제이션 데이터 존재 여부 확인
        /// </summary>
        public bool HasKey(string key)
        {
            return _localizationData.ContainsKey(key);
        }

        /// <summary>
        /// 캐시 초기화
        /// </summary>
        public void ClearCache()
        {
            _localizationData.Clear();
        }

        /// <summary>
        /// 로드된 키 개수
        /// </summary>
        public int LoadedKeyCount => _localizationData.Count;
    }
}
