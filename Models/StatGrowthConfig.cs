using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 티어 설정 (공식 기반, 무한 확장)
    /// </summary>
    public class TierConfig
    {
        /// <summary>
        /// 티어 구간 (예: 1000 = 1000레벨마다 티어 1 증가)
        /// </summary>
        [JsonPropertyName("tier_interval")]
        public int TierInterval { get; set; } = 1000;

        /// <summary>
        /// 기본 multiplier (Tier 0 기준)
        /// </summary>
        [JsonPropertyName("base_multiplier")]
        public double BaseMultiplier { get; set; } = 1.6;

        /// <summary>
        /// 티어당 multiplier 감소량 (예: 0.1 = 티어마다 0.1씩 감소)
        /// </summary>
        [JsonPropertyName("multiplier_decrease_per_tier")]
        public double MultiplierDecreasePerTier { get; set; } = 0.1;

        /// <summary>
        /// 기본 softcap_interval (Tier 0 기준)
        /// </summary>
        [JsonPropertyName("base_softcap")]
        public int BaseSoftcap { get; set; } = 12;

        /// <summary>
        /// 티어당 softcap 증가량 (예: 3 = 티어마다 3씩 증가)
        /// </summary>
        [JsonPropertyName("softcap_increase_per_tier")]
        public int SoftcapIncreasePerTier { get; set; } = 3;
    }

    /// <summary>
    /// 스탯 성장 곡선 설정 (JSON에서 로드)
    /// </summary>
    public class StatGrowthConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("stat_name")]
        public string StatName { get; set; } = "";

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "";

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

        [JsonPropertyName("localization")]
        public Dictionary<string, StatLocalizedText>? Localization { get; set; }

        [JsonPropertyName("damage_bonus_per_level")]
        public double? DamageBonusPerLevel { get; set; }

        /// <summary>
        /// 티어 기반 비용 시스템 (선택적, 무한 레벨 대응)
        /// </summary>
        [JsonPropertyName("tier_config")]
        public TierConfig? TierConfig { get; set; }

        /// <summary>
        /// 비용 계산
        /// cost = base × (1 + level × growth_rate) × multiplier^(level / softcap_interval)
        /// 티어 시스템이 활성화되면 레벨에 따라 multiplier와 softcap이 자동 조정됩니다.
        /// </summary>
        public int CalculateCost(int currentLevel, double? discountPercent = null)
        {
            // 레벨 0은 비용 없음
            if (currentLevel <= 0)
                return 0;

            // 최대 레벨 체크
            if (MaxLevel > 0 && currentLevel >= MaxLevel)
                return int.MaxValue;

            // 티어 기반 파라미터 계산
            int softcap = SoftcapInterval;
            double multiplier = Multiplier;

            if (TierConfig != null)
            {
                // 공식으로 티어 계산
                int tier = currentLevel / TierConfig.TierInterval;

                // 티어에 따라 파라미터 조정
                multiplier = TierConfig.BaseMultiplier - (tier * TierConfig.MultiplierDecreasePerTier);
                softcap = TierConfig.BaseSoftcap + (tier * TierConfig.SoftcapIncreasePerTier);

                // 안전장치: multiplier 최소값 1.0
                if (multiplier < 1.0)
                {
                    multiplier = 1.0;
                }
            }

            double linearFactor = 1.0 + currentLevel * GrowthRate;
            double exponentialFactor = System.Math.Pow(multiplier, (double)currentLevel / softcap);
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

    /// <summary>
    /// 다국어 텍스트
    /// </summary>
    public class StatLocalizedText
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }
}
