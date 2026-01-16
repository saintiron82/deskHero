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
            _viewModel.LoadData();

            // 헤더 통화 정보 업데이트
            CurrentCrystalsText.Text = _viewModel.CurrentCrystals.ToString("N0");
            LifetimeEarnedText.Text = _viewModel.LifetimeEarned.ToString("N0");
            LifetimeSpentText.Text = _viewModel.LifetimeSpent.ToString("N0");

            // 뱃지 업데이트
            UpdateBadges();
        }

        /// <summary>
        /// 탭 뱃지 업데이트 (구매 가능 개수 표시)
        /// </summary>
        private void UpdateBadges()
        {
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
                    .Count(u => u.CategoryKey == cat.Key && u.CanAfford && !u.IsMaxed);

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
                UpgradeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
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
        /// 컴팩트 업그레이드 카드 생성
        /// </summary>
        private Border CreateCompactUpgradeCard(UpgradeCardViewModel upgrade)
        {
            var card = new Border
            {
                Style = (Style)FindResource("CompactUpgradeCard"),
                Tag = upgrade.Id,
                Padding = new Thickness(10)
            };

            // 툴팁 추가
            var tooltip = new ToolTip
            {
                Content = CreateTooltipContent(upgrade),
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 206, 209)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12)
            };
            card.ToolTip = tooltip;

            var mainStack = new StackPanel();

            // === 헤더: 아이콘 + 이름 + 현재 효과 ===
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };

            var leftStack = new StackPanel { Orientation = Orientation.Horizontal };

            var icon = new TextBlock
            {
                Text = upgrade.Icon,
                FontSize = 16,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var nameText = new TextBlock
            {
                Text = upgrade.ShortName,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 80
            };

            leftStack.Children.Add(icon);
            leftStack.Children.Add(nameText);

            var currentEffect = new TextBlock
            {
                Text = upgrade.CurrentEffect,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            headerGrid.Children.Add(leftStack);
            headerGrid.Children.Add(currentEffect);
            mainStack.Children.Add(headerGrid);

            // === 레벨 표시 ===
            var levelText = new TextBlock
            {
                Text = upgrade.LevelDisplay,
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(110, 118, 129)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            mainStack.Children.Add(levelText);

            // === 구분선 ===
            var separator1 = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(48, 54, 61)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            mainStack.Children.Add(separator1);

            // === 다음 레벨 효과 ===
            var nextLevelStack = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

            var nextLevelLabel = new TextBlock
            {
                Text = upgrade.IsMaxed ? "상태" : "다음",
                FontSize = 8,
                Foreground = new SolidColorBrush(Color.FromRgb(110, 118, 129))
            };

            var nextLevelValue = new TextBlock
            {
                Text = upgrade.IsMaxed ? "MAX" : upgrade.NextLevelEffect,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = upgrade.IsMaxed
                    ? new SolidColorBrush(Color.FromRgb(96, 165, 250))
                    : new SolidColorBrush(Color.FromRgb(16, 185, 129))
            };

            nextLevelStack.Children.Add(nextLevelLabel);
            nextLevelStack.Children.Add(nextLevelValue);
            mainStack.Children.Add(nextLevelStack);

            // === 구분선 ===
            var separator2 = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(48, 54, 61)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            mainStack.Children.Add(separator2);

            // === 구매 버튼 ===
            var button = new Button
            {
                Height = 28,
                Tag = upgrade.Id,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold
            };
            button.Click += BuyUpgrade_Click;

            var buttonText = new TextBlock
            {
                Text = upgrade.ButtonText,
                FontSize = 10
            };
            button.Content = buttonText;

            // 버튼 스타일 설정 (인라인)
            if (upgrade.IsMaxed)
            {
                button.Background = new SolidColorBrush(Color.FromRgb(48, 54, 61));
                button.Foreground = new SolidColorBrush(Color.FromRgb(96, 165, 250));
                button.IsEnabled = false;
            }
            else if (upgrade.CanAfford)
            {
                button.Background = new SolidColorBrush(Color.FromRgb(5, 150, 105));
                button.Foreground = Brushes.White;
                button.IsEnabled = true;
            }
            else
            {
                button.Background = new SolidColorBrush(Color.FromRgb(55, 65, 81));
                button.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                button.IsEnabled = false;
            }

            mainStack.Children.Add(button);
            card.Child = mainStack;

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
                Foreground = Brushes.White
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
                Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158))
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
                Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            tooltipStack.Children.Add(descText);

            // 다음 레벨 정보
            if (!upgrade.IsMaxed)
            {
                var nextBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(13, 17, 23)),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var nextStack = new StackPanel();
                var nextLabel = new TextBlock
                {
                    Text = "다음 레벨 효과",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(110, 118, 129))
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

                // 비용
                var costText = new TextBlock
                {
                    Text = $"비용: 💎 {upgrade.Cost:N0}",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = upgrade.CanAfford
                        ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
                        : new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                tooltipStack.Children.Add(costText);
            }
            else
            {
                var maxText = new TextBlock
                {
                    Text = "최대 레벨 달성",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(96, 165, 250)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                tooltipStack.Children.Add(maxText);
            }

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
        /// 업그레이드 구매 버튼 클릭
        /// </summary>
        private void BuyUpgrade_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            string? upgradeId = button.Tag as string;
            if (string.IsNullOrEmpty(upgradeId))
                return;

            // ViewModel을 통해 구매 시도
            bool success = _viewModel.TryPurchaseUpgrade(upgradeId);

            if (success)
            {
                // 성공 시 UI 새로고침
                RefreshUI();
                LoadCategoryUpgrades(_currentCategory);

                // 성공 피드백
                PlayPurchaseAnimation(button);
            }
            else
            {
                // 실패 피드백
                PlayErrorAnimation(button);
            }
        }

        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
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
