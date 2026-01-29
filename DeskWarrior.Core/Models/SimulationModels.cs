namespace DeskWarrior.Core.Models;

/// <summary>
/// 시뮬레이션용 영구 스탯 (게임의 PermanentStats와 동일 구조)
/// </summary>
public class SimPermanentStats
{
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

    // 계산된 효과 (PermanentStats의 Legacy Properties와 동일)
    public int BaseAttack => BaseAttackLevel;
    public double AttackPercentBonus => AttackPercentLevel * 0.01;  // PermanentStats.json: 1% per level
    public double CriticalChanceBonus => CritChanceLevel * 0.005;   // 0.5% per level
    public double CriticalDamageBonus => CritDamageLevel * 0.002;   // 0.2 per level
    public double MultiHitChance => MultiHitLevel * 0.005;          // 0.5% per level
    public double GoldFlatPerm => GoldFlatPermLevel * 2;            // +2 gold per level
    public double GoldMultiPerm => GoldMultiPermLevel * 3;          // +3% per level
    public int CrystalFlat => CrystalFlatLevel;                     // +1 crystal per level
    public double CrystalDropChanceBonus => CrystalMultiLevel * 0.02; // +2% drop chance per level
    public double TimeExtend => TimeExtendLevel * 0.1;              // +0.1s per level
    public double UpgradeCostReduction => UpgradeDiscountLevel * 0.001; // 0.1% per level
    public int StartLevel => StartLevelLevel;
    public int StartGold => StartGoldLevel * 50;
    public int StartKeyboardPower => (int)(StartKeyboardLevel * 0.1);
    public int StartMousePower => (int)(StartMouseLevel * 0.1);

    public SimPermanentStats Clone()
    {
        return new SimPermanentStats
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
/// </summary>
public class SimMonster
{
    public int Level { get; private set; }
    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public bool IsBoss { get; private set; }
    public int GoldReward { get; private set; }
    public bool IsAlive => CurrentHp > 0;

    public SimMonster(int level, bool isBoss, int baseHp, int hpGrowth, int baseGold, int goldGrowth)
    {
        Level = level;
        IsBoss = isBoss;
        MaxHp = baseHp + (level - 1) * hpGrowth;
        CurrentHp = MaxHp;
        GoldReward = baseGold + level * goldGrowth;
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
    public int MaxLevel { get; set; }
    public int TotalGold { get; set; }
    public long TotalDamage { get; set; }
    public int MonstersKilled { get; set; }
    public int BossesKilled { get; set; }
    public int TotalInputs { get; set; }
    public int CriticalHits { get; set; }
    public double SessionDuration { get; set; }  // seconds
    public string EndReason { get; set; } = "timeout";

    // 크리스털 획득 (Phase 2)
    public int CrystalsFromBosses { get; set; }     // 보스 드롭
    public int CrystalsFromStages { get; set; }     // 스테이지 클리어 보너스
    public int CrystalsFromGoldConvert { get; set; } // 골드 변환
    public int TotalCrystals => CrystalsFromBosses + CrystalsFromStages + CrystalsFromGoldConvert;
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
