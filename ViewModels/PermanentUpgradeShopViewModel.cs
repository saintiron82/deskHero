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

                string fullName = def.Localization.ContainsKey("ko-KR") ? def.Localization["ko-KR"].Name : def.Id;

                // 설명문에서 {value}를 실제 값으로 치환
                string description = def.Localization.ContainsKey("ko-KR") ? def.Localization["ko-KR"].Description : "";
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
                "time_extend" => $"{(value * 5):F0}초",          // 레벨당 5초
                "upgrade_discount" => $"{(value * 2):F0}%",      // 레벨당 2%

                // D. 시작 보너스 (8종)
                "start_level" => $"Lv.{value:F0}",
                "start_gold" => $"{(value * 50):F0}G",           // 레벨당 50G
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
            return category switch
            {
                "base_stats" => "기본 능력",
                "currency_bonus" => "재화 보너스",
                "utility" => "유틸리티",
                "starting_bonus" => "시작 보너스",
                // Legacy 카테고리 (하위 호환)
                "percentage" => "배율 증가",
                "abilities" => "특수 능력",
                _ => category
            };
        }

        /// <summary>
        /// 짧은 이름 생성 (컴팩트 카드용)
        /// </summary>
        private string GenerateShortName(string fullName)
        {
            var shortNameMap = new Dictionary<string, string>
            {
                { "기본 공격력", "공격력" },
                { "공격력 배수", "공격*" },
                { "크리티컬 확률", "크리확률" },
                { "크리티컬 배율", "크리배율" },
                { "멀티히트 확률", "멀티히트" },
                { "영구 골드+", "골드+" },
                { "영구 골드*", "골드*" },
                { "크리스탈+", "크리+" },
                { "크리스탈*", "크리*" },
                { "기본 시간 연장", "시간↑" },
                { "업그레이드 할인", "할인" },
                { "시작 레벨", "시작Lv" },
                { "시작 골드", "시작G" },
                { "시작 키보드", "시작⌨️" },
                { "시작 마우스", "시작🖱️" },
                { "시작 골드+", "시작G+" },
                { "시작 골드*", "시작G*" },
                { "시작 콤보유연성", "시작콤보" },
                { "시작 콤보데미지", "시작콤보D" }
            };

            return shortNameMap.ContainsKey(fullName) ? shortNameMap[fullName] : fullName;
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
                if (MaxLevel > 0)
                    return $"Lv.{CurrentLevel}/{MaxLevel}";
                return $"Lv.{CurrentLevel}";
            }
        }

        public string ButtonText
        {
            get
            {
                if (IsMaxed)
                    return "MAX";
                return $"💎 {Cost:N0}";
            }
        }
    }
}
