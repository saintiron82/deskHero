using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskWarrior.Core.Models;

/// <summary>
/// 몬스터 스폰 설정 데이터 (완전한 테이블 기반 - 모든 값은 JSON에서 로드)
/// </summary>
public class MonsterSpawningConfig
{
    [JsonPropertyName("element_weights")]
    public Dictionary<string, int> ElementWeights { get; set; } = new();

    [JsonPropertyName("use_weighted_selection")]
    public bool UseWeightedSelection { get; set; } = true;

    [JsonPropertyName("use_batch_progression")]
    public bool UseBatchProgression { get; set; } = true;
}

/// <summary>
/// 속성별 설정 데이터 (element_properties)
/// </summary>
public class ElementProperty
{
    [JsonPropertyName("hp_modifier")]
    public double HpModifier { get; set; } = 1.0;

    [JsonPropertyName("time_scale")]
    public double TimeScale { get; set; } = 1.0;

    [JsonPropertyName("keyboard_resistance")]
    public double KeyboardResistance { get; set; } = 1.0;

    [JsonPropertyName("mouse_resistance")]
    public double MouseResistance { get; set; } = 1.0;

    [JsonPropertyName("crystal_multiplier")]
    public double CrystalMultiplier { get; set; } = 1.0;
}

/// <summary>
/// 게임 밸런스 설정 (GameData.json 로드)
/// </summary>
public class GameConfig
{
    [JsonPropertyName("balance")]
    public BalanceConfig Balance { get; set; } = new();

    [JsonPropertyName("upgrade")]
    public UpgradeConfig Upgrade { get; set; } = new();

    [JsonPropertyName("monster_spawning")]
    public MonsterSpawningConfig MonsterSpawning { get; set; } = new();

    [JsonPropertyName("element_properties")]
    public Dictionary<string, ElementProperty> ElementProperties { get; set; } = new();

    public static GameConfig LoadFromFile(string path)
    {
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<GameConfig>(json) ?? new GameConfig();
        }
        return new GameConfig();
    }
}

public class BalanceConfig
{
    [JsonPropertyName("base_hp")]
    public int BaseHp { get; set; } = 100;

    [JsonPropertyName("hp_growth")]
    public double HpGrowth { get; set; } = 1.2;

    [JsonPropertyName("boss_interval")]
    public int BossInterval { get; set; } = 10;

    [JsonPropertyName("boss_hp_multiplier")]
    public double BossHpMultiplier { get; set; } = 3.0;

    [JsonPropertyName("time_limit")]
    public int TimeLimit { get; set; } = 30;

    [JsonPropertyName("base_gold_multiplier")]
    public double BaseGoldMultiplier { get; set; } = 1.5;

    [JsonPropertyName("critical_chance")]
    public double CriticalChance { get; set; } = 0.1;

    [JsonPropertyName("critical_multiplier")]
    public double CriticalMultiplier { get; set; } = 2.0;

    [JsonPropertyName("upgrade_cost_interval")]
    public int UpgradeCostInterval { get; set; } = 50;  // 50스테이지마다 비용 2배

    [JsonPropertyName("upgrade_cost_tier_multiplier")]
    public double UpgradeCostTierMultiplier { get; set; } = 2.0;

    [JsonPropertyName("tier_hp_system")]
    public TierHpSystemConfig TierHpSystem { get; set; } = new();
}

/// <summary>
/// 티어 기반 HP 시스템 설정
/// </summary>
public class TierHpSystemConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("tier_interval")]
    public int TierInterval { get; set; } = 1000;

    [JsonPropertyName("tier_multiplier")]
    public double TierMultiplier { get; set; } = 1.0;

    [JsonPropertyName("tier_multiplier_decay_per_tier")]
    public double TierMultiplierDecayPerTier { get; set; } = 1.0;

    [JsonPropertyName("min_tier_multiplier")]
    public double MinTierMultiplier { get; set; } = 0.0;

    [JsonPropertyName("tier_curve_exponent")]
    public double TierCurveExponent { get; set; } = 1.0;

    [JsonPropertyName("linear_growth_per_level")]
    public int LinearGrowthPerLevel { get; set; } = 5;

    [JsonPropertyName("min_linear_growth_per_level")]
    public double MinLinearGrowthPerLevel { get; set; } = 0.0;

    [JsonPropertyName("growth_decrease_per_tier")]
    public double GrowthDecreasePerTier { get; set; } = 0.85;

    [JsonPropertyName("late_start_level")]
    public int LateStartLevel { get; set; } = 0;

    [JsonPropertyName("late_tier_interval")]
    public int LateTierInterval { get; set; } = 0;

    [JsonPropertyName("late_tier_multiplier")]
    public double LateTierMultiplier { get; set; } = 1.0;

    [JsonPropertyName("max_late_tiers")]
    public int MaxLateTiers { get; set; } = 0;
}

public class UpgradeConfig
{
    [JsonPropertyName("base_cost")]
    public int BaseCost { get; set; } = 100;

    [JsonPropertyName("cost_multiplier")]
    public double CostMultiplier { get; set; } = 1.5;

    [JsonPropertyName("attack_increase")]
    public double AttackIncrease { get; set; } = 0.5;
}

/// <summary>
/// 몬스터 데이터 설정
/// </summary>
public class MonsterConfig
{
    public int BaseHp { get; set; } = 10;
    public int HpGrowth { get; set; } = 5;
    public int BaseGold { get; set; } = 10;
    public int GoldGrowth { get; set; } = 2;
}

/// <summary>
/// 스탯 성장 설정 (PermanentStats.json 로드)
/// </summary>
public class StatGrowthConfig
{
    [JsonPropertyName("stat_name")]
    public string StatName { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

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

    [JsonPropertyName("damage_bonus_per_level")]
    public double? DamageBonusPerLevel { get; set; }

    [JsonPropertyName("max_level")]
    public int MaxLevel { get; set; } = 0;

    [JsonPropertyName("max_effect")]
    public double MaxEffect { get; set; } = 0;  // 0 = 무제한

    // Optional: tier-based effect scaling
    [JsonPropertyName("tier_config")]
    public StatTierEffectConfig? TierConfig { get; set; }

    public int CalculateCost(int level, double? discountPercent = null)
    {
        if (level <= 0) return 0;
        if (MaxLevel > 0 && level >= MaxLevel) return int.MaxValue;

        double linearFactor = 1.0 + level * GrowthRate;
        double exponentialFactor = Math.Pow(Multiplier, (double)level / SoftcapInterval);
        double cost = BaseCost * linearFactor * exponentialFactor;

        if (discountPercent.HasValue)
        {
            cost *= (1.0 - discountPercent.Value);
        }

        return (int)Math.Ceiling(cost);
    }

    public double CalculateEffect(int level)
    {
        if (level <= 0) return 0;

        double effect;

        if (TierConfig != null &&
            (TierConfig.EffectMultiplierPerTier != 1.0 || TierConfig.EffectAddPerTier != 0.0))
        {
            int interval = Math.Max(1, TierConfig.TierInterval);
            int remaining = level;
            int tier = 0;
            double total = 0;

            while (remaining > 0)
            {
                int inTier = Math.Min(remaining, interval);
                double perLevel = EffectPerLevel * Math.Pow(TierConfig.EffectMultiplierPerTier, tier)
                                  + (TierConfig.EffectAddPerTier * tier);
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

        // 데이터 한계 적용
        if (MaxEffect > 0 && effect > MaxEffect)
        {
            return MaxEffect;
        }
        return effect;
    }

    /// <summary>
    /// 데이터 한계 기반 실제 만렙 계산
    /// </summary>
    public int CalculateMaxLevel()
    {
        // max_effect가 0이면 무제한
        if (MaxEffect <= 0 || EffectPerLevel <= 0)
        {
            return MaxLevel > 0 ? MaxLevel : int.MaxValue;
        }
        // 데이터 한계 기반 만렙 = max_effect / effect_per_level
        int dataLimit = (int)Math.Ceiling(MaxEffect / EffectPerLevel);
        // max_level이 지정되어 있으면 둘 중 작은 값
        if (MaxLevel > 0)
        {
            return Math.Min(MaxLevel, dataLimit);
        }
        return dataLimit;
    }

    /// <summary>
    /// 해당 레벨에서 업그레이드 가능한지 확인
    /// </summary>
    public bool CanUpgrade(int currentLevel)
    {
        int maxLv = CalculateMaxLevel();
        return currentLevel < maxLv;
    }
}

/// <summary>
/// 영구 스탯 효과용 티어 설정
/// </summary>
public class StatTierEffectConfig
{
    [JsonPropertyName("tier_interval")]
    public int TierInterval { get; set; } = 1000;

    [JsonPropertyName("effect_multiplier_per_tier")]
    public double EffectMultiplierPerTier { get; set; } = 1.0;

    [JsonPropertyName("effect_add_per_tier")]
    public double EffectAddPerTier { get; set; } = 0.0;
}

/// <summary>
/// 스탯 성장 설정 루트
/// </summary>
public class StatGrowthConfigRoot
{
    [JsonPropertyName("stats")]
    public Dictionary<string, StatGrowthConfig> Stats { get; set; } = new();

    public static StatGrowthConfigRoot LoadFromFile(string path)
    {
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<StatGrowthConfigRoot>(json) ?? new StatGrowthConfigRoot();
        }
        return new StatGrowthConfigRoot();
    }
}

/// <summary>
/// 인게임 스탯 성장 설정
/// </summary>
public class InGameStatGrowthRoot
{
    [JsonPropertyName("stats")]
    public Dictionary<string, StatGrowthConfig> Stats { get; set; } = new();

    public static InGameStatGrowthRoot LoadFromFile(string path)
    {
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<InGameStatGrowthRoot>(json) ?? new InGameStatGrowthRoot();
        }
        return new InGameStatGrowthRoot();
    }
}
