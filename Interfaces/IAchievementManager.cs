using System;
using System.Collections.Generic;
using DeskWarrior.Models;

namespace DeskWarrior.Interfaces
{
    /// <summary>
    /// 업적 해금 이벤트 인자
    /// </summary>
    public class AchievementUnlockedEventArgs : EventArgs
    {
        public AchievementDefinition Achievement { get; }

        public AchievementUnlockedEventArgs(AchievementDefinition achievement)
        {
            Achievement = achievement;
        }
    }

    /// <summary>
    /// 업적 관리자 인터페이스 (SOLID: DIP, ISP)
    /// </summary>
    public interface IAchievementManager
    {
        #region Events

        event EventHandler<AchievementUnlockedEventArgs>? AchievementUnlocked;

        #endregion

        #region Methods

        void LoadDefinitions();
        void CheckAllAchievements();
        void CheckAchievements(string metric);
        bool IsAlreadyUnlocked(string achievementId);
        List<AchievementDefinition> GetAllDefinitions();
        List<AchievementDefinition> GetByCategory(string category);
        double GetProgress(string achievementId);
        (int unlocked, int total) GetAchievementStats();
        List<(AchievementDefinition def, DateTime unlockedAt)> GetRecentlyUnlocked(int count = 5);

        #endregion
    }
}
