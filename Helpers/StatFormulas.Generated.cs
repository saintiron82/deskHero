// ============================================================
// DeskWarrior 스탯 공식 (자동 생성)
// 생성일: 2026-01-16 00:30:26
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

        #endregion
    }
}