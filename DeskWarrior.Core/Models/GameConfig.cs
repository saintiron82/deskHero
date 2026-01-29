using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskWarrior.Core.Models;

/// <summary>
/// 게임 밸런스 설정 (GameData.json 로드)
/// </summary>
public class GameConfig
{
    public BalanceConfig Balance { get; set; } = new();
    public UpgradeConfig Upgrade { get; set; } = new();

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
    public int BaseGoldMultiplier { get; set; } = 1;

    [JsonPropertyName("critical_chance")]
    public double CriticalChance { get; set; } = 0.1;

    [JsonPropertyName("critical_multiplier")]
    public double CriticalMultiplier { get; set; } = 2.0;

    [JsonPropertyName("upgrade_cost_interval")]
    public int UpgradeCostInterval { get; set; } = 50;  // 50스테이지마다 비용 2배
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

    [JsonPropertyName("max_level")]
    public int MaxLevel { get; set; } = 0;

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
        return level * EffectPerLevel;
    }
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
