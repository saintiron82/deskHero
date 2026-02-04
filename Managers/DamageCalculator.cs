using System;
using DeskWarrior.Models;
using DeskWarrior.Helpers;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 공격 타입 (저항 시스템용)
    /// </summary>
    public enum AttackType
    {
        Keyboard,
        Mouse
    }

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
        public bool IsResisted { get; init; } // NEW: 저항 적용 여부

        // 계산 과정 상세 (Damage Meter용)
        public int BasePower { get; init; }
        public int BaseAttackBonus { get; init; }
        public double AttackMultiplier { get; init; }
        public double CritMultiplier { get; init; }
        public double UtilityBonus { get; init; } // NEW: 유틸리티 스탯 보너스
        public double ResistanceModifier { get; init; } // NEW: 저항 배율
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
        private readonly StatGrowthManager? _statGrowth;

        #endregion

        #region Constructor

        /// <summary>
        /// 데미지 계산기 생성
        /// </summary>
        /// <param name="criticalChance">크리티컬 확률 (0.0 ~ 1.0)</param>
        /// <param name="criticalMultiplier">크리티컬 데미지 배율</param>
        /// <param name="random">랜덤 인스턴스 (선택적, 테스트용)</param>
        /// <param name="statGrowth">스탯 성장 매니저 (영구 스탯 데미지 보너스용)</param>
        public DamageCalculator(double criticalChance, double criticalMultiplier, Random? random = null, StatGrowthManager? statGrowth = null)
        {
            _criticalChance = criticalChance;
            _criticalMultiplier = criticalMultiplier;
            _random = random ?? new Random();
            _statGrowth = statGrowth;
        }

        /// <summary>
        /// GameData에서 설정을 로드하여 생성
        /// </summary>
        public DamageCalculator(GameData gameData, Random? random = null, StatGrowthManager? statGrowth = null)
            : this(gameData.Balance.CriticalChance, gameData.Balance.CriticalMultiplier, random, statGrowth)
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
        /// <param name="attackType">공격 타입 (키보드/마우스)</param>
        /// <param name="targetMonster">대상 몬스터 (저항 적용용)</param>
        /// <returns>계산된 데미지와 크리티컬 여부</returns>
        public DamageResult Calculate(int basePower, PermanentStats? permStats, double comboDamageBonus = 0, int comboStack = 0,
            AttackType attackType = AttackType.Keyboard, Monster? targetMonster = null)
        {
            // ① 기본 = BasePower (keyboard/mouse_power) - BaseAttack 분리
            // basePower에는 이미 BaseAttack이 포함되어 있으므로 분리
            int baseAttackBonus = 0;
            if (permStats != null)
            {
                baseAttackBonus = permStats.GetBaseAttack();
            }
            int pureBasePower = basePower - baseAttackBonus;
            double effectivePower = pureBasePower;

            // ② ×배수 = 기본 × (1 + attack_percent) - attack_percent는 pureBasePower에만 적용
            double attackMultiplier = 0;
            if (permStats != null)
            {
                attackMultiplier = permStats.GetAttackPercentBonus() / 100.0;
                effectivePower *= (1.0 + attackMultiplier);
            }

            // ③ +가산 = ② + base_attack - attack_percent 효과 제외
            effectivePower += baseAttackBonus;

            // ④ ×크리티컬 = ③ × crit_damage (확률: crit_chance)
            double critChance = _criticalChance;
            double critMultiplier = _criticalMultiplier;

            if (permStats != null)
            {
                critChance += permStats.GetCriticalChanceBonus() / 100.0;
                critMultiplier += permStats.GetCriticalDamageBonus();
            }

            bool isCritical = _random.NextDouble() < critChance;
            if (isCritical)
            {
                effectivePower *= critMultiplier;
            }

            // ⑤ ×멀티히트 = ④ × 2 (확률: multi_hit)
            bool multiHit = permStats != null && _random.NextDouble() < permStats.GetMultiHitChance();
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

            // ⑦ ×유틸리티 = ⑥ × utility_bonus
            double utilityBonus = 1.0;
            if (permStats != null && _statGrowth != null)
            {
                double totalDamageBonus = 0;

                // time_extend 데미지 보너스
                var timeExtendBonus = _statGrowth.GetDamageBonusEffect("time_extend", permStats.TimeExtendLevel);
                if (timeExtendBonus.HasValue)
                    totalDamageBonus += timeExtendBonus.Value;

                // upgrade_discount 데미지 보너스
                var upgradeDiscountBonus = _statGrowth.GetDamageBonusEffect("upgrade_discount", permStats.UpgradeDiscountLevel);
                if (upgradeDiscountBonus.HasValue)
                    totalDamageBonus += upgradeDiscountBonus.Value;

                utilityBonus = 1.0 + totalDamageBonus / 100.0;  // 퍼센트를 배율로 변환
                effectivePower *= utilityBonus;
            }

            // ⑧ ×저항 = ⑦ × resistance_modifier (속성별 저항)
            double resistanceModifier = 1.0;
            bool isResisted = false;
            if (targetMonster != null)
            {
                resistanceModifier = attackType switch
                {
                    AttackType.Keyboard => targetMonster.KeyboardResistance,
                    AttackType.Mouse => targetMonster.MouseResistance,
                    _ => 1.0
                };

                if (resistanceModifier < 1.0)
                {
                    isResisted = true;
                }

                effectivePower *= resistanceModifier;
            }

            // 최종 데미지 = (int)⑧
            return new DamageResult
            {
                Damage = (int)effectivePower,
                IsCritical = isCritical,
                IsMultiHit = multiHit,
                IsCombo = isCombo,
                ComboStack = comboStack,
                IsResisted = isResisted,
                BasePower = pureBasePower,
                BaseAttackBonus = baseAttackBonus,
                AttackMultiplier = attackMultiplier,
                CritMultiplier = critMultiplier,
                UtilityBonus = utilityBonus,
                ResistanceModifier = resistanceModifier
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
