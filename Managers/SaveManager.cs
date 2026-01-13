using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 저장/로드 관리 클래스
    /// </summary>
    public class SaveManager
    {
        #region Constants

        private const int MaxSessionHistory = 100;

        #endregion

        #region Fields

        private readonly string _savePath;
        private readonly string _sessionHistoryPath;
        private readonly string _achievementsPath;
        private readonly string _saveDir;
        private UserSave _currentSave;
        private List<SessionStats> _sessionHistory;
        private UserAchievements _userAchievements;

        // Dirty Flags
        private bool _isMainDirty = false;
        private bool _isHistoryDirty = false;


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

            _savePath = Path.Combine(_saveDir, "UserSave.json");
            _sessionHistoryPath = Path.Combine(_saveDir, "SessionHistory.json");
            _achievementsPath = Path.Combine(_saveDir, "Achievements.json");

            _currentSave = new UserSave();
            _sessionHistory = new List<SessionStats>();
            _userAchievements = new UserAchievements();
        }

        #endregion

        #region Public Methods

        public void Load()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // 메인 세이브 로드
            try
            {
                if (File.Exists(_savePath))
                {
                    var json = File.ReadAllText(_savePath);
                    _currentSave = JsonSerializer.Deserialize<UserSave>(json, options) ?? new UserSave();
                }
            }
            catch
            {
                _currentSave = new UserSave();
            }

            // 세션 히스토리 로드
            try
            {
                if (File.Exists(_sessionHistoryPath))
                {
                    var json = File.ReadAllText(_sessionHistoryPath);
                    _sessionHistory = JsonSerializer.Deserialize<List<SessionStats>>(json, options) ?? new List<SessionStats>();
                }
            }
            catch
            {
                _sessionHistory = new List<SessionStats>();
            }

            // 업적 진행 로드
            try
            {
                if (File.Exists(_achievementsPath))
                {
                    var json = File.ReadAllText(_achievementsPath);
                    _userAchievements = JsonSerializer.Deserialize<UserAchievements>(json, options) ?? new UserAchievements();
                }
            }
            catch
            {
                _userAchievements = new UserAchievements();
            }

            // 날짜 확인 - 다른 날이면 오늘 입력 수 초기화 및 연속 플레이 체크
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            if (_currentSave.Stats.LastPlayed != today)
            {
                _currentSave.Stats.TodayInputs = 0;
                _currentSave.Stats.LastPlayed = today;

                // 연속 플레이 체크
                UpdateConsecutiveDays(today);
            }

            // 초기화 후 Dirty flag 리셋
            _isMainDirty = false;
            _isHistoryDirty = false;
        }

        public void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            // 메인 세이브 저장 (변경된 경우만)
            if (_isMainDirty)
            {
                try
                {
                    var json = JsonSerializer.Serialize(_currentSave, options);
                    File.WriteAllText(_savePath, json);
                    _isMainDirty = false; // 저장 완료
                }
                catch
                {
                    // 저장 실패 시 무시
                }
            }

            // 세션 히스토리 저장 (변경된 경우만)
            if (_isHistoryDirty)
            {
                try
                {
                    var json = JsonSerializer.Serialize(_sessionHistory, options);
                    File.WriteAllText(_sessionHistoryPath, json);
                    _isHistoryDirty = false; // 저장 완료
                }
                catch
                {
                    // 저장 실패 시 무시
                }
            }

            // 업적 진행 저장 (항상 저장 - 외부 수정 감지 어려움)
            try
            {
                _userAchievements.LastUpdated = DateTime.Now;
                var json = JsonSerializer.Serialize(_userAchievements, options);
                File.WriteAllText(_achievementsPath, json);
            }
            catch
            {
                // 저장 실패 시 무시
            }
        }

        private void UpdateConsecutiveDays(string today)
        {
            var lastDate = _currentSave.LifetimeStats.LastPlayDate;
            if (string.IsNullOrEmpty(lastDate))
            {
                _currentSave.LifetimeStats.ConsecutiveDays = 1;
            }
            else
            {
                if (DateTime.TryParse(lastDate, out var lastDateTime) &&
                    DateTime.TryParse(today, out var todayDateTime))
                {
                    var diff = (todayDateTime - lastDateTime).Days;
                    if (diff == 1)
                    {
                        _currentSave.LifetimeStats.ConsecutiveDays++;
                    }
                    else if (diff > 1)
                    {
                        _currentSave.LifetimeStats.ConsecutiveDays = 1;
                    }
                }
            }
            _currentSave.LifetimeStats.LastPlayDate = today;
        }

        public void UpdateWindowPosition(double x, double y)
        {
            _currentSave.Position.X = x;
            _currentSave.Position.Y = y;
            _isMainDirty = true;
        }

        public void AddInput()
        {
            _currentSave.Stats.TotalInputs++;
            _currentSave.Stats.TodayInputs++;
            _isMainDirty = true;
        }

        public void UpdateMaxLevel(int level)
        {
            if (level > _currentSave.Stats.MaxLevel)
            {
                _currentSave.Stats.MaxLevel = level;
                _isMainDirty = true;
            }
        }

        public void UpdateUpgrades(int keyboardPower, int mousePower)
        {
            _currentSave.Upgrades.KeyboardPower = keyboardPower;
            _currentSave.Upgrades.MousePower = mousePower;
            _isMainDirty = true;
        }

        public (int keyboard, int mouse) GetUpgrades()
        {
            return (_currentSave.Upgrades.KeyboardPower, _currentSave.Upgrades.MousePower);
        }

        public void AddDamage(long damage)
        {
            // Total Damage
            _currentSave.Stats.TotalDamage += damage;

            // Max Damage
            if (damage > _currentSave.Stats.MaxDamage)
            {
                _currentSave.Stats.MaxDamage = damage;
            }

            // History (Sliding Window)
            UpdateHistory(damage, 0);
            _isMainDirty = true;
        }

        public void AddKill()
        {
            _currentSave.Stats.MonsterKills++;
            UpdateHistory(0, 1);
            _isMainDirty = true;
        }

        private void UpdateHistory(long damage, int kills)
        {
            var now = DateTime.Now;
            var history = _currentSave.Stats.History;

            // 히스토리가 비었거나, 마지막 기록이 다른 시간대(Hour)라면 새 버킷 생성
            if (history.Count == 0 || history[history.Count - 1].TimeStamp.Hour != now.Hour || (now - history[history.Count - 1].TimeStamp).TotalHours >= 1)
            {
                // 빈 버킷 채우기 (그래프가 끊기지 않게 하려면 필요하지만, 여기서는 단순화하여 새 시간대만 추가)
                // 만약 24시간이 넘게 지났다면 리셋하는게 맞을 수도 있지만, 일단은 단순 추가
                history.Add(new HourlyData { TimeStamp = now, Damage = damage, Kills = kills });
            }
            else
            {
                // 현재 시간대 버킷 업데이트
                var currentBucket = history[history.Count - 1];
                currentBucket.Damage += damage;
                currentBucket.Kills += kills;
                // TimeStamp는 업데이트 하지 않음 (시작 시간 기준)
            }

            // 24개 초과 시 오래된 데이터 제거
            while (history.Count > 24)
            {
                history.RemoveAt(0);
            }

            _currentSave.Stats.LastUpdated = now;
        }

        #endregion

        #region Session Management

        /// <summary>
        /// 세션 종료 시 기록 저장
        /// </summary>
        public void SaveSession(SessionStats session)
        {
            session.EndTime = DateTime.Now;

            // 세션 히스토리에 추가
            _sessionHistory.Add(session);

            // 최대 100개 유지
            while (_sessionHistory.Count > MaxSessionHistory)
            {
                _sessionHistory.RemoveAt(0);
            }

            // 통산 기록 업데이트
            UpdateLifetimeStats(session);

            _isHistoryDirty = true;
            _isMainDirty = true;
            Save();
        }

        /// <summary>
        /// 통산 기록 업데이트
        /// </summary>
        private void UpdateLifetimeStats(SessionStats session)
        {
            var lifetime = _currentSave.LifetimeStats;

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
            
            _isMainDirty = true;
        }

        /// <summary>
        /// 크리티컬 히트 기록 추가
        /// </summary>
        public void AddCriticalHit()
        {
            _currentSave.LifetimeStats.CriticalHits++;
            _isMainDirty = true;
        }

        /// <summary>
        /// 보스 처치 기록 추가
        /// </summary>
        public void AddBossKill()
        {
            _currentSave.LifetimeStats.BossesDefeated++;
            _isMainDirty = true;
        }

        /// <summary>
        /// 골드 사용 기록 추가
        /// </summary>
        public void AddGoldSpent(int amount)
        {
            _currentSave.LifetimeStats.TotalGoldSpent += amount;
            _isMainDirty = true;
        }

        /// <summary>
        /// 키보드/마우스 입력 구분 추가
        /// </summary>
        public void AddKeyboardInput()
        {
            _currentSave.Stats.TotalInputs++;
            _currentSave.Stats.TodayInputs++;
            _currentSave.LifetimeStats.KeyboardInputs++;
            _isMainDirty = true;
        }

        public void AddMouseInput()
        {
            _currentSave.Stats.TotalInputs++;
            _currentSave.Stats.TodayInputs++;
            _currentSave.LifetimeStats.MouseInputs++;
            _isMainDirty = true;
        }

        /// <summary>
        /// 세션 통계 계산
        /// </summary>
        public SessionStatsSummary GetSessionSummary()
        {
            if (_sessionHistory.Count == 0)
            {
                return new SessionStatsSummary();
            }

            var avgLevel = _sessionHistory.Average(s => s.MaxLevel);
            var avgDamage = _sessionHistory.Average(s => s.TotalDamage);
            var avgGold = _sessionHistory.Average(s => s.TotalGold);
            var avgDuration = _sessionHistory.Average(s => s.DurationMinutes);

            var bestSession = _sessionHistory.OrderByDescending(s => s.MaxLevel).First();

            return new SessionStatsSummary
            {
                AverageLevel = avgLevel,
                AverageDamage = avgDamage,
                AverageGold = avgGold,
                AverageDurationMinutes = avgDuration,
                BestSession = bestSession,
                TotalSessions = _sessionHistory.Count
            };
        }

        /// <summary>
        /// 최근 세션 목록 가져오기
        /// </summary>
        public List<SessionStats> GetRecentSessions(int count = 10)
        {
            return _sessionHistory
                .OrderByDescending(s => s.StartTime)
                .Take(count)
                .ToList();
        }

        #endregion

        #region Achievement Management

        /// <summary>
        /// 업적 진행 상태 가져오기
        /// </summary>
        public AchievementProgress? GetAchievementProgress(string achievementId)
        {
            return _userAchievements.Progress.FirstOrDefault(p => p.Id == achievementId);
        }

        /// <summary>
        /// 업적 해금
        /// </summary>
        public void UnlockAchievement(string achievementId)
        {
            var progress = GetAchievementProgress(achievementId);
            if (progress == null)
            {
                progress = new AchievementProgress { Id = achievementId };
                _userAchievements.Progress.Add(progress);
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
        public void UpdateAchievementProgress(string achievementId, long progress)
        {
            var existing = GetAchievementProgress(achievementId);
            if (existing == null)
            {
                existing = new AchievementProgress { Id = achievementId };
                _userAchievements.Progress.Add(existing);
            }

            existing.CurrentProgress = progress;
        }

        /// <summary>
        /// 해금된 업적 수
        /// </summary>
        public int GetUnlockedAchievementCount()
        {
            return _userAchievements.Progress.Count(p => p.IsUnlocked);
        }

        #endregion



        /// <summary>
        /// 전체 세션 중 각 항목별 최고 기록 반환
        /// </summary>
        public SessionStats GetBestSessionStats()
        {
            var lifetime = _currentSave.LifetimeStats;
            return new SessionStats
            {
                MonstersKilled = lifetime.BestSessionKills,
                TotalDamage = lifetime.BestSessionDamage,
                MaxLevel = lifetime.BestSessionLevel,
                TotalGold = lifetime.BestSessionGold
            };
        }


    }

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
