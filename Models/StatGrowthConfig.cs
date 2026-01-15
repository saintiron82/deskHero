using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 스탯 성장 곡선 설정 (JSON에서 로드)
    /// </summary>
    public class StatGrowthConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("base_cost")]
        public double BaseCost { get; set; } = 100;

        [JsonPropertyName("growth_rate")]
        public double GrowthRate { get; set; } = 0.5;

        [JsonPropertyName("multiplier")]
        public double Multiplier { get; set; } = 1.5;

        [JsonPropertyName("softcap_interval")]
        public int SoftcapInterval { get; set; } = 10;

        [JsonPropertyName("effect_per_level")]
        public double EffectPerLevel { get; set; } = 1;

        [JsonPropertyName("max_level")]
        public int MaxLevel { get; set; } = 0; // 0 = unlimited

        [JsonPropertyName("cap_type")]
        public string? CapType { get; set; } // "base_time" for time_thief

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        /// <summary>
        /// 비용 계산
        /// cost = base × (1 + level × growth_rate) × multiplier^(level / softcap_interval)
        /// </summary>
        public int CalculateCost(int currentLevel, double? discountPercent = null)
        {
            // 레벨 0은 비용 없음
            if (currentLevel <= 0)
                return 0;

            // 최대 레벨 체크
            if (MaxLevel > 0 && currentLevel >= MaxLevel)
                return int.MaxValue;

            double linearFactor = 1.0 + currentLevel * GrowthRate;
            double exponentialFactor = System.Math.Pow(Multiplier, (double)currentLevel / SoftcapInterval);
            double cost = BaseCost * linearFactor * exponentialFactor;

            // 할인 적용 (영구 스탯)
            if (discountPercent.HasValue)
            {
                cost *= (1.0 - discountPercent.Value);
            }

            return (int)System.Math.Ceiling(cost);
        }

        /// <summary>
        /// 효과 값 계산
        /// </summary>
        public double CalculateEffect(int level)
        {
            return level * EffectPerLevel;
        }
    }

    /// <summary>
    /// 전체 스탯 성장 설정 (JSON 루트)
    /// </summary>
    public class StatGrowthConfigRoot
    {
        [JsonPropertyName("stats")]
        public Dictionary<string, StatGrowthConfig> Stats { get; set; } = new();

        [JsonPropertyName("defaults")]
        public StatGrowthConfig? Defaults { get; set; }

        [JsonPropertyName("categories")]
        public Dictionary<string, CategoryInfo>? Categories { get; set; }
    }

    /// <summary>
    /// 스탯 카테고리 정보
    /// </summary>
    public class CategoryInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("order")]
        public int Order { get; set; } = 0;
    }
}
