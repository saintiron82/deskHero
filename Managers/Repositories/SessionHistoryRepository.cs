using System.Collections.Generic;
using System.Linq;
using DeskWarrior.Models;

namespace DeskWarrior.Managers.Repositories
{
    /// <summary>
    /// SessionHistory.json 저장소
    /// </summary>
    public class SessionHistoryRepository : JsonFileRepository<List<SessionStats>>
    {
        private const int MaxSessionHistory = 100;

        public SessionHistoryRepository(string filePath) : base(filePath)
        {
        }

        /// <summary>
        /// 세션 추가
        /// </summary>
        public void AddSession(List<SessionStats> history, SessionStats session)
        {
            history.Add(session);

            // 최대 개수 유지
            while (history.Count > MaxSessionHistory)
            {
                history.RemoveAt(0);
            }

            MarkDirty();
        }

        /// <summary>
        /// 최근 세션 가져오기
        /// </summary>
        public List<SessionStats> GetRecentSessions(List<SessionStats> history, int count = 10)
        {
            return history
                .OrderByDescending(s => s.StartTime)
                .Take(count)
                .ToList();
        }
    }
}
