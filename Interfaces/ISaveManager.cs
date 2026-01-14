using System;
using System.Collections.Generic;
using DeskWarrior.Models;

namespace DeskWarrior.Interfaces
{
    /// <summary>
    /// 저장 관리자 인터페이스 (SOLID: DIP, ISP)
    /// </summary>
    public interface ISaveManager
    {
        #region Properties

        UserSave CurrentSave { get; }
        List<SessionStats> SessionHistory { get; }
        UserAchievements UserAchievements { get; }

        #endregion

        #region Core Methods

        void Load();
        void Save();

        #endregion

        #region Position & Settings

        void UpdateWindowPosition(double x, double y);

        #endregion

        #region Stats Updates

        void AddInput();
        void UpdateMaxLevel(int level);
        void UpdateUpgrades(int keyboardPower, int mousePower);
        (int keyboard, int mouse) GetUpgrades();
        void AddDamage(long damage);
        void AddKill();
        void AddCriticalHit();
        void AddBossKill();
        void AddGoldSpent(int amount);
        void AddKeyboardInput();
        void AddMouseInput();

        #endregion

        #region Session Management

        void SaveSession(SessionStats session);
        SessionStatsSummary GetSessionSummary();
        List<SessionStats> GetRecentSessions(int count = 10);
        SessionStats GetBestSessionStats();

        #endregion

        #region Achievement Management

        AchievementProgress? GetAchievementProgress(string achievementId);
        void UnlockAchievement(string achievementId);
        void UpdateAchievementProgress(string achievementId, long progress);
        int GetUnlockedAchievementCount();

        #endregion
    }
}
