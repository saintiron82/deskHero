using System;
using System.Collections.Generic;
using System.IO;
using DeskWarrior.Interfaces;
using DeskWarrior.Managers.Repositories;
using DeskWarrior.Managers.Services;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 저장/로드 관리 Facade (Repository + Service 패턴 적용)
    /// </summary>
    public class SaveManager : ISaveManager
    {
        #region Fields

        private readonly string _saveDir;
        private readonly UserSaveRepository _userSaveRepo;
        private readonly SessionHistoryRepository _sessionHistoryRepo;
        private readonly AchievementRepository _achievementRepo;
        private readonly StatisticsCalculator _statsCalculator;
        private readonly DateTrackingService _dateTrackingService;

        private UserSave _currentSave;
        private List<SessionStats> _sessionHistory;
        private UserAchievements _userAchievements;

        #endregion

        #region Properties

        public UserSave CurrentSave => _currentSave;
        public List<SessionStats> SessionHistory => _sessionHistory;
        public UserAchievements UserAchievements => _userAchievements;

        #endregion

        #region Constructor

        public SaveManager()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _saveDir = Path.Combine(appData, "DeskWarrior");

            if (!Directory.Exists(_saveDir))
            {
                Directory.CreateDirectory(_saveDir);
            }

            // Initialize Repositories
            _userSaveRepo = new UserSaveRepository(Path.Combine(_saveDir, "UserSave.json"));
            _sessionHistoryRepo = new SessionHistoryRepository(Path.Combine(_saveDir, "SessionHistory.json"));
            _achievementRepo = new AchievementRepository(Path.Combine(_saveDir, "Achievements.json"));

            // Initialize Services
            _statsCalculator = new StatisticsCalculator();
            _dateTrackingService = new DateTrackingService();

            // Initialize data
            _currentSave = new UserSave();
            _sessionHistory = new List<SessionStats>();
            _userAchievements = new UserAchievements();
        }

        #endregion

        #region Core Methods

        public void Load()
        {
            _currentSave = _userSaveRepo.Load();
            _sessionHistory = _sessionHistoryRepo.Load();
            _userAchievements = _achievementRepo.Load();

            // 날짜 확인 및 연속 플레이 체크
            _dateTrackingService.CheckAndUpdateDate(_currentSave);

            // Dirty flag 초기화
            _userSaveRepo.ClearDirty();
            _sessionHistoryRepo.ClearDirty();
        }

        public void Save()
        {
            _userSaveRepo.Save(_currentSave);
            _sessionHistoryRepo.Save(_sessionHistory);
            _achievementRepo.ForceSave(_userAchievements); // 업적은 항상 저장
        }

        #endregion

        #region Position & Settings

        public void UpdateWindowPosition(double x, double y)
        {
            _currentSave.Position.X = x;
            _currentSave.Position.Y = y;
            _userSaveRepo.MarkDirty();
        }

        #endregion

        #region Stats Updates

        public void AddInput()
        {
            _currentSave.Stats.TotalInputs++;
            _currentSave.Stats.TodayInputs++;
            _userSaveRepo.MarkDirty();
        }

        public void UpdateMaxLevel(int level)
        {
            if (level > _currentSave.Stats.MaxLevel)
            {
                _currentSave.Stats.MaxLevel = level;
                _userSaveRepo.MarkDirty();
            }
        }

        public void UpdateUpgrades(int keyboardPower, int mousePower)
        {
            _currentSave.Upgrades.KeyboardPower = keyboardPower;
            _currentSave.Upgrades.MousePower = mousePower;
            _userSaveRepo.MarkDirty();
        }

        public (int keyboard, int mouse) GetUpgrades()
        {
            return (_currentSave.Upgrades.KeyboardPower, _currentSave.Upgrades.MousePower);
        }

        public void AddDamage(long damage)
        {
            _currentSave.Stats.TotalDamage += damage;

            if (damage > _currentSave.Stats.MaxDamage)
            {
                _currentSave.Stats.MaxDamage = damage;
            }

            UpdateHistory(damage, 0);
            _userSaveRepo.MarkDirty();
        }

        public void AddKill()
        {
            _currentSave.Stats.MonsterKills++;
            UpdateHistory(0, 1);
            _userSaveRepo.MarkDirty();
        }

        private void UpdateHistory(long damage, int kills)
        {
            var now = DateTime.Now;
            var history = _currentSave.Stats.History;

            if (history.Count == 0 ||
                history[history.Count - 1].TimeStamp.Hour != now.Hour ||
                (now - history[history.Count - 1].TimeStamp).TotalHours >= 1)
            {
                history.Add(new HourlyData { TimeStamp = now, Damage = damage, Kills = kills });
            }
            else
            {
                var currentBucket = history[history.Count - 1];
                currentBucket.Damage += damage;
                currentBucket.Kills += kills;
            }

            while (history.Count > 24)
            {
                history.RemoveAt(0);
            }

            _currentSave.Stats.LastUpdated = now;
        }

        public void AddCriticalHit()
        {
            _currentSave.LifetimeStats.CriticalHits++;
            _userSaveRepo.MarkDirty();
        }

        public void AddBossKill()
        {
            _currentSave.LifetimeStats.BossesDefeated++;
            _userSaveRepo.MarkDirty();
        }

        public void AddGoldSpent(int amount)
        {
            _currentSave.LifetimeStats.TotalGoldSpent += amount;
            _userSaveRepo.MarkDirty();
        }

        public void AddKeyboardInput()
        {
            _currentSave.Stats.TotalInputs++;
            _currentSave.Stats.TodayInputs++;
            _currentSave.LifetimeStats.KeyboardInputs++;
            _userSaveRepo.MarkDirty();
        }

        public void AddMouseInput()
        {
            _currentSave.Stats.TotalInputs++;
            _currentSave.Stats.TodayInputs++;
            _currentSave.LifetimeStats.MouseInputs++;
            _userSaveRepo.MarkDirty();
        }

        #endregion

        #region Session Management

        public void SaveSession(SessionStats session)
        {
            session.EndTime = DateTime.Now;

            _sessionHistoryRepo.AddSession(_sessionHistory, session);
            _statsCalculator.UpdateLifetimeStats(_currentSave.LifetimeStats, session);

            // 골드 → 크리스탈 변환 (1000:1 비율)
            int crystalsEarned = (int)(session.TotalGold / 1000);
            if (crystalsEarned > 0)
            {
                _currentSave.PermanentCurrency.Crystals += crystalsEarned;
                _currentSave.PermanentCurrency.LifetimeCrystalsEarned += crystalsEarned;
            }

            _userSaveRepo.MarkDirty();
            Save();
        }

        public SessionStatsSummary GetSessionSummary()
        {
            return _statsCalculator.CalculateSummary(_sessionHistory);
        }

        public List<SessionStats> GetRecentSessions(int count = 10)
        {
            return _sessionHistoryRepo.GetRecentSessions(_sessionHistory, count);
        }

        public SessionStats GetBestSessionStats()
        {
            return _statsCalculator.GetBestSessionStats(_currentSave.LifetimeStats);
        }

        #endregion

        #region Reset

        /// <summary>
        /// 모든 데이터를 초기화하고 처음부터 다시 시작
        /// </summary>
        public void ResetAllData()
        {
            // 새로운 기본 데이터로 초기화
            _currentSave = new UserSave();
            _sessionHistory = new List<SessionStats>();
            _userAchievements = new UserAchievements();

            // 저장
            Save();

            Helpers.Logger.Log("[SaveManager] All data has been reset");
        }

        #endregion

        #region Achievement Management

        public AchievementProgress? GetAchievementProgress(string achievementId)
        {
            return _achievementRepo.GetProgress(_userAchievements, achievementId);
        }

        public void UnlockAchievement(string achievementId)
        {
            _achievementRepo.UnlockAchievement(_userAchievements, achievementId);
        }

        public void UpdateAchievementProgress(string achievementId, long progress)
        {
            _achievementRepo.UpdateProgress(_userAchievements, achievementId, progress);
        }

        public int GetUnlockedAchievementCount()
        {
            return _achievementRepo.GetUnlockedCount(_userAchievements);
        }

        #endregion
    }

    /// <summary>
    /// 시간 범위 타입
    /// </summary>
    public enum TimeRangeType
    {
        Hour1,
        Hour24,
        Day7
    }

    /// <summary>
    /// 시간 범위별 통계
    /// </summary>
    public class TimeRangeStats
    {
        public TimeRangeType RangeType { get; set; }
        public long TotalDamage { get; set; }
        public int TotalKills { get; set; }
        public long TotalGold { get; set; }
        public int TotalInputs { get; set; }
        public int KeyboardInputs { get; set; }
        public int MouseInputs { get; set; }

        public long MaxSessionDamage { get; set; }
        public int MaxSessionKills { get; set; }
        public long MaxSessionGold { get; set; }
        public int MaxSessionInputs { get; set; }
    }

    /// <summary>
    /// 그래프 데이터 포인트
    /// </summary>
    public class GraphDataPoint
    {
        public string Label { get; set; } = "";
        public long Damage { get; set; }
        public int Kills { get; set; }
    }
}
