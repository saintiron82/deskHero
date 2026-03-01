using DeskWarrior.Core.Simulation;

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
    public int CostFlatReductionLevel { get; set; } = 0;
    public int CrystalDiscountLevel { get; set; } = 0;
    public int CrystalFlatReductionLevel { get; set; } = 0;

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
    public long CostFlatReduction => (long)GetEffect("cost_flat_reduction", CostFlatReductionLevel);
    public double CrystalDiscount => GetEffect("crystal_discount", CrystalDiscountLevel) / 100.0;
    public long CrystalFlatReduction => (long)GetEffect("crystal_flat_reduction", CrystalFlatReductionLevel);
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
            CostFlatReductionLevel = this.CostFlatReductionLevel,
            CrystalDiscountLevel = this.CrystalDiscountLevel,
            CrystalFlatReductionLevel = this.CrystalFlatReductionLevel,
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
/// 디버그용 레벨별 정보
/// </summary>
public class DebugLevelInfo
{
    public long Level { get; set; }
    public long MonsterHp { get; set; }
    public bool IsBoss { get; set; }
    public bool IsGoldenGoblin { get; set; }
    public string Element { get; set; } = "normal";
    public int BaseDamage { get; set; }
    public long Gold { get; set; }
    public long GoldReward { get; set; }
    public int KeyboardLevel { get; set; }
    public int MouseLevel { get; set; }
    public double TimeElapsed { get; set; }
    public double TimeLimit { get; set; }
    public bool Survived { get; set; }
}

/// <summary>
/// 시뮬레이션용 몬스터 데이터
/// 게임과 동일한 공식 사용 (지수 성장)
/// </summary>
public class SimMonster
{
    public long Level { get; private set; }
    public long MaxHp { get; private set; }
    public long CurrentHp { get; private set; }
    public bool IsBoss { get; private set; }
    public long GoldReward { get; private set; }
    public string Element { get; private set; }  // ✅ 추가: 몬스터 속성
    public double KeyboardResistance { get; private set; }  // ✅ 추가: 키보드 저항
    public double MouseResistance { get; private set; }  // ✅ 추가: 마우스 저항
    public bool IsAlive => CurrentHp > 0;

    public SimMonster(long level, bool isBoss, int baseHp, double hpGrowth, int baseGold, double goldGrowth, TierHpSystemConfig? tierConfig = null, string element = "normal", double keyboardResistance = 1.0, double mouseResistance = 1.0)
    {
        Level = level;
        IsBoss = isBoss;
        Element = element;  // ✅ 추가
        KeyboardResistance = keyboardResistance;  // ✅ 추가
        MouseResistance = mouseResistance;  // ✅ 추가

        // 게임 공식: baseHp + (level - 1) * hpGrowth (선형 성장) 또는 티어 기반
        MaxHp = CalculateHp(baseHp, hpGrowth, level, tierConfig);

        // 보스는 HP 배율 적용 (CreateMonster에서 이미 적용됨)
        CurrentHp = MaxHp;

        // 골드 보상: baseGold + level * goldGrowth (게임과 동일)
        GoldReward = (long)baseGold + (long)level * (long)goldGrowth;
    }

    public void ApplyHpModifier(double modifier)
    {
        if (modifier == 1.0)
            return;

        MaxHp = OverflowGuard.ToLong((double)MaxHp * modifier, "MonsterHP.Modifier", Level);
        CurrentHp = MaxHp;
    }

    /// <summary>
    /// HP 계산 (티어 시스템 지원)
    /// </summary>
    private static long CalculateHp(int baseHp, double hpGrowth, long level, TierHpSystemConfig? tierConfig)
    {
        // Feature Flag: 티어 시스템 활성화 시
        if (tierConfig?.Enabled == true)
        {
            long tier = (level - 1) / tierConfig.TierInterval;
            double tierIndex = tier;
            if (tierConfig.TierCurveExponent > 0.0 && tierConfig.TierCurveExponent != 1.0 && tierIndex > 0.0)
            {
                tierIndex = Math.Pow(tierIndex, tierConfig.TierCurveExponent);
            }
            double tierMultiplier = Math.Pow(tierConfig.TierMultiplier, tierIndex);
            if (tierConfig.TierMultiplierDecayPerTier != 1.0)
            {
                double decay = Math.Pow(tierConfig.TierMultiplierDecayPerTier, tierIndex * (tierIndex - 1) / 2.0);
                tierMultiplier *= decay;
            }
            if (tierConfig.MinTierMultiplier > 0.0 && tierMultiplier < tierConfig.MinTierMultiplier)
            {
                tierMultiplier = tierConfig.MinTierMultiplier;
            }
            long tierBaseHp = OverflowGuard.ToLong(baseHp * tierMultiplier, "MonsterHP.TierBase", level);

            long levelInTier = (level - 1) % tierConfig.TierInterval;

            // 티어마다 성장률 감소 적용
            double tierGrowthRate = tierConfig.LinearGrowthPerLevel * Math.Pow(tierConfig.GrowthDecreasePerTier, tierIndex);
            if (tierConfig.MinLinearGrowthPerLevel > 0.0 && tierGrowthRate < tierConfig.MinLinearGrowthPerLevel)
            {
                tierGrowthRate = tierConfig.MinLinearGrowthPerLevel;
            }
            long linearIncrease = OverflowGuard.ToLong(levelInTier * tierGrowthRate, "MonsterHP.Linear", level);
            long hp = OverflowGuard.Add(tierBaseHp, linearIncrease, "MonsterHP.Total", level);

            if (tierConfig.LateStartLevel > 0 && level >= tierConfig.LateStartLevel)
            {
                int lateInterval = tierConfig.LateTierInterval > 0 ? tierConfig.LateTierInterval : tierConfig.TierInterval;
                long lateTier = (level - tierConfig.LateStartLevel) / Math.Max(1, lateInterval);
                if (tierConfig.MaxLateTiers > 0 && lateTier > tierConfig.MaxLateTiers)
                    lateTier = tierConfig.MaxLateTiers;
                double lateMultiplier = tierConfig.LateTierMultiplier != 1.0
                    ? Math.Pow(tierConfig.LateTierMultiplier, lateTier)
                    : 1.0;
                hp = OverflowGuard.ToLong((double)hp * lateMultiplier, "MonsterHP.Late", level);
            }

            return hp;
        }

        // Legacy: 선형 공식
        return baseHp + (level - 1) * (int)hpGrowth;
    }

    public long TakeDamage(long damage)
    {
        long actualDamage = Math.Min(damage, CurrentHp);
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

    // 크리스털 획득
    public long CrystalsFromBosses { get; set; }     // 보스 드롭
    public long CrystalsFromStages { get; set; }     // 스테이지 클리어 보너스
    public long CrystalsFromGoldConvert { get; set; } // 골드 변환
    public long TotalCrystals => CrystalsFromBosses + CrystalsFromStages + CrystalsFromGoldConvert;

    // 황금 고블린 통계
    public int GoldenGoblinsKilled { get; set; }     // 처치한 황금 고블린 수
    public int GoldenGoblinsEscaped { get; set; }    // 도주한 황금 고블린 수
    public long GoldenGoblinGoldEarned { get; set; } // 황금 고블린에서 획득한 골드

    // ✅ 인게임 업그레이드 (세션 종료 시 최종값)
    public long SpentGold { get; set; }                // 소비한 골드
    public int FinalKeyboardPowerLevel { get; set; }  // 최종 키보드 파워 레벨
    public int FinalMousePowerLevel { get; set; }     // 최종 마우스 파워 레벨

    // ✅ 영구 스탯 투자 (세션 후 처리)
    public long SpentCrystals { get; set; }            // 이번 세션 후 소비한 크리스탈
    public Dictionary<string, int> PermanentStatLevels { get; set; } = new(); // 세션 종료 시 영구 스탯 레벨

    // ✅ 메타데이터
    public int SessionNumber { get; set; }            // 세션 번호 (1, 2, 3...)
    public double TotalPlaytime { get; set; }         // 누적 플레이 시간 (초)
    public long RemainingCrystals { get; set; }        // 남은 크리스탈 (누적)

    // 오버플로우 감지
    public bool OverflowDetected { get; set; }
    public string? OverflowLocation { get; set; }
    public long OverflowLevel { get; set; }
    public double OverflowValue { get; set; }
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

    // 황금 고블린 통계
    public double AverageGoldenGoblinsKilled { get; set; }
    public double AverageGoldenGoblinsEscaped { get; set; }
    public double AverageGoldenGoblinGold { get; set; }
}
