namespace DeskWarrior.Core.Models;

/// <summary>
/// 시뮬레이션용 영구 스탯 (게임의 PermanentStats와 동일 구조)
/// Config에서 effect_per_level을 읽어서 효과 계산
/// </summary>
public class SimPermanentStats
{
    // Config 참조 (효과 계산에 사용)
    private Dictionary<string, StatGrowthConfig>? _config;

    public void SetConfig(Dictionary<string, StatGrowthConfig> config)
    {
        _config = config;
    }

    // 기본 능력
    public int BaseAttackLevel { get; set; } = 0;
    public int AttackPercentLevel { get; set; } = 0;
    public int CritChanceLevel { get; set; } = 0;
    public int CritDamageLevel { get; set; } = 0;
    public int MultiHitLevel { get; set; } = 0;

    // 재화 보너스
    public int GoldFlatPermLevel { get; set; } = 0;
    public int GoldMultiPermLevel { get; set; } = 0;
    public int CrystalFlatLevel { get; set; } = 0;
    public int CrystalMultiLevel { get; set; } = 0;

    // 유틸리티
    public int TimeExtendLevel { get; set; } = 0;
    public int UpgradeDiscountLevel { get; set; } = 0;

    // 시작 보너스
    public int StartLevelLevel { get; set; } = 0;
    public int StartGoldLevel { get; set; } = 0;
    public int StartKeyboardLevel { get; set; } = 0;
    public int StartMouseLevel { get; set; } = 0;
    public int StartGoldFlatLevel { get; set; } = 0;
    public int StartGoldMultiLevel { get; set; } = 0;
    public int StartComboFlexLevel { get; set; } = 0;
    public int StartComboDamageLevel { get; set; } = 0;

    // Config에서 효과 계산하는 헬퍼 메서드
    private double GetEffect(string statKey, int level)
    {
        if (_config != null && _config.TryGetValue(statKey, out var cfg))
        {
            return cfg.CalculateEffect(level);
        }
        // Config가 없으면 레벨 그대로 반환 (fallback)
        return level;
    }

    // 계산된 효과 (Config의 effect_per_level 사용)
    public int BaseAttack => (int)GetEffect("base_attack", BaseAttackLevel);
    public double AttackPercentBonus => GetEffect("attack_percent", AttackPercentLevel) / 100.0;  // % → 소수
    public double CriticalChanceBonus => GetEffect("crit_chance", CritChanceLevel) / 100.0;       // % → 소수
    public double CriticalDamageBonus => GetEffect("crit_damage", CritDamageLevel);
    public double MultiHitChance => GetEffect("multi_hit", MultiHitLevel) / 100.0;               // % → 소수
    public double GoldFlatPerm => GetEffect("gold_flat_perm", GoldFlatPermLevel);
    public double GoldMultiPerm => GetEffect("gold_multi_perm", GoldMultiPermLevel) / 100.0;     // % → 소수
    public int CrystalFlat => (int)GetEffect("crystal_flat", CrystalFlatLevel);
    public double CrystalDropChanceBonus => GetEffect("crystal_chance", CrystalMultiLevel) / 100.0; // % → 소수
    public double TimeExtend => GetEffect("time_extend", TimeExtendLevel);
    public double UpgradeCostReduction => GetEffect("upgrade_discount", UpgradeDiscountLevel) / 100.0; // % → 소수
    public int StartLevel => (int)GetEffect("start_level", StartLevelLevel);
    public int StartGold => (int)GetEffect("start_gold", StartGoldLevel);
    public int StartKeyboardPower => (int)GetEffect("start_keyboard", StartKeyboardLevel);
    public int StartMousePower => (int)GetEffect("start_mouse", StartMouseLevel);

    public SimPermanentStats Clone()
    {
        var clone = new SimPermanentStats
        {
            BaseAttackLevel = this.BaseAttackLevel,
            AttackPercentLevel = this.AttackPercentLevel,
            CritChanceLevel = this.CritChanceLevel,
            CritDamageLevel = this.CritDamageLevel,
            MultiHitLevel = this.MultiHitLevel,
            GoldFlatPermLevel = this.GoldFlatPermLevel,
            GoldMultiPermLevel = this.GoldMultiPermLevel,
            CrystalFlatLevel = this.CrystalFlatLevel,
            CrystalMultiLevel = this.CrystalMultiLevel,
            TimeExtendLevel = this.TimeExtendLevel,
            UpgradeDiscountLevel = this.UpgradeDiscountLevel,
            StartLevelLevel = this.StartLevelLevel,
            StartGoldLevel = this.StartGoldLevel,
            StartKeyboardLevel = this.StartKeyboardLevel,
            StartMouseLevel = this.StartMouseLevel,
            StartGoldFlatLevel = this.StartGoldFlatLevel,
            StartGoldMultiLevel = this.StartGoldMultiLevel,
            StartComboFlexLevel = this.StartComboFlexLevel,
            StartComboDamageLevel = this.StartComboDamageLevel
        };
        if (_config != null)
        {
            clone.SetConfig(_config);
        }
        return clone;
    }
}

/// <summary>
/// 시뮬레이션용 인게임 스탯
/// </summary>
public class SimInGameStats
{
    public int KeyboardPowerLevel { get; set; } = 0;
    public int MousePowerLevel { get; set; } = 0;

    public void Reset()
    {
        KeyboardPowerLevel = 0;
        MousePowerLevel = 0;
    }

    public SimInGameStats Clone()
    {
        return new SimInGameStats
        {
            KeyboardPowerLevel = this.KeyboardPowerLevel,
            MousePowerLevel = this.MousePowerLevel
        };
    }
}

/// <summary>
/// 시뮬레이션용 몬스터 데이터
/// 게임과 동일한 공식 사용 (지수 성장)
/// </summary>
public class SimMonster
{
    public int Level { get; private set; }
    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public bool IsBoss { get; private set; }
    public int GoldReward { get; private set; }
    public bool IsAlive => CurrentHp > 0;

    public SimMonster(int level, bool isBoss, int baseHp, double hpGrowth, int baseGold, double goldGrowth)
    {
        Level = level;
        IsBoss = isBoss;

        // 게임 공식: baseHp + (level - 1) * hpGrowth (선형 성장)
        MaxHp = baseHp + (level - 1) * (int)hpGrowth;

        // 보스는 HP 배율 적용 (CreateMonster에서 이미 적용됨)
        CurrentHp = MaxHp;

        // 골드 보상: stage * BASE_GOLD_MULTI
        GoldReward = (int)(level * goldGrowth);
    }

    public int TakeDamage(int damage)
    {
        int actualDamage = Math.Min(damage, CurrentHp);
        CurrentHp -= actualDamage;
        return actualDamage;
    }
}

/// <summary>
/// 세션 결과
/// </summary>
public class SessionResult
{
    public long MaxLevel { get; set; }
    public long TotalGold { get; set; }
    public long TotalDamage { get; set; }
    public int MonstersKilled { get; set; }
    public int BossesKilled { get; set; }
    public int TotalInputs { get; set; }
    public int CriticalHits { get; set; }
    public double SessionDuration { get; set; }  // seconds
    public string EndReason { get; set; } = "timeout";

    // 크리스털 획득 (Phase 2)
    public long CrystalsFromBosses { get; set; }     // 보스 드롭
    public long CrystalsFromStages { get; set; }     // 스테이지 클리어 보너스
    public long CrystalsFromGoldConvert { get; set; } // 골드 변환
    public long TotalCrystals => CrystalsFromBosses + CrystalsFromStages + CrystalsFromGoldConvert;
}

/// <summary>
/// 배치 시뮬레이션 결과
/// </summary>
public class BatchResult
{
    public int NumSimulations { get; set; }
    public double AverageLevel { get; set; }
    public double MedianLevel { get; set; }
    public double MinLevel { get; set; }
    public double MaxLevel { get; set; }
    public double StandardDeviation { get; set; }
    public double[] LevelDistribution { get; set; } = [];
    public int TargetLevel { get; set; }
    public double SuccessRate { get; set; }
    public double MedianAttemptsToTarget { get; set; }
    public double AverageDuration { get; set; }
    public List<SessionResult> AllResults { get; set; } = [];

    // 크리스털 통계
    public double AverageCrystals { get; set; }
    public double AverageCrystalsFromBosses { get; set; }
    public double AverageCrystalsFromStages { get; set; }
    public double AverageCrystalsFromGoldConvert { get; set; }
}
