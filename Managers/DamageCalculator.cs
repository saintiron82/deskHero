using System;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 데미지 계산 결과
    /// </summary>
    public struct DamageResult
    {
        public int Damage { get; init; }
        public bool IsCritical { get; init; }
        public bool IsMultiHit { get; init; }
        public bool IsCombo { get; init; }
        public int ComboStack { get; init; } // 0, 1, 2, 3
    }

    /// <summary>
    /// 데미지 계산 클래스 (SRP: 데미지 계산만 담당)
    /// </summary>
    public class DamageCalculator
    {
        #region Fields

        private readonly Random _random;
        private readonly double _criticalChance;
        private readonly double _criticalMultiplier;

        #endregion

        #region Constructor

        /// <summary>
        /// 데미지 계산기 생성
        /// </summary>
        /// <param name="criticalChance">크리티컬 확률 (0.0 ~ 1.0)</param>
        /// <param name="criticalMultiplier">크리티컬 데미지 배율</param>
        /// <param name="random">랜덤 인스턴스 (선택적, 테스트용)</param>
        public DamageCalculator(double criticalChance, double criticalMultiplier, Random? random = null)
        {
            _criticalChance = criticalChance;
            _criticalMultiplier = criticalMultiplier;
            _random = random ?? new Random();
        }

        /// <summary>
        /// GameData에서 설정을 로드하여 생성
        /// </summary>
        public DamageCalculator(GameData gameData, Random? random = null)
            : this(gameData.Balance.CriticalChance, gameData.Balance.CriticalMultiplier, random)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 데미지 계산
        /// </summary>
        /// <param name="basePower">기본 공격력</param>
        /// <returns>계산된 데미지와 크리티컬 여부</returns>
        public DamageResult Calculate(int basePower)
        {
            return Calculate(basePower, null);
        }

        /// <summary>
        /// 데미지 계산 (영구 스탯 적용)
        /// </summary>
        /// <param name="basePower">기본 공격력</param>
        /// <param name="permStats">영구 스탯 (null 가능)</param>
        /// <param name="comboDamageBonus">콤보 데미지 보너스 (0.0 ~ 1.0)</param>
        /// <param name="comboStack">콤보 스택 (0 = 없음, 1-3 = 스택)</param>
        /// <returns>계산된 데미지와 크리티컬 여부</returns>
        public DamageResult Calculate(int basePower, PermanentStats? permStats, double comboDamageBonus = 0, int comboStack = 0)
        {
            // ① 기본 = BasePower (keyboard/mouse_power)
            double effectivePower = basePower;

            // ② +가산 = 기본 + base_attack
            if (permStats != null)
            {
                effectivePower += permStats.BaseAttack;
            }

            // ③ ×배수 = ② × (1 + attack_percent)
            if (permStats != null)
            {
                effectivePower *= (1.0 + permStats.AttackPercentBonus);
            }

            // ④ ×크리티컬 = ③ × crit_damage (확률: crit_chance)
            double critChance = _criticalChance;
            double critMultiplier = _criticalMultiplier;

            if (permStats != null)
            {
                critChance += permStats.CriticalChanceBonus;
                critMultiplier += permStats.CriticalDamageBonus;
            }

            bool isCritical = _random.NextDouble() < critChance;
            if (isCritical)
            {
                effectivePower *= critMultiplier;
            }

            // ⑤ ×멀티히트 = ④ × 2 (확률: multi_hit)
            bool multiHit = permStats != null && _random.NextDouble() < permStats.MultiHitChance;
            if (multiHit)
            {
                effectivePower *= 2;
            }

            // ⑥ ×콤보 = ⑤ × (1 + combo_damage) (리듬 발동 시, 스택별 2/4/8배)
            bool isCombo = comboStack > 0;
            if (isCombo)
            {
                // 콤보 데미지 보너스 적용
                effectivePower *= (1.0 + comboDamageBonus);

                // 콤보 스택별 배율 (1=×2, 2=×4, 3=×8)
                double stackMultiplier = Math.Pow(2, comboStack);
                effectivePower *= stackMultiplier;
            }

            // 최종 데미지 = (int)⑥
            return new DamageResult
            {
                Damage = (int)effectivePower,
                IsCritical = isCritical,
                IsMultiHit = multiHit,
                IsCombo = isCombo,
                ComboStack = comboStack
            };
        }

        /// <summary>
        /// 크리티컬 확률 가져오기
        /// </summary>
        public double CriticalChance => _criticalChance;

        /// <summary>
        /// 크리티컬 배율 가져오기
        /// </summary>
        public double CriticalMultiplier => _criticalMultiplier;

        #endregion
    }
}
