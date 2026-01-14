using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DeskWarrior.Helpers;
using DeskWarrior.Interfaces;
using DeskWarrior.Managers;
using DeskWarrior.Models;
using DeskWarrior.ViewModels;

namespace DeskWarrior
{
    /// <summary>
    /// 메인 윈도우 코드비하인드 (MVVM: View 역할)
    /// UI 렌더링과 애니메이션만 담당, 비즈니스 로직은 ViewModel에 위임
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constants

        private const double MONSTER_SIZE = 80;
        private const double BOSS_SIZE = 130;
        private const double HERO_SIZE = 100;

        #endregion

        #region Fields

        private readonly MainViewModel _viewModel;
        private readonly Random _random = new();

        private IntPtr _hwnd;
        private bool _isManageMode;
        private bool _isModeButtonVisible;

        // Auto Restart
        private System.Windows.Threading.DispatcherTimer _autoRestartTimer;
        private int _autoRestartCountdown;

        // Achievement Toast Queue
        private readonly Queue<AchievementDefinition> _toastQueue = new();
        private bool _isShowingToast;

        // Hero Sprite
        private HeroData? _currentHero;
        private System.Windows.Threading.DispatcherTimer? _heroAttackTimer;

        // Mode Button Hover Timer
        private System.Windows.Threading.DispatcherTimer? _hoverCheckTimer;

        #endregion

        #region Properties (ViewModel 접근용)

        private GameManager GameManager => _viewModel.GameManager;
        private SaveManager SaveManager => _viewModel.SaveManager;
        private SoundManager SoundManager => _viewModel.SoundManager;
        private TrayManager TrayManager => _viewModel.TrayManager;
        private AchievementManager AchievementManager => _viewModel.AchievementManager;
        private IInputHandler InputHandler => _viewModel.InputHandler;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            // ViewModel 생성 및 DataContext 설정
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // ViewModel 이벤트 구독
            SubscribeToViewModelEvents();

            // UI 전용 타이머 초기화
            InitializeUITimers();

            // 윈도우 이벤트 구독
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            LocationChanged += MainWindow_LocationChanged;

            // 초기 UI 업데이트
            UpdateUI();
        }

        #endregion

        #region Initialization

        private void SubscribeToViewModelEvents()
        {
            // ViewModel 이벤트 → View 애니메이션/UI
            _viewModel.DamageDealt += OnDamageDealt;
            _viewModel.MonsterDefeated += OnMonsterDefeated;
            _viewModel.MonsterSpawned += OnMonsterSpawned;
            _viewModel.GameOver += OnGameOver;
            _viewModel.ManageModeChanged += OnManageModeChanged;
            _viewModel.InputReceived += OnInputReceived;
            _viewModel.SettingsRequested += OnSettingsRequested;
            _viewModel.StatsRequested += OnStatsRequested;

            // GameManager 이벤트 (UI 업데이트용)
            GameManager.TimerTick += OnTimerTick;
            GameManager.StatsChanged += OnStatsChanged;

            // AchievementManager 이벤트
            AchievementManager.AchievementUnlocked += OnAchievementUnlocked;

            // TrayManager 이벤트
            TrayManager.ManageModeToggled += OnTrayManageModeToggled;
            TrayManager.ExitRequested += OnExitRequested;
        }

        private void InitializeUITimers()
        {
            // Auto Restart Timer
            _autoRestartTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _autoRestartTimer.Tick += AutoRestartTimer_Tick;

            // Hero Attack Timer
            _heroAttackTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _heroAttackTimer.Tick += HeroAttackTimer_Tick;

            // Hover Check Timer
            _hoverCheckTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _hoverCheckTimer.Tick += HoverCheckTimer_Tick;
        }

        #endregion

        #region Window Event Handlers

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;

            // WndProc 훅 추가 (WM_NCHITTEST 처리용)
            HwndSource source = HwndSource.FromHwnd(_hwnd);
            source.AddHook(WndProc);

            // 저장 데이터 로드
            SaveManager.Load();

            // 다국어 초기화
            LocalizationManager.Instance.Initialize(SaveManager.CurrentSave.Settings.Language);
            LocalizationManager.Instance.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == "Item[]")
                {
                    Dispatcher.Invoke(UpdateLocalizedUI);
                }
            };
            UpdateLocalizedUI();

            // 저장된 위치 복원
            Left = SaveManager.CurrentSave.Position.X;
            Top = SaveManager.CurrentSave.Position.Y;

            // 태스크바에서 숨기기
            Win32Helper.SetWindowToolWindow(_hwnd);

            // 트레이 아이콘 초기화
            _viewModel.InitializeTray();

            // 입력 감지 시작
            InputHandler.ShouldBlockKey = (vkCode) =>
            {
                if (vkCode == 112) // F1
                {
                    return IsMouseOverWindow();
                }
                return false;
            };

            // 게임 시작 및 업그레이드 로드
            _viewModel.LoadSavedData();
            _viewModel.StartGame();

            // 설정 적용
            ApplySettings();

            // 이미지 로드
            LoadCharacterImages();

            // 호버 타이머 시작
            _hoverCheckTimer?.Start();

            // UI 초기화
            UpdateAllUI();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.Log("=== EXIT START ===");

            // 위치 업데이트
            SaveManager.UpdateWindowPosition(Left, Top);

            // 저장
            _viewModel.SaveCurrentState();
            Logger.Log("SaveManager.Save() Completed");

            // 타이머 정리
            _hoverCheckTimer?.Stop();
            _heroAttackTimer?.Stop();
            _autoRestartTimer?.Stop();

            // ViewModel 정리
            _viewModel.Dispose();
            Logger.Log("ViewModel Disposed");

            Logger.Log("=== EXIT END ===");
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            SaveManager.UpdateWindowPosition(Left, Top);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isManageMode)
            {
                try
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        DragMove();
                    }
                }
                catch (InvalidOperationException) { }
                catch (Exception ex)
                {
                    Logger.LogError("DragMove Failed", ex);
                }
            }
        }

        #endregion

        #region ViewModel Event Handlers

        private void OnInputReceived(object? sender, GameInputEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // F1 키로 관리 모드 토글
                if (e.Type == GameInputType.Keyboard && e.VirtualKeyCode == 112)
                {
                    if (IsMouseOverWindow())
                    {
                        TrayManager.ToggleManageMode();
                    }
                    return;
                }

                // 통계 업데이트
                if (e.Type == GameInputType.Keyboard)
                {
                    SaveManager.AddKeyboardInput();
                }
                else
                {
                    SaveManager.AddMouseInput();
                }

                // 공격 사운드
                SoundManager.Play(SoundType.Hit);

                // 영웅 공격 스프라이트 전환
                ShowHeroAttackSprite();

                // 몬스터 흔들림 효과
                ShakeMonster();

                // 디버그 텍스트
                string inputInfo = e.Type == GameInputType.Keyboard
                    ? $"⌨️ Key:{e.VirtualKeyCode}"
                    : $"🖱️ {e.MouseButton}";
                DebugText.Text = inputInfo;
            });
        }

        private void OnDamageDealt(object? sender, DamageEventArgs e)
        {
            // 통계 업데이트
            SaveManager.AddDamage(e.Damage);
            if (e.IsCritical)
            {
                SaveManager.AddCriticalHit();
            }

            // 업적 체크
            AchievementManager.CheckAchievements("total_damage");
            AchievementManager.CheckAchievements("max_damage");
            AchievementManager.CheckAchievements("critical_hits");

            Dispatcher.Invoke(() =>
            {
                ShowDamagePopup(e.Damage, e.IsCritical);
                UpdateMonsterUI();
            });
        }

        private void OnMonsterDefeated(object? sender, EventArgs e)
        {
            // 통계 업데이트
            SaveManager.AddKill();
            if (GameManager.CurrentMonster?.IsBoss == true)
            {
                SaveManager.AddBossKill();
            }

            // 업적 체크
            AchievementManager.CheckAchievements("monster_kills");
            AchievementManager.CheckAchievements("bosses_defeated");
            AchievementManager.CheckAchievements("max_level");

            Dispatcher.Invoke(() =>
            {
                SoundManager.Play(SoundType.Defeat);

                if (GameManager.CurrentLevel > SaveManager.CurrentSave.Stats.MaxLevel)
                {
                    SaveManager.UpdateMaxLevel(GameManager.CurrentLevel);
                    SaveManager.Save();
                }

                FlashEffect();
                UpdateAllUI();
            });
        }

        private void OnMonsterSpawned(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (GameManager.CurrentMonster?.IsBoss == true)
                {
                    SoundManager.Play(SoundType.BossAppear);
                    BossEntranceEffect();
                }
                UpdateMonsterUI();
            });
        }

        private void OnGameOver(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(StartGameOverSequence);
        }

        private void OnManageModeChanged(object? sender, bool isManageMode)
        {
            _isManageMode = isManageMode;
            UpdateManageModeUI();
        }

        private void OnTrayManageModeToggled(object? sender, EventArgs e)
        {
            _viewModel.IsManageMode = TrayManager.IsManageMode;
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(UpdateTimerUI);
        }

        private void OnStatsChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(UpdateAllUI);
        }

        private void OnSettingsRequested()
        {
            Dispatcher.Invoke(() => SettingsButton_Click(this, new RoutedEventArgs()));
        }

        private void OnStatsRequested()
        {
            Dispatcher.Invoke(() => StatsButton_Click(this, new RoutedEventArgs()));
        }

        private void OnExitRequested(object? sender, EventArgs e)
        {
            Logger.Log("OnExitRequested: Before Close()");
            Close();
            Logger.Log("OnExitRequested: After Close()");
        }

        #endregion

        #region UI Event Handlers

        private void ModeToggle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TrayManager.ToggleManageMode();
            e.Handled = true;
        }

        private void GameElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isManageMode)
            {
                TrayManager.ToggleManageMode();
            }
            e.Handled = true;
        }

        private void UpgradeKeyboard_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpgradeKeyboardCommand.Execute(null);
            UpdateAllUI();
            UpdateUpgradeCosts();
        }

        private void UpgradeMouse_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpgradeMouseCommand.Execute(null);
            UpdateAllUI();
            UpdateUpgradeCosts();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Windows.SettingsWindow(
                SaveManager.CurrentSave.Settings,
                ApplyWindowOpacity,
                ApplyBackgroundOpacity,
                (volume) => SoundManager.Volume = volume,
                () => TrayManager.UpdateLanguage()
            );
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
            SaveManager.Save();
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            var statsWindow = new Windows.StatisticsWindow(SaveManager, AchievementManager, GameManager);
            statsWindow.Owner = this;
            statsWindow.ShowDialog();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ExitButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Application.Current.Shutdown();
        }

        private void CloseOverlayButton_Click(object sender, RoutedEventArgs e)
        {
            _autoRestartTimer.Stop();
            CloseGameOverOverlay();
        }

        #endregion

        #region UI Update Methods

        private void UpdateUI()
        {
            if (GameManager == null) return;
            UpdateCoreUI();
            UpdateUpgradeCosts();
        }

        private void UpdateAllUI()
        {
            UpdateCoreUI();
            UpdateMonsterUI();
            UpdateTimerUI();
        }

        private void UpdateCoreUI()
        {
            if (LevelText != null) LevelText.Text = $"Lv.{GameManager.CurrentLevel}";
            if (MaxLevelText != null)
            {
                int bestLevel = Math.Max(GameManager.CurrentLevel, SaveManager.CurrentSave.Stats.MaxLevel);
                MaxLevelText.Text = $"(Best: {bestLevel})";
            }
            if (GoldText != null) GoldText.Text = $"💰 {GameManager.Gold:N0}";

            if (GameManager.CurrentMonster != null && HpText != null)
            {
                HpText.Text = $"{GameManager.CurrentMonster.CurrentHp:N0}/{GameManager.CurrentMonster.MaxHp:N0}";
            }

            if (InputCountText != null) InputCountText.Text = $"⌨️ {_viewModel.SessionInputCount}";
            if (KeyboardPowerText != null) KeyboardPowerText.Text = $"⌨️ Atk: {GameManager.KeyboardPower:N0}";
            if (MousePowerText != null) MousePowerText.Text = $"🖱️ Atk: {GameManager.MousePower:N0}";
        }

        private void UpdateMonsterUI()
        {
            var monster = GameManager.CurrentMonster;
            if (monster == null) return;

            MonsterEmoji.Text = monster.Emoji;
            UpdateMonsterImage(monster);
            HpText.Text = $"{monster.CurrentHp}/{monster.MaxHp}";
            UpdateHpBar(monster);
        }

        private void UpdateMonsterImage(Monster monster)
        {
            try
            {
                string spritePath = monster.SkinType;
                string imagePath = spritePath.EndsWith(".png")
                    ? $"pack://application:,,,/Assets/Images/{spritePath}"
                    : $"pack://application:,,,/Assets/Images/{spritePath}.png";

                MonsterImage.Source = ImageHelper.LoadWithChromaKey(imagePath);
                MonsterImage.Width = monster.IsBoss ? BOSS_SIZE : MONSTER_SIZE;
                MonsterImage.Height = monster.IsBoss ? BOSS_SIZE : MONSTER_SIZE;

                bool needsFlip = NeedsFlip(spritePath);
                MonsterImage.RenderTransformOrigin = new Point(0.5, 0.5);

                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(needsFlip ? -1 : 1, 1));
                transformGroup.Children.Add(MonsterShakeTransform);
                MonsterImage.RenderTransform = transformGroup;
            }
            catch (Exception ex)
            {
                Logger.Log($"Monster image load failed: {ex.Message}");
            }
        }

        private static bool NeedsFlip(string spritePath)
        {
            return spritePath.Contains("slime") || spritePath.Contains("bat") ||
                   spritePath.Contains("skeleton") || spritePath.Contains("goblin") ||
                   spritePath.Contains("orc") || spritePath.Contains("ghost") ||
                   spritePath.Contains("golem") || spritePath.Contains("mushroom") ||
                   spritePath.Contains("spider") || spritePath.Contains("wolf") ||
                   spritePath.Contains("snake") || spritePath.Contains("boar");
        }

        private void UpdateHpBar(Monster monster)
        {
            var hpRatio = monster.HpRatio;
            double targetWidth = hpRatio * 80;

            var widthAnim = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            HpBar.BeginAnimation(WidthProperty, widthAnim);
            HpBar.Background = new SolidColorBrush(GetHpBarColor(hpRatio));
        }

        private static Color GetHpBarColor(double hpRatio)
        {
            if (hpRatio > 0.5) return Color.FromRgb(0, 255, 0);
            if (hpRatio > 0.25) return Color.FromRgb(255, 255, 0);
            return Color.FromRgb(255, 0, 0);
        }

        private void UpdateTimerUI()
        {
            int time = GameManager.RemainingTime;
            TimerText.Text = time.ToString();

            if (time > 20)
            {
                TimerText.BeginAnimation(OpacityProperty, null);
                TimerText.Opacity = 1.0;
                TimerText.Foreground = new SolidColorBrush(Color.FromRgb(135, 206, 235));
            }
            else if (time > 10)
            {
                TimerText.BeginAnimation(OpacityProperty, null);
                TimerText.Opacity = 1.0;
                TimerText.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 0));
            }
            else
            {
                TimerText.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                if (time <= 5 && time > 0)
                {
                    var blinkAnim = new DoubleAnimation
                    {
                        From = 1.0, To = 0.3,
                        Duration = TimeSpan.FromMilliseconds(300),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    TimerText.BeginAnimation(OpacityProperty, blinkAnim);
                }
            }
        }

        private void UpdateUpgradeCosts()
        {
            var keyboardCost = GameManager.CalculateUpgradeCost(GameManager.KeyboardPower);
            var mouseCost = GameManager.CalculateUpgradeCost(GameManager.MousePower);
            int gold = GameManager.Gold;

            KeyboardCostText.Text = $"💰 {keyboardCost}";
            MouseCostText.Text = $"💰 {mouseCost}";

            bool canBuyKeyboard = gold >= keyboardCost;
            bool canBuyMouse = gold >= mouseCost;

            UpgradeKeyboardBtn.IsEnabled = canBuyKeyboard;
            UpgradeMouseBtn.IsEnabled = canBuyMouse;

            KeyboardCostText.Foreground = new SolidColorBrush(
                canBuyKeyboard ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 100, 100));
            MouseCostText.Foreground = new SolidColorBrush(
                canBuyMouse ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 100, 100));
        }

        private void UpdateManageModeUI()
        {
            if (_isManageMode)
            {
                ManageModeBorder.Visibility = Visibility.Visible;
                UpgradePanel.Visibility = Visibility.Visible;
                PowerInfoBar.Visibility = Visibility.Visible;
                ExitButtonBorder.Visibility = Visibility.Visible;
                MaxLevelText.Visibility = Visibility.Visible;
                HpText.Visibility = Visibility.Visible;

                ModeIcon.Text = "✋";
                ModeIcon.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0));
                ModeToggleBorder.ToolTip = "👁️ 관전 모드로 전환 (F1)";
                ModeToggleBorder.Opacity = 1;
                _isModeButtonVisible = true;

                UpdateUpgradeCosts();
            }
            else
            {
                ManageModeBorder.Visibility = Visibility.Collapsed;
                UpgradePanel.Visibility = Visibility.Collapsed;
                PowerInfoBar.Visibility = Visibility.Collapsed;
                ExitButtonBorder.Visibility = Visibility.Collapsed;
                MaxLevelText.Visibility = Visibility.Collapsed;
                HpText.Visibility = Visibility.Collapsed;

                ModeIcon.Text = "👁️";
                ModeIcon.Foreground = new SolidColorBrush(Color.FromRgb(0, 206, 209));
                ModeToggleBorder.ToolTip = "✋ 관리 모드로 전환 (F1)";

                if (!IsMouseOverWindow())
                {
                    ModeToggleBorder.Opacity = 0;
                    _isModeButtonVisible = false;
                }
            }

            ApplyBackgroundOpacity(SaveManager.CurrentSave.Settings.BackgroundOpacity);
        }

        private void UpdateLocalizedUI()
        {
            var loc = LocalizationManager.Instance;

            if (UpgradeKeyboardText != null) UpgradeKeyboardText.Text = loc["ui.main.upgradeKeyboard"];
            if (UpgradeMouseText != null) UpgradeMouseText.Text = loc["ui.main.upgradeMouse"];
            if (StatsBtn != null) StatsBtn.Content = loc["ui.main.stats"];
            if (SettingsBtn != null) SettingsBtn.Content = loc["ui.main.settings"];

            if (KeyboardPowerText != null)
                KeyboardPowerText.Text = $"{loc["ui.main.keyboardAtk"]}: {GameManager?.KeyboardPower ?? 1:N0}";
            if (MousePowerText != null)
                MousePowerText.Text = $"{loc["ui.main.mouseAtk"]}: {GameManager?.MousePower ?? 1:N0}";

            if (GameOverTitleText != null) GameOverTitleText.Text = loc["ui.gameover.title"];
            if (ReportLevelLabel != null) ReportLevelLabel.Text = loc["ui.gameover.maxLevel"];
            if (ReportGoldLabel != null) ReportGoldLabel.Text = loc["ui.gameover.goldEarned"];
            if (ReportDamageLabel != null) ReportDamageLabel.Text = loc["ui.gameover.damageDealt"];
            if (CloseOverlayButton != null) CloseOverlayButton.Content = loc.CurrentLanguage == "ko-KR" ? "닫기" : "Close";

            if (UpgradeKeyboardBtn != null) UpgradeKeyboardBtn.ToolTip = loc["tooltips.upgradeKeyboard"];
            if (UpgradeMouseBtn != null) UpgradeMouseBtn.ToolTip = loc["tooltips.upgradeMouse"];
            if (StatsBtn != null) StatsBtn.ToolTip = loc["tooltips.stats"];
            if (SettingsBtn != null) SettingsBtn.ToolTip = loc["tooltips.settings"];
            if (ExitButtonBorder != null) ExitButtonBorder.ToolTip = loc["tooltips.exit"];
        }

        #endregion

        #region Animation & Effects

        private void ShakeMonster()
        {
            double shakePower = GameManager.Config.Visual.ShakePower;
            double offsetX = (_random.NextDouble() - 0.5) * 2 * shakePower;
            double offsetY = (_random.NextDouble() - 0.5) * 2 * shakePower;

            var animX = new DoubleAnimation
            {
                From = offsetX, To = 0,
                Duration = TimeSpan.FromMilliseconds(50),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var animY = new DoubleAnimation
            {
                From = offsetY, To = 0,
                Duration = TimeSpan.FromMilliseconds(50),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            MonsterShakeTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            MonsterShakeTransform.BeginAnimation(TranslateTransform.YProperty, animY);

            var opacityFlash = new DoubleAnimation
            {
                From = 1.0, To = 0.5,
                Duration = TimeSpan.FromMilliseconds(80),
                AutoReverse = true
            };
            MonsterImage.BeginAnimation(OpacityProperty, opacityFlash);
        }

        private void ShowDamagePopup(int damage, bool isCritical = false)
        {
            var popup = new Controls.DamagePopup(damage, isCritical);
            double x = 30 + _random.NextDouble() * 40;
            double y = 30 + _random.NextDouble() * 30;

            Canvas.SetLeft(popup, x);
            Canvas.SetTop(popup, y);
            DamagePopupCanvas.Children.Add(popup);

            popup.Animate(() => DamagePopupCanvas.Children.Remove(popup));
        }

        private void FlashEffect()
        {
            var goldReward = GameManager.CurrentMonster?.GoldReward ?? 0;
            var brush = new SolidColorBrush(Colors.Gold);
            GoldText.Foreground = brush;

            var colorAnim = new ColorAnimation
            {
                From = Colors.White, To = Colors.Gold,
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = true
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
            DebugText.Text = $"+{goldReward} 💰";
        }

        private void BossEntranceEffect()
        {
            DebugText.Text = "⚠️ BOSS APPEARED!";
            DebugText.Foreground = new SolidColorBrush(Colors.Purple);
            MonsterImage.Width = BOSS_SIZE;
            MonsterImage.Height = BOSS_SIZE;
        }

        #endregion

        #region Game Over Sequence

        private void StartGameOverSequence()
        {
            if (MainBackgroundBorder != null)
                MainBackgroundBorder.IsHitTestVisible = false;

            var growAnim = new DoubleAnimation
            {
                To = 500,
                Duration = TimeSpan.FromSeconds(1.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            MonsterImage.BeginAnimation(WidthProperty, growAnim);
            MonsterImage.BeginAnimation(HeightProperty, growAnim);

            var shakeAnim = new DoubleAnimation
            {
                From = -5, To = 5,
                Duration = TimeSpan.FromMilliseconds(50),
                RepeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(1.5)),
                AutoReverse = true
            };
            MonsterShakeTransform.BeginAnimation(TranslateTransform.XProperty, shakeAnim);

            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                ShowLifeReport();
            };
            timer.Start();

            SoundManager.Play(SoundType.GameOver);
        }

        private void ShowLifeReport()
        {
            Logger.Log("=== GAME OVER START ===");

            string? deathType = GameManager.CurrentMonster?.IsBoss == true ? "boss"
                : GameManager.RemainingTime <= 0 ? "timeout" : "normal";

            _viewModel.SaveSession();

            AchievementManager.CheckAchievements("total_sessions");
            AchievementManager.CheckAchievements("total_gold_earned");
            AchievementManager.CheckAchievements("total_playtime_minutes");
            AchievementManager.CheckAchievements("keyboard_inputs");
            AchievementManager.CheckAchievements("mouse_inputs");
            AchievementManager.CheckAchievements("consecutive_days");

            GameOverMessageText.Text = GameManager.GetGameOverMessage(deathType);
            ReportLevelText.Text = $"{GameManager.CurrentLevel}";
            ReportGoldText.Text = $"{GameManager.SessionTotalGold:N0}";
            ReportDamageText.Text = $"{GameManager.SessionDamage:N0}";

            GameOverOverlay.Opacity = 0;
            GameOverOverlay.Visibility = Visibility.Visible;
            GameOverOverlay.IsHitTestVisible = true;

            var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.5) };
            GameOverOverlay.BeginAnimation(OpacityProperty, fadeIn);

            ApplyBackgroundOpacity(SaveManager.CurrentSave.Settings.BackgroundOpacity);

            MonsterImage.BeginAnimation(WidthProperty, null);
            MonsterImage.BeginAnimation(HeightProperty, null);
            MonsterImage.Width = MONSTER_SIZE;
            MonsterImage.Height = MONSTER_SIZE;
            MonsterShakeTransform.BeginAnimation(TranslateTransform.XProperty, null);

            GameManager.RestartGame();

            _autoRestartCountdown = 10;
            UpdateAutoCloseCountdown();
            _autoRestartTimer.Start();

            Logger.Log("=== GAME OVER END ===");
        }

        private void UpdateAutoCloseCountdown()
        {
            var loc = LocalizationManager.Instance;
            AutoCloseCountdownText.Text = loc.CurrentLanguage == "ko-KR"
                ? $"{_autoRestartCountdown}초후 닫힘"
                : $"Closes in {_autoRestartCountdown}s";
        }

        private void AutoRestartTimer_Tick(object? sender, EventArgs e)
        {
            _autoRestartCountdown--;
            UpdateAutoCloseCountdown();
            if (_autoRestartCountdown <= 0)
            {
                _autoRestartTimer.Stop();
                CloseGameOverOverlay();
            }
        }

        private void CloseGameOverOverlay()
        {
            GameOverOverlay.Visibility = Visibility.Collapsed;
            if (MainBackgroundBorder != null)
                MainBackgroundBorder.IsHitTestVisible = true;
            UpdateAllUI();
        }

        #endregion

        #region Hero Sprite

        private void LoadCharacterImages()
        {
            try
            {
                var heroes = GameManager.Heroes;
                if (heroes.Count > 0)
                {
                    _currentHero = heroes[_random.Next(heroes.Count)];
                    HeroImage.Source = ImageHelper.LoadWithChromaKey(
                        $"pack://application:,,,/Assets/Images/{_currentHero.IdleSprite}.png");
                }
            }
            catch { }
        }

        private void HeroAttackTimer_Tick(object? sender, EventArgs e)
        {
            _heroAttackTimer?.Stop();
            if (_currentHero != null)
            {
                HeroImage.Source = ImageHelper.LoadWithChromaKey(
                    $"pack://application:,,,/Assets/Images/{_currentHero.IdleSprite}.png");
            }
        }

        private void ShowHeroAttackSprite()
        {
            if (_currentHero == null) return;
            _heroAttackTimer?.Stop();
            HeroImage.Source = ImageHelper.LoadWithChromaKey(
                $"pack://application:,,,/Assets/Images/{_currentHero.AttackSprite}.png");
            _heroAttackTimer?.Start();
        }

        #endregion

        #region Settings

        private void ApplySettings()
        {
            var settings = SaveManager.CurrentSave.Settings;
            ApplyWindowOpacity(settings.WindowOpacity);
            ApplyBackgroundOpacity(settings.BackgroundOpacity);
            SoundManager.Volume = settings.Volume;
        }

        private void ApplyWindowOpacity(double opacity)
        {
            this.Opacity = opacity;
        }

        private void ApplyBackgroundOpacity(double opacity)
        {
            double effectiveOpacity = _isManageMode ? Math.Max(opacity, 0.05) : opacity;
            double infoOpacity = Math.Clamp(effectiveOpacity, 0.0, 0.8);
            double upgradeOpacity = Math.Clamp(effectiveOpacity * 1.5, 0.0, 0.95);

            if (MainBackgroundBorder != null)
                MainBackgroundBorder.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x2e)) { Opacity = effectiveOpacity };
            if (EnemyInfoBorder != null)
                EnemyInfoBorder.Background = new SolidColorBrush(Colors.Black) { Opacity = infoOpacity };
            if (HeroInfoBar != null)
                HeroInfoBar.Background = new SolidColorBrush(Colors.Black) { Opacity = Math.Max(infoOpacity, 0.7) };
            if (PowerInfoBar != null)
                PowerInfoBar.Background = new SolidColorBrush(Colors.Black) { Opacity = infoOpacity };
            if (UpgradePanel != null)
                UpgradePanel.Background = new SolidColorBrush(Colors.Black) { Opacity = upgradeOpacity };
            if (GameOverOverlay != null)
            {
                byte overlayAlpha = (byte)(Math.Max(opacity, 0.8) * 255);
                GameOverOverlay.Background = new SolidColorBrush(Color.FromArgb(overlayAlpha, 0, 0, 0));
            }
        }

        #endregion

        #region Win32 & Mode Button

        private bool IsMouseOverWindow()
        {
            if (Win32Helper.GetCursorPos(out var pt))
            {
                try
                {
                    var localPoint = PointFromScreen(new Point(pt.x, pt.y));
                    return localPoint.X >= 0 && localPoint.X < ActualWidth &&
                           localPoint.Y >= 0 && localPoint.Y < ActualHeight;
                }
                catch { return false; }
            }
            return false;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = -1;
            const int HTCLIENT = 1;

            if (msg == WM_NCHITTEST)
            {
                if (!_isManageMode)
                {
                    if (GameOverOverlay.Visibility == Visibility.Visible)
                    {
                        handled = true;
                        return new IntPtr(HTCLIENT);
                    }

                    int x = (short)(lParam.ToInt32() & 0xFFFF);
                    int y = (short)(lParam.ToInt32() >> 16);
                    Point screenPoint = new Point(x, y);
                    Point clientPoint = PointFromScreen(screenPoint);

                    if (IsPointOverModeButton(clientPoint))
                    {
                        handled = true;
                        return new IntPtr(HTCLIENT);
                    }

                    handled = true;
                    return new IntPtr(HTTRANSPARENT);
                }
            }
            return IntPtr.Zero;
        }

        private void HoverCheckTimer_Tick(object? sender, EventArgs e)
        {
            if (_isManageMode) return;

            bool isOver = IsMouseOverWindow();
            if (isOver && !_isModeButtonVisible)
            {
                _isModeButtonVisible = true;
                ShowModeButton();
            }
            else if (!isOver && _isModeButtonVisible)
            {
                _isModeButtonVisible = false;
                HideModeButton();
            }
        }

        private void ShowModeButton()
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
            ModeToggleBorder.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void HideModeButton()
        {
            if (_isManageMode) return;
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            ModeToggleBorder.BeginAnimation(OpacityProperty, fadeOut);
        }

        private bool IsPointOverModeButton(Point point)
        {
            try
            {
                GeneralTransform transform = ModeToggleBorder.TransformToAncestor(this);
                Rect bounds = transform.TransformBounds(
                    new Rect(0, 0, ModeToggleBorder.ActualWidth, ModeToggleBorder.ActualHeight));
                return bounds.Contains(point);
            }
            catch { return false; }
        }

        #endregion

        #region Achievement Toast

        private void OnAchievementUnlocked(object? sender, AchievementUnlockedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _toastQueue.Enqueue(e.Achievement);
                if (!_isShowingToast)
                {
                    ShowNextToast();
                }
            });
        }

        private void ShowNextToast()
        {
            if (_toastQueue.Count == 0)
            {
                _isShowingToast = false;
                return;
            }

            _isShowingToast = true;
            var achievement = _toastQueue.Dequeue();

            var toast = new Controls.AchievementToast();
            toast.HorizontalAlignment = HorizontalAlignment.Right;
            toast.VerticalAlignment = VerticalAlignment.Bottom;
            toast.Margin = new Thickness(0, 0, 10, 10);

            var mainGrid = Content as Grid;
            if (mainGrid != null)
            {
                Panel.SetZIndex(toast, 999);
                mainGrid.Children.Add(toast);

                toast.AnimationCompleted += (s, args) =>
                {
                    mainGrid.Children.Remove(toast);
                    ShowNextToast();
                };

                toast.Show(achievement);
                SoundManager.Play(SoundType.Upgrade);
            }
            else
            {
                _isShowingToast = false;
            }
        }

        #endregion
    }
}
