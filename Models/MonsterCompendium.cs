using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 몬스터 도감 항목 로컬라이제이션
    /// </summary>
    public class CompendiumLocalization
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// 몬스터 도감 정의 (config/MonsterCompendium.json)
    /// </summary>
    public class CompendiumMonsterDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("category")]
        public string Category { get; set; } = "";

        [JsonPropertyName("element")]
        public string Element { get; set; } = "";

        [JsonPropertyName("rarity")]
        public int Rarity { get; set; } = 1;

        [JsonPropertyName("localization")]
        public Dictionary<string, CompendiumLocalization> Localization { get; set; } = new();

        /// <summary>
        /// 현재 언어에 맞는 로컬라이즈 텍스트 가져오기
        /// </summary>
        public CompendiumLocalization GetLocalization(string languageCode)
        {
            if (Localization.TryGetValue(languageCode, out var loc))
                return loc;
            if (Localization.TryGetValue("en-US", out var fallback))
                return fallback;
            return new CompendiumLocalization();
        }
    }

    /// <summary>
    /// 도감 완성 보상
    /// </summary>
    public class CompendiumReward
    {
        [JsonPropertyName("threshold")]
        public int Threshold { get; set; }

        [JsonPropertyName("crystal_reward")]
        public int CrystalReward { get; set; }
    }

    /// <summary>
    /// 몬스터 도감 설정 파일 루트
    /// </summary>
    public class MonsterCompendiumConfig
    {
        [JsonPropertyName("monsters")]
        public List<CompendiumMonsterDefinition> Monsters { get; set; } = new();

        [JsonPropertyName("bosses")]
        public List<CompendiumMonsterDefinition> Bosses { get; set; } = new();

        [JsonPropertyName("completion_rewards")]
        public List<CompendiumReward> CompletionRewards { get; set; } = new();
    }

    /// <summary>
    /// 사용자 도감 진행 상태
    /// </summary>
    public class UserCompendium
    {
        [JsonPropertyName("entries")]
        public Dictionary<string, CompendiumEntry> Entries { get; set; } = new();

        [JsonPropertyName("claimed_rewards")]
        public List<int> ClaimedRewards { get; set; } = new();
    }

    /// <summary>
    /// 개별 몬스터 도감 항목
    /// </summary>
    public class CompendiumEntry
    {
        [JsonPropertyName("first_encounter")]
        public DateTime? FirstEncounter { get; set; }

        [JsonPropertyName("kill_count")]
        public int KillCount { get; set; }

        [JsonPropertyName("max_damage_dealt")]
        public long MaxDamageDealt { get; set; }
    }
}
