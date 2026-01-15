using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DeskWarrior.Interfaces;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 업적 관리 클래스
    /// </summary>
    public class AchievementManager : IAchievementManager
    {
        #region Fields

        private readonly SaveManager _saveManager;
        private List<AchievementDefinition> _definitions;
        private readonly string _configPath;
        private PermanentProgressionManager? _permanentProgression;

        #endregion

        #region Events

        public event EventHandler<AchievementUnlockedEventArgs>? AchievementUnlocked;

        #endregion

        #region Constructor

        public AchievementManager(SaveManager saveManager)
        {
            _saveManager = saveManager;
            _definitions = new List<AchievementDefinition>();
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "Achievements.json");

            LoadDefinitions();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// PermanentProgressionManager 초기화
        /// </summary>
        public void Initialize(PermanentProgressionManager permanentProgression)
        {
            _permanentProgression = permanentProgression;
        }

        /// <summary>
        /// 업적 정의 로드
        /// </summary>
        public void LoadDefinitions()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<AchievementsConfig>(json);
                    _definitions = config?.Achievements ?? new List<AchievementDefinition>();
                }
            }
            catch
            {
                _definitions = new List<AchievementDefinition>();
            }
        }

        /// <summary>
        /// 모든 업적 체크
        /// </summary>
        public void CheckAllAchievements()
        {
            var stats = _saveManager.CurrentSave.Stats;
            var lifetime = _saveManager.CurrentSave.LifetimeStats;

            foreach (var def in _definitions)
            {
                if (IsAlreadyUnlocked(def.Id)) continue;

                long currentValue = GetMetricValue(def.Metric, stats, lifetime);

                // 진행 상황 업데이트
                _saveManager.UpdateAchievementProgress(def.Id, currentValue);

                // 목표 달성 체크
                if (currentValue >= def.Target)
                {
                    Unlock(def);
                }
            }
        }

        /// <summary>
        /// 특정 메트릭 업적만 체크
        /// </summary>
        public void CheckAchievements(string metric)
        {
            var stats = _saveManager.CurrentSave.Stats;
            var lifetime = _saveManager.CurrentSave.LifetimeStats;

            var relevantAchievements = _definitions.Where(d => d.Metric == metric);

            foreach (var def in relevantAchievements)
            {
                if (IsAlreadyUnlocked(def.Id)) continue;

                long currentValue = GetMetricValue(def.Metric, stats, lifetime);

                _saveManager.UpdateAchievementProgress(def.Id, currentValue);

                if (currentValue >= def.Target)
                {
                    Unlock(def);
                }
            }
        }

        /// <summary>
        /// 업적 해금
        /// </summary>
        private void Unlock(AchievementDefinition def)
        {
            _saveManager.UnlockAchievement(def.Id);

            // 크리스탈 보상 지급
            if (def.CrystalReward > 0 && _permanentProgression != null)
            {
                _permanentProgression.AddCrystals(def.CrystalReward, $"achievement:{def.Id}");
            }

            // 이벤트 발생
            AchievementUnlocked?.Invoke(this, new AchievementUnlockedEventArgs(def));
        }

        /// <summary>
        /// 이미 해금된 업적인지 확인
        /// </summary>
        public bool IsAlreadyUnlocked(string achievementId)
        {
            var progress = _saveManager.GetAchievementProgress(achievementId);
            return progress?.IsUnlocked ?? false;
        }

        /// <summary>
        /// 모든 업적 정의 가져오기
        /// </summary>
        public List<AchievementDefinition> GetAllDefinitions()
        {
            return _definitions;
        }

        /// <summary>
        /// 카테고리별 업적 가져오기
        /// </summary>
        public List<AchievementDefinition> GetByCategory(string category)
        {
            return _definitions.Where(d => d.Category == category).ToList();
        }

        /// <summary>
        /// 업적 진행률 가져오기 (0.0 ~ 1.0)
        /// </summary>
        public double GetProgress(string achievementId)
        {
            var def = _definitions.FirstOrDefault(d => d.Id == achievementId);
            if (def == null) return 0;

            var progress = _saveManager.GetAchievementProgress(achievementId);
            if (progress == null) return 0;

            if (progress.IsUnlocked) return 1.0;

            return Math.Min(1.0, (double)progress.CurrentProgress / def.Target);
        }

        /// <summary>
        /// 업적 통계
        /// </summary>
        public (int unlocked, int total) GetAchievementStats()
        {
            int unlocked = _saveManager.GetUnlockedAchievementCount();
            int total = _definitions.Count;
            return (unlocked, total);
        }

        /// <summary>
        /// 최근 해금된 업적 가져오기
        /// </summary>
        public List<(AchievementDefinition def, DateTime unlockedAt)> GetRecentlyUnlocked(int count = 5)
        {
            var result = new List<(AchievementDefinition, DateTime)>();

            foreach (var def in _definitions)
            {
                var progress = _saveManager.GetAchievementProgress(def.Id);
                if (progress?.IsUnlocked == true && progress.UnlockedAt.HasValue)
                {
                    result.Add((def, progress.UnlockedAt.Value));
                }
            }

            return result
                .OrderByDescending(x => x.Item2)
                .Take(count)
                .ToList();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 메트릭에 해당하는 현재 값 가져오기
        /// </summary>
        private long GetMetricValue(string metric, UserStats stats, LifetimeStats lifetime)
        {
            return metric switch
            {
                // 기본 통계
                "total_damage" => stats.TotalDamage,
                "max_damage" => stats.MaxDamage,
                "max_level" => stats.MaxLevel,
                "monster_kills" => stats.MonsterKills,
                "total_inputs" => stats.TotalInputs,

                // 통산 통계
                "total_gold_earned" => lifetime.TotalGoldEarned,
                "total_gold_spent" => lifetime.TotalGoldSpent,
                "bosses_defeated" => lifetime.BossesDefeated,
                "critical_hits" => lifetime.CriticalHits,
                "keyboard_inputs" => lifetime.KeyboardInputs,
                "mouse_inputs" => lifetime.MouseInputs,
                "total_playtime_minutes" => (long)lifetime.TotalPlaytimeMinutes,
                "total_sessions" => lifetime.TotalSessions,
                "best_session_level" => lifetime.BestSessionLevel,
                "best_session_damage" => lifetime.BestSessionDamage,
                "consecutive_days" => lifetime.ConsecutiveDays,

                _ => 0
            };
        }

        #endregion
    }
}
