using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DeskWarrior.Managers;
using DeskWarrior.Models;

namespace DeskWarrior.ViewModels
{
    /// <summary>
    /// 영구 업그레이드 상점 ViewModel
    /// </summary>
    public class PermanentUpgradeShopViewModel : ViewModelBase
    {
        private readonly PermanentProgressionManager _progressionManager;
        private readonly SaveManager _saveManager;

        #region Properties

        private long _currentCrystals;
        public long CurrentCrystals
        {
            get => _currentCrystals;
            set => SetProperty(ref _currentCrystals, value);
        }

        private long _lifetimeEarned;
        public long LifetimeEarned
        {
            get => _lifetimeEarned;
            set => SetProperty(ref _lifetimeEarned, value);
        }

        private long _lifetimeSpent;
        public long LifetimeSpent
        {
            get => _lifetimeSpent;
            set => SetProperty(ref _lifetimeSpent, value);
        }

        public ObservableCollection<UpgradeCardViewModel> AllUpgrades { get; set; }

        #endregion

        #region Constructor

        public PermanentUpgradeShopViewModel(PermanentProgressionManager progressionManager, SaveManager saveManager)
        {
            _progressionManager = progressionManager;
            _saveManager = saveManager;
            AllUpgrades = new ObservableCollection<UpgradeCardViewModel>();

            LoadData();
        }

        #endregion

        #region Methods

        /// <summary>
        /// 데이터 로드 및 새로고침
        /// </summary>
        public void LoadData()
        {
            var currency = _saveManager.CurrentSave.PermanentCurrency;
            CurrentCrystals = currency.Crystals;
            LifetimeEarned = currency.LifetimeCrystalsEarned;
            LifetimeSpent = currency.LifetimeCrystalsSpent;

            AllUpgrades.Clear();

            var statConfigs = _progressionManager.GetAllStatConfigs();
            foreach (var kvp in statConfigs)
            {
                string id = kvp.Key;
                var config = kvp.Value;

                var progress = _saveManager.CurrentSave.PermanentUpgrades.FirstOrDefault(p => p.Id == id);
                int currentLevel = progress?.CurrentLevel ?? 0;

                // 현재 언어에 맞는 로컬라이제이션 사용
                string currentLang = LocalizationManager.Instance.CurrentLanguage;
                string fullName = GetLocalizedText(config.Localization, currentLang, l => l.Name, config.Name);

                // 설명문에서 {n}을 실제 값으로 치환
                string description = GetLocalizedText(config.Localization, currentLang, l => l.Description, config.Description);
                string formattedDescription = FormatDescription(id, config, currentLevel, description);

                var card = new UpgradeCardViewModel
                {
                    Id = id,
                    Icon = config.Icon,
                    Name = fullName,
                    ShortName = GenerateShortName(fullName),
                    Description = formattedDescription,
                    Category = GetCategoryDisplayName(config.Category ?? ""),
                    CategoryKey = config.Category ?? "",
                    CurrentLevel = currentLevel,
                    MaxLevel = config.MaxLevel,
                    IncrementPerLevel = config.EffectPerLevel,
                    IsMaxed = config.MaxLevel > 0 && currentLevel >= config.MaxLevel
                };

                // 현재 효과 계산
                card.CurrentEffect = FormatEffect(id, config, currentLevel);

                // 다음 레벨 효과 계산
                if (!card.IsMaxed)
                {
                    card.NextLevelEffect = FormatEffect(id, config, currentLevel + 1);
                    card.Cost = _progressionManager.CalculateUpgradeCost(id, currentLevel);
                    card.CanAfford = CurrentCrystals >= card.Cost;
                }

                AllUpgrades.Add(card);
            }
        }

        /// <summary>
        /// 업그레이드 구매 시도
        /// </summary>
        public bool TryPurchaseUpgrade(string upgradeId)
        {
            bool success = _progressionManager.PurchaseUpgrade(upgradeId);
            if (success)
            {
                LoadData(); // 데이터 새로고침
            }
            return success;
        }

        /// <summary>
        /// 설명문 포맷팅 ({n} 치환)
        /// </summary>
        private string FormatDescription(string id, StatGrowthConfig config, int level, string template)
        {
            // effect_per_level이 이미 실제 효과값을 가지고 있음
            double effectValue = config.EffectPerLevel * level;
            string formattedValue = $"{effectValue:F0}";

            // 소수점이 필요한 경우
            if (effectValue != Math.Floor(effectValue))
            {
                formattedValue = $"{effectValue:F1}";
            }

            return template.Replace("{n}", formattedValue);
        }

        /// <summary>
        /// 효과 포맷팅 - effect_per_level 기반
        /// </summary>
        private string FormatEffect(string id, StatGrowthConfig config, int level)
        {
            // effect_per_level이 이미 실제 효과값을 가지고 있음
            double value = config.EffectPerLevel * level;

            // 카테고리/ID 기반 포맷팅
            return id switch
            {
                // 퍼센트 스탯
                "attack_percent" or "crit_chance" or "multi_hit" or
                "gold_multi_perm" or "crystal_multi" or "upgrade_discount" or
                "start_gold_multi" or "start_combo_damage" => $"{value:F1}%",

                // 배율 스탯
                "crit_damage" => $"{value:F1}x",

                // 시간 스탯
                "time_extend" => $"+{value:F1}s",

                // 기본값 (정수 또는 소수)
                _ => value == Math.Floor(value) ? $"+{value:F0}" : $"+{value:F1}"
            };
        }

        /// <summary>
        /// 카테고리 표시 이름 가져오기
        /// </summary>
        private string GetCategoryDisplayName(string category)
        {
            var loc = LocalizationManager.Instance;
            return category switch
            {
                "base" => loc["ui.shop.categoryName.baseStats"],
                "currency" => loc["ui.shop.categoryName.currencyBonus"],
                "utility" => loc["ui.shop.categoryName.utility"],
                "starting" => loc["ui.shop.categoryName.startingBonus"],
                // Legacy 카테고리 (하위 호환)
                "base_stats" => loc["ui.shop.categoryName.baseStats"],
                "currency_bonus" => loc["ui.shop.categoryName.currencyBonus"],
                "starting_bonus" => loc["ui.shop.categoryName.startingBonus"],
                _ => category
            };
        }

        /// <summary>
        /// 현재 언어에 맞는 로컬라이즈된 텍스트 가져오기
        /// </summary>
        private string GetLocalizedText(
            Dictionary<string, LocalizedText>? localizations,
            string currentLang,
            Func<LocalizedText, string> selector,
            string fallback)
        {
            if (localizations == null) return fallback;

            // 현재 언어 우선
            if (localizations.ContainsKey(currentLang))
                return selector(localizations[currentLang]);

            // 영어 폴백
            if (localizations.ContainsKey("en-US"))
                return selector(localizations["en-US"]);

            // 한국어 폴백
            if (localizations.ContainsKey("ko-KR"))
                return selector(localizations["ko-KR"]);

            // 아무것도 없으면 기본값
            return fallback;
        }

        /// <summary>
        /// 짧은 이름 생성 (컴팩트 카드용)
        /// </summary>
        private string GenerateShortName(string fullName)
        {
            var loc = LocalizationManager.Instance;
            string shortNameKey = $"ui.shop.shortName.{fullName}";
            string shortName = loc[shortNameKey];

            // 키가 존재하면 약식 이름 반환, 아니면 원본 이름 반환
            return shortName != shortNameKey ? shortName : fullName;
        }

        #endregion
    }

    /// <summary>
    /// 업그레이드 카드 ViewModel
    /// </summary>
    public class UpgradeCardViewModel : ViewModelBase
    {
        private bool _canAfford;
        private bool _isMaxed;

        public string Id { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Name { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string CategoryKey { get; set; } = "";
        public int CurrentLevel { get; set; }
        public int MaxLevel { get; set; }
        public double IncrementPerLevel { get; set; }
        public string CurrentEffect { get; set; } = "";
        public string NextLevelEffect { get; set; } = "";
        public int Cost { get; set; }

        public bool CanAfford
        {
            get => _canAfford;
            set => SetProperty(ref _canAfford, value);
        }

        public bool IsMaxed
        {
            get => _isMaxed;
            set => SetProperty(ref _isMaxed, value);
        }

        public string LevelDisplay
        {
            get
            {
                var loc = LocalizationManager.Instance;
                if (MaxLevel > 0)
                    return loc.Format("ui.shop.levelFormat", CurrentLevel, MaxLevel);
                return loc.Format("ui.shop.levelUnlimited", CurrentLevel);
            }
        }

        public string ButtonText
        {
            get
            {
                if (IsMaxed)
                    return LocalizationManager.Instance["ui.common.max"];
                return $"💎 {Cost:N0}";
            }
        }
    }
}
