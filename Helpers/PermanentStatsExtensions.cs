using System;
using DeskWarrior.Models;
using DeskWarrior.Managers;

namespace DeskWarrior.Helpers
{
    /// <summary>
    /// PermanentStats 확장 메서드 - Config 기반 효과 계산
    /// 하드코딩 제거 및 게임-시뮬레이터 동기화
    /// Fail-fast 정책: Config 누락 시 즉시 예외 발생
    /// </summary>
    public static class PermanentStatsExtensions
    {
        private static StatGrowthManager? _manager;

        /// <summary>
        /// 초기화 (App 시작 시 호출 필수)
        /// </summary>
        public static void Initialize(StatGrowthManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Config에서 효과 값 가져오기 (Fail-fast)
        /// </summary>
        private static double GetRequiredEffect(string statId, int level)
        {
            if (_manager == null)
                throw new InvalidOperationException(
                    "PermanentStatsExtensions not initialized. Call Initialize() in App startup.");

            if (!_manager.HasStat(statId))
                throw new InvalidOperationException(
                    $"Missing stat config: '{statId}'. Check config/PermanentStats.json");

            return _manager.GetPermanentStatEffect(statId, level);
        }

        #region A. 기본 능력 (5종)

        /// <summary>
        /// 기본 공격력 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static int GetBaseAttack(this PermanentStats stats)
        {
            return (int)GetRequiredEffect("base_attack", stats.BaseAttackLevel);
        }

        /// <summary>
        /// 공격력 배수 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static double GetAttackPercentBonus(this PermanentStats stats)
        {
            return GetRequiredEffect("attack_percent", stats.AttackPercentLevel);
        }

        /// <summary>
        /// 크리티컬 확률 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static double GetCriticalChanceBonus(this PermanentStats stats)
        {
            return GetRequiredEffect("crit_chance", stats.CritChanceLevel);
        }

        /// <summary>
        /// 크리티컬 배율 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static double GetCriticalDamageBonus(this PermanentStats stats)
        {
            return GetRequiredEffect("crit_damage", stats.CritDamageLevel);
        }

        /// <summary>
        /// 멀티히트 확률 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static double GetMultiHitChance(this PermanentStats stats)
        {
            return GetRequiredEffect("multi_hit", stats.MultiHitLevel) / 100.0;
        }

        #endregion

        #region B. 재화 보너스 (4종)

        /// <summary>
        /// 영구 골드+ 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static int GetGoldFlatBonus(this PermanentStats stats)
        {
            return (int)GetRequiredEffect("gold_flat_perm", stats.GoldFlatPermLevel);
        }

        /// <summary>
        /// 영구 골드* 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static double GetGoldPercentBonus(this PermanentStats stats)
        {
            return GetRequiredEffect("gold_multi_perm", stats.GoldMultiPermLevel);
        }

        /// <summary>
        /// 크리스탈+ 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static int GetCrystalFlatBonus(this PermanentStats stats)
        {
            return (int)GetRequiredEffect("crystal_flat", stats.CrystalFlatLevel);
        }

        /// <summary>
        /// 크리스탈* 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static double GetCrystalDropChanceBonus(this PermanentStats stats)
        {
            return GetRequiredEffect("crystal_chance", stats.CrystalMultiLevel) / 100.0;
        }

        #endregion

        #region C. 유틸리티 (2종)

        /// <summary>
        /// 기본 시간 연장 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static double GetGameOverTimeExtension(this PermanentStats stats)
        {
            return GetRequiredEffect("time_extend", stats.TimeExtendLevel);
        }

        /// <summary>
        /// 업그레이드 할인 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static double GetUpgradeCostReduction(this PermanentStats stats)
        {
            return GetRequiredEffect("upgrade_discount", stats.UpgradeDiscountLevel) / 100.0;
        }

        #endregion

        #region D. 시작 보너스 (8종)

        /// <summary>
        /// 시작 레벨 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static int GetStartingLevelBonus(this PermanentStats stats)
        {
            return (int)GetRequiredEffect("start_level", stats.StartLevelLevel);
        }

        /// <summary>
        /// 시작 골드 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static int GetStartingGoldBonus(this PermanentStats stats)
        {
            return (int)GetRequiredEffect("start_gold", stats.StartGoldLevel);
        }

        /// <summary>
        /// 시작 키보드 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static int GetStartingKeyboardPower(this PermanentStats stats)
        {
            return (int)GetRequiredEffect("start_keyboard", stats.StartKeyboardLevel);
        }

        /// <summary>
        /// 시작 마우스 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static int GetStartingMousePower(this PermanentStats stats)
        {
            return (int)GetRequiredEffect("start_mouse", stats.StartMouseLevel);
        }

        /// <summary>
        /// 시작 골드+ 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static double GetStartingGoldFlat(this PermanentStats stats)
        {
            return GetRequiredEffect("start_gold_flat", stats.StartGoldFlatLevel);
        }

        /// <summary>
        /// 시작 골드* 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static double GetStartingGoldMulti(this PermanentStats stats)
        {
            return GetRequiredEffect("start_gold_multi", stats.StartGoldMultiLevel) / 100.0;
        }

        /// <summary>
        /// 시작 콤보유연성 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static double GetStartingComboFlex(this PermanentStats stats)
        {
            return GetRequiredEffect("start_combo_flex", stats.StartComboFlexLevel);
        }

        /// <summary>
        /// 시작 콤보데미지 효과 계산 (Config 기반, Fail-fast)
        /// </summary>
        public static double GetStartingComboDamage(this PermanentStats stats)
        {
            return GetRequiredEffect("start_combo_damage", stats.StartComboDamageLevel) / 100.0;
        }

        #endregion
    }
}
