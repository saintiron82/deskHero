using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 영구 진행도 관리자
    /// </summary>
    public class PermanentProgressionManager
    {
        #region Fields

        private readonly SaveManager _saveManager;
        private readonly List<PermanentUpgradeDefinition> _upgradeDefinitions;
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
            _upgradeDefinitions = LoadUpgradeDefinitions();
            _bossDropConfig = LoadBossDropConfig();
        }

        #endregion

        #region Boss Drop

        /// <summary>
        /// 보스 처치 시 드롭 계산
        /// </summary>
        public BossDropResult ProcessBossKill(int bossLevel)
        {
            var save = _saveManager.CurrentSave;
            save.BossKillCounter++;

            // 피티 시스템 체크
            bool isGuaranteed = save.BossKillCounter >= _bossDropConfig.GuaranteedDropInterval;

            // 드롭 확률 계산
            double dropChance = _bossDropConfig.BaseDropChance +
                               (bossLevel * _bossDropConfig.DropChancePerLevel);
            dropChance = Math.Min(dropChance, _bossDropConfig.MaxDropChance);

            bool dropped = isGuaranteed || _random.NextDouble() < dropChance;

            if (!dropped)
            {
                return new BossDropResult { Dropped = false };
            }

            // 카운터 리셋
            save.BossKillCounter = 0;

            // 크리스탈 양 계산
            int baseCrystals = _bossDropConfig.BaseCrystalAmount +
                              (bossLevel * _bossDropConfig.CrystalPerLevel);

            // 분산 적용 (±20%)
            double variance = 1.0 + ((_random.NextDouble() * 2 - 1) * _bossDropConfig.CrystalVariance);
            int crystals = (int)(baseCrystals * variance);
            crystals = Math.Max(1, crystals);

            // 크리스탈 지급
            AddCrystals(crystals, "boss_drop");

            return new BossDropResult
            {
                Dropped = true,
                CrystalsDropped = crystals,
                WasGuaranteed = isGuaranteed
            };
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
            const int conversionRate = 1000; // 1000 골드 = 1 크리스탈
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
            var definition = _upgradeDefinitions.FirstOrDefault(u => u.Id == upgradeId);
            if (definition == null) return false;

            var save = _saveManager.CurrentSave;
            var progress = GetOrCreateProgress(upgradeId);

            // 최대 레벨 체크
            if (definition.MaxLevel > 0 && progress.CurrentLevel >= definition.MaxLevel)
                return false;

            // 비용 계산
            int cost = CalculateUpgradeCost(definition, progress.CurrentLevel);

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
            ApplyStatUpgrade(definition, progress.CurrentLevel);

            UpgradePurchased?.Invoke(this, new UpgradePurchasedEventArgs(upgradeId, progress.CurrentLevel));

            return true;
        }

        /// <summary>
        /// 업그레이드 비용 계산
        /// </summary>
        public int CalculateUpgradeCost(PermanentUpgradeDefinition def, int currentLevel)
        {
            return (int)(def.BaseCost * Math.Pow(def.CostMultiplier, currentLevel));
        }

        /// <summary>
        /// 업그레이드 정의 가져오기
        /// </summary>
        public PermanentUpgradeDefinition? GetUpgradeDefinition(string upgradeId)
        {
            return _upgradeDefinitions.FirstOrDefault(u => u.Id == upgradeId);
        }

        /// <summary>
        /// 모든 업그레이드 정의 가져오기
        /// </summary>
        public List<PermanentUpgradeDefinition> GetAllUpgradeDefinitions()
        {
            return _upgradeDefinitions;
        }

        /// <summary>
        /// 카테고리별 업그레이드 가져오기
        /// </summary>
        public List<PermanentUpgradeDefinition> GetUpgradesByCategory(string category)
        {
            return _upgradeDefinitions.Where(u => u.Category == category).ToList();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 스탯 업그레이드 적용
        /// </summary>
        private void ApplyStatUpgrade(PermanentUpgradeDefinition def, int newLevel)
        {
            var stats = _saveManager.CurrentSave.PermanentStats;
            var property = typeof(PermanentStats).GetProperty(def.StatName);

            if (property == null) return;

            double newValue = def.IncrementPerLevel * newLevel;

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
        /// 업그레이드 정의 로드
        /// </summary>
        private List<PermanentUpgradeDefinition> LoadUpgradeDefinitions()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "PermanentUpgrades.json");
                if (!File.Exists(configPath))
                {
                    return new List<PermanentUpgradeDefinition>();
                }

                var json = File.ReadAllText(configPath);
                var root = JsonSerializer.Deserialize<PermanentUpgradesRoot>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return root?.Upgrades ?? new List<PermanentUpgradeDefinition>();
            }
            catch
            {
                return new List<PermanentUpgradeDefinition>();
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
