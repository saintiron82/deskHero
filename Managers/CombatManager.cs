using System;
using System.Collections.Generic;
using System.Diagnostics;
using DeskWarrior.Helpers;
using DeskWarrior.Interfaces;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 전투 시스템 관리 (입력 처리, 데미지 계산, Rate Limiting)
    /// Extracted from GameManager for Single Responsibility Principle
    /// </summary>
    public class CombatManager
    {
        #region Fields

        private readonly GameData _gameData;
        private readonly DamageCalculator _damageCalculator;
        private readonly ComboTracker _comboTracker;
        private readonly ConsecutiveKeyTracker _consecutiveKeyTracker;
        private readonly StatGrowthManager _statGrowth;
        private readonly SessionTracker _sessionTracker;

        // Rate limiting (슬라이딩 윈도우 - 1초 내 입력 수 카운트)
        private readonly Queue<long> _rateLimitTicks = new();

        #endregion

        #region Events

        /// <summary>
        /// 데미지 처리 완료 이벤트 (DamageDealt와 동일한 시그니처)
        /// </summary>
        public event EventHandler<DamageEventArgs>? DamageDealt;

        /// <summary>
        /// 스탯 변경 이벤트
        /// </summary>
        public event EventHandler? StatsChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// CombatManager 생성자
        /// </summary>
        /// <param name="gameData">게임 설정 데이터</param>
        /// <param name="damageCalculator">데미지 계산기</param>
        /// <param name="comboTracker">콤보 추적기</param>
        /// <param name="consecutiveKeyTracker">연속 키 추적기</param>
        /// <param name="statGrowth">스탯 성장 매니저</param>
        /// <param name="sessionTracker">세션 추적기</param>
        public CombatManager(
            GameData gameData,
            DamageCalculator damageCalculator,
            ComboTracker comboTracker,
            ConsecutiveKeyTracker consecutiveKeyTracker,
            StatGrowthManager statGrowth,
            SessionTracker sessionTracker)
        {
            _gameData = gameData;
            _damageCalculator = damageCalculator;
            _comboTracker = comboTracker;
            _consecutiveKeyTracker = consecutiveKeyTracker;
            _statGrowth = statGrowth;
            _sessionTracker = sessionTracker;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 키보드 입력 처리
        /// </summary>
        /// <param name="monster">현재 몬스터</param>
        /// <param name="keyboardPower">키보드 공격력</param>
        /// <param name="permStats">영구 스탯</param>
        /// <param name="isGoldenGoblinActive">황금 고블린 활성화 여부</param>
        /// <param name="vkCode">가상 키 코드</param>
        /// <returns>처리된 데미지 결과 (null이면 입력 차단됨)</returns>
        public DamageResult? ProcessKeyboardInput(
            Monster? monster,
            int keyboardPower,
            PermanentStats? permStats,
            bool isGoldenGoblinActive,
            int vkCode = 0)
        {
            if (monster == null || !monster.IsAlive)
                return null;

            // 입력 속도 제한 (슬라이딩 윈도우 - 1초 내 입력 수 제한)
            if (!CheckRateLimit())
            {
                Logger.Log($"[BLOCKED] RateLimit: queue={_rateLimitTicks.Count}/{_gameData.ConsecutiveKeyPenalty.MaxCps}, vkCode={vkCode}");
                DamageDealt?.Invoke(this, new DamageEventArgs(0, false, false));
                return null;
            }

            // CPS 추적 (속도 제한 통과한 유효 입력만)
            _sessionTracker.RecordInput();

            // 콤보 처리
            int comboStack = _comboTracker.ProcessInput();

            // 연속 키 페널티 계산
            double penalty = _consecutiveKeyTracker.ProcessKeyboardInput(vkCode, _comboTracker.IsComboActive);

            var result = CalculateDamage(keyboardPower, permStats, comboStack, AttackType.Keyboard, penalty, monster);
            ApplyDamageToMonster(monster, result, isMouse: false, isGoldenGoblinActive);

            return result;
        }

        /// <summary>
        /// 마우스 입력 처리
        /// </summary>
        /// <param name="monster">현재 몬스터</param>
        /// <param name="mousePower">마우스 공격력</param>
        /// <param name="permStats">영구 스탯</param>
        /// <param name="isGoldenGoblinActive">황금 고블린 활성화 여부</param>
        /// <param name="button">마우스 버튼</param>
        /// <returns>처리된 데미지 결과 (null이면 입력 차단됨)</returns>
        public DamageResult? ProcessMouseInput(
            Monster? monster,
            int mousePower,
            PermanentStats? permStats,
            bool isGoldenGoblinActive,
            GameMouseButton button = GameMouseButton.None)
        {
            if (monster == null || !monster.IsAlive)
                return null;

            // 마우스 면제가 아닌 경우만 입력 속도 제한
            if (!_gameData.ConsecutiveKeyPenalty.MouseExempt)
            {
                if (!CheckRateLimit())
                {
                    DamageDealt?.Invoke(this, new DamageEventArgs(0, false, true));
                    return null;
                }
            }

            // CPS 추적 (속도 제한 통과한 유효 입력만)
            _sessionTracker.RecordInput();

            // 콤보 처리
            int comboStack = _comboTracker.ProcessInput();

            // 마우스는 연속 페널티 면제
            var result = CalculateDamage(mousePower, permStats, comboStack, AttackType.Mouse, 1.0, monster);
            ApplyDamageToMonster(monster, result, isMouse: true, isGoldenGoblinActive);

            return result;
        }

        /// <summary>
        /// Rate limit 초기화 (게임 시작 시)
        /// </summary>
        public void ResetRateLimit()
        {
            _rateLimitTicks.Clear();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Rate limiting 체크
        /// </summary>
        /// <returns>입력 허용 여부</returns>
        private bool CheckRateLimit()
        {
            int maxCps = _gameData.ConsecutiveKeyPenalty.MaxCps;
            if (maxCps <= 0)
                return true;

            long now = Stopwatch.GetTimestamp();
            long oneSecondAgo = now - Stopwatch.Frequency;

            // 1초 이전 입력 제거
            while (_rateLimitTicks.Count > 0 && _rateLimitTicks.Peek() < oneSecondAgo)
                _rateLimitTicks.Dequeue();

            // 최대 CPS 초과 체크
            if (_rateLimitTicks.Count >= maxCps)
                return false;

            _rateLimitTicks.Enqueue(now);
            return true;
        }

        /// <summary>
        /// 데미지 계산 (연속 키 페널티 적용 포함)
        /// </summary>
        private DamageResult CalculateDamage(
            int basePower,
            PermanentStats? permStats,
            int comboStack,
            AttackType attackType,
            double consecutivePenalty,
            Monster? targetMonster)
        {
            var result = _damageCalculator.Calculate(basePower, permStats, 0, comboStack, attackType, targetMonster);

            // 연속 키 페널티 적용
            if (consecutivePenalty < 1.0)
            {
                long originalDamage = result.Damage;
                long penalizedDamage = Helpers.SafeMath.ToLong(result.Damage * consecutivePenalty);

                Logger.Log($"[Damage] Original: {originalDamage}, Penalty: {consecutivePenalty:F2}, Final: {penalizedDamage}");

                result = new DamageResult
                {
                    Damage = penalizedDamage,
                    IsCritical = result.IsCritical,
                    IsMultiHit = result.IsMultiHit,
                    IsCombo = result.IsCombo,
                    ComboStack = result.ComboStack,
                    IsResisted = result.IsResisted,
                    BasePower = result.BasePower,
                    BaseAttackBonus = result.BaseAttackBonus,
                    AttackMultiplier = result.AttackMultiplier,
                    CritMultiplier = result.CritMultiplier,
                    UtilityBonus = result.UtilityBonus,
                    ResistanceModifier = result.ResistanceModifier * consecutivePenalty
                };
            }

            return result;
        }

        /// <summary>
        /// 몬스터에 데미지 적용
        /// </summary>
        private void ApplyDamageToMonster(Monster monster, DamageResult result, bool isMouse, bool isGoldenGoblinActive)
        {
            // 황금 고블린은 모든 업그레이드 무시, 1 데미지 고정
            long actualDamage = isGoldenGoblinActive ? 1 : result.Damage;
            monster.TakeDamage(actualDamage);

            // 상세 데미지 기록 생성
            var record = new DamageRecord
            {
                BasePower = isGoldenGoblinActive ? 1 : result.BasePower,
                BaseAttackBonus = isGoldenGoblinActive ? 0 : result.BaseAttackBonus,
                AttackMultiplier = isGoldenGoblinActive ? 0 : result.AttackMultiplier,
                IsCritical = isGoldenGoblinActive ? false : result.IsCritical,
                CritMultiplier = isGoldenGoblinActive ? 1 : result.CritMultiplier,
                IsMultiHit = isGoldenGoblinActive ? false : result.IsMultiHit,
                IsCombo = isGoldenGoblinActive ? false : result.IsCombo,
                ComboStack = isGoldenGoblinActive ? 0 : result.ComboStack,
                FinalDamage = actualDamage,
                IsMouse = isMouse
            };

            // 세션 트래커에 상세 기록
            _sessionTracker.RecordDamageDetailed(record);

            // 데미지 이벤트 발생
            DamageDealt?.Invoke(this, new DamageEventArgs(actualDamage, result.IsCritical && !isGoldenGoblinActive, isMouse));

            StatsChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
