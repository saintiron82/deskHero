using System.Collections.Generic;
using System.Linq;
using DeskWarrior.Models;

namespace DeskWarrior.Managers.Services
{
    /// <summary>
    /// 세션 통계 계산 서비스
    /// </summary>
    public class StatisticsCalculator
    {
        /// <summary>
        /// 세션 통계 요약 계산
        /// </summary>
        public SessionStatsSummary CalculateSummary(List<SessionStats> history)
        {
            if (history.Count == 0)
            {
                return new SessionStatsSummary();
            }

            var avgLevel = history.Average(s => s.MaxLevel);
            var avgDamage = history.Average(s => s.TotalDamage);
            var avgGold = history.Average(s => s.TotalGold);
            var avgDuration = history.Average(s => s.DurationMinutes);

            var bestSession = history.OrderByDescending(s => s.MaxLevel).First();

            return new SessionStatsSummary
            {
                AverageLevel = avgLevel,
                AverageDamage = avgDamage,
                AverageGold = avgGold,
                AverageDurationMinutes = avgDuration,
                BestSession = bestSession,
                TotalSessions = history.Count
            };
        }

        /// <summary>
        /// 통산 기록 업데이트
        /// </summary>
        public void UpdateLifetimeStats(LifetimeStats lifetime, SessionStats session)
        {
            lifetime.TotalGoldEarned += session.TotalGold;
            lifetime.KeyboardInputs += session.KeyboardInputs;
            lifetime.MouseInputs += session.MouseInputs;
            lifetime.TotalPlaytimeMinutes += session.DurationMinutes;
            lifetime.TotalSessions++;

            // 최고 기록 갱신
            if (session.MaxLevel > lifetime.BestSessionLevel)
            {
                lifetime.BestSessionLevel = session.MaxLevel;
            }
            if (session.TotalDamage > lifetime.BestSessionDamage)
            {
                lifetime.BestSessionDamage = session.TotalDamage;
            }
            if (session.MonstersKilled > lifetime.BestSessionKills)
            {
                lifetime.BestSessionKills = session.MonstersKilled;
            }
            if (session.TotalGold > lifetime.BestSessionGold)
            {
                lifetime.BestSessionGold = session.TotalGold;
            }
        }

        /// <summary>
        /// 최고 세션 기록 가져오기
        /// </summary>
        public SessionStats GetBestSessionStats(LifetimeStats lifetime)
        {
            return new SessionStats
            {
                MonstersKilled = lifetime.BestSessionKills,
                TotalDamage = lifetime.BestSessionDamage,
                MaxLevel = lifetime.BestSessionLevel,
                TotalGold = lifetime.BestSessionGold
            };
        }
    }
}
