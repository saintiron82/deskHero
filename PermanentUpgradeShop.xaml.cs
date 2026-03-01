using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DeskWarrior.Managers;
using DeskWarrior.Models;
using DeskWarrior.ViewModels;

namespace DeskWarrior
{
    /// <summary>
    /// 영구 업그레이드 상점 윈도우
    /// </summary>
    public partial class PermanentUpgradeShop : Window
    {
        private readonly PermanentProgressionManager _progressionManager;
        private readonly SaveManager _saveManager;
        private PermanentUpgradeShopViewModel _viewModel;
        private string _currentCategory = "";

        // 연속 구매 시 UI 업데이트를 위한 요소 참조 (버튼 포함)
        private readonly Dictionary<string, (TextBlock levelText, TextBlock effectText, TextBlock costText, Button button, TextBlock icon, TextBlock nameText)> _cardElements = new();

        // 동적 탭 뱃지 참조
        private readonly Dictionary<string, TextBlock> _tabBadges = new();

        // 연속 구매용 타이머
        private System.Windows.Threading.DispatcherTimer? _repeatPurchaseTimer;
        private string? _repeatPurchaseUpgradeId;

        public PermanentUpgradeShop(PermanentProgressionManager progressionManager, SaveManager saveManager)
        {
            try
            {
                InitializeComponent();

                _progressionManager = progressionManager;
                _saveManager = saveManager;
                _viewModel = new PermanentUpgradeShopViewModel(_progressionManager, _saveManager);

                GenerateTabs();
                RefreshUI();

                // 첫 번째 카테고리 로드
                var categories = _progressionManager.GetOrderedCategories();
                if (categories.Count > 0)
                {
                    _currentCategory = categories[0].Key;
                    LoadCategoryUpgrades(_currentCategory);
                }
            }
            catch (Exception ex)
            {
                DeskWarrior.Helpers.Logger.LogError("PermanentUpgradeShop Initialization Failed", ex);
                var loc = LocalizationManager.Instance;
                MessageBox.Show($"{loc["ui.shop.error.init"]}: {ex.Message}", loc["ui.common.error"], MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        /// <summary>
        /// 로컬라이즈된 UI 텍스트 업데이트
        /// </summary>
        private void UpdateLocalizedUI()
        {
            var loc = LocalizationManager.Instance;

            // 윈도우 타이틀
            Title = loc["ui.shop.title"];

            // 헤더 텍스트
            if (ShopTitleText != null) ShopTitleText.Text = loc["ui.shop.title"].Replace("💎 ", "");

            // 버튼 툴팁
            if (HelpButton != null) HelpButton.ToolTip = loc["ui.shop.help"];
            if (CloseButton != null) CloseButton.ToolTip = loc["ui.shop.close"];

            // 탭 텍스트는 GenerateTabs()에서 동적 생성

            // 힌트 텍스트
            if (HintText != null) HintText.Text = loc["ui.shop.hint"];
        }

        #region Tab Generation

        /// <summary>
        /// JSON 카테고리 데이터에서 동적으로 탭 생성
        /// </summary>
        private void GenerateTabs()
        {
            if (TabPanel == null) return;

            TabPanel.Children.Clear();
            _tabBadges.Clear();

            var categories = _progressionManager.GetOrderedCategories();
            bool isFirst = true;

            foreach (var (key, info) in categories)
            {
                var badge = new TextBlock
                {
                    Text = "",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    Margin = new Thickness(4, 0, 0, 0),
                    Visibility = Visibility.Collapsed
                };
                _tabBadges[key] = badge;

                // 로컬라이제이션 키 우선, 없으면 JSON name 사용
                var loc = LocalizationManager.Instance;
                string locKey = $"ui.shop.category.{key}";
                string tabText = loc[locKey];
                if (tabText == locKey) // 키가 없으면 원본 반환
                    tabText = $"{info.Icon} {info.Name}";

                var tabContent = new StackPanel { Orientation = Orientation.Horizontal };
                tabContent.Children.Add(new TextBlock { Text = tabText });
                tabContent.Children.Add(badge);

                var tab = new RadioButton
                {
                    Style = (Style)FindResource("TabButton"),
                    Tag = key,
                    Margin = new Thickness(0, 0, 6, 4),
                    IsChecked = isFirst,
                    Content = tabContent
                };
                tab.Checked += Tab_Checked;

                TabPanel.Children.Add(tab);
                isFirst = false;
            }
        }

        /// <summary>
        /// 카테고리 색상을 JSON에서 가져오기
        /// </summary>
        private Color GetCategoryColor(string categoryKey)
        {
            var categories = _progressionManager.GetOrderedCategories();
            var match = categories.FirstOrDefault(c => c.Key == categoryKey);

            if (match.Info != null && !string.IsNullOrEmpty(match.Info.Color))
            {
                try
                {
                    var wpfColor = (Color)ColorConverter.ConvertFromString(match.Info.Color);
                    return wpfColor;
                }
                catch { }
            }

            return Color.FromRgb(156, 163, 175); // 기본 회색
        }

        #endregion

        #region UI Update

        /// <summary>
        /// UI 새로고침
        /// </summary>
        private void RefreshUI()
        {
            // InitializeComponent 완료 전에 호출되면 무시
            if (CurrentCrystalsText == null)
                return;

            _viewModel.LoadData();

            // 헤더 통화 정보 업데이트
            CurrentCrystalsText.Text = _viewModel.CurrentCrystals.ToString("N0");

            // 로컬라이즈된 UI 텍스트 업데이트
            UpdateLocalizedUI();

            // 뱃지 업데이트
            UpdateBadges();
        }

        /// <summary>
        /// 탭 뱃지 업데이트 (구매 가능 개수 표시)
        /// </summary>
        private void UpdateBadges()
        {
            if (_tabBadges.Count == 0)
                return;

            foreach (var kvp in _tabBadges)
            {
                int affordableCount = _viewModel.AllUpgrades
                    .Count(u => u.CategoryKey == kvp.Key && u.CanAfford);

                if (affordableCount > 0)
                {
                    kvp.Value.Text = $"({affordableCount})";
                    kvp.Value.Visibility = Visibility.Visible;
                }
                else
                {
                    kvp.Value.Text = "";
                    kvp.Value.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 카테고리별 업그레이드 로드 (3열 컴팩트 그리드, 통일/독립 비용 분리 표시)
        /// </summary>
        private void LoadCategoryUpgrades(string category)
        {
            // InitializeComponent 완료 전에 호출되면 무시
            if (UpgradeGrid == null)
                return;

            DeskWarrior.Helpers.Logger.Log($"[Shop] LoadCategoryUpgrades: {category}");

            _currentCategory = category;
            _cardElements.Clear(); // 카드 요소 참조 초기화
            UpgradeGrid.Children.Clear();
            UpgradeGrid.ColumnDefinitions.Clear();
            UpgradeGrid.RowDefinitions.Clear();

            // 해당 카테고리의 업그레이드 필터링 (통일 비용 → 독립 비용 순)
            var categoryUpgrades = _viewModel.AllUpgrades
                .Where(u => u.CategoryKey == category)
                .ToList();

            var unifiedUpgrades = categoryUpgrades.Where(u => u.UnifiedCost).ToList();
            var independentUpgrades = categoryUpgrades.Where(u => !u.UnifiedCost).ToList();

            DeskWarrior.Helpers.Logger.Log($"[Shop] Found {categoryUpgrades.Count} upgrades for category '{category}' (unified:{unifiedUpgrades.Count}, independent:{independentUpgrades.Count})");

            if (categoryUpgrades.Count == 0)
            {
                // 업그레이드가 없는 경우 메시지 표시
                var message = new TextBlock
                {
                    Text = LocalizationManager.Instance["ui.shop.noUpgrades"],
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                UpgradeGrid.Children.Add(message);
                UpdateBadges();
                return;
            }

            // 3열 그리드 구성
            int columns = 3;
            for (int i = 0; i < columns; i++)
            {
                UpgradeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            int currentRow = 0;

            // 통일 비용 스탯 배치
            for (int i = 0; i < unifiedUpgrades.Count; i++)
            {
                var upgrade = unifiedUpgrades[i];
                int col = i % columns;
                int row = i / columns;

                while (UpgradeGrid.RowDefinitions.Count <= row)
                {
                    UpgradeGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                var card = CreateCompactUpgradeCard(upgrade);
                Grid.SetColumn(card, col);
                Grid.SetRow(card, row);
                UpgradeGrid.Children.Add(card);

                currentRow = row;
            }

            // 독립 비용 (%) 스탯이 있으면 구분선 추가 후 배치
            if (independentUpgrades.Count > 0 && unifiedUpgrades.Count > 0)
            {
                currentRow++;
                UpgradeGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // 구분선
                var separator = new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                    Margin = new Thickness(4, 8, 4, 4)
                };
                Grid.SetRow(separator, currentRow);
                Grid.SetColumn(separator, 0);
                Grid.SetColumnSpan(separator, columns);
                UpgradeGrid.Children.Add(separator);
            }

            int baseRow = (unifiedUpgrades.Count > 0 && independentUpgrades.Count > 0)
                ? currentRow + 1
                : (unifiedUpgrades.Count > 0 ? currentRow + 1 : 0);

            for (int i = 0; i < independentUpgrades.Count; i++)
            {
                var upgrade = independentUpgrades[i];
                int col = i % columns;
                int row = baseRow + i / columns;

                while (UpgradeGrid.RowDefinitions.Count <= row)
                {
                    UpgradeGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                var card = CreateCompactUpgradeCard(upgrade);
                Grid.SetColumn(card, col);
                Grid.SetRow(card, row);
                UpgradeGrid.Children.Add(card);
            }

            UpdateBadges();
        }

        /// <summary>
        /// 컴팩트 업그레이드 카드 생성 (Casual Game Style)
        /// </summary>
        private Border CreateCompactUpgradeCard(UpgradeCardViewModel upgrade)
        {
            var card = new Border
            {
                Style = (Style)FindResource("CompactUpgradeCard"),
                Tag = upgrade.Id,
                Padding = new Thickness(8)
            };

            // 카테고리별 테두리 색상 (JSON에서 로드)
            Color categoryColor = GetCategoryColor(upgrade.CategoryKey);

            // 구매/승급 가능 여부에 따라 카드 스타일 변경
            if (upgrade.NeedsPromotion)
            {
                if (upgrade.CanPromote)
                {
                    // 승급 가능: 골든 테두리 + 밝은 배경
                    card.Background = new SolidColorBrush(Color.FromRgb(255, 251, 235));
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));  // Amber
                    card.BorderThickness = new Thickness(3);
                    card.Cursor = Cursors.Hand;

                    card.MouseLeftButtonDown += (s, e) => {
                        if (s is Border clickedCard && clickedCard.Tag is string upgradeId)
                        {
                            TryPurchaseUpgrade(upgradeId);
                        }
                    };
                }
                else
                {
                    // 승급 불가: 어두운 회색 + 주황 테두리
                    card.Background = new SolidColorBrush(Color.FromRgb(220, 210, 190));
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(180, 150, 100));
                    card.BorderThickness = new Thickness(2);
                    card.Cursor = Cursors.No;
                }
            }
            else if (upgrade.CanAfford)
            {
                // 구매 가능: 카테고리 색상 테두리 + 밝은 배경
                card.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                card.BorderBrush = new SolidColorBrush(categoryColor);
                card.BorderThickness = new Thickness(3);
                card.Cursor = Cursors.Hand;

                // 카드 클릭 시 구매 처리
                card.MouseLeftButtonDown += (s, e) => {
                    if (s is Border clickedCard && clickedCard.Tag is string upgradeId)
                    {
                        TryPurchaseUpgrade(upgradeId);
                    }
                };
            }
            else
            {
                // 구매 불가: 회색 배경 + 회색 테두리
                card.Background = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(180, 180, 180));
                card.BorderThickness = new Thickness(1);
                card.Cursor = Cursors.No;
            }

            // 툴팁 (밝은 스타일)
            var tooltip = new ToolTip
            {
                Content = CreateTooltipContent(upgrade),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(15)
            };
            card.ToolTip = tooltip;

            // Grid를 사용하여 레벨을 좌상단에, 비용을 우상단에 배치
            var mainGrid = new Grid();

            // === 레벨 (좌상단) ===
            var levelText = new TextBlock
            {
                Text = upgrade.LevelDisplay,
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 0)
            };
            mainGrid.Children.Add(levelText);

            // === 비용 (우상단) - 항상 표시 ===
            var costPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 0)
            };

            var costIcon = new Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(
                    ResourceManager.Instance.GetUIImageUri("crystal")),
                Width = 14,
                Height = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 2, 0)
            };
            costPanel.Children.Add(costIcon);

            int displayCost = upgrade.NeedsPromotion ? upgrade.PromotionCrystalCost : upgrade.Cost;
            bool canDo = upgrade.NeedsPromotion ? upgrade.CanPromote : upgrade.CanAfford;
            var costText = new TextBlock
            {
                Text = $"{displayCost:N0}",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = canDo
                    ? new SolidColorBrush(Color.FromRgb(0, 153, 204))  // 파란색 (구매 가능)
                    : new SolidColorBrush(Color.FromRgb(120, 120, 120)), // 회색 (구매 불가)
                VerticalAlignment = VerticalAlignment.Center
            };
            costPanel.Children.Add(costText);

            mainGrid.Children.Add(costPanel);

            // === 중앙 콘텐츠 StackPanel ===
            var mainStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            // === 아이콘 (중형) ===
            var icon = new TextBlock
            {
                Text = upgrade.Icon,
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 3)
            };
            mainStack.Children.Add(icon);

            // === 이름 ===
            var nameText = new TextBlock
            {
                Text = upgrade.Name,
                FontSize = 10,
                FontWeight = FontWeights.Black,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 160,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            mainStack.Children.Add(nameText);

            // === 능력치 (1줄로 표기: 현재 → 다음) ===
            var effectText = new TextBlock
            {
                Text = $"{upgrade.CurrentEffect} → {upgrade.NextLevelEffect}",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            mainStack.Children.Add(effectText);

            // === 승급 상태 표시 (등급 경계일 때) ===
            if (upgrade.NeedsPromotion)
            {
                var loc = LocalizationManager.Instance;

                // 레벨 조건 표시
                var reqText = new TextBlock
                {
                    Text = upgrade.PromotionLevelMet
                        ? loc.Format("ui.shop.promotion.requirementMet", upgrade.PromotionRequiredLevel)
                        : loc.Format("ui.shop.promotion.requirementNotMet", upgrade.PromotionRequiredLevel, upgrade.PlayerMaxLevel),
                    FontSize = 8,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = upgrade.PromotionLevelMet
                        ? new SolidColorBrush(Color.FromRgb(16, 185, 129))  // Green
                        : new SolidColorBrush(Color.FromRgb(239, 68, 68)),  // Red
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                mainStack.Children.Add(reqText);
            }

            // === 구매/승급 버튼 ===
            Button button;
            if (upgrade.NeedsPromotion)
            {
                var loc = LocalizationManager.Instance;
                string promoteStyle = upgrade.CanPromote ? "BuyButtonAffordable" : "BuyButtonUnaffordable";

                button = new Button
                {
                    Height = 24,
                    Tag = upgrade.Id,
                    Content = upgrade.CanPromote
                        ? $"⬆ {loc["ui.shop.promotion.promote"]}"
                        : loc["ui.shop.promotion.locked"],
                    Style = (Style)FindResource(promoteStyle),
                    IsEnabled = upgrade.CanPromote
                };
                button.Click += BuyUpgrade_Click;
            }
            else
            {
                button = new Button
                {
                    Height = 24,
                    Tag = upgrade.Id,
                    Content = LocalizationManager.Instance["ui.common.buy"],
                    Style = (Style)FindResource(upgrade.CanAfford ? "BuyButtonAffordable" : "BuyButtonUnaffordable"),
                    IsEnabled = upgrade.CanAfford
                };
                button.Click += BuyUpgrade_Click;
                // 연속 구매: 마우스 누르고 있으면 반복 구매 (승급에는 적용하지 않음)
                button.PreviewMouseLeftButtonDown += BuyButton_MouseDown;
                button.PreviewMouseLeftButtonUp += BuyButton_MouseUp;
                button.MouseLeave += BuyButton_MouseLeave;
            }
            mainStack.Children.Add(button);

            // StackPanel을 Grid에 추가
            mainGrid.Children.Add(mainStack);

            // 구매 불가능한 경우 모든 텍스트를 회색으로 처리
            if (!upgrade.CanAfford)
            {
                var grayBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120));

                // 아이콘
                icon.Foreground = grayBrush;

                // 이름
                nameText.Foreground = grayBrush;

                // 레벨
                levelText.Foreground = grayBrush;

                // 능력치
                effectText.Foreground = grayBrush;
            }

            card.Child = mainGrid;

            // 연속 구매 시 UI 업데이트를 위해 요소 참조 저장
            _cardElements[upgrade.Id] = (levelText, effectText, costText, button, icon, nameText);

            return card;
        }

        /// <summary>
        /// 특정 카드의 UI 요소만 업데이트 (연속 구매 시 사용)
        /// </summary>
        private void UpdateCardUI(string upgradeId)
        {
            if (!_cardElements.TryGetValue(upgradeId, out var elements))
                return;

            var upgrade = _viewModel.AllUpgrades.FirstOrDefault(u => u.Id == upgradeId);
            if (upgrade == null)
                return;

            // 레벨 업데이트
            elements.levelText.Text = upgrade.LevelDisplay;

            // 효과 업데이트
            elements.effectText.Text = $"{upgrade.CurrentEffect} → {upgrade.NextLevelEffect}";

            // 비용 업데이트
            elements.costText.Text = $"{upgrade.Cost:N0}";

            // 구매 가능 여부에 따른 스타일 업데이트
            if (upgrade.CanAfford)
            {
                var normalColor = new SolidColorBrush(Color.FromRgb(0, 153, 204)); // Blue
                var darkColor = new SolidColorBrush(Color.FromRgb(31, 41, 55));
                var grayColor = new SolidColorBrush(Color.FromRgb(107, 114, 128));

                elements.costText.Foreground = normalColor;
                elements.button.Style = (Style)FindResource("BuyButtonAffordable");
                elements.button.IsEnabled = true;
                elements.icon.Foreground = new SolidColorBrush(Colors.Black);
                elements.nameText.Foreground = darkColor;
                elements.levelText.Foreground = grayColor;
                elements.effectText.Foreground = grayColor;
            }
            else
            {
                var grayBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120));

                elements.costText.Foreground = grayBrush;
                elements.button.Style = (Style)FindResource("BuyButtonUnaffordable");
                elements.button.IsEnabled = false;
                elements.icon.Foreground = grayBrush;
                elements.nameText.Foreground = grayBrush;
                elements.levelText.Foreground = grayBrush;
                elements.effectText.Foreground = grayBrush;

                // 현재 연속 구매 중인 업그레이드가 구매 불가능해지면 타이머 중지
                if (_repeatPurchaseUpgradeId == upgradeId)
                {
                    StopRepeatPurchase();
                }
            }
        }

        /// <summary>
        /// 모든 카드의 구매 가능 상태 업데이트 (크리스탈 변경 시)
        /// </summary>
        private void UpdateAllCardsAffordability()
        {
            foreach (var kvp in _cardElements)
            {
                var upgrade = _viewModel.AllUpgrades.FirstOrDefault(u => u.Id == kvp.Key);
                if (upgrade == null) continue;

                var elements = kvp.Value;

                if (upgrade.CanAfford)
                {
                    var normalColor = new SolidColorBrush(Color.FromRgb(0, 153, 204));
                    var darkColor = new SolidColorBrush(Color.FromRgb(31, 41, 55));
                    var grayColor = new SolidColorBrush(Color.FromRgb(107, 114, 128));

                    elements.costText.Foreground = normalColor;
                    elements.button.Style = (Style)FindResource("BuyButtonAffordable");
                    elements.button.IsEnabled = true;
                    elements.icon.Foreground = new SolidColorBrush(Colors.Black);
                    elements.nameText.Foreground = darkColor;
                    elements.levelText.Foreground = grayColor;
                    elements.effectText.Foreground = grayColor;
                }
                else
                {
                    var grayBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120));

                    elements.costText.Foreground = grayBrush;
                    elements.button.Style = (Style)FindResource("BuyButtonUnaffordable");
                    elements.button.IsEnabled = false;
                    elements.icon.Foreground = grayBrush;
                    elements.nameText.Foreground = grayBrush;
                    elements.levelText.Foreground = grayBrush;
                    elements.effectText.Foreground = grayBrush;
                }
            }
        }

        /// <summary>
        /// 툴팁 콘텐츠 생성
        /// </summary>
        private StackPanel CreateTooltipContent(UpgradeCardViewModel upgrade)
        {
            var tooltipStack = new StackPanel { Width = 250 };

            // 타이틀
            var titleStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            var titleIcon = new TextBlock
            {
                Text = upgrade.Icon,
                FontSize = 20,
                Margin = new Thickness(0, 0, 8, 0)
            };
            var titleText = new TextBlock
            {
                Text = upgrade.Name,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55))
            };
            titleStack.Children.Add(titleIcon);
            titleStack.Children.Add(titleText);
            tooltipStack.Children.Add(titleStack);

            // 레벨 & 현재 효과
            var levelGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            levelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            levelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var levelText = new TextBlock
            {
                Text = upgrade.LevelDisplay,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
            Grid.SetColumn(levelText, 0);

            var currentEffectText = new TextBlock
            {
                Text = LocalizationManager.Instance.Format("ui.shop.currentEffect", upgrade.CurrentEffect),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(currentEffectText, 1);

            levelGrid.Children.Add(levelText);
            levelGrid.Children.Add(currentEffectText);
            tooltipStack.Children.Add(levelGrid);

            // 설명
            var descText = new TextBlock
            {
                Text = upgrade.Description,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            tooltipStack.Children.Add(descText);

            // 다음 레벨 정보 (항상 표시)
            var nextBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(236, 253, 245)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var nextStack = new StackPanel();
            var nextLabel = new TextBlock
            {
                Text = LocalizationManager.Instance["ui.shop.nextLevel"],
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
            var nextValue = new TextBlock
            {
                Text = upgrade.NextLevelEffect,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129))
            };

            nextStack.Children.Add(nextLabel);
            nextStack.Children.Add(nextValue);
            nextBorder.Child = nextStack;
            tooltipStack.Children.Add(nextBorder);

            // 비용 (항상 표시)
            var tooltipCostPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var tooltipCostLabel = new TextBlock
            {
                Text = $"{LocalizationManager.Instance["ui.common.cost"]}: ",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = upgrade.CanAfford
                    ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
                    : new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                VerticalAlignment = VerticalAlignment.Center
            };
            tooltipCostPanel.Children.Add(tooltipCostLabel);

            var tooltipCostIcon = new Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(
                    ResourceManager.Instance.GetUIImageUri("crystal")),
                Width = 18,
                Height = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0)
            };
            tooltipCostPanel.Children.Add(tooltipCostIcon);

            var tooltipCostValue = new TextBlock
            {
                Text = $"{upgrade.Cost:N0}",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = upgrade.CanAfford
                    ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
                    : new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                VerticalAlignment = VerticalAlignment.Center
            };
            tooltipCostPanel.Children.Add(tooltipCostValue);

            tooltipStack.Children.Add(tooltipCostPanel);

            return tooltipStack;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 탭 변경 이벤트
        /// </summary>
        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton radioButton || radioButton.Tag is not string category)
                return;

            LoadCategoryUpgrades(category);
        }

        /// <summary>
        /// 업그레이드 구매 시도 (공통 메서드)
        /// </summary>
        private void TryPurchaseUpgrade(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId))
                return;

            // ViewModel을 통해 구매 시도
            bool success = _viewModel.TryPurchaseUpgrade(upgradeId);

            if (success)
            {
                // 즉시 저장 (크래시 시 데이터 손실 방지)
                _saveManager.Save();

                // 성공 시 통화 정보 업데이트
                RefreshUI();

                // 구매한 카드 업데이트 (레벨, 효과, 비용)
                UpdateCardUI(upgradeId);

                // 모든 카드의 구매 가능 상태 업데이트 (크리스탈 감소로 인해)
                UpdateAllCardsAffordability();

                // 메인 윈도우 크리스탈 표시도 업데이트
                if (Owner is MainWindow mainWindow)
                {
                    mainWindow.UpdateCrystalDisplay();
                }

                // 성공 사운드 (있다면)
                DeskWarrior.Helpers.Logger.Log($"[PermanentUpgradeShop] Purchased: {upgradeId}");
            }
            else
            {
                // 실패 로그
                DeskWarrior.Helpers.Logger.Log($"[PermanentUpgradeShop] Purchase failed: {upgradeId}");
            }
        }

        /// <summary>
        /// 업그레이드 구매 버튼 클릭
        /// </summary>
        private void BuyUpgrade_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ButtonBase button)
                return;

            string? upgradeId = button.Tag as string;
            if (string.IsNullOrEmpty(upgradeId))
                return;

            TryPurchaseUpgrade(upgradeId);

            // 버튼 애니메이션은 구매 성공 여부와 관계없이 피드백 제공
            var upgrade = _viewModel.AllUpgrades.FirstOrDefault(u => u.Id == upgradeId);
            if (upgrade != null && upgrade.CanAfford)
            {
                PlayPurchaseAnimation(button);
            }
            else
            {
                PlayErrorAnimation(button);
            }
        }

        /// <summary>
        /// 구매 버튼 마우스 다운 - 연속 구매 타이머 시작
        /// </summary>
        private void BuyButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DeskWarrior.Helpers.Logger.Log("[Shop] MouseDown fired");

            if (sender is not ButtonBase button)
            {
                DeskWarrior.Helpers.Logger.Log("[Shop] sender is not ButtonBase");
                return;
            }

            string? upgradeId = button.Tag as string;
            if (string.IsNullOrEmpty(upgradeId))
            {
                DeskWarrior.Helpers.Logger.Log("[Shop] upgradeId is null or empty");
                return;
            }

            DeskWarrior.Helpers.Logger.Log($"[Shop] Starting repeat timer for {upgradeId}");
            _repeatPurchaseUpgradeId = upgradeId;

            // 400ms 후 연속 구매 시작
            _repeatPurchaseTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(400)
            };
            _repeatPurchaseTimer.Tick += RepeatPurchase_FirstTick;
            _repeatPurchaseTimer.Start();
        }

        /// <summary>
        /// 첫 번째 틱 - 딜레이 후 빠른 반복으로 전환
        /// </summary>
        private void RepeatPurchase_FirstTick(object? sender, EventArgs e)
        {
            DeskWarrior.Helpers.Logger.Log("[Shop] FirstTick fired");
            if (_repeatPurchaseTimer == null) return;

            // 첫 구매 실행
            if (!string.IsNullOrEmpty(_repeatPurchaseUpgradeId))
            {
                DeskWarrior.Helpers.Logger.Log($"[Shop] FirstTick purchase: {_repeatPurchaseUpgradeId}");
                TryPurchaseUpgrade(_repeatPurchaseUpgradeId);
            }

            // 타이머가 StopRepeatPurchase()에 의해 null이 되었을 수 있음
            if (_repeatPurchaseTimer == null) return;

            // 빠른 반복 간격으로 전환 (80ms)
            _repeatPurchaseTimer.Stop();
            _repeatPurchaseTimer.Tick -= RepeatPurchase_FirstTick;
            _repeatPurchaseTimer.Tick += RepeatPurchase_Tick;
            _repeatPurchaseTimer.Interval = TimeSpan.FromMilliseconds(80);
            _repeatPurchaseTimer.Start();
        }

        /// <summary>
        /// 연속 구매 틱
        /// </summary>
        private void RepeatPurchase_Tick(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_repeatPurchaseUpgradeId))
            {
                DeskWarrior.Helpers.Logger.Log($"[Shop] RepeatTick purchase: {_repeatPurchaseUpgradeId}");
                TryPurchaseUpgrade(_repeatPurchaseUpgradeId);
            }
        }

        /// <summary>
        /// 구매 버튼 마우스 업 - 연속 구매 중지
        /// </summary>
        private void BuyButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StopRepeatPurchase();
            // 카드 전체 갱신 (버튼 상태 업데이트)
            LoadCategoryUpgrades(_currentCategory);
        }

        /// <summary>
        /// 구매 버튼에서 마우스 벗어남 - 연속 구매 중지
        /// </summary>
        private void BuyButton_MouseLeave(object sender, MouseEventArgs e)
        {
            StopRepeatPurchase();
        }

        /// <summary>
        /// 연속 구매 타이머 중지
        /// </summary>
        private void StopRepeatPurchase()
        {
            if (_repeatPurchaseTimer != null)
            {
                _repeatPurchaseTimer.Stop();
                _repeatPurchaseTimer.Tick -= RepeatPurchase_FirstTick;
                _repeatPurchaseTimer.Tick -= RepeatPurchase_Tick;
                _repeatPurchaseTimer = null;
            }
            _repeatPurchaseUpgradeId = null;
        }

        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var loc = LocalizationManager.Instance;
            var helpContent = loc["ui.shop.help.content"];

            var helpPopup = new Windows.HelpPopup(loc["ui.shop.title"], helpContent);
            helpPopup.Owner = this;
            helpPopup.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// ESC 키로 닫기
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        /// <summary>
        /// 헤더 드래그로 창 이동
        /// </summary>
        private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        #endregion

        #region Animations

        /// <summary>
        /// 구매 성공 애니메이션
        /// </summary>
        private void PlayPurchaseAnimation(ButtonBase button)
        {
            var card = FindVisualParent<Border>(button);
            if (card == null)
                return;

            // 간단한 색상 플래시 애니메이션
            var originalBrush = card.BorderBrush;
            card.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            timer.Tick += (s, e) =>
            {
                card.BorderBrush = originalBrush;
                timer.Stop();
            };
            timer.Start();
        }

        /// <summary>
        /// 구매 실패 애니메이션
        /// </summary>
        private void PlayErrorAnimation(ButtonBase button)
        {
            var card = FindVisualParent<Border>(button);
            if (card == null)
                return;

            // 빨간색 플래시
            var originalBrush = card.BorderBrush;
            card.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            timer.Tick += (s, e) =>
            {
                card.BorderBrush = originalBrush;
                timer.Stop();
            };
            timer.Start();
        }

        /// <summary>
        /// 비주얼 트리에서 부모 요소 찾기
        /// </summary>
        private T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null)
                return null;

            if (parent is T typedParent)
                return typedParent;

            return FindVisualParent<T>(parent);
        }

        #endregion
    }
}
