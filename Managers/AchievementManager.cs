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
        /// 비밀 업적 체크 (데이터 드리븐 - 시간/상태 기반)
        /// </summary>
        public void CheckSecretAchievements()
        {
            var secretAchievements = _definitions.Where(d => d.SpecialCondition != null);

            foreach (var def in secretAchievements)
            {
                if (IsAlreadyUnlocked(def.Id)) continue;

                if (EvaluateSpecialCondition(def.SpecialCondition!))
                {
                    _saveManager.UpdateAchievementProgress(def.Id, 1);
                    Unlock(def);
                }
            }
        }

        /// <summary>
        /// 전투 관련 비밀 업적 체크 (데이터 드리븐)
        /// </summary>
        public void CheckCombatSecretAchievements(int currentLevel, int damage, int monsterHp, bool isBoss, bool isCritical, int consecutiveCrits)
        {
            var context = new CombatContext
            {
                CurrentLevel = currentLevel,
                Damage = damage,
                MonsterHp = monsterHp,
                IsBoss = isBoss,
                IsCritical = isCritical,
                ConsecutiveCrits = consecutiveCrits
            };

            var combatAchievements = _definitions.Where(d =>
                d.SpecialCondition?.Type == "combat_event" && !IsAlreadyUnlocked(d.Id));

            foreach (var def in combatAchievements)
            {
                if (EvaluateCombatCondition(def.SpecialCondition!, context))
                {
                    _saveManager.UpdateAchievementProgress(def.Id, 1);
                    Unlock(def);
                }
            }
        }

        /// <summary>
        /// 세션 종료 시 비밀 업적 체크 (데이터 드리븐)
        /// </summary>
        public void CheckSessionEndSecretAchievements(int maxLevel, int timeoutCount, double sessionMinutes, bool keyboardOnly, bool mouseOnly)
        {
            var context = new SessionEndContext
            {
                MaxLevel = maxLevel,
                TimeoutCount = timeoutCount,
                SessionMinutes = sessionMinutes,
                KeyboardOnly = keyboardOnly,
                MouseOnly = mouseOnly
            };

            var sessionAchievements = _definitions.Where(d =>
                d.SpecialCondition?.Type == "session_stat" && !IsAlreadyUnlocked(d.Id));

            foreach (var def in sessionAchievements)
            {
                if (EvaluateSessionCondition(def.SpecialCondition!, context))
                {
                    _saveManager.UpdateAchievementProgress(def.Id, 1);
                    Unlock(def);
                }
            }
        }

        /// <summary>
        /// 특수 조건 평가 (데이터 드리븐)
        /// </summary>
        private bool EvaluateSpecialCondition(SpecialCondition condition)
        {
            var now = DateTime.Now;
            var lifetime = _saveManager.CurrentSave.LifetimeStats;
            var stats = _saveManager.CurrentSave.Stats;

            return condition.Type switch
            {
                "time_hour" => now.Hour == condition.GetInt("hour"),

                "day_of_week" => EvaluateDayOfWeek(condition, now),

                "ratio_between" => EvaluateRatioBetween(condition, lifetime),

                "multi_metric" => EvaluateMultiMetric(condition, lifetime),

                "days_since_last_play" => EvaluateDaysSinceLastPlay(condition, lifetime, stats),

                "metric_threshold" => EvaluateMetricThreshold(condition, lifetime),

                _ => false
            };
        }

        /// <summary>
        /// 요일 조건 평가
        /// </summary>
        private bool EvaluateDayOfWeek(SpecialCondition condition, DateTime now)
        {
            var days = condition.GetIntArray("days");
            int currentDay = (int)now.DayOfWeek;

            if (days.Length > 0 && !days.Contains(currentDay))
                return false;

            // 추가 조건: 최소 플레이 시간
            int minPlaytime = condition.GetInt("min_playtime_minutes", 0);
            if (minPlaytime > 0)
            {
                var lifetime = _saveManager.CurrentSave.LifetimeStats;
                if (lifetime.TotalPlaytimeMinutes < minPlaytime)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 비율 조건 평가 (예: 키보드:마우스 균형)
        /// </summary>
        private bool EvaluateRatioBetween(SpecialCondition condition, LifetimeStats lifetime)
        {
            string metricA = condition.GetString("metric_a");
            string metricB = condition.GetString("metric_b");
            double minRatio = condition.GetDouble("min_ratio", 0);
            double maxRatio = condition.GetDouble("max_ratio", 1);
            int minTotal = condition.GetInt("min_total", 0);

            long valueA = GetLifetimeMetricValue(metricA, lifetime);
            long valueB = GetLifetimeMetricValue(metricB, lifetime);
            long total = valueA + valueB;

            if (total < minTotal) return false;
            if (total == 0) return false;

            double ratio = (double)valueA / total;
            return ratio >= minRatio && ratio <= maxRatio;
        }

        /// <summary>
        /// 다중 메트릭 조건 평가
        /// </summary>
        private bool EvaluateMultiMetric(SpecialCondition condition, LifetimeStats lifetime)
        {
            string metric = condition.GetString("metric");
            int minValue = condition.GetInt("min_value", 0);

            long value = GetLifetimeMetricValue(metric, lifetime);
            return value >= minValue;
        }

        /// <summary>
        /// 미접속 일수 조건 평가
        /// </summary>
        private bool EvaluateDaysSinceLastPlay(SpecialCondition condition, LifetimeStats lifetime, UserStats stats)
        {
            int minDays = condition.GetInt("min_days", 0);
            int minLevel = condition.GetInt("min_level", 0);

            if (string.IsNullOrEmpty(lifetime.LastPlayDate))
                return false;

            if (!DateTime.TryParse(lifetime.LastPlayDate, out var lastPlay))
                return false;

            var daysSince = (DateTime.Now - lastPlay).TotalDays;
            return daysSince >= minDays && stats.MaxLevel >= minLevel;
        }

        /// <summary>
        /// 메트릭 임계값 조건 평가
        /// </summary>
        private bool EvaluateMetricThreshold(SpecialCondition condition, LifetimeStats lifetime)
        {
            string metric = condition.GetString("metric");
            int minValue = condition.GetInt("min_value", 0);

            long value = GetLifetimeMetricValue(metric, lifetime);
            return value >= minValue;
        }

        /// <summary>
        /// 전투 조건 평가 (데이터 드리븐)
        /// </summary>
        private bool EvaluateCombatCondition(SpecialCondition condition, CombatContext ctx)
        {
            string eventType = condition.GetString("event");

            return eventType switch
            {
                "exact_damage_at_level" =>
                    ctx.CurrentLevel == condition.GetInt("level") &&
                    ctx.Damage == condition.GetInt("damage"),

                "overkill" =>
                    ctx.MonsterHp > 0 &&
                    ctx.Damage >= ctx.MonsterHp * condition.GetInt("multiplier", 10),

                "boss_one_shot" =>
                    ctx.IsBoss && ctx.Damage >= ctx.MonsterHp,

                "consecutive_crits" =>
                    ctx.ConsecutiveCrits >= condition.GetInt("count", 10),

                _ => false
            };
        }

        /// <summary>
        /// 세션 종료 조건 평가 (데이터 드리븐)
        /// </summary>
        private bool EvaluateSessionCondition(SpecialCondition condition, SessionEndContext ctx)
        {
            string statType = condition.GetString("stat");

            return statType switch
            {
                "perfect_run" =>
                    ctx.TimeoutCount == 0 &&
                    ctx.MaxLevel >= condition.GetInt("min_level", 50),

                "speedrun" =>
                    ctx.SessionMinutes <= condition.GetDouble("max_minutes", 10) &&
                    ctx.MaxLevel >= condition.GetInt("min_level", 100),

                "input_only" =>
                    EvaluateInputOnly(condition, ctx),

                _ => false
            };
        }

        /// <summary>
        /// 입력 전용 조건 평가
        /// </summary>
        private bool EvaluateInputOnly(SpecialCondition condition, SessionEndContext ctx)
        {
            string inputType = condition.GetString("input_type");
            int minLevel = condition.GetInt("min_level", 50);

            if (ctx.MaxLevel < minLevel) return false;

            return inputType switch
            {
                "keyboard" => ctx.KeyboardOnly,
                "mouse" => ctx.MouseOnly,
                _ => false
            };
        }

        /// <summary>
        /// LifetimeStats에서 메트릭 값 가져오기
        /// </summary>
        private long GetLifetimeMetricValue(string metric, LifetimeStats lifetime)
        {
            return metric switch
            {
                "keyboard_inputs" => lifetime.KeyboardInputs,
                "mouse_inputs" => lifetime.MouseInputs,
                "total_sessions" => lifetime.TotalSessions,
                "total_playtime_minutes" => (long)lifetime.TotalPlaytimeMinutes,
                "bosses_defeated" => lifetime.BossesDefeated,
                "critical_hits" => lifetime.CriticalHits,
                _ => 0
            };
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
            // PermanentCurrency에서 크리스탈 정보 가져오기
            var currency = _saveManager.CurrentSave.PermanentCurrency;
            var permStats = _saveManager.CurrentSave.PermanentStats;

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
                "best_session_kills" => lifetime.BestSessionKills,
                "best_session_gold" => lifetime.BestSessionGold,
                "consecutive_days" => lifetime.ConsecutiveDays,

                // 크리스탈 관련
                "lifetime_crystals_earned" => currency.LifetimeCrystalsEarned,
                "lifetime_crystals_spent" => currency.LifetimeCrystalsSpent,

                // 콤보/멀티히트 관련
                "combo_triggers" => lifetime.ComboTriggers,
                "max_combo_stack_count" => lifetime.MaxComboStackCount,
                "multi_hits" => lifetime.MultiHits,

                // 영구 스탯 관련
                "total_perm_levels" => CalculateTotalPermLevels(permStats),
                "max_single_perm_level" => CalculateMaxSinglePermLevel(permStats),

                _ => 0
            };
        }

        /// <summary>
        /// 영구 스탯 총 레벨 계산 (19종 스탯)
        /// </summary>
        private long CalculateTotalPermLevels(PermanentStats permStats)
        {
            return permStats.BaseAttackLevel + permStats.AttackPercentLevel +
                   permStats.CritChanceLevel + permStats.CritDamageLevel + permStats.MultiHitLevel +
                   permStats.GoldFlatPermLevel + permStats.GoldMultiPermLevel +
                   permStats.CrystalFlatLevel + permStats.CrystalMultiLevel +
                   permStats.TimeExtendLevel + permStats.UpgradeDiscountLevel +
                   permStats.StartLevelLevel + permStats.StartGoldLevel +
                   permStats.StartKeyboardLevel + permStats.StartMouseLevel +
                   permStats.StartGoldFlatLevel + permStats.StartGoldMultiLevel +
                   permStats.StartComboFlexLevel + permStats.StartComboDamageLevel;
        }

        /// <summary>
        /// 영구 스탯 중 최대 레벨 계산
        /// </summary>
        private long CalculateMaxSinglePermLevel(PermanentStats permStats)
        {
            var levels = new int[]
            {
                permStats.BaseAttackLevel, permStats.AttackPercentLevel,
                permStats.CritChanceLevel, permStats.CritDamageLevel, permStats.MultiHitLevel,
                permStats.GoldFlatPermLevel, permStats.GoldMultiPermLevel,
                permStats.CrystalFlatLevel, permStats.CrystalMultiLevel,
                permStats.TimeExtendLevel, permStats.UpgradeDiscountLevel,
                permStats.StartLevelLevel, permStats.StartGoldLevel,
                permStats.StartKeyboardLevel, permStats.StartMouseLevel,
                permStats.StartGoldFlatLevel, permStats.StartGoldMultiLevel,
                permStats.StartComboFlexLevel, permStats.StartComboDamageLevel
            };
            return levels.Max();
        }

        #endregion
    }

    /// <summary>
    /// 전투 컨텍스트 (비밀 업적 평가용)
    /// </summary>
    public class CombatContext
    {
        public int CurrentLevel { get; set; }
        public int Damage { get; set; }
        public int MonsterHp { get; set; }
        public bool IsBoss { get; set; }
        public bool IsCritical { get; set; }
        public int ConsecutiveCrits { get; set; }
    }

    /// <summary>
    /// 세션 종료 컨텍스트 (비밀 업적 평가용)
    /// </summary>
    public class SessionEndContext
    {
        public int MaxLevel { get; set; }
        public int TimeoutCount { get; set; }
        public double SessionMinutes { get; set; }
        public bool KeyboardOnly { get; set; }
        public bool MouseOnly { get; set; }
    }
}
