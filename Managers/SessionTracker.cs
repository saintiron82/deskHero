using System;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 세션 통계 추적 클래스 (SRP: 세션 데이터 관리만 담당)
    /// </summary>
    public class SessionTracker
    {
        #region Properties

        /// <summary>
        /// 세션 시작 시간
        /// </summary>
        public DateTime StartTime { get; private set; } = DateTime.Now;

        /// <summary>
        /// 세션 중 누적 데미지
        /// </summary>
        public long TotalDamage { get; private set; }

        /// <summary>
        /// 세션 중 획득한 총 골드
        /// </summary>
        public long TotalGold { get; private set; }

        /// <summary>
        /// 세션 중 처치한 몬스터 수
        /// </summary>
        public int MonstersKilled { get; private set; }

        /// <summary>
        /// 세션 중 처치한 보스 수
        /// </summary>
        public int BossesKilled { get; private set; }

        /// <summary>
        /// 세션 중 키보드 입력 수
        /// </summary>
        public int KeyboardInputs { get; private set; }

        /// <summary>
        /// 세션 중 마우스 입력 수
        /// </summary>
        public int MouseInputs { get; private set; }

        /// <summary>
        /// 세션 중 크리티컬 히트 수
        /// </summary>
        public int CriticalHits { get; private set; }

        /// <summary>
        /// 세션 중 보스 드롭으로 획득한 크리스탈
        /// </summary>
        public int SessionBossDropCrystals { get; private set; }

        /// <summary>
        /// 세션 중 업적 보상으로 획득한 크리스탈
        /// </summary>
        public int SessionAchievementCrystals { get; private set; }

        /// <summary>
        /// 세션 경과 시간 (분)
        /// </summary>
        public double DurationMinutes => (DateTime.Now - StartTime).TotalMinutes;

        #endregion

        #region Public Methods

        /// <summary>
        /// 데미지 기록
        /// </summary>
        public void RecordDamage(int damage, bool isCritical, bool isMouse)
        {
            TotalDamage += damage;

            if (isCritical)
            {
                CriticalHits++;
            }

            if (isMouse)
            {
                MouseInputs++;
            }
            else
            {
                KeyboardInputs++;
            }
        }

        /// <summary>
        /// 몬스터 처치 기록
        /// </summary>
        public void RecordKill(bool isBoss, int goldReward)
        {
            MonstersKilled++;
            TotalGold += goldReward;

            if (isBoss)
            {
                BossesKilled++;
            }
        }

        /// <summary>
        /// 크리스탈 획득 기록 (보스 드롭)
        /// </summary>
        public void RecordBossDropCrystals(int amount)
        {
            SessionBossDropCrystals += amount;
        }

        /// <summary>
        /// 크리스탈 획득 기록 (업적 보상)
        /// </summary>
        public void RecordAchievementCrystals(int amount)
        {
            SessionAchievementCrystals += amount;
        }

        /// <summary>
        /// 세션 초기화
        /// </summary>
        public void Reset()
        {
            StartTime = DateTime.Now;
            TotalDamage = 0;
            TotalGold = 0;
            MonstersKilled = 0;
            BossesKilled = 0;
            KeyboardInputs = 0;
            MouseInputs = 0;
            CriticalHits = 0;
            SessionBossDropCrystals = 0;
            SessionAchievementCrystals = 0;
        }

        /// <summary>
        /// 현재 세션 데이터를 SessionStats로 변환
        /// </summary>
        public SessionStats ToSessionStats(int maxLevel, string endReason = "timeout")
        {
            return new SessionStats
            {
                StartTime = StartTime,
                EndTime = DateTime.Now,
                MaxLevel = maxLevel,
                TotalDamage = TotalDamage,
                TotalGold = (int)TotalGold,
                MonstersKilled = MonstersKilled,
                BossesKilled = BossesKilled,
                KeyboardInputs = KeyboardInputs,
                MouseInputs = MouseInputs,
                EndReason = endReason
            };
        }

        #endregion
    }
}
