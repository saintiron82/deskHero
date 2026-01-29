namespace DeskWarrior.Core.Formulas;

/// <summary>
/// 스탯 공식 계산 (StatFormulas.Generated.cs와 동일)
/// 게임과 시뮬레이터 간 공식 일관성 보장
/// </summary>
public static class StatFormulas
{
    #region Constants

    public const double BASE_CRIT_CHANCE = 0.1;
    public const double BASE_CRIT_MULTIPLIER = 2.0;
    public const int BASE_TIME_LIMIT = 30;
    public const double COMBO_DURATION = 3.0;
    public const int MAX_COMBO_STACK = 3;
    public const int GOLD_TO_CRYSTAL_RATE = 1000;
    public const int BASE_HP = 100;
    public const double HP_GROWTH = 1.2;
    public const int BOSS_INTERVAL = 10;
    public const double BOSS_HP_MULTI = 5.0;
    public const double BASE_GOLD_MULTI = 1.5;

    #endregion

    #region Formula Methods

    /// <summary>
    /// 업그레이드 비용
    /// 공식: base_cost * (1 + level * growth_rate) * pow(multiplier, level / softcap_interval)
    /// </summary>
    public static int CalcUpgradeCost(double base_cost, double growth_rate, double multiplier, double softcap_interval, int level)
    {
        return (int)(base_cost * (1 + level * growth_rate) * Math.Pow(multiplier, level / softcap_interval));
    }

    /// <summary>
    /// 스탯 효과
    /// 공식: effect_per_level * level
    /// </summary>
    public static double CalcStatEffect(int effect_per_level, int level)
    {
        return effect_per_level * level;
    }

    /// <summary>
    /// 데미지 계산
    /// 공식: (base_power + base_attack) * (1 + attack_percent) * crit_multiplier * multi_hit_multiplier * combo_multiplier
    /// </summary>
    public static int CalcDamage(double base_power, double base_attack, double attack_percent, double crit_multiplier, double multi_hit_multiplier, double combo_multiplier)
    {
        return (int)((base_power + base_attack) * (1 + attack_percent) * crit_multiplier * multi_hit_multiplier * combo_multiplier);
    }

    /// <summary>
    /// 골드 획득
    /// 공식: (base_gold + gold_flat + gold_flat_perm) * (1 + gold_multi + gold_multi_perm)
    /// </summary>
    public static int CalcGoldEarned(double base_gold, int gold_flat, int gold_flat_perm, double gold_multi, double gold_multi_perm)
    {
        return (int)((base_gold + gold_flat + gold_flat_perm) * (1 + gold_multi + gold_multi_perm));
    }

    /// <summary>
    /// 콤보 배율
    /// 공식: (1 + combo_damage / 100) * pow(2, combo_stack)
    /// </summary>
    public static double CalcComboMultiplier(double combo_damage, int combo_stack)
    {
        return (1 + combo_damage / 100) * Math.Pow(2, combo_stack);
    }

    /// <summary>
    /// 몬스터 HP
    /// 스테이지별 일반 몬스터 HP
    /// 공식: BASE_HP * pow(HP_GROWTH, stage)
    /// </summary>
    public static int CalcMonsterHp(double stage)
    {
        return (int)(BASE_HP * Math.Pow(HP_GROWTH, stage));
    }

    /// <summary>
    /// 보스 HP
    /// 보스 몬스터 HP (BOSS_INTERVAL 스테이지마다 등장)
    /// 공식: BASE_HP * pow(HP_GROWTH, stage) * BOSS_HP_MULTI
    /// </summary>
    public static int CalcBossHp(double stage)
    {
        return (int)(BASE_HP * Math.Pow(HP_GROWTH, stage) * BOSS_HP_MULTI);
    }

    /// <summary>
    /// 기본 골드
    /// 스테이지별 기본 골드 획득량
    /// 공식: stage * BASE_GOLD_MULTI
    /// </summary>
    public static int CalcBaseGold(double stage)
    {
        return (int)(stage * BASE_GOLD_MULTI);
    }

    /// <summary>
    /// 필요 CPS
    /// 해당 스테이지 클리어에 필요한 초당 클릭 수
    /// 공식: monster_hp / damage / time_limit
    /// </summary>
    public static double CalcRequiredCps(double monster_hp, double damage, double time_limit)
    {
        return monster_hp / damage / time_limit;
    }

    #endregion
}
