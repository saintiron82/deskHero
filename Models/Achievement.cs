using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using DeskWarrior.Managers;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 업적 로컬라이즈 텍스트
    /// </summary>
    public class AchievementLocalization
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("unlock_message")]
        public string UnlockMessage { get; set; } = "";
    }

    /// <summary>
    /// 업적 정의 (config/Achievements.json에서 로드)
    /// </summary>
    public class AchievementDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "";

        [JsonPropertyName("category")]
        public string Category { get; set; } = ""; // combat, progression, wealth, engagement, secret

        [JsonPropertyName("metric")]
        public string Metric { get; set; } = ""; // total_damage, max_level, total_gold, etc.

        [JsonPropertyName("target")]
        public long Target { get; set; }

        [JsonPropertyName("is_hidden")]
        public bool IsHidden { get; set; }

        [JsonPropertyName("localization")]
        public Dictionary<string, AchievementLocalization> Localization { get; set; } = new();

        // 현재 언어에 맞는 로컬라이즈 텍스트 가져오기
        private AchievementLocalization GetCurrentLocalization()
        {
            var lang = LocalizationManager.Instance.CurrentLanguage;
            if (Localization.TryGetValue(lang, out var loc))
                return loc;
            if (Localization.TryGetValue("en-US", out var fallback))
                return fallback;
            if (Localization.Count > 0)
            {
                foreach (var val in Localization.Values)
                    return val;
            }
            return new AchievementLocalization();
        }

        // 동적 속성들 (현재 언어 기반)
        [JsonIgnore]
        public string Name => GetCurrentLocalization().Name;

        [JsonIgnore]
        public string Description => GetCurrentLocalization().Description;

        [JsonIgnore]
        public string UnlockMessage => GetCurrentLocalization().UnlockMessage;
    }

    /// <summary>
    /// 업적 진행 상태 (사용자별 저장)
    /// </summary>
    public class AchievementProgress
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("is_unlocked")]
        public bool IsUnlocked { get; set; }

        [JsonPropertyName("unlocked_at")]
        public DateTime? UnlockedAt { get; set; }

        [JsonPropertyName("current_progress")]
        public long CurrentProgress { get; set; }
    }

    /// <summary>
    /// 업적 설정 파일 루트
    /// </summary>
    public class AchievementsConfig
    {
        [JsonPropertyName("achievements")]
        public List<AchievementDefinition> Achievements { get; set; } = new();
    }

    /// <summary>
    /// 사용자 업적 진행 저장 파일
    /// </summary>
    public class UserAchievements
    {
        [JsonPropertyName("progress")]
        public List<AchievementProgress> Progress { get; set; } = new();

        [JsonPropertyName("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
