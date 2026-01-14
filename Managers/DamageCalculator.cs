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
            bool isCritical = _random.NextDouble() < _criticalChance;
            double damage = basePower;

            if (isCritical)
            {
                damage *= _criticalMultiplier;
            }

            return new DamageResult
            {
                Damage = (int)damage,
                IsCritical = isCritical
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
