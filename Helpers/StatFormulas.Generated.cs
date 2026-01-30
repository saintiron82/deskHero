// ============================================================
// DeskWarrior 스탯 공식 (자동 생성)
// 생성일: 2026-01-29 20:58:49
// 경고: 이 파일을 직접 수정하지 마세요!
//       config/StatFormulas.json을 수정 후 generate_stat_code.py 실행
// ============================================================

using System;

namespace DeskWarrior.Helpers
{
    /// <summary>
    /// 스탯 공식 계산 (자동 생성)
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
        public const double HP_GROWTH = 1.12;
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
        /// 기본 데미지 공식 - 원복
        /// 공식: (base_power + base_attack) * (1 + attack_percent) * crit_multiplier * multi_hit_multiplier * combo_multiplier * utility_bonus
        /// </summary>
        public static int CalcDamage(double base_power, double base_attack, double attack_percent, double crit_multiplier, double multi_hit_multiplier, double combo_multiplier, double utility_bonus)
        {
            return (int)((base_power + base_attack) * (1 + attack_percent) * crit_multiplier * multi_hit_multiplier * combo_multiplier * utility_bonus);
        }

        /// <summary>
        /// 유틸리티 보너스
        /// 유틸리티 스탯 투자에 따른 데미지 보너스 (0.5%/레벨)
        /// 공식: 1 + (time_extend_level + upgrade_discount_level) * 0.005
        /// </summary>
        public static double CalcUtilityBonus(int time_extend_level, int upgrade_discount_level)
        {
            return 1 + (time_extend_level + upgrade_discount_level) * 0.005;
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
        /// 시간 도둑
        /// 최대 기본시간의 2배까지만 연장
        /// 공식: min(current_time + bonus_time, base_time * 2)
        /// </summary>
        public static double CalcTimeThief(double current_time, double bonus_time, double base_time)
        {
            return Math.Min(current_time + bonus_time, base_time * 2);
        }

        /// <summary>
        /// 콤보 허용 오차
        /// 공식: 0.01 + combo_flex * 0.005
        /// </summary>
        public static double CalcComboTolerance(double combo_flex)
        {
            return 0.01 + combo_flex * 0.005;
        }

        /// <summary>
        /// 크리스탈 드롭 확률
        /// 공식: min(base_chance + crystal_multi / 100, max_chance)
        /// </summary>
        public static double CalcCrystalDropChance(double base_chance, double crystal_multi, double max_chance)
        {
            return Math.Min(base_chance + crystal_multi / 100, max_chance);
        }

        /// <summary>
        /// 크리스탈 드롭량
        /// 공식: base_amount + crystal_flat
        /// </summary>
        public static int CalcCrystalDropAmount(int base_amount, int crystal_flat)
        {
            return (int)(base_amount + crystal_flat);
        }

        /// <summary>
        /// 할인된 비용
        /// 공식: original_cost * (1 - upgrade_discount / 100)
        /// </summary>
        public static int CalcDiscountedCost(double original_cost, double upgrade_discount)
        {
            return (int)(original_cost * (1 - upgrade_discount / 100));
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
}