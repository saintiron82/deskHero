using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DeskWarrior.Models;
using DeskWarrior.Helpers;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 영구 진행도 관리자
    /// </summary>
    public class PermanentProgressionManager
    {
        #region Fields

        private readonly SaveManager _saveManager;
        private readonly Dictionary<string, StatGrowthConfig> _statConfigs;
        private readonly BossDropConfig _bossDropConfig;
        private readonly Random _random = new();

        #endregion

        #region Events

        public event EventHandler<CrystalEarnedEventArgs>? CrystalEarned;
        public event EventHandler<UpgradePurchasedEventArgs>? UpgradePurchased;

        #endregion

        #region Constructor

        public PermanentProgressionManager(SaveManager saveManager)
        {
            _saveManager = saveManager;
            _statConfigs = LoadStatConfigs();
            _bossDropConfig = LoadBossDropConfig();
        }

        #endregion

        #region Boss Drop

        /// <summary>
        /// 보스 처치 시 크리스탈 지급 (100% 확정, 속성별 배율 적용)
        /// </summary>
        public BossDropResult ProcessBossKill(int bossLevel, string bossElement, Dictionary<string, double> crystalMultipliers)
        {
            var save = _saveManager.CurrentSave;
            var permStats = save.PermanentStats;

            // ✅ 기본 크리스탈 계산 (100% 지급)
            double growth;
            if (_bossDropConfig.CrystalGrowthBreakpoint > 0 && bossLevel > _bossDropConfig.CrystalGrowthBreakpoint)
            {
                double tail = Math.Pow(bossLevel - _bossDropConfig.CrystalGrowthBreakpoint, _bossDropConfig.CrystalGrowthExponent);
                growth = _bossDropConfig.CrystalGrowthBreakpoint + tail;
            }
            else
            {
                growth = bossLevel;
            }
            int baseCrystals = Helpers.SafeMath.ToInt(Math.Round(
                _bossDropConfig.BaseCrystalAmount +
                (_bossDropConfig.CrystalPerLevel * growth)
            ));

            // ✅ 영구 스탯 보너스 적용
            if (permStats != null)
            {
                baseCrystals += permStats.GetCrystalFlatBonus();
            }

            // ✅ 속성별 배율 적용
            if (!crystalMultipliers.TryGetValue(bossElement, out double elementMultiplier))
            {
                Logger.Log($"[Warning] Crystal multiplier not found for element '{bossElement}', defaulting to 1.0");
                elementMultiplier = 1.0;
            }
            int finalCrystals = Helpers.SafeMath.ToInt(baseCrystals * elementMultiplier);

            // ✅ 분산 제거 - 모든 보상은 고정값 (황금 고블린 제외)
            // 이전: variance ±20% 적용 (제거됨)
            finalCrystals = Math.Max(1, finalCrystals);

            // ✅ 크리스탈 지급
            AddCrystals(finalCrystals, "boss_kill");

            // ✅ 속성 보너스 계산 (UI 표시용)
            int elementBonus = (int)((elementMultiplier - 1.0) * baseCrystals);

            return new BossDropResult
            {
                Dropped = true,  // 항상 true
                CrystalsDropped = finalCrystals,
                ElementBonus = elementBonus,
                WasGuaranteed = false  // 더 이상 의미 없음
            };
        }

        /// <summary>
        /// 스테이지 클리어 시 크리스탈 보상 (초반 부스터)
        /// </summary>
        public void ProcessStageClear(int clearedStage)
        {
            // 매 스테이지 클리어 시 크리스탈 (100레벨마다 +1)
            int crystalAmount = _bossDropConfig.StageCompletionCrystal + (clearedStage / 100);
            AddCrystals(crystalAmount, "stage_clear");
        }

        #endregion

        #region Currency Management

        /// <summary>
        /// 크리스탈 추가
        /// </summary>
        public void AddCrystals(int amount, string source)
        {
            var currency = _saveManager.CurrentSave.PermanentCurrency;
            currency.Crystals += amount;
            currency.LifetimeCrystalsEarned += amount;

            CrystalEarned?.Invoke(this, new CrystalEarnedEventArgs(amount, source));
        }

        /// <summary>
        /// 골드를 크리스탈로 변환
        /// </summary>
        public int ConvertGoldToCrystals(int sessionGold)
        {
            int conversionRate = _bossDropConfig.GoldToCrystalRate; // config에서 로드 (기본값 100)
            int crystals = sessionGold / conversionRate;

            if (crystals > 0)
            {
                AddCrystals(crystals, "gold_conversion");
            }

            return crystals;
        }

        #endregion

        #region Upgrade Management

        /// <summary>
        /// 영구 업그레이드 구매
        /// </summary>
        public bool PurchaseUpgrade(string upgradeId)
        {
            if (!_statConfigs.TryGetValue(upgradeId, out var config))
                return false;

            var save = _saveManager.CurrentSave;
            var progress = GetOrCreateProgress(upgradeId);

            // 최대 레벨 체크
            if (config.MaxLevel > 0 && progress.CurrentLevel >= config.MaxLevel)
                return false;

            // 비용 계산
            int cost = config.CalculateCost(progress.CurrentLevel + 1);

            // 크리스탈 체크
            if (save.PermanentCurrency.Crystals < cost)
                return false;

            // 크리스탈 차감
            save.PermanentCurrency.Crystals -= cost;
            save.PermanentCurrency.LifetimeCrystalsSpent += cost;

            // 레벨 증가
            progress.CurrentLevel++;
            progress.TotalInvested += cost;

            // 스탯 업그레이드 적용
            ApplyStatUpgrade(upgradeId, config, progress.CurrentLevel);

            UpgradePurchased?.Invoke(this, new UpgradePurchasedEventArgs(upgradeId, progress.CurrentLevel));

            return true;
        }

        /// <summary>
        /// 업그레이드 비용 계산
        /// </summary>
        public int CalculateUpgradeCost(string upgradeId, int currentLevel)
        {
            if (!_statConfigs.TryGetValue(upgradeId, out var config))
                return int.MaxValue;
            return config.CalculateCost(currentLevel + 1);
        }

        /// <summary>
        /// 스탯 설정 가져오기
        /// </summary>
        public StatGrowthConfig? GetStatConfig(string upgradeId)
        {
            return _statConfigs.TryGetValue(upgradeId, out var config) ? config : null;
        }

        /// <summary>
        /// 모든 스탯 설정 가져오기
        /// </summary>
        public Dictionary<string, StatGrowthConfig> GetAllStatConfigs()
        {
            return _statConfigs;
        }

        /// <summary>
        /// 카테고리별 스탯 가져오기
        /// </summary>
        public Dictionary<string, StatGrowthConfig> GetStatsByCategory(string category)
        {
            return _statConfigs.Where(kvp => kvp.Value.Category == category)
                               .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 스탯 업그레이드 적용
        /// </summary>
        private void ApplyStatUpgrade(string upgradeId, StatGrowthConfig config, int newLevel)
        {
            var stats = _saveManager.CurrentSave.PermanentStats;
            var statName = !string.IsNullOrEmpty(config.StatName) ? config.StatName : upgradeId;
            var property = typeof(PermanentStats).GetProperty(statName);

            if (property == null) return;

            double newValue = config.EffectPerLevel * newLevel;

            if (property.PropertyType == typeof(int))
            {
                property.SetValue(stats, (int)newValue);
            }
            else if (property.PropertyType == typeof(double))
            {
                property.SetValue(stats, newValue);
            }
            else if (property.PropertyType == typeof(bool))
            {
                property.SetValue(stats, newLevel > 0);
            }
        }

        /// <summary>
        /// 업그레이드 진행 상태 가져오기 또는 생성
        /// </summary>
        private PermanentUpgradeProgress GetOrCreateProgress(string upgradeId)
        {
            var save = _saveManager.CurrentSave;
            var progress = save.PermanentUpgrades.FirstOrDefault(p => p.Id == upgradeId);

            if (progress == null)
            {
                progress = new PermanentUpgradeProgress { Id = upgradeId };
                save.PermanentUpgrades.Add(progress);
            }

            return progress;
        }

        /// <summary>
        /// 스탯 설정 로드
        /// </summary>
        private Dictionary<string, StatGrowthConfig> LoadStatConfigs()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "PermanentStats.json");
                if (!File.Exists(configPath))
                {
                    return new Dictionary<string, StatGrowthConfig>();
                }

                var json = File.ReadAllText(configPath);
                var root = JsonSerializer.Deserialize<StatGrowthConfigRoot>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return root?.Stats ?? new Dictionary<string, StatGrowthConfig>();
            }
            catch
            {
                return new Dictionary<string, StatGrowthConfig>();
            }
        }

        /// <summary>
        /// 보스 드롭 설정 로드
        /// </summary>
        private BossDropConfig LoadBossDropConfig()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "BossDrops.json");
                if (!File.Exists(configPath))
                {
                    return new BossDropConfig();
                }

                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<BossDropConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new BossDropConfig();
            }
            catch
            {
                return new BossDropConfig();
            }
        }

        #endregion
    }

    #region Event Args

    /// <summary>
    /// 크리스탈 획득 이벤트 인자
    /// </summary>
    public class CrystalEarnedEventArgs : EventArgs
    {
        public int Amount { get; }
        public string Source { get; }

        public CrystalEarnedEventArgs(int amount, string source)
        {
            Amount = amount;
            Source = source;
        }
    }

    /// <summary>
    /// 업그레이드 구매 이벤트 인자
    /// </summary>
    public class UpgradePurchasedEventArgs : EventArgs
    {
        public string UpgradeId { get; }
        public int NewLevel { get; }

        public UpgradePurchasedEventArgs(string upgradeId, int newLevel)
        {
            UpgradeId = upgradeId;
            NewLevel = newLevel;
        }
    }

    #endregion
}
