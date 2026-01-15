using System;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 세션 통계 요약
    /// </summary>
    public class SessionStatsSummary
    {
        public double AverageLevel { get; set; }
        public double AverageDamage { get; set; }
        public double AverageGold { get; set; }
        public double AverageDurationMinutes { get; set; }
        public SessionStats? BestSession { get; set; }
        public int TotalSessions { get; set; }
    }
}
