using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 스탯 성장 시스템 관리자
    /// JSON 설정 로드 및 비용/효과 계산 담당
    /// </summary>
    public class StatGrowthManager
    {
        #region Fields

        private readonly Dictionary<string, StatGrowthConfig> _inGameStats = new();
        private readonly Dictionary<string, StatGrowthConfig> _permanentStats = new();

        #endregion

        #region Constructor

        public StatGrowthManager()
        {
            LoadConfigs();
        }

        #endregion

        #region Public Methods - In-Game Stats (Gold)

        /// <summary>
        /// 인게임 스탯 업그레이드 비용 계산 (골드)
        /// </summary>
        public int GetInGameUpgradeCost(string statId, int currentLevel, double? discountPercent = null)
        {
            if (!_inGameStats.TryGetValue(statId, out var config))
                return int.MaxValue;

            return config.CalculateCost(currentLevel + 1, discountPercent);
        }

        /// <summary>
        /// 인게임 스탯 효과 계산
        /// </summary>
        public double GetInGameStatEffect(string statId, int level)
        {
            if (!_inGameStats.TryGetValue(statId, out var config))
                return 0;

            return config.CalculateEffect(level);
        }

        /// <summary>
        /// 인게임 스탯 최대 레벨 체크
        /// </summary>
        public bool CanUpgradeInGameStat(string statId, int currentLevel)
        {
            if (!_inGameStats.TryGetValue(statId, out var config))
                return false;

            return config.MaxLevel == 0 || currentLevel < config.MaxLevel;
        }

        /// <summary>
        /// 인게임 스탯 설정 가져오기
        /// </summary>
        public StatGrowthConfig? GetInGameStatConfig(string statId)
        {
            return _inGameStats.TryGetValue(statId, out var config) ? config : null;
        }

        #endregion

        #region Public Methods - Permanent Stats (Crystal)

        /// <summary>
        /// 영구 스탯 업그레이드 비용 계산 (크리스탈)
        /// </summary>
        public int GetPermanentUpgradeCost(string statId, int currentLevel, double? discountPercent = null)
        {
            if (!_permanentStats.TryGetValue(statId, out var config))
                return int.MaxValue;

            return config.CalculateCost(currentLevel + 1, discountPercent);
        }

        /// <summary>
        /// 영구 스탯 효과 계산
        /// </summary>
        public double GetPermanentStatEffect(string statId, int level)
        {
            if (!_permanentStats.TryGetValue(statId, out var config))
                return 0;

            return config.CalculateEffect(level);
        }

        /// <summary>
        /// 영구 스탯 최대 레벨 체크
        /// </summary>
        public bool CanUpgradePermanentStat(string statId, int currentLevel)
        {
            if (!_permanentStats.TryGetValue(statId, out var config))
                return false;

            return config.MaxLevel == 0 || currentLevel < config.MaxLevel;
        }

        /// <summary>
        /// 영구 스탯 설정 가져오기
        /// </summary>
        public StatGrowthConfig? GetPermanentStatConfig(string statId)
        {
            return _permanentStats.TryGetValue(statId, out var config) ? config : null;
        }

        /// <summary>
        /// 영구 스탯 목록 (카테고리별 정렬)
        /// </summary>
        public IEnumerable<KeyValuePair<string, StatGrowthConfig>> GetPermanentStatsByCategory()
        {
            var sorted = new List<KeyValuePair<string, StatGrowthConfig>>(_permanentStats);

            // 카테고리 순서: base(1), currency(2), utility(3), starting(4)
            var categoryOrder = new Dictionary<string, int>
            {
                ["base"] = 1,
                ["currency"] = 2,
                ["utility"] = 3,
                ["starting"] = 4
            };

            sorted.Sort((a, b) =>
            {
                int orderA = a.Value.Category != null && categoryOrder.TryGetValue(a.Value.Category, out int o1) ? o1 : 999;
                int orderB = b.Value.Category != null && categoryOrder.TryGetValue(b.Value.Category, out int o2) ? o2 : 999;
                return orderA.CompareTo(orderB);
            });

            return sorted;
        }

        #endregion

        #region Private Methods

        private void LoadConfigs()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var inGamePath = Path.Combine(baseDir, "config", "InGameStatGrowth.json");
                var permanentPath = Path.Combine(baseDir, "config", "PermanentStats.json");

                // 인게임 스탯 로드
                if (File.Exists(inGamePath))
                {
                    var json = File.ReadAllText(inGamePath);
                    var root = JsonSerializer.Deserialize<StatGrowthConfigRoot>(json);
                    if (root?.Stats != null)
                    {
                        foreach (var kvp in root.Stats)
                        {
                            // "_category" 같은 주석 키는 무시
                            if (!kvp.Key.StartsWith("_"))
                            {
                                _inGameStats[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }

                // 영구 스탯 로드
                if (File.Exists(permanentPath))
                {
                    var json = File.ReadAllText(permanentPath);
                    var root = JsonSerializer.Deserialize<StatGrowthConfigRoot>(json);
                    if (root?.Stats != null)
                    {
                        foreach (var kvp in root.Stats)
                        {
                            // "_category" 같은 주석 키는 무시
                            if (!kvp.Key.StartsWith("_"))
                            {
                                _permanentStats[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DeskWarrior.Helpers.Logger.LogError("Failed to load stat growth configs", ex);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 시간 도둑 최대값 계산 (기본 시간까지만)
        /// </summary>
        public double CalculateTimeThiefCap(int baseTimeLimit)
        {
            return baseTimeLimit; // 최대 기본 시간만큼만 추가 가능
        }

        /// <summary>
        /// 콤보 허용 오차 계산
        /// </summary>
        public double CalculateComboFlexTolerance(int level)
        {
            const double BASE_TOLERANCE = 0.01; // 기본 ±0.01초
            return BASE_TOLERANCE + GetInGameStatEffect("combo_flex", level);
        }

        #endregion
    }
}
