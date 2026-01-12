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

        public StatisticsWindow(SaveManager saveManager, AchievementManager achievementManager, GameManager gameManager)
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
            LoadDashboard();
            LoadSessions();
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
            OverviewTab.Header = "DASHBOARD";
            SessionsTab.Header = loc["ui.statistics.tabs.sessions"];
            AchievementsTab.Header = loc["ui.statistics.tabs.achievements"];

            // Dashboard labels
            LblBestRecords.Text = "🏆 " + loc["ui.statistics.overview.lifetimeRecords"];
            LblMaxLevel.Text = loc["ui.statistics.labels.maxLevel"];
            LblMaxHit.Text = loc["ui.statistics.labels.maxHit"];
            LblTotalGoldEarned.Text = loc["ui.statistics.labels.totalGoldEarned"];
            LblLevelProgress.Text = "📈 " + loc["ui.statistics.labels.levelProgress"];
            LblTodayStats.Text = "🔥 " + loc["ui.statistics.overview.currentSession"];
            LblSessionKills.Text = loc["ui.statistics.labels.kills"];
            LblSessionDamage.Text = loc["ui.statistics.labels.damage"];
            LblCriticalHits.Text = loc["ui.statistics.labels.criticalHits"];
            LblBossKills.Text = loc["ui.statistics.labels.bossesDefeated"];
            LblInputRatio.Text = "⌨️ vs 🖱️ " + loc["ui.statistics.labels.inputRatio"];

            // Sessions Tab
            LblBestSession.Text = loc["ui.statistics.sessions.bestSession"];
            LblBestLevel.Text = loc["ui.statistics.labels.level"];
            LblBestDamage.Text = loc["ui.statistics.labels.damage"];
            LblBestDuration.Text = loc["ui.statistics.labels.duration"];
            LblCurrentVsAverage.Text = loc["ui.statistics.sessions.currentVsAverage"];
            LblCompareLevel.Text = loc["ui.statistics.labels.level"];
            LblCompareDamage.Text = loc["ui.statistics.labels.damage"];
            LblCompareGold.Text = loc["ui.statistics.labels.gold"];
            LblRecentSessions.Text = loc["ui.statistics.sessions.recentSessions"];

            // Close Button
            CloseButton.Content = loc["ui.common.close"];
        }

        #region Dashboard Tab

        private void LoadDashboard()
        {
            var stats = _saveManager.CurrentSave.Stats;
            var lifetime = _saveManager.CurrentSave.LifetimeStats;

            // Best Records
            TxtMaxLevel.Text = $"{stats.MaxLevel}";
            TxtMaxDamage.Text = FormatNumber(stats.MaxDamage);
            TxtTotalGold.Text = FormatNumber(lifetime.TotalGoldEarned);

            // Today's Stats Cards
            TxtSessionKills.Text = $"{_gameManager.SessionKills}";
            TxtSessionDamage.Text = FormatNumber(_gameManager.SessionDamage);
            TxtCriticalHits.Text = $"{_gameManager.SessionCriticalHits}";
            TxtBossKills.Text = $"{_gameManager.SessionBossKills}";

            // Achievement Badge
            var (unlocked, total) = _achievementManager.GetAchievementStats();
            AchievementBadge.Text = $" ({unlocked}/{total})";

            // Draw Charts
            DrawLevelGraph();
            DrawInputDonut();
            UpdateDayLabels();
        }

        private void DrawLevelGraph()
        {
            LevelGraphCanvas.Children.Clear();

            // Get recent sessions for last 7 days
            var recentSessions = _saveManager.GetRecentSessions(30);
            var last7Days = new List<int>();

            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                var daySession = recentSessions
                    .Where(s => s.StartTime.Date == date)
                    .OrderByDescending(s => s.MaxLevel)
                    .FirstOrDefault();

                last7Days.Add(daySession?.MaxLevel ?? 0);
            }

            // Add current session if today
            if (last7Days.Count > 0 && _gameManager.CurrentLevel > last7Days[^1])
            {
                last7Days[^1] = _gameManager.CurrentLevel;
            }

            // If all zeros, show placeholder
            int maxLevel = last7Days.Max();
            if (maxLevel == 0) maxLevel = 10; // Default scale

            double width = LevelGraphCanvas.ActualWidth > 0 ? LevelGraphCanvas.ActualWidth : 300;
            double height = LevelGraphCanvas.ActualHeight > 0 ? LevelGraphCanvas.ActualHeight : 80;
            double stepX = width / 6; // 7 points = 6 segments
            double padding = 10;

            // Draw grid lines
            for (int i = 0; i <= 3; i++)
            {
                double y = padding + (height - 2 * padding) * (1 - i / 3.0);
                var gridLine = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                LevelGraphCanvas.Children.Add(gridLine);
            }

            // Draw line graph - align points with column centers
            var points = new PointCollection();
            double columnWidth = width / 7; // 7 columns for 7 days
            for (int i = 0; i < 7; i++)
            {
                double x = (i * columnWidth) + (columnWidth / 2); // Center of each column
                double normalizedY = maxLevel > 0 ? (double)last7Days[i] / maxLevel : 0;
                double y = height - padding - (normalizedY * (height - 2 * padding));
                points.Add(new Point(x, y));
            }

            // Gradient line
            var polyline = new Polyline
            {
                Points = points,
                Stroke = new LinearGradientBrush(
                    Color.FromRgb(136, 255, 255),
                    Color.FromRgb(136, 255, 136),
                    90),
                StrokeThickness = 3,
                StrokeLineJoin = PenLineJoin.Round
            };
            LevelGraphCanvas.Children.Add(polyline);

            // Draw points
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                
                // Glow effect
                var glow = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = new RadialGradientBrush(
                        Color.FromArgb(100, 136, 255, 255),
                        Colors.Transparent)
                };
                Canvas.SetLeft(glow, point.X - 6);
                Canvas.SetTop(glow, point.Y - 6);
                LevelGraphCanvas.Children.Add(glow);

                // Point
                var ellipse = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Color.FromRgb(136, 255, 255)),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(ellipse, point.X - 4);
                Canvas.SetTop(ellipse, point.Y - 4);
                LevelGraphCanvas.Children.Add(ellipse);

                // Value label (only show if > 0)
                if (last7Days[i] > 0)
                {
                    var label = new TextBlock
                    {
                        Text = last7Days[i].ToString(),
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = 9,
                        FontWeight = FontWeights.Bold
                    };
                    Canvas.SetLeft(label, point.X - 6);
                    Canvas.SetTop(label, point.Y - 20);
                    LevelGraphCanvas.Children.Add(label);
                }
            }
        }

        private void DrawInputDonut()
        {
            InputDonutCanvas.Children.Clear();

            var lifetime = _saveManager.CurrentSave.LifetimeStats;
            long keyboardInputs = lifetime.KeyboardInputs + _gameManager.SessionKeyboardInputs;
            long mouseInputs = lifetime.MouseInputs + _gameManager.SessionMouseInputs;
            long total = keyboardInputs + mouseInputs;

            if (total == 0)
            {
                // Draw empty donut
                DrawEmptyDonut();
                TxtKeyboardPercent.Text = "0% Keyboard";
                TxtMousePercent.Text = "0% Mouse";
                return;
            }

            double keyboardPercent = (double)keyboardInputs / total * 100;
            double mousePercent = 100 - keyboardPercent;

            TxtKeyboardPercent.Text = $"{keyboardPercent:F0}% Keyboard";
            TxtMousePercent.Text = $"{mousePercent:F0}% Mouse";

            double centerX = 40;
            double centerY = 40;
            double outerRadius = 35;
            double innerRadius = 20;

            // Keyboard arc (cyan)
            double keyboardAngle = (keyboardPercent / 100) * 360;
            DrawArc(centerX, centerY, outerRadius, innerRadius, 0, keyboardAngle,
                Color.FromRgb(136, 255, 255));

            // Mouse arc (magenta)
            DrawArc(centerX, centerY, outerRadius, innerRadius, keyboardAngle, 360 - keyboardAngle,
                Color.FromRgb(255, 136, 255));
        }

        private void DrawEmptyDonut()
        {
            double centerX = 40;
            double centerY = 40;
            double outerRadius = 35;
            double innerRadius = 20;

            var ellipse = new Ellipse
            {
                Width = outerRadius * 2,
                Height = outerRadius * 2,
                Stroke = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                StrokeThickness = outerRadius - innerRadius,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(ellipse, centerX - outerRadius);
            Canvas.SetTop(ellipse, centerY - outerRadius);
            InputDonutCanvas.Children.Add(ellipse);
        }

        private void DrawArc(double centerX, double centerY, double outerRadius, double innerRadius,
            double startAngle, double sweepAngle, Color color)
        {
            if (sweepAngle <= 0) return;

            // Convert to radians
            double startRad = (startAngle - 90) * Math.PI / 180;
            double endRad = (startAngle + sweepAngle - 90) * Math.PI / 180;

            // Calculate points
            Point outerStart = new Point(
                centerX + outerRadius * Math.Cos(startRad),
                centerY + outerRadius * Math.Sin(startRad));
            Point outerEnd = new Point(
                centerX + outerRadius * Math.Cos(endRad),
                centerY + outerRadius * Math.Sin(endRad));
            Point innerStart = new Point(
                centerX + innerRadius * Math.Cos(endRad),
                centerY + innerRadius * Math.Sin(endRad));
            Point innerEnd = new Point(
                centerX + innerRadius * Math.Cos(startRad),
                centerY + innerRadius * Math.Sin(startRad));

            bool isLargeArc = sweepAngle > 180;

            var pathFigure = new PathFigure { StartPoint = outerStart };

            // Outer arc
            pathFigure.Segments.Add(new ArcSegment
            {
                Point = outerEnd,
                Size = new Size(outerRadius, outerRadius),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise
            });

            // Line to inner
            pathFigure.Segments.Add(new LineSegment { Point = innerStart });

            // Inner arc (reverse direction)
            pathFigure.Segments.Add(new ArcSegment
            {
                Point = innerEnd,
                Size = new Size(innerRadius, innerRadius),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Counterclockwise
            });

            // Close path
            pathFigure.IsClosed = true;

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            var path = new Path
            {
                Data = pathGeometry,
                Fill = new SolidColorBrush(color)
            };

            InputDonutCanvas.Children.Add(path);
        }

        private void UpdateDayLabels()
        {
            var dayLabels = new[] { Day1Label, Day2Label, Day3Label, Day4Label, Day5Label, Day6Label, Day7Label };
            var dayNames = new[] { "일", "월", "화", "수", "목", "금", "토" };

            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.Today.AddDays(-6 + i);
                dayLabels[i].Text = dayNames[(int)date.DayOfWeek];
            }
        }

        #endregion

        #region Sessions Tab

        private void LoadSessions()
        {
            var summary = _saveManager.GetSessionSummary();
            var recentSessions = _saveManager.GetRecentSessions(10);

            // Best Session
            var loc = LocalizationManager.Instance;
            if (summary.BestSession != null)
            {
                var best = summary.BestSession;
                TxtBestSessionDate.Text = $" - {best.StartTime:yyyy.MM.dd HH:mm}";
                TxtBestLevel.Text = $"{best.MaxLevel}";
                TxtBestDamage.Text = FormatNumber(best.TotalDamage);
                TxtBestDuration.Text = $"{(int)best.DurationMinutes}m";
            }
            else
            {
                TxtBestSessionDate.Text = $" - {loc["ui.statistics.sessions.noSessionsYet"]}";
                TxtBestLevel.Text = "0";
                TxtBestDamage.Text = "0";
                TxtBestDuration.Text = "0m";
            }

            // Current vs Average
            UpdateComparison(summary);

            // Recent Sessions
            LoadRecentSessions(recentSessions);
        }

        private void UpdateComparison(SessionStatsSummary summary)
        {
            if (summary.TotalSessions == 0)
            {
                SetComparisonBar(LevelCompareBar, TxtLevelCompare, 0, "#88FF88");
                SetComparisonBar(DamageCompareBar, TxtDamageCompare, 0, "#FF8888");
                SetComparisonBar(GoldCompareBar, TxtGoldCompare, 0, "Gold");
                return;
            }

            double levelPercent = summary.AverageLevel > 0
                ? ((_gameManager.CurrentLevel - summary.AverageLevel) / summary.AverageLevel) * 100
                : 0;
            SetComparisonBar(LevelCompareBar, TxtLevelCompare, levelPercent, "#88FF88");

            double damagePercent = summary.AverageDamage > 0
                ? ((_gameManager.SessionDamage - summary.AverageDamage) / summary.AverageDamage) * 100
                : 0;
            SetComparisonBar(DamageCompareBar, TxtDamageCompare, damagePercent, "#FF8888");

            double goldPercent = summary.AverageGold > 0
                ? ((_gameManager.SessionTotalGold - summary.AverageGold) / summary.AverageGold) * 100
                : 0;
            SetComparisonBar(GoldCompareBar, TxtGoldCompare, goldPercent, "Gold");
        }

        private void SetComparisonBar(Border bar, TextBlock text, double percent, string color)
        {
            percent = Math.Max(-100, Math.Min(200, percent));

            double barPercent = 50 + (percent / 4);
            barPercent = Math.Max(5, Math.Min(100, barPercent));

            var parent = bar.Parent as Grid;
            if (parent != null)
            {
                double maxWidth = parent.ActualWidth > 0 ? parent.ActualWidth : 300;
                bar.Width = maxWidth * (barPercent / 100);
            }

            string sign = percent >= 0 ? "+" : "";
            text.Text = $"{sign}{percent:F0}%";

            if (percent >= 0)
            {
                text.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
            }
            else
            {
                text.Foreground = new SolidColorBrush(Colors.Red);
            }
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

        #endregion

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
