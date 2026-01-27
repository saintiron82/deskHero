using System;
using System.Linq;
using DeskWarrior.Models;

namespace DeskWarrior.Managers.Repositories
{
    /// <summary>
    /// Achievements.json 저장소
    /// </summary>
    public class AchievementRepository : JsonFileRepository<UserAchievements>
    {
        public AchievementRepository(string filePath) : base(filePath)
        {
        }

        /// <summary>
        /// 저장 전 LastUpdated 갱신
        /// </summary>
        protected override void OnSaving(UserAchievements data)
        {
            data.LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// 업적 진행 상태 가져오기
        /// </summary>
        public AchievementProgress? GetProgress(UserAchievements achievements, string achievementId)
        {
            return achievements.Progress.FirstOrDefault(p => p.Id == achievementId);
        }

        /// <summary>
        /// 업적 해금
        /// </summary>
        public void UnlockAchievement(UserAchievements achievements, string achievementId)
        {
            var progress = GetProgress(achievements, achievementId);
            if (progress == null)
            {
                progress = new AchievementProgress { Id = achievementId };
                achievements.Progress.Add(progress);
            }

            if (!progress.IsUnlocked)
            {
                progress.IsUnlocked = true;
                progress.UnlockedAt = DateTime.Now;
            }
        }

        /// <summary>
        /// 업적 진행 업데이트
        /// </summary>
        public void UpdateProgress(UserAchievements achievements, string achievementId, long progressValue)
        {
            var existing = GetProgress(achievements, achievementId);
            if (existing == null)
            {
                existing = new AchievementProgress { Id = achievementId };
                achievements.Progress.Add(existing);
            }

            existing.CurrentProgress = progressValue;
        }

        /// <summary>
        /// 해금된 업적 수
        /// </summary>
        public int GetUnlockedCount(UserAchievements achievements)
        {
            return achievements.Progress.Count(p => p.IsUnlocked);
        }
    }
}
