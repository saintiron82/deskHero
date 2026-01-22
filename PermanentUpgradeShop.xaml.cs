using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        private string _currentCategory = "base_stats";

        public PermanentUpgradeShop(PermanentProgressionManager progressionManager, SaveManager saveManager)
        {
            try
            {
                InitializeComponent();

                _progressionManager = progressionManager;
                _saveManager = saveManager;
                _viewModel = new PermanentUpgradeShopViewModel(_progressionManager, _saveManager);

                RefreshUI();
                LoadCategoryUpgrades("base_stats");
            }
            catch (Exception ex)
            {
                DeskWarrior.Helpers.Logger.LogError("PermanentUpgradeShop Initialization Failed", ex);
                MessageBox.Show($"상점 초기화 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

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

            // 뱃지 업데이트
            UpdateBadges();
        }

        /// <summary>
        /// 탭 뱃지 업데이트 (구매 가능 개수 표시)
        /// </summary>
        private void UpdateBadges()
        {
            // InitializeComponent 완료 전에 호출되면 무시
            if (BadgeBaseStats == null)
                return;

            var categories = new[]
            {
                new { Key = "base_stats", Badge = BadgeBaseStats },
                new { Key = "currency_bonus", Badge = BadgeCurrencyBonus },
                new { Key = "utility", Badge = BadgeUtility },
                new { Key = "starting_bonus", Badge = BadgeStartingBonus }
            };

            foreach (var cat in categories)
            {
                int affordableCount = _viewModel.AllUpgrades
                    .Count(u => u.CategoryKey == cat.Key && u.CanAfford);

                if (affordableCount > 0)
                {
                    cat.Badge.Text = $"({affordableCount})";
                    cat.Badge.Visibility = Visibility.Visible;
                }
                else
                {
                    cat.Badge.Text = "";
                    cat.Badge.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 카테고리별 업그레이드 로드 (3열 컴팩트 그리드)
        /// </summary>
        private void LoadCategoryUpgrades(string category)
        {
            // InitializeComponent 완료 전에 호출되면 무시
            if (UpgradeGrid == null)
                return;

            _currentCategory = category;
            UpgradeGrid.Children.Clear();
            UpgradeGrid.ColumnDefinitions.Clear();
            UpgradeGrid.RowDefinitions.Clear();

            // 해당 카테고리의 업그레이드 필터링
            var categoryUpgrades = _viewModel.AllUpgrades
                .Where(u => u.CategoryKey == category)
                .ToList();

            if (categoryUpgrades.Count == 0)
            {
                // 업그레이드가 없는 경우 메시지 표시
                var message = new TextBlock
                {
                    Text = "이 카테고리에는 업그레이드가 없습니다.",
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

            // 카드 배치
            for (int i = 0; i < categoryUpgrades.Count; i++)
            {
                var upgrade = categoryUpgrades[i];
                int col = i % columns;
                int row = i / columns;

                // 행 추가 (필요한 경우)
                while (UpgradeGrid.RowDefinitions.Count <= row)
                {
                    UpgradeGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                // 컴팩트 카드 생성
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

            // 카테고리별 테두리 색상
            Color categoryColor = upgrade.CategoryKey switch
            {
                "base_stats" => Color.FromRgb(220, 38, 38),      // 빨강 (전투력)
                "currency_bonus" => Color.FromRgb(250, 204, 21), // 금색 (재화)
                "utility" => Color.FromRgb(59, 130, 246),        // 파랑 (유틸)
                "starting_bonus" => Color.FromRgb(168, 85, 247), // 보라 (시작)
                _ => Color.FromRgb(156, 163, 175)                // 회색 (기본)
            };

            // 구매 가능 여부에 따라 카드 스타일 변경
            if (upgrade.CanAfford)
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
                    new Uri("pack://application:,,,/Assets/Images/UI/crystal.png")),
                Width = 14,
                Height = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 2, 0)
            };
            costPanel.Children.Add(costIcon);

            var costText = new TextBlock
            {
                Text = $"{upgrade.Cost:N0}",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = upgrade.CanAfford
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

            // === 구매 버튼 (구매 가능할 때만 표시) ===
            if (upgrade.CanAfford)
            {
                var button = new Button
                {
                    Height = 24,
                    Tag = upgrade.Id,
                    Content = "구매",
                    Style = (Style)FindResource("BuyButtonAffordable")
                };
                button.Click += BuyUpgrade_Click;
                mainStack.Children.Add(button);
            }

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

            return card;
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
                Text = $"현재: {upgrade.CurrentEffect}",
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
                Text = "다음 레벨 효과",
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
                Text = "비용: ",
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
                    new Uri("pack://application:,,,/Assets/Images/UI/crystal.png")),
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
            if (sender is not RadioButton radioButton)
                return;

            string category = radioButton.Name switch
            {
                "TabBaseStats" => "base_stats",
                "TabCurrencyBonus" => "currency_bonus",
                "TabUtility" => "utility",
                "TabStartingBonus" => "starting_bonus",
                _ => "base_stats"
            };

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
                // 성공 시 UI 새로고침
                RefreshUI();
                LoadCategoryUpgrades(_currentCategory);

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
            if (sender is not Button button)
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
        /// 닫기 버튼 클릭
        /// </summary>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var helpContent =
                "크리스탈로 영구적인 능력을 강화할 수 있습니다.\n\n" +
                "• 보스를 처치하면 크리스탈을 획득합니다.\n" +
                "• 업그레이드는 모든 게임 세션에 적용됩니다.\n" +
                "• 카드를 클릭하여 구매할 수 있습니다.\n" +
                "• 레벨이 높을수록 비용이 증가합니다.\n\n" +
                "탭을 사용하여 카테고리별로 업그레이드를 확인할 수 있습니다.";

            var helpPopup = new Windows.HelpPopup("상점", helpContent);
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
        private void PlayPurchaseAnimation(Button button)
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
        private void PlayErrorAnimation(Button button)
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
