using System.Collections.Generic;
using System.Text.Json.Serialization;
using DeskWarrior.Helpers;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 티어별 오버라이드 파라미터 (tier_overrides에서 사용)
    /// </summary>
    public class TierOverride
    {
        [JsonPropertyName("base_cost")]
        public double? BaseCost { get; set; }

        [JsonPropertyName("growth_rate")]
        public double? GrowthRate { get; set; }

        [JsonPropertyName("multiplier")]
        public double? Multiplier { get; set; }

        [JsonPropertyName("softcap_interval")]
        public int? SoftcapInterval { get; set; }

        [JsonPropertyName("effect_per_level")]
        public double? EffectPerLevel { get; set; }
    }

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

        /// <summary>
        /// 티어당 효과 배율 (예: 1.15 = 티어마다 15% 효과 증가)
        /// </summary>
        [JsonPropertyName("effect_multiplier_per_tier")]
        public double EffectMultiplierPerTier { get; set; } = 1.0;

        /// <summary>
        /// 티어당 효과 가산 (예: 0.5 = 티어마다 레벨당 +0.5 추가)
        /// </summary>
        [JsonPropertyName("effect_add_per_tier")]
        public double EffectAddPerTier { get; set; } = 0.0;

        /// <summary>
        /// 티어별 독립 파라미터 오버라이드 (키: 티어 인덱스 "0"=Z, "1"=Y, ...)
        /// </summary>
        [JsonPropertyName("tier_overrides")]
        public Dictionary<string, TierOverride>? TierOverrides { get; set; }

        /// <summary>
        /// 해당 티어의 오버라이드 반환 (없으면 null)
        /// </summary>
        public TierOverride? GetOverride(int tier)
        {
            if (TierOverrides == null) return null;
            return TierOverrides.TryGetValue(tier.ToString(), out var o) ? o : null;
        }
    }

    /// <summary>
    /// 등급 시스템 유틸리티
    /// </summary>
    public static class GradeSystem
    {
        private static readonly string[] GradeNames =
        {
            "α", "β", "γ", "δ", "ε", "ζ", "η", "θ",
            "ι", "κ", "λ", "μ", "ν", "ξ", "ο", "π",
            "ρ", "σ", "τ", "υ", "φ", "χ", "ψ", "Ω"
        };

        /// <summary>
        /// 등급 이름 반환 (0=α, 1=β, ..., 23=Ω, 24+=Ω+1)
        /// </summary>
        public static string GetGradeName(int grade)
        {
            if (grade < GradeNames.Length)
                return GradeNames[grade];
            int beyond = grade - GradeNames.Length + 1;
            return $"Ω+{beyond}";
        }

        /// <summary>
        /// 절대 레벨 → 등급 + 표시 레벨 계산
        /// </summary>
        public static (int grade, int displayLevel, string gradeName) GetGradeInfo(int absoluteLevel, int tierInterval)
        {
            if (tierInterval <= 0 || absoluteLevel <= 0)
                return (0, absoluteLevel, "α");

            int grade = absoluteLevel / tierInterval;
            int displayLevel = absoluteLevel % tierInterval;

            // displayLevel이 0이면 이전 등급의 max로 표시
            if (displayLevel == 0 && grade > 0)
            {
                grade--;
                displayLevel = tierInterval;
            }

            return (grade, displayLevel, GetGradeName(grade));
        }
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

        [JsonPropertyName("max_effect")]
        public double MaxEffect { get; set; } = 0; // 0 = 무제한

        [JsonPropertyName("cap_type")]
        public string? CapType { get; set; } // "base_time" for time_thief

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("localization")]
        public Dictionary<string, StatLocalizedText>? Localization { get; set; }

        [JsonPropertyName("damage_bonus_per_level")]
        public double? DamageBonusPerLevel { get; set; }

        /// <summary>
        /// 카테고리 통일 비용 여부 (true = 같은 카테고리 내 동일 비용, false = 독립 비용)
        /// </summary>
        [JsonPropertyName("unified_cost")]
        public bool UnifiedCost { get; set; }

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
        public int CalculateCost(int currentLevel, double? discountPercent = null, long flatReduction = 0)
        {
            // 레벨 0은 비용 없음
            if (currentLevel <= 0)
                return 0;

            // 최대 레벨 체크
            if (MaxLevel > 0 && currentLevel >= MaxLevel)
                return int.MaxValue;

            double baseCost = BaseCost;
            double growthRate = GrowthRate;
            int softcap = SoftcapInterval;
            double multiplier = Multiplier;
            int levelForCalc = currentLevel;

            if (TierConfig != null)
            {
                int interval = System.Math.Max(1, TierConfig.TierInterval);

                if (TierConfig.TierOverrides != null)
                {
                    // tier_overrides 모드: 상대 레벨 + 오버라이드 파라미터
                    int tier = (currentLevel - 1) / interval;
                    int relativeLevel = ((currentLevel - 1) % interval) + 1;
                    levelForCalc = relativeLevel;

                    var ov = TierConfig.GetOverride(tier);
                    if (ov != null)
                    {
                        baseCost = ov.BaseCost ?? BaseCost;
                        growthRate = ov.GrowthRate ?? GrowthRate;
                        multiplier = ov.Multiplier ?? Multiplier;
                        softcap = ov.SoftcapInterval ?? SoftcapInterval;
                    }
                }
                else
                {
                    // 기존 formula-based 티어 조정 (역호환)
                    int tier = currentLevel / interval;
                    multiplier = TierConfig.BaseMultiplier - (tier * TierConfig.MultiplierDecreasePerTier);
                    softcap = TierConfig.BaseSoftcap + (tier * TierConfig.SoftcapIncreasePerTier);

                    if (multiplier < 1.0) multiplier = 1.0;
                }
            }

            double linearFactor = 1.0 + levelForCalc * growthRate;
            double exponentialFactor = System.Math.Pow(multiplier, (double)levelForCalc / softcap);
            double cost = baseCost * linearFactor * exponentialFactor;

            // 할인 적용 (영구 스탯)
            if (discountPercent.HasValue)
            {
                cost *= (1.0 - discountPercent.Value);
            }

            // 고정액 차감 적용
            if (flatReduction > 0)
            {
                cost -= flatReduction;
                if (cost < 1.0) cost = 1.0;
            }

            return SafeMath.CostToInt(cost);
        }

        /// <summary>
        /// 효과 값 계산 (등급 시스템 적용)
        /// tier_config의 effect_multiplier_per_tier가 있으면 등급별 효과 증폭
        /// </summary>
        public double CalculateEffect(int level)
        {
            if (level <= 0) return 0;

            double effect;

            bool hasTierEffect = TierConfig != null &&
                (TierConfig.EffectMultiplierPerTier != 1.0 || TierConfig.EffectAddPerTier != 0.0 ||
                 TierConfig.TierOverrides != null);

            if (hasTierEffect)
            {
                int interval = System.Math.Max(1, TierConfig!.TierInterval);
                int remaining = level;
                int tier = 0;
                double total = 0;

                while (remaining > 0)
                {
                    int inTier = System.Math.Min(remaining, interval);

                    // tier_overrides의 effect_per_level 우선, 없으면 formula-based
                    var ov = TierConfig.GetOverride(tier);
                    double perLevel;
                    if (ov?.EffectPerLevel != null)
                    {
                        perLevel = ov.EffectPerLevel.Value;
                    }
                    else
                    {
                        perLevel = EffectPerLevel * System.Math.Pow(TierConfig.EffectMultiplierPerTier, tier)
                                   + (TierConfig.EffectAddPerTier * tier);
                    }

                    total += inTier * perLevel;
                    remaining -= inTier;
                    tier++;
                }

                effect = total;
            }
            else
            {
                effect = level * EffectPerLevel;
            }

            // max_effect 캡 적용
            if (MaxEffect > 0 && effect > MaxEffect)
            {
                return MaxEffect;
            }

            return effect;
        }
    }

    /// <summary>
    /// 등급 승급 요구사항
    /// </summary>
    public class GradePromotion
    {
        [JsonPropertyName("required_level")]
        public int RequiredLevel { get; set; }

        [JsonPropertyName("crystal_cost")]
        public int CrystalCost { get; set; }
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

        [JsonPropertyName("grade_promotions")]
        public Dictionary<string, GradePromotion>? GradePromotions { get; set; }
    }

    /// <summary>
    /// 스탯 카테고리 정보
    /// </summary>
    public class CategoryInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "";

        [JsonPropertyName("order")]
        public int Order { get; set; } = 0;

        [JsonPropertyName("color")]
        public string Color { get; set; } = "#9CA3AF";
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
