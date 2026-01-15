using DeskWarrior.Managers;
using DeskWarrior.Models;

namespace DeskWarrior.Helpers
{
    /// <summary>
    /// PermanentStats 확장 메서드
    /// 레벨 → 실제 효과 값 변환
    /// </summary>
    public static class PermanentStatsHelper
    {
        private static StatGrowthManager? _statGrowth;

        private static StatGrowthManager StatGrowth
        {
            get
            {
                _statGrowth ??= new StatGrowthManager();
                return _statGrowth;
            }
        }

        #region A. 기본 능력 (5종)

        /// <summary>
        /// 기본 공격력 (가산)
        /// </summary>
        public static int GetBaseAttackValue(this PermanentStats stats)
        {
            return (int)StatGrowth.GetPermanentStatEffect("base_attack", stats.BaseAttackLevel);
        }

        /// <summary>
        /// 공격력 배수 (%)
        /// </summary>
        public static double GetAttackPercentValue(this PermanentStats stats)
        {
            return StatGrowth.GetPermanentStatEffect("attack_percent", stats.AttackPercentLevel) / 100.0;
        }

        /// <summary>
        /// 크리티컬 확률 (%)
        /// </summary>
        public static double GetCritChanceValue(this PermanentStats stats)
        {
            return StatGrowth.GetPermanentStatEffect("crit_chance", stats.CritChanceLevel) / 100.0;
        }

        /// <summary>
        /// 크리티컬 배율 (추가 배율)
        /// </summary>
        public static double GetCritDamageValue(this PermanentStats stats)
        {
            return StatGrowth.GetPermanentStatEffect("crit_damage", stats.CritDamageLevel);
        }

        /// <summary>
        /// 멀티히트 확률 (%)
        /// </summary>
        public static double GetMultiHitValue(this PermanentStats stats)
        {
            return StatGrowth.GetPermanentStatEffect("multi_hit", stats.MultiHitLevel) / 100.0;
        }

        #endregion

        #region B. 재화 보너스 (4종)

        /// <summary>
        /// 영구 골드+ (가산)
        /// </summary>
        public static int GetGoldFlatPermValue(this PermanentStats stats)
        {
            return (int)StatGrowth.GetPermanentStatEffect("gold_flat_perm", stats.GoldFlatPermLevel);
        }

        /// <summary>
        /// 영구 골드* (%)
        /// </summary>
        public static double GetGoldMultiPermValue(this PermanentStats stats)
        {
            return StatGrowth.GetPermanentStatEffect("gold_multi_perm", stats.GoldMultiPermLevel) / 100.0;
        }

        /// <summary>
        /// 크리스탈+ (가산)
        /// </summary>
        public static int GetCrystalFlatValue(this PermanentStats stats)
        {
            return (int)StatGrowth.GetPermanentStatEffect("crystal_flat", stats.CrystalFlatLevel);
        }

        /// <summary>
        /// 크리스탈* (%)
        /// </summary>
        public static double GetCrystalMultiValue(this PermanentStats stats)
        {
            return StatGrowth.GetPermanentStatEffect("crystal_multi", stats.CrystalMultiLevel) / 100.0;
        }

        #endregion

        #region C. 유틸리티 (2종)

        /// <summary>
        /// 기본 시간 연장 (초)
        /// </summary>
        public static int GetTimeExtendValue(this PermanentStats stats)
        {
            return (int)StatGrowth.GetPermanentStatEffect("time_extend", stats.TimeExtendLevel);
        }

        /// <summary>
        /// 업그레이드 할인 (%)
        /// </summary>
        public static double GetUpgradeDiscountValue(this PermanentStats stats)
        {
            return StatGrowth.GetPermanentStatEffect("upgrade_discount", stats.UpgradeDiscountLevel) / 100.0;
        }

        #endregion

        #region D. 시작 보너스 (8종)

        /// <summary>
        /// 시작 레벨
        /// </summary>
        public static int GetStartLevelValue(this PermanentStats stats)
        {
            return (int)StatGrowth.GetPermanentStatEffect("start_level", stats.StartLevelLevel);
        }

        /// <summary>
        /// 시작 골드
        /// </summary>
        public static int GetStartGoldValue(this PermanentStats stats)
        {
            return (int)StatGrowth.GetPermanentStatEffect("start_gold", stats.StartGoldLevel);
        }

        /// <summary>
        /// 시작 키보드 (레벨)
        /// </summary>
        public static int GetStartKeyboardValue(this PermanentStats stats)
        {
            return (int)StatGrowth.GetPermanentStatEffect("start_keyboard", stats.StartKeyboardLevel);
        }

        /// <summary>
        /// 시작 마우스 (레벨)
        /// </summary>
        public static int GetStartMouseValue(this PermanentStats stats)
        {
            return (int)StatGrowth.GetPermanentStatEffect("start_mouse", stats.StartMouseLevel);
        }

        /// <summary>
        /// 시작 골드+ (레벨)
        /// </summary>
        public static int GetStartGoldFlatValue(this PermanentStats stats)
        {
            return (int)StatGrowth.GetPermanentStatEffect("start_gold_flat", stats.StartGoldFlatLevel);
        }

        /// <summary>
        /// 시작 골드* (레벨)
        /// </summary>
        public static int GetStartGoldMultiValue(this PermanentStats stats)
        {
            return (int)StatGrowth.GetPermanentStatEffect("start_gold_multi", stats.StartGoldMultiLevel);
        }

        /// <summary>
        /// 시작 콤보유연성 (레벨)
        /// </summary>
        public static int GetStartComboFlexValue(this PermanentStats stats)
        {
            return (int)StatGrowth.GetPermanentStatEffect("start_combo_flex", stats.StartComboFlexLevel);
        }

        /// <summary>
        /// 시작 콤보데미지 (레벨)
        /// </summary>
        public static int GetStartComboDamageValue(this PermanentStats stats)
        {
            return (int)StatGrowth.GetPermanentStatEffect("start_combo_damage", stats.StartComboDamageLevel);
        }

        #endregion
    }
}
