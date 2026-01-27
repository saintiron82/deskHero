using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DeskWarrior.Interfaces;
using DeskWarrior.Managers;
using DeskWarrior.Models;

namespace DeskWarrior.Windows
{
    public partial class StatisticsWindow : Window
    {
        private readonly SaveManager _saveManager;
        private readonly AchievementManager _achievementManager;
        private readonly GameManager _gameManager;
        private string _currentFilter = "24H";

        public StatisticsWindow(SaveManager saveManager, AchievementManager achievementManager, GameManager gameManager)
        {
            InitializeComponent();
            _saveManager = saveManager;
            _achievementManager = achievementManager;
            _gameManager = gameManager;

            Loaded += OnLoaded;
            Closed += OnClosed;
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

            // 데미지 이벤트 구독 (실시간 업데이트)
            _gameManager.DamageDealt += OnDamageDealt;
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            // 이벤트 구독 해제
            _gameManager.DamageDealt -= OnDamageDealt;
        }

        private void OnDamageDealt(object? sender, DamageEventArgs e)
        {
            // 데미지 미터 탭이 보이면 실시간 업데이트
            if (DamageMeterContent?.Visibility == Visibility.Visible)
            {
                Dispatcher.Invoke(LoadDamageMeter);
            }
        }

        private void UpdateLocalizedUI()
        {
            var loc = LocalizationManager.Instance;
            bool isKo = loc.CurrentLanguage == "ko-KR";

            // 타이틀 (emoji 제외)
            TitleText.Text = loc["ui.statistics.title"].Replace("📊 ", "").Replace("📊", "");

            // 탭 헤더
            TabBattleRecordText.Text = loc["ui.statistics.tabs.battleRecord"];
            TabAchievementsText.Text = loc["ui.statistics.tabs.achievements"];

            // Time range
            LblTimeRange.Text = loc["ui.statistics.labels.timeRange"];
            BtnRange1H.Content = isKo ? "1시간" : "1H";
            BtnRange24H.Content = isKo ? "24시간" : "24H";
            BtnRange7D.Content = isKo ? "7일" : "7D";

            // BEST 섹션
            LblBestRecord.Text = isKo ? "최고 기록" : "BEST";
            LblBestKeyboard.Text = "⌨️";
            LblBestMouse.Text = "🖱️";
            LblBestLevel.Text = "LV";
            LblBestKills.Text = "KILL";
            LblBestDamage.Text = "DMG";
            LblBestGold.Text = "GOLD";
            LblBestCrystals.Text = "💎";
            LblBestIPM.Text = "IPM";

            // RATIO 섹션
            LblInputRatio.Text = isKo ? "비율" : "RATIO";

            // CUMULATIVE 섹션
            LblCumulative.Text = isKo ? "누적" : "CUMULATIVE";
            LblCumKeyboard.Text = "⌨️";
            LblCumMouse.Text = "🖱️";
            LblCumLevel.Text = "LV";
            LblCumKills.Text = "KILL";
            LblCumDamage.Text = "DMG";
            LblCumGold.Text = "GOLD";
            LblCumCrystals.Text = "💎";
            LblCumIPM.Text = "IPM";

            // Close Button
            CloseButton.Content = loc["ui.common.close"];

            // Help Button Tooltip
            HelpButton.ToolTip = loc["ui.help.title"];

            // Damage Meter 탭
            TabDamageMeterText.Text = isKo ? "데미지 미터" : "DMG METER";
            LblDmRecordCount.Text = isKo ? "기록 수" : "RECORDS";
            LblDmAvgDamage.Text = isKo ? "평균" : "AVG";
            LblDmMaxDamage.Text = isKo ? "최대" : "MAX";
            LblDmRecentHits.Text = isKo ? "최근 히트" : "RECENT HITS";
            LblDmLegend.Text = isKo ? "범례" : "LEGEND";
            TxtDmNoRecords.Text = isKo ? "데미지 기록 없음" : "No damage records yet";
        }

        #region Tab Navigation

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton radioButton)
                return;

            // 탭 콘텐츠 전환
            if (radioButton.Name == "TabBattleRecord")
            {
                if (BattleRecordContent != null)
                    BattleRecordContent.Visibility = Visibility.Visible;
                if (AchievementsContent != null)
                    AchievementsContent.Visibility = Visibility.Collapsed;
                if (DamageMeterContent != null)
                    DamageMeterContent.Visibility = Visibility.Collapsed;
            }
            else if (radioButton.Name == "TabAchievements")
            {
                if (BattleRecordContent != null)
                    BattleRecordContent.Visibility = Visibility.Collapsed;
                if (AchievementsContent != null)
                    AchievementsContent.Visibility = Visibility.Visible;
                if (DamageMeterContent != null)
                    DamageMeterContent.Visibility = Visibility.Collapsed;
            }
            else if (radioButton.Name == "TabDamageMeter")
            {
                if (BattleRecordContent != null)
                    BattleRecordContent.Visibility = Visibility.Collapsed;
                if (AchievementsContent != null)
                    AchievementsContent.Visibility = Visibility.Collapsed;
                if (DamageMeterContent != null)
                    DamageMeterContent.Visibility = Visibility.Visible;

                LoadDamageMeter();
            }
        }

        #endregion

        #region Filter Buttons

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

            long totalKills = 0;
            int maxLevel = 0;
            long keyboardInputs = 0;
            long mouseInputs = 0;
            long totalDamage = 0;
            long totalGold = 0;
            int totalCrystals = 0;

            List<SessionStats> filteredSessions = new();

            DateTime cutoff = filter switch
            {
                "1H" => DateTime.Now.AddHours(-1),
                "24H" => DateTime.Now.AddHours(-24),
                "7D" => DateTime.Now.AddDays(-7),
                _ => DateTime.Now.AddHours(-24)
            };

            filteredSessions = sessions.Where(s => s.EndTime >= cutoff).ToList();

            // 현재 진행 중인 세션도 통계에 포함
            bool includeCurrentSession = _gameManager.SessionStartTime >= cutoff;
            if (includeCurrentSession)
            {
                totalKills = filteredSessions.Sum(s => (long)s.MonstersKilled) + _gameManager.SessionKills;
                maxLevel = Math.Max(
                    filteredSessions.Any() ? filteredSessions.Max(s => s.MaxLevel) : 0,
                    _gameManager.CurrentLevel);
                keyboardInputs = filteredSessions.Sum(s => (long)s.KeyboardInputs) + _gameManager.SessionKeyboardInputs;
                mouseInputs = filteredSessions.Sum(s => (long)s.MouseInputs) + _gameManager.SessionMouseInputs;
                totalDamage = filteredSessions.Sum(s => s.TotalDamage) + _gameManager.SessionDamage;
                totalGold = filteredSessions.Sum(s => s.TotalGold) + _gameManager.SessionTotalGold;
                totalCrystals = _gameManager.SessionBossDropCrystals + _gameManager.SessionAchievementCrystals;
            }
            else
            {
                totalKills = filteredSessions.Sum(s => (long)s.MonstersKilled);
                maxLevel = filteredSessions.Any() ? filteredSessions.Max(s => s.MaxLevel) : 0;
                keyboardInputs = filteredSessions.Sum(s => (long)s.KeyboardInputs);
                mouseInputs = filteredSessions.Sum(s => (long)s.MouseInputs);
                totalDamage = filteredSessions.Sum(s => s.TotalDamage);
                totalGold = filteredSessions.Sum(s => s.TotalGold);
                totalCrystals = 0; // 과거 세션의 크리스탈 정보 없음
            }

            // 누적 총 플레이 시간 계산 (분)
            double totalMinutes = filteredSessions.Sum(s => (s.EndTime - s.StartTime).TotalMinutes);
            if (includeCurrentSession)
            {
                totalMinutes += (DateTime.Now - _gameManager.SessionStartTime).TotalMinutes;
            }

            // IPM 계산
            double cumulativeIPM = totalMinutes > 0 ? (keyboardInputs + mouseInputs) / totalMinutes : 0;

            // CUMULATIVE 업데이트
            TxtCumKeyboard.Text = FormatNumber(keyboardInputs);
            TxtCumMouse.Text = FormatNumber(mouseInputs);
            TxtCumLevel.Text = $"{maxLevel}";
            TxtCumKills.Text = FormatNumber(totalKills);
            TxtCumDamage.Text = FormatNumber(totalDamage);
            TxtCumGold.Text = FormatNumber(totalGold);
            TxtCumCrystals.Text = FormatNumber(totalCrystals);
            TxtCumIPM.Text = $"{cumulativeIPM:F0}";

            // RATIO 업데이트
            UpdateInputRatio(keyboardInputs, mouseInputs);

            // BEST 업데이트
            LoadBestRecord(filteredSessions);
        }

        private void UpdateFilterButtons(string filter)
        {
            SetButtonStyle(BtnRange1H, filter == "1H");
            SetButtonStyle(BtnRange24H, filter == "24H");
            SetButtonStyle(BtnRange7D, filter == "7D");
        }

        private void SetButtonStyle(Button btn, bool isActive)
        {
            if (isActive)
            {
                // Active: #0099CC (상점과 동일한 강조색)
                btn.Background = new SolidColorBrush(Color.FromRgb(0, 153, 204));
                btn.Foreground = new SolidColorBrush(Colors.White);
                btn.FontWeight = FontWeights.Bold;
            }
            else
            {
                // Inactive: Glass morphism style
                btn.Background = new SolidColorBrush(Color.FromArgb(0x44, 255, 255, 255));
                btn.Foreground = new SolidColorBrush(Colors.White);
                btn.FontWeight = FontWeights.Bold;
            }
        }

        private void UpdateInputRatio(long keyboard, long mouse)
        {
            long total = keyboard + mouse;

            if (total == 0)
            {
                TxtKeyboardPercent.Text = "0%";
                TxtMousePercent.Text = "0%";
                return;
            }

            double kp = (double)keyboard / total * 100.0;
            double mp = 100.0 - kp;

            TxtKeyboardPercent.Text = $"{kp:F0}%";
            TxtMousePercent.Text = $"{mp:F0}%";
        }

        #endregion

        #region Best Record

        private void LoadBestRecord(List<SessionStats> sessions)
        {
            var loc = LocalizationManager.Instance;

            if (sessions.Count == 0)
            {
                // 기록 없음
                TxtNoRecord.Text = loc["ui.statistics.noRecord"];
                TxtNoRecord.Visibility = Visibility.Visible;
                BestRecordGrid.Visibility = Visibility.Collapsed;
                TxtBestIPM.Text = "0";
                return;
            }

            // 기록 있음 - UI 표시
            TxtNoRecord.Visibility = Visibility.Collapsed;
            BestRecordGrid.Visibility = Visibility.Visible;

            // 기간 내 최고 레벨 세션 찾기 (동점시 입력수로 정렬)
            var best = sessions.OrderByDescending(s => s.MaxLevel)
                               .ThenByDescending(s => s.KeyboardInputs + s.MouseInputs)
                               .First();

            // 세션 플레이 시간 (분)
            double sessionMinutes = (best.EndTime - best.StartTime).TotalMinutes;
            double bestIPM = sessionMinutes > 0 ? (best.KeyboardInputs + best.MouseInputs) / sessionMinutes : 0;

            // 값 설정
            TxtBestKeyboard.Text = FormatNumber(best.KeyboardInputs);
            TxtBestMouse.Text = FormatNumber(best.MouseInputs);
            TxtBestLevel.Text = best.MaxLevel.ToString();
            TxtBestKills.Text = FormatNumber(best.MonstersKilled);
            TxtBestDamage.Text = FormatNumber(best.TotalDamage);
            TxtBestGold.Text = FormatNumber(best.TotalGold);
            TxtBestCrystals.Text = "-"; // 세션별 크리스탈 정보 없음
            TxtBestIPM.Text = $"{bestIPM:F0}";
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
                    Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
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
                    : new SolidColorBrush(Color.FromArgb(0x22, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 2, 0, 2)
            };

            var stack = new StackPanel();

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };

            var icon = new TextBlock
            {
                Text = def.IsHidden && !isUnlocked ? "?" : def.Icon,
                FontSize = 18,
                Margin = new Thickness(0, 0, 10, 0)
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
                    Text = " OK",
                    Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)), // #10B981
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
                Margin = new Thickness(28, 4, 0, 0)
            };
            stack.Children.Add(description);

            if (!isUnlocked && !def.IsHidden)
            {
                var progressGrid = new Grid
                {
                    Height = 6,
                    Margin = new Thickness(28, 8, 0, 0)
                };

                var bgBar = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0x44, 255, 255, 255)),
                    CornerRadius = new CornerRadius(3)
                };
                progressGrid.Children.Add(bgBar);

                var fgBar = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0, 153, 204)), // #0099CC
                    CornerRadius = new CornerRadius(3),
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
                    Margin = new Thickness(28, 4, 0, 0)
                };
                stack.Children.Add(progressText);
            }

            border.Child = stack;
            return border;
        }

        #endregion

        #region Damage Meter Tab

        private void LoadDamageMeter()
        {
            var records = _gameManager.SessionDamageRecords?.ToList() ?? new List<DamageRecord>();

            if (records.Count == 0)
            {
                TxtDmNoRecords.Visibility = Visibility.Visible;
                DamageRecordsList.Visibility = Visibility.Collapsed;
                TxtDmRecordCount.Text = "0";
                TxtDmAvgDamage.Text = "0";
                TxtDmMaxDamage.Text = "0";
                return;
            }

            TxtDmNoRecords.Visibility = Visibility.Collapsed;
            DamageRecordsList.Visibility = Visibility.Visible;

            // Summary 계산
            int totalRecords = records.Count;
            double avgDamage = records.Average(r => r.FinalDamage);
            int maxDamage = records.Max(r => r.FinalDamage);

            TxtDmRecordCount.Text = totalRecords.ToString();
            TxtDmAvgDamage.Text = FormatNumber((long)avgDamage);
            TxtDmMaxDamage.Text = FormatNumber(maxDamage);

            // 최근 기록 (역순으로 - 최신이 위로)
            var displayItems = records.AsEnumerable().Reverse().Select(r => new DamageRecordDisplayItem(r)).ToList();
            DamageRecordsList.ItemsSource = displayItems;
        }

        #endregion

        #region Window Controls

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var loc = LocalizationManager.Instance;
            var helpContent = loc.CurrentLanguage == "ko-KR"
                ? "통계 창에서 게임 기록을 확인할 수 있습니다.\n\n" +
                  "• 전투 기록: 시간대별 플레이 통계\n" +
                  "• 업적: 달성한 업적 목록\n" +
                  "• 시간 필터로 기간별 통계 확인 가능"
                : "View your game statistics here.\n\n" +
                  "• Battle Record: Play statistics by time period\n" +
                  "• Achievements: List of unlocked achievements\n" +
                  "• Use time filters to view stats by period";

            var helpPopup = new HelpPopup(loc["ui.statistics.title"], helpContent);
            helpPopup.Owner = this;
            helpPopup.ShowDialog();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
    }

    /// <summary>
    /// Breakdown 파트 (텍스트 + 색상)
    /// </summary>
    public class BreakdownPart
    {
        public string Text { get; }
        public Brush Color { get; }

        public BreakdownPart(string text, Brush color)
        {
            Text = text;
            Color = color;
        }
    }

    /// <summary>
    /// 데미지 기록 표시용 뷰모델
    /// </summary>
    public class DamageRecordDisplayItem
    {
        // 색상 상수 (범례와 동일)
        private static readonly Brush ColorBase = new SolidColorBrush(Color.FromRgb(136, 136, 136));    // #888888
        private static readonly Brush ColorAtk = new SolidColorBrush(Color.FromRgb(0, 153, 204));       // #0099CC
        private static readonly Brush ColorMult = new SolidColorBrush(Color.FromRgb(16, 185, 129));     // #10B981
        private static readonly Brush ColorCrit = new SolidColorBrush(Color.FromRgb(255, 107, 107));    // #FF6B6B
        private static readonly Brush ColorMultiHit = new SolidColorBrush(Color.FromRgb(255, 215, 0));  // #FFD700
        private static readonly Brush ColorCombo = new SolidColorBrush(Color.FromRgb(255, 105, 180));   // #FF69B4

        public string InputIcon { get; }
        public List<BreakdownPart> BreakdownParts { get; }
        public string FinalDamageText { get; }
        public Brush DamageColor { get; }

        public DamageRecordDisplayItem(DamageRecord record)
        {
            InputIcon = record.IsMouse ? "🖱️" : "⌨️";
            BreakdownParts = BuildBreakdownParts(record);
            FinalDamageText = record.FinalDamage.ToString("N0");

            // 데미지 크기에 따른 색상
            DamageColor = record.FinalDamage switch
            {
                >= 1000 => new SolidColorBrush(Color.FromRgb(255, 107, 107)), // Red for high damage
                >= 500 => new SolidColorBrush(Color.FromRgb(255, 215, 0)),   // Gold for medium-high
                >= 100 => new SolidColorBrush(Color.FromRgb(0, 153, 204)),   // Blue for medium
                _ => new SolidColorBrush(Colors.White)                        // White for low
            };
        }

        private static List<BreakdownPart> BuildBreakdownParts(DamageRecord record)
        {
            var parts = new List<BreakdownPart>();

            // Base power (회색)
            parts.Add(new BreakdownPart($"{record.BasePower}", ColorBase));

            // Base attack bonus (파랑 - +Atk)
            if (record.BaseAttackBonus > 0)
            {
                parts.Add(new BreakdownPart($"+{record.BaseAttackBonus}", ColorAtk));
            }

            // Attack multiplier (녹색 - ×Mult)
            if (record.AttackMultiplier > 0)
            {
                parts.Add(new BreakdownPart($"×{1 + record.AttackMultiplier:F1}", ColorMult));
            }

            // Critical (빨강 - ×Crit)
            if (record.IsCritical)
            {
                parts.Add(new BreakdownPart($"×{record.CritMultiplier:F1}!", ColorCrit));
            }

            // Multi-hit (금색 - ×Multi)
            if (record.IsMultiHit)
            {
                parts.Add(new BreakdownPart("×2", ColorMultiHit));
            }

            // Combo (핑크 - ×Combo)
            if (record.IsCombo)
            {
                int comboMult = (int)Math.Pow(2, record.ComboStack);
                parts.Add(new BreakdownPart($"×{comboMult}C", ColorCombo));
            }

            return parts;
        }
    }
}
