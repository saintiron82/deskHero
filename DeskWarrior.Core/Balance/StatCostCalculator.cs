using DeskWarrior.Core.Models;

namespace DeskWarrior.Core.Balance;

/// <summary>
/// 영구 스탯 비용 계산기
/// cost = base_cost × (1 + level × growth_rate) × multiplier^(level / softcap_interval)
/// </summary>
public class StatCostCalculator
{
    private readonly Dictionary<string, StatConfig> _statConfigs;

    public StatCostCalculator(Dictionary<string, StatConfig> statConfigs)
    {
        _statConfigs = statConfigs;
    }

    /// <summary>
    /// 특정 스탯의 다음 레벨 업그레이드 비용 계산
    /// 티어 시스템이 활성화되면 레벨에 따라 multiplier와 softcap이 자동 조정됩니다.
    /// </summary>
    public long GetUpgradeCost(string statId, int currentLevel, double crystalDiscount = 0, long crystalFlatReduction = 0)
    {
        if (!_statConfigs.TryGetValue(statId, out var config))
            return long.MaxValue;

        // MaxLevel 체크 (0 = 무제한)
        if (config.MaxLevel > 0 && currentLevel >= config.MaxLevel)
            return long.MaxValue;

        int targetLevel = currentLevel + 1;

        // 파라미터 (tier_overrides로 변경 가능)
        double baseCost = config.BaseCost;
        double growthRate = config.GrowthRate;
        int softcap = config.SoftcapInterval;
        double multiplier = config.Multiplier;
        int levelForCalc = targetLevel;

        if (config.TierConfig != null)
        {
            int interval = Math.Max(1, config.TierConfig.TierInterval);

            if (config.TierConfig.TierOverrides != null)
            {
                // tier_overrides 모드: 상대 레벨 + 오버라이드 파라미터
                int tier = (targetLevel - 1) / interval;
                int relativeLevel = ((targetLevel - 1) % interval) + 1;
                levelForCalc = relativeLevel;

                var ov = config.TierConfig.GetOverride(tier);
                if (ov != null)
                {
                    baseCost = ov.BaseCost ?? config.BaseCost;
                    growthRate = ov.GrowthRate ?? config.GrowthRate;
                    multiplier = ov.Multiplier ?? config.Multiplier;
                    softcap = ov.SoftcapInterval ?? config.SoftcapInterval;
                }
            }
            else
            {
                // 기존 formula-based 티어 조정 (역호환)
                int tier = targetLevel / interval;
                multiplier = config.TierConfig.BaseMultiplier - (tier * config.TierConfig.MultiplierDecreasePerTier);
                softcap = config.TierConfig.BaseSoftcap + (tier * config.TierConfig.SoftcapIncreasePerTier);

                if (multiplier < 1.0)
                {
                    multiplier = 1.0;
                }
            }
        }

        double linearFactor = 1.0 + levelForCalc * growthRate;
        double exponentialFactor = Math.Pow(multiplier, (double)levelForCalc / softcap);
        double cost = baseCost * linearFactor * exponentialFactor;

        // 크리스탈 % 할인 적용
        if (crystalDiscount > 0)
        {
            cost *= (1.0 - crystalDiscount);
        }

        // 크리스탈 고정 차감 적용
        if (crystalFlatReduction > 0)
        {
            cost -= crystalFlatReduction;
            if (cost < 1.0) cost = 1.0;
        }

        // 오버플로우 방지: long 범위 초과 시 센티넬 반환 (구매 불가)
        if (double.IsNaN(cost) || double.IsInfinity(cost) || cost > 9.2E+18)
            return long.MaxValue;

        return (long)Math.Ceiling(cost);
    }

    /// <summary>
    /// 특정 스탯의 현재 레벨에서 목표 레벨까지 총 비용 계산
    /// </summary>
    public long GetTotalCost(string statId, int fromLevel, int toLevel)
    {
        long total = 0;
        for (int level = fromLevel; level < toLevel; level++)
        {
            long cost = GetUpgradeCost(statId, level);
            if (cost == long.MaxValue)
                return long.MaxValue;
            // 오버플로우 방지
            if (total > long.MaxValue - cost)
                return long.MaxValue;
            total += cost;
        }
        return total;
    }

    /// <summary>
    /// 주어진 예산으로 달성 가능한 최대 레벨 계산
    /// </summary>
    public int MaxLevelForBudget(string statId, long budget, int startLevel = 0)
    {
        if (!_statConfigs.TryGetValue(statId, out var config))
            return startLevel;

        int level = startLevel;
        long spent = 0;

        // MaxLevel 상한 체크 (0 = 무제한)
        int maxLevel = config.MaxLevel > 0 ? config.MaxLevel : int.MaxValue;

        while (level < maxLevel)
        {
            long cost = GetUpgradeCost(statId, level);
            if (cost == long.MaxValue || spent + cost > budget)
                break;
            spent += cost;
            level++;
        }

        return level;
    }

    /// <summary>
    /// 비용 대비 효과 효율 계산 (다음 레벨)
    /// </summary>
    public double GetEfficiency(string statId, int currentLevel, double crystalDiscount = 0, long crystalFlatReduction = 0)
    {
        if (!_statConfigs.TryGetValue(statId, out var config))
            return 0;

        long cost = GetUpgradeCost(statId, currentLevel, crystalDiscount, crystalFlatReduction);
        if (cost <= 0) return 0;

        return config.EffectPerLevel / cost;
    }

    /// <summary>
    /// 모든 스탯 중 가장 효율적인 업그레이드 찾기
    /// </summary>
    public (string statId, long cost, double efficiency)? FindBestUpgrade(
        SimPermanentStats stats,
        long availableCrystals)
    {
        var candidates = new List<(string statId, long cost, double efficiency)>();
        double crystalDiscount = stats.CrystalDiscount;
        long crystalFlatReduction = stats.CrystalFlatReduction;

        foreach (var (statId, config) in _statConfigs)
        {
            int currentLevel = GetStatLevel(stats, statId);

            // MaxLevel 체크 - 이미 최대면 스킵
            if (!CanUpgrade(statId, currentLevel))
                continue;

            long cost = GetUpgradeCost(statId, currentLevel, crystalDiscount, crystalFlatReduction);

            if (cost <= availableCrystals && cost != long.MaxValue)
            {
                double efficiency = GetEfficiency(statId, currentLevel, crystalDiscount, crystalFlatReduction);
                candidates.Add((statId, cost, efficiency));
            }
        }

        if (candidates.Count == 0)
            return null;

        return candidates.OrderByDescending(c => c.efficiency).First();
    }

    /// <summary>
    /// 스탯 업그레이드 가능 여부 (MaxLevel 체크)
    /// </summary>
    public bool CanUpgrade(string statId, int currentLevel)
    {
        if (!_statConfigs.TryGetValue(statId, out var config))
            return false;

        return config.MaxLevel == 0 || currentLevel < config.MaxLevel;
    }

    /// <summary>
    /// 스탯의 최대 레벨 조회 (0 = 무제한)
    /// </summary>
    public int GetMaxLevel(string statId)
    {
        if (!_statConfigs.TryGetValue(statId, out var config))
            return 0;

        return config.MaxLevel;
    }

    /// <summary>
    /// SimPermanentStats에서 특정 스탯의 현재 레벨 조회
    /// </summary>
    public int GetStatLevel(SimPermanentStats stats, string statId)
    {
        return statId switch
        {
            "base_attack" => stats.BaseAttackLevel,
            "attack_percent" => stats.AttackPercentLevel,
            "crit_chance" => stats.CritChanceLevel,
            "crit_damage" => stats.CritDamageLevel,
            "multi_hit" => stats.MultiHitLevel,
            "gold_flat_perm" => stats.GoldFlatPermLevel,
            "gold_multi_perm" => stats.GoldMultiPermLevel,
            "crystal_flat" => stats.CrystalFlatLevel,
            "crystal_multi" => stats.CrystalMultiLevel,
            "time_extend" => stats.TimeExtendLevel,
            "upgrade_discount" => stats.UpgradeDiscountLevel,
            "cost_flat_reduction" => stats.CostFlatReductionLevel,
            "crystal_discount" => stats.CrystalDiscountLevel,
            "crystal_flat_reduction" => stats.CrystalFlatReductionLevel,
            "start_level" => stats.StartLevelLevel,
            "start_gold" => stats.StartGoldLevel,
            "start_keyboard" => stats.StartKeyboardLevel,
            "start_mouse" => stats.StartMouseLevel,
            "start_gold_flat" => stats.StartGoldFlatLevel,
            "start_gold_multi" => stats.StartGoldMultiLevel,
            "start_combo_flex" => stats.StartComboFlexLevel,
            "start_combo_damage" => stats.StartComboDamageLevel,
            _ => 0
        };
    }

    /// <summary>
    /// SimPermanentStats에서 특정 스탯 레벨 설정
    /// </summary>
    public void SetStatLevel(SimPermanentStats stats, string statId, int level)
    {
        switch (statId)
        {
            case "base_attack": stats.BaseAttackLevel = level; break;
            case "attack_percent": stats.AttackPercentLevel = level; break;
            case "crit_chance": stats.CritChanceLevel = level; break;
            case "crit_damage": stats.CritDamageLevel = level; break;
            case "multi_hit": stats.MultiHitLevel = level; break;
            case "gold_flat_perm": stats.GoldFlatPermLevel = level; break;
            case "gold_multi_perm": stats.GoldMultiPermLevel = level; break;
            case "crystal_flat": stats.CrystalFlatLevel = level; break;
            case "crystal_multi": stats.CrystalMultiLevel = level; break;
            case "time_extend": stats.TimeExtendLevel = level; break;
            case "upgrade_discount": stats.UpgradeDiscountLevel = level; break;
            case "cost_flat_reduction": stats.CostFlatReductionLevel = level; break;
            case "crystal_discount": stats.CrystalDiscountLevel = level; break;
            case "crystal_flat_reduction": stats.CrystalFlatReductionLevel = level; break;
            case "start_level": stats.StartLevelLevel = level; break;
            case "start_gold": stats.StartGoldLevel = level; break;
            case "start_keyboard": stats.StartKeyboardLevel = level; break;
            case "start_mouse": stats.StartMouseLevel = level; break;
            case "start_gold_flat": stats.StartGoldFlatLevel = level; break;
            case "start_gold_multi": stats.StartGoldMultiLevel = level; break;
            case "start_combo_flex": stats.StartComboFlexLevel = level; break;
            case "start_combo_damage": stats.StartComboDamageLevel = level; break;
        }
    }

    /// <summary>
    /// 모든 스탯 ID 목록
    /// </summary>
    public IEnumerable<string> AllStatIds => _statConfigs.Keys;
}

/// <summary>
/// 티어 설정 (공식 기반, 무한 확장 + tier_overrides 지원)
/// </summary>
public class TierConfigSim
{
    public int TierInterval { get; set; } = 1000;
    public double BaseMultiplier { get; set; } = 1.6;
    public double MultiplierDecreasePerTier { get; set; } = 0.1;
    public int BaseSoftcap { get; set; } = 12;
    public int SoftcapIncreasePerTier { get; set; } = 3;

    /// <summary>
    /// 티어별 독립 파라미터 오버라이드 (키: 티어 인덱스 "0"=Z, "1"=Y, ...)
    /// </summary>
    public Dictionary<string, TierOverride>? TierOverrides { get; set; }

    public TierOverride? GetOverride(int tier)
    {
        if (TierOverrides == null) return null;
        return TierOverrides.TryGetValue(tier.ToString(), out var o) ? o : null;
    }
}

/// <summary>
/// 스탯 설정 (PermanentStats.json에서 로드)
/// </summary>
public class StatConfig
{
    public string StatName { get; set; } = "";
    public string Category { get; set; } = "";
    public double BaseCost { get; set; }
    public double GrowthRate { get; set; }
    public double Multiplier { get; set; }
    public int SoftcapInterval { get; set; }
    public double EffectPerLevel { get; set; }
    public int MaxLevel { get; set; }
    public double Weight { get; set; } = 1.0;
    public TierConfigSim? TierConfig { get; set; }
}
