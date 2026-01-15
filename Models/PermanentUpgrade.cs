using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 영구 업그레이드 정의
    /// </summary>
    public class PermanentUpgradeDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("category")]
        public string Category { get; set; } = ""; // "base_stats", "percentage", "abilities", "starting_bonus", "utility"

        [JsonPropertyName("stat_name")]
        public string StatName { get; set; } = ""; // Property name in PermanentStats

        [JsonPropertyName("base_cost")]
        public int BaseCost { get; set; }

        [JsonPropertyName("cost_multiplier")]
        public double CostMultiplier { get; set; } = 1.2;

        [JsonPropertyName("increment_per_level")]
        public double IncrementPerLevel { get; set; } = 1.0;

        [JsonPropertyName("max_level")]
        public int MaxLevel { get; set; } = 0; // 0 = unlimited

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "⭐";

        [JsonPropertyName("requires_unlock")]
        public string RequiresUnlock { get; set; } = ""; // Achievement or upgrade ID prerequisite

        [JsonPropertyName("localization")]
        public Dictionary<string, UpgradeLocalization> Localization { get; set; } = new();
    }

    /// <summary>
    /// 업그레이드 현지화 정보
    /// </summary>
    public class UpgradeLocalization
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// 사용자별 영구 업그레이드 진행 상태
    /// </summary>
    public class PermanentUpgradeProgress
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("current_level")]
        public int CurrentLevel { get; set; } = 0;

        [JsonPropertyName("total_invested")]
        public long TotalInvested { get; set; } = 0;
    }

    /// <summary>
    /// 업그레이드 설정 파일 루트
    /// </summary>
    public class PermanentUpgradesRoot
    {
        [JsonPropertyName("upgrades")]
        public List<PermanentUpgradeDefinition> Upgrades { get; set; } = new();
    }
}
