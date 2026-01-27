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

            var definitions = _progressionManager.GetAllUpgradeDefinitions();
            foreach (var def in definitions)
            {
                var progress = _saveManager.CurrentSave.PermanentUpgrades.FirstOrDefault(p => p.Id == def.Id);
                int currentLevel = progress?.CurrentLevel ?? 0;

                // 현재 언어에 맞는 로컬라이제이션 사용
                string currentLang = LocalizationManager.Instance.CurrentLanguage;
                string fullName = GetLocalizedText(def.Localization, currentLang, l => l.Name, def.Id);

                // 설명문에서 {value}를 실제 값으로 치환
                string description = GetLocalizedText(def.Localization, currentLang, l => l.Description, "");
                string formattedDescription = FormatDescription(def, currentLevel, description);

                var card = new UpgradeCardViewModel
                {
                    Id = def.Id,
                    Icon = def.Icon,
                    Name = fullName,
                    ShortName = GenerateShortName(fullName),
                    Description = formattedDescription,
                    Category = GetCategoryDisplayName(def.Category),
                    CategoryKey = def.Category,
                    CurrentLevel = currentLevel,
                    MaxLevel = def.MaxLevel,
                    IncrementPerLevel = def.IncrementPerLevel,
                    IsMaxed = def.MaxLevel > 0 && currentLevel >= def.MaxLevel
                };

                // 현재 효과 계산
                card.CurrentEffect = FormatEffect(def, currentLevel);

                // 다음 레벨 효과 계산
                if (!card.IsMaxed)
                {
                    card.NextLevelEffect = FormatEffect(def, currentLevel + 1);
                    card.Cost = _progressionManager.CalculateUpgradeCost(def, currentLevel);
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
        /// 설명문 포맷팅 ({value} 치환)
        /// </summary>
        private string FormatDescription(PermanentUpgradeDefinition def, int level, string template)
        {
            double rawValue = def.IncrementPerLevel * level;
            string formattedValue = "";

            // ID 기반 값 포맷팅
            formattedValue = def.Id switch
            {
                // A. 기본 능력
                "base_attack" => $"{rawValue:F0}",
                "attack_percent" => $"{(rawValue * 5):F0}",
                "crit_chance" => $"{rawValue:F0}",
                "crit_damage" => $"{(rawValue * 0.1):F1}",
                "multi_hit" => $"{rawValue:F0}",

                // B. 재화 보너스
                "gold_flat_perm" => $"{rawValue:F0}",
                "gold_multi_perm" => $"{(rawValue * 3):F0}",
                "crystal_flat" => $"{rawValue:F0}",
                "crystal_multi" => $"{(rawValue * 5):F0}",

                // C. 유틸리티
                "time_extend" => $"{(rawValue * 5):F0}",
                "upgrade_discount" => $"{(rawValue * 2):F0}",

                // D. 시작 보너스
                "start_level" => $"{rawValue:F0}",
                "start_gold" => $"{(rawValue * 50):F0}",
                "start_keyboard" => $"{rawValue:F0}",
                "start_mouse" => $"{rawValue:F0}",
                "start_gold_flat" => $"{rawValue:F0}",
                "start_gold_multi" => $"{(rawValue * 2):F0}",
                "start_combo_flex" => $"{rawValue:F0}",
                "start_combo_damage" => $"{rawValue:F0}",

                _ => $"{rawValue:F0}"
            };

            return template.Replace("{value}", formattedValue);
        }

        /// <summary>
        /// 효과 포맷팅 (19종 스탯 대응)
        /// </summary>
        private string FormatEffect(PermanentUpgradeDefinition def, int level)
        {
            double value = def.IncrementPerLevel * level;
            var loc = LocalizationManager.Instance;

            // ID 기반 개별 포맷팅
            return def.Id switch
            {
                // A. 기본 능력 (5종)
                "base_attack" => $"+{value:F0}",
                "attack_percent" => $"{(value * 5):F1}%",        // 레벨당 5%
                "crit_chance" => $"{value:F1}%",                 // 레벨당 1%
                "crit_damage" => $"{(value * 0.1):F1}x",         // 레벨당 0.1x
                "multi_hit" => $"{value:F1}%",                   // 레벨당 1%

                // B. 재화 보너스 (4종)
                "gold_flat_perm" => $"+{value:F0}",
                "gold_multi_perm" => $"{(value * 3):F1}%",       // 레벨당 3%
                "crystal_flat" => $"+{value:F0}",
                "crystal_multi" => $"{(value * 5):F1}%",         // 레벨당 5%

                // C. 유틸리티 (2종)
                "time_extend" => loc.Format("ui.shop.effectUnit.seconds", $"{(value * 5):F0}"),  // 레벨당 5초
                "upgrade_discount" => $"{(value * 2):F0}%",      // 레벨당 2%

                // D. 시작 보너스 (8종)
                "start_level" => loc.Format("ui.shop.effectUnit.level", $"{value:F0}"),
                "start_gold" => loc.Format("ui.shop.effectUnit.gold", $"{(value * 50):F0}"),  // 레벨당 50G
                "start_keyboard" => $"{value:F0}",
                "start_mouse" => $"{value:F0}",
                "start_gold_flat" => $"+{value:F0}",
                "start_gold_multi" => $"{(value * 2):F1}%",      // 레벨당 2%
                "start_combo_flex" => $"{value:F0}",
                "start_combo_damage" => $"{value:F0}",

                // 기본값
                _ => $"+{value:F0}"
            };
        }

        /// <summary>
        /// 카테고리 표시 이름 가져오기 (19종 스탯 카테고리)
        /// </summary>
        private string GetCategoryDisplayName(string category)
        {
            var loc = LocalizationManager.Instance;
            return category switch
            {
                "base_stats" => loc["ui.shop.categoryName.baseStats"],
                "currency_bonus" => loc["ui.shop.categoryName.currencyBonus"],
                "utility" => loc["ui.shop.categoryName.utility"],
                "starting_bonus" => loc["ui.shop.categoryName.startingBonus"],
                // Legacy 카테고리 (하위 호환)
                "percentage" => loc["ui.shop.categoryName.percentage"],
                "abilities" => loc["ui.shop.categoryName.abilities"],
                _ => category
            };
        }

        /// <summary>
        /// 현재 언어에 맞는 로컬라이즈된 텍스트 가져오기
        /// </summary>
        private string GetLocalizedText(
            Dictionary<string, UpgradeLocalization> localizations,
            string currentLang,
            Func<UpgradeLocalization, string> selector,
            string fallback)
        {
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
