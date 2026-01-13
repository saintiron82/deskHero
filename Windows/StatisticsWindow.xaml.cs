using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DeskWarrior.Managers;
using DeskWarrior.Models;

namespace DeskWarrior.Windows
{
    public partial class StatisticsWindow : Window
    {
        private readonly SaveManager _saveManager;
        private readonly AchievementManager _achievementManager;
        private readonly GameManager _gameManager;
        private string _currentFilter = "24H";        public StatisticsWindow(SaveManager saveManager, AchievementManager achievementManager, GameManager gameManager)
        {
            InitializeComponent();
            _saveManager = saveManager;
            _achievementManager = achievementManager;
            _gameManager = gameManager;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateLocalizedUI();
            LoadBattleRecord("24H");
            LoadAchievements();

            // 언어 변경 이벤트 구독
            LocalizationManager.Instance.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == "Item[]")
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateLocalizedUI();
                        LoadAchievements();
                    });
                }
            };
        }

        private void UpdateLocalizedUI()
        {
            var loc = LocalizationManager.Instance;

            // 타이틀
            TitleText.Text = "📊 " + loc["ui.statistics.title"];

            // 탭 헤더
            BattleRecordTab.Header = loc.CurrentLanguage == "ko-KR" ? "전투 기록" : "BATTLE RECORD";
            AchievementsTab.Header = loc["ui.statistics.tabs.achievements"];

            // Dashboard labels
            LblSummaryTitle.Text = "📊 " + loc["ui.statistics.overview.cumulativeStats"];
            LblSummaryKills.Text = loc["ui.statistics.labels.kills"];
            LblSummaryLevel.Text = loc["ui.statistics.labels.maxLevel"];
            LblSummaryDamage.Text = loc["ui.statistics.labels.totalDamage"];
            LblSummaryGold.Text = loc["ui.statistics.labels.totalGoldEarned"];
            LblInputRatio.Text = loc["ui.statistics.labels.inputRatio"];

            BtnRange1H.Content = loc.CurrentLanguage == "ko-KR" ? "1시간" : "Last 1H";
            BtnRange24H.Content = loc.CurrentLanguage == "ko-KR" ? "24시간" : "Last 24H";
            BtnRangeAll.Content = loc.CurrentLanguage == "ko-KR" ? "전체" : "All Time";

            LblRecentSessions.Text = loc["ui.statistics.sessions.recentSessions"];

            // Close Button
            CloseButton.Content = loc["ui.common.close"];
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string filter)
            {
                _currentFilter = filter;
                LoadBattleRecord(filter);
            }
        }

        private void LoadBattleRecord(string filter)
        {
            UpdateFilterButtons(filter);

            var sessions = _saveManager.GetRecentSessions(200);
            var lifetime = _saveManager.CurrentSave.LifetimeStats;
            
            long totalKills = 0;
            long totalGold = 0;
            long totalDamage = 0;
            int maxLevel = 0;
            
            long keyboardInputs = 0;
            long mouseInputs = 0;

            List<SessionStats> filteredSessions = new();

            if (filter == "All")
            {
                totalKills = _saveManager.CurrentSave.Stats.MonsterKills;
                totalGold = lifetime.TotalGoldEarned;
                totalDamage = _saveManager.CurrentSave.Stats.TotalDamage;
                maxLevel = _saveManager.CurrentSave.Stats.MaxLevel;
                
                keyboardInputs = lifetime.KeyboardInputs;
                mouseInputs = lifetime.MouseInputs;
                
                filteredSessions = sessions;
            }
            else
            {
                DateTime cutoff = filter == "1H" ? DateTime.Now.AddHours(-1) : DateTime.Now.AddHours(-24);
                filteredSessions = sessions.Where(s => s.EndTime >= cutoff).ToList();
                
                totalKills = filteredSessions.Sum(s => (long)s.MonstersKilled);
                totalGold = filteredSessions.Sum(s => s.TotalGold);
                totalDamage = filteredSessions.Sum(s => s.TotalDamage);
                maxLevel = filteredSessions.Any() ? filteredSessions.Max(s => s.MaxLevel) : 0;
                
                keyboardInputs = filteredSessions.Sum(s => (long)s.KeyboardInputs);
                mouseInputs = filteredSessions.Sum(s => (long)s.MouseInputs);
            }

            TxtSummaryKills.Text = FormatNumber(totalKills);
            TxtSummaryGold.Text = FormatNumber(totalGold);
            TxtSummaryDamage.Text = FormatNumber(totalDamage);
            TxtSummaryLevel.Text = $"{maxLevel}";

            // Update Input Ratio
            UpdateInputRatio(keyboardInputs, mouseInputs);

            // Update List
            LoadRecentSessions(filteredSessions);
        }

        private void UpdateFilterButtons(string filter)
        {
            SetButtonStyle(BtnRange1H, filter == "1H");
            SetButtonStyle(BtnRange24H, filter == "24H");
            SetButtonStyle(BtnRangeAll, filter == "All");
        }

        private void SetButtonStyle(Button btn, bool isActive)
        {
            if (isActive)
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(255, 170, 0)); // Orange
                btn.Foreground = new SolidColorBrush(Colors.Black);
                btn.FontWeight = FontWeights.Bold;
            }
            else
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(51, 51, 51)); // Dark Gray
                btn.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)); // Light Gray
                btn.FontWeight = FontWeights.Normal;
            }
        }

        private void UpdateInputRatio(long keyboard, long mouse)
        {
            long total = keyboard + mouse;
            var loc = LocalizationManager.Instance;

            if (total == 0)
            {
                TxtKeyboardPercent.Text = $"0% {loc["ui.statistics.labels.keyboard"]}";
                TxtMousePercent.Text = $"0% {loc["ui.statistics.labels.mouse"]}";
                return;
            }

            double kp = (double)keyboard / total * 100.0;
            double mp = 100.0 - kp;

            TxtKeyboardPercent.Text = $"{kp:F0}% {loc["ui.statistics.labels.keyboard"]}";
            TxtMousePercent.Text = $"{mp:F0}% {loc["ui.statistics.labels.mouse"]}";
        }






        private void LoadRecentSessions(List<SessionStats> sessions)
        {
            RecentSessionsPanel.Children.Clear();

            if (sessions.Count == 0)
            {
                RecentSessionsPanel.Children.Add(new TextBlock
                {
                    Text = LocalizationManager.Instance["ui.statistics.sessions.noSessionsRecorded"],
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                return;
            }

            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                var row = CreateSessionRow(i + 1, session);
                RecentSessionsPanel.Children.Add(row);
            }
        }

        private Border CreateSessionRow(int index, SessionStats session)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(34, 255, 255, 255)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 2, 0, 2)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var indexText = new TextBlock
            {
                Text = $"#{index}",
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(indexText, 0);
            grid.Children.Add(indexText);

            var levelText = new TextBlock
            {
                Text = $"Lv.{session.MaxLevel}",
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(levelText, 1);
            grid.Children.Add(levelText);

            var goldText = new TextBlock
            {
                Text = $"{session.TotalGold:N0}G",
                Foreground = new SolidColorBrush(Colors.Gold),
                FontSize = 11,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(goldText, 2);
            grid.Children.Add(goldText);

            var durationText = new TextBlock
            {
                Text = $"{(int)session.DurationMinutes}m",
                Foreground = new SolidColorBrush(Color.FromRgb(136, 255, 255)),
                FontSize = 11,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(durationText, 3);
            grid.Children.Add(durationText);

            var dateText = new TextBlock
            {
                Text = session.StartTime.ToString("MM/dd"),
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                FontSize = 10,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dateText, 4);
            grid.Children.Add(dateText);

            border.Child = grid;
            return border;
        }



        #region Achievements Tab

        private void LoadAchievements()
        {
            AchievementsPanel.Children.Clear();

            var loc = LocalizationManager.Instance;
            var definitions = _achievementManager.GetAllDefinitions();
            var categories = new[] { "progression", "combat", "wealth", "engagement", "secret" };
            var categoryNames = new Dictionary<string, string>
            {
                { "progression", loc["ui.statistics.achievements.categories.progression"] },
                { "combat", loc["ui.statistics.achievements.categories.combat"] },
                { "wealth", loc["ui.statistics.achievements.categories.wealth"] },
                { "engagement", loc["ui.statistics.achievements.categories.engagement"] },
                { "secret", loc["ui.statistics.achievements.categories.secret"] }
            };

            foreach (var category in categories)
            {
                var categoryAchievements = definitions.Where(d => d.Category == category).ToList();
                if (categoryAchievements.Count == 0) continue;

                var header = new TextBlock
                {
                    Text = categoryNames.GetValueOrDefault(category, category.ToUpper()),
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Margin = new Thickness(0, 10, 0, 8)
                };
                AchievementsPanel.Children.Add(header);

                foreach (var def in categoryAchievements)
                {
                    var row = CreateAchievementRow(def);
                    AchievementsPanel.Children.Add(row);
                }
            }
        }

        private Border CreateAchievementRow(AchievementDefinition def)
        {
            bool isUnlocked = _achievementManager.IsAlreadyUnlocked(def.Id);
            double progress = _achievementManager.GetProgress(def.Id);

            var border = new Border
            {
                Background = isUnlocked
                    ? new SolidColorBrush(Color.FromArgb(51, 255, 215, 0))
                    : new SolidColorBrush(Color.FromArgb(34, 255, 255, 255)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 2, 0, 2)
            };

            var stack = new StackPanel();

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };

            var icon = new TextBlock
            {
                Text = def.IsHidden && !isUnlocked ? "❓" : def.Icon,
                FontSize = 16,
                Margin = new Thickness(0, 0, 8, 0)
            };
            headerStack.Children.Add(icon);

            var name = new TextBlock
            {
                Text = def.IsHidden && !isUnlocked ? "???" : def.Name,
                Foreground = isUnlocked
                    ? new SolidColorBrush(Color.FromRgb(255, 215, 0))
                    : new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };
            headerStack.Children.Add(name);

            if (isUnlocked)
            {
                var checkmark = new TextBlock
                {
                    Text = " ✓",
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 255, 136)),
                    FontWeight = FontWeights.Bold
                };
                headerStack.Children.Add(checkmark);
            }

            stack.Children.Add(headerStack);

            var description = new TextBlock
            {
                Text = def.IsHidden && !isUnlocked ? "???" : def.Description,
                Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                FontSize = 10,
                Margin = new Thickness(24, 2, 0, 0)
            };
            stack.Children.Add(description);

            if (!isUnlocked && !def.IsHidden)
            {
                var progressGrid = new Grid
                {
                    Height = 4,
                    Margin = new Thickness(24, 6, 0, 0)
                };

                var bgBar = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    CornerRadius = new CornerRadius(2)
                };
                progressGrid.Children.Add(bgBar);

                var fgBar = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 170, 0)),
                    CornerRadius = new CornerRadius(2),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = Math.Max(0, progressGrid.ActualWidth * progress)
                };

                progressGrid.SizeChanged += (s, e) =>
                {
                    fgBar.Width = e.NewSize.Width * progress;
                };

                progressGrid.Children.Add(fgBar);
                stack.Children.Add(progressGrid);

                var currentProgress = _saveManager.GetAchievementProgress(def.Id)?.CurrentProgress ?? 0;
                var progressText = new TextBlock
                {
                    Text = $"{FormatNumber(currentProgress)} / {FormatNumber(def.Target)}",
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                    FontSize = 9,
                    Margin = new Thickness(24, 2, 0, 0)
                };
                stack.Children.Add(progressText);
            }

            border.Child = stack;
            return border;
        }

        #endregion

        #region Helpers

        private static string FormatNumber(long value)
        {
            if (value >= 1_000_000)
                return $"{value / 1_000_000.0:F1}M";
            if (value >= 1_000)
                return $"{value / 1_000.0:F1}K";
            return value.ToString("N0");
        }

        #endregion

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
