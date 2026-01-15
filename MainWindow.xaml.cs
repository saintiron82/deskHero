using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DeskWarrior.Helpers;
using DeskWarrior.Interfaces;
using DeskWarrior.Managers;
using DeskWarrior.Models;
using DeskWarrior.ViewModels;
using DeskWarrior.ViewControllers;

namespace DeskWarrior
{
    /// <summary>
    /// 메인 윈도우 코드비하인드 (MVVM: View 역할)
    /// UI 렌더링과 애니메이션만 담당, 비즈니스 로직은 ViewModel에 위임
    /// 리팩토링: 주요 로직을 ViewControllers로 분리함
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields & Properties

        // ViewModel (Controllers에서 접근 가능하도록 public/internal)
        public MainViewModel ViewModel { get; private set; }

        // Controllers
        private WindowInteropController _windowInterop;
        private VisualEffectController _visualEffect;
        private HeroAvatarController _heroAvatar;
        private GameOverController _gameOver;

        // ViewModel Property Shortcuts
        private GameManager GameManager => ViewModel.GameManager;
        private SaveManager SaveManager => ViewModel.SaveManager; // Accessed by GameOverController
        internal SoundManager SoundManager => ViewModel.SoundManager; // Accessed by Controllers
        private TrayManager TrayManager => ViewModel.TrayManager;
        private AchievementManager AchievementManager => ViewModel.AchievementManager;
        private IInputHandler InputHandler => ViewModel.InputHandler;

        private const double MONSTER_SIZE = 80;
        private const double BOSS_SIZE = 130;  // Used in UpdateMonsterImage

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            // ViewModel 생성 및 DataContext 설정
            ViewModel = new MainViewModel();
            DataContext = ViewModel;

            // Controllers 초기화
            _windowInterop = new WindowInteropController(this);
            _visualEffect = new VisualEffectController(this);
            _heroAvatar = new HeroAvatarController(this);
            _gameOver = new GameOverController(this);

            // 이벤트 구독
            SubscribeToViewModelEvents();

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
            ViewModel.DamageDealt += OnDamageDealt;
            ViewModel.MonsterDefeated += OnMonsterDefeated;
            ViewModel.MonsterSpawned += OnMonsterSpawned;
            ViewModel.GameOver += OnGameOver;
            ViewModel.ManageModeChanged += OnManageModeChanged;
            ViewModel.InputReceived += OnInputReceived;
            ViewModel.SettingsRequested += OnSettingsRequested;
            ViewModel.StatsRequested += OnStatsRequested;

            // GameManager 이벤트 (UI 업데이트용)
            GameManager.TimerTick += OnTimerTick;
            GameManager.StatsChanged += OnStatsChanged;

            // AchievementManager 이벤트
            AchievementManager.AchievementUnlocked += OnAchievementUnlocked;

            // TrayManager 이벤트
            TrayManager.ManageModeToggled += OnTrayManageModeToggled;
            TrayManager.ExitRequested += OnExitRequested;
        }

        #endregion

        #region Window Event Handlers

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _windowInterop.InitializeWindow();

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

            // 트레이 아이콘 초기화
            ViewModel.InitializeTray();

            // 입력 감지 시작
            InputHandler.ShouldBlockKey = (vkCode) =>
            {
                if (vkCode == 112) // F1
                {
                    return _windowInterop.IsMouseOverWindow();
                }
                return false;
            };

            // 게임 시작 및 업그레이드 로드
            ViewModel.LoadSavedData();
            ViewModel.StartGame();

            // 설정 적용
            ApplySettings();

            // 이미지 로드
            _heroAvatar.LoadCharacterImages(GameManager.Heroes);

            // UI 초기화
            UpdateAllUI();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.Log("=== EXIT START ===");

            SaveManager.UpdateWindowPosition(Left, Top);
            ViewModel.SaveCurrentState();
            Logger.Log("SaveManager.Save() Completed");

            _windowInterop.Dispose();
            _heroAvatar.Dispose();
            _gameOver.Dispose();

            ViewModel.Dispose();
            Logger.Log("ViewModel Disposed"); // _viewModel renamed to ViewModel, property access works

            Logger.Log("=== EXIT END ===");
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            SaveManager.UpdateWindowPosition(Left, Top);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _windowInterop.HandleMouseLeftButtonDown(sender, e);
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
                    if (_windowInterop.IsMouseOverWindow())
                    {
                        TrayManager.ToggleManageMode();
                    }
                    return;
                }

                if (e.Type == GameInputType.Keyboard)
                {
                    SaveManager.AddKeyboardInput();
                }
                else
                {
                    SaveManager.AddMouseInput();
                }

                SoundManager.Play(SoundType.Hit);
                _heroAvatar.ShowHeroAttackSprite();
                _visualEffect.ShakeMonster(GameManager.Config.Visual.ShakePower);

                string inputInfo = e.Type == GameInputType.Keyboard
                    ? $"⌨️ Key:{e.VirtualKeyCode}"
                    : $"🖱️ {e.MouseButton}";
                DebugText.Text = inputInfo;
            });
        }

        private void OnDamageDealt(object? sender, DamageEventArgs e)
        {
            SaveManager.AddDamage(e.Damage);
            if (e.IsCritical) SaveManager.AddCriticalHit();

            AchievementManager.CheckAchievements("total_damage");
            AchievementManager.CheckAchievements("max_damage");
            AchievementManager.CheckAchievements("critical_hits");

            Dispatcher.Invoke(() =>
            {
                _visualEffect.ShowDamagePopup(e.Damage, e.IsCritical);
                UpdateMonsterUI();
            });
        }

        private void OnMonsterDefeated(object? sender, EventArgs e)
        {
            SaveManager.AddKill();
            if (GameManager.CurrentMonster?.IsBoss == true) SaveManager.AddBossKill();

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

                _visualEffect.FlashEffect(GameManager.CurrentMonster?.GoldReward ?? 0);
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
                    _visualEffect.BossEntranceEffect();
                }
                UpdateMonsterUI();
            });
        }

        private void OnGameOver(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() => _gameOver.StartGameOverSequence(SoundManager));
        }

        private void OnManageModeChanged(object? sender, bool isManageMode)
        {
            _windowInterop.IsManageMode = isManageMode;
            UpdateManageModeUI(isManageMode);
        }

        private void OnTrayManageModeToggled(object? sender, EventArgs e)
        {
            ViewModel.IsManageMode = TrayManager.IsManageMode;
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
            Close();
        }

        private void OnAchievementUnlocked(object? sender, AchievementUnlockedEventArgs e)
        {
            Dispatcher.Invoke(() => _visualEffect.OnAchievementUnlocked(e.Achievement, SoundManager));
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
            if (!_windowInterop.IsManageMode)
            {
                TrayManager.ToggleManageMode();
            }
            e.Handled = true;
        }

        private void UpgradeKeyboard_Click(object sender, RoutedEventArgs e)
        {
            if (GameManager.UpgradeInGameStat("keyboard_power"))
            {
                SoundManager.Play(SoundType.Upgrade);
                UpdateAllUI();
                UpdateUpgradeCosts();
            }
        }

        private void UpgradeMouse_Click(object sender, RoutedEventArgs e)
        {
            if (GameManager.UpgradeInGameStat("mouse_power"))
            {
                SoundManager.Play(SoundType.Upgrade);
                UpdateAllUI();
                UpdateUpgradeCosts();
            }
        }

        private void UpgradeGoldFlat_Click(object sender, RoutedEventArgs e)
        {
            if (GameManager.UpgradeInGameStat("gold_flat"))
            {
                SoundManager.Play(SoundType.Upgrade);
                UpdateAllUI();
                UpdateUpgradeCosts();
            }
        }

        private void UpgradeGoldMulti_Click(object sender, RoutedEventArgs e)
        {
            if (GameManager.UpgradeInGameStat("gold_multi"))
            {
                SoundManager.Play(SoundType.Upgrade);
                UpdateAllUI();
                UpdateUpgradeCosts();
            }
        }

        private void UpgradeTimeThief_Click(object sender, RoutedEventArgs e)
        {
            if (GameManager.UpgradeInGameStat("time_thief"))
            {
                SoundManager.Play(SoundType.Upgrade);
                UpdateAllUI();
                UpdateUpgradeCosts();
            }
        }

        private void UpgradeComboFlex_Click(object sender, RoutedEventArgs e)
        {
            if (GameManager.UpgradeInGameStat("combo_flex"))
            {
                SoundManager.Play(SoundType.Upgrade);
                UpdateAllUI();
                UpdateUpgradeCosts();
            }
        }

        private void UpgradeComboDamage_Click(object sender, RoutedEventArgs e)
        {
            if (GameManager.UpgradeInGameStat("combo_damage"))
            {
                SoundManager.Play(SoundType.Upgrade);
                UpdateAllUI();
                UpdateUpgradeCosts();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Windows.SettingsWindow(
                SaveManager.CurrentSave.Settings,
                ApplyWindowOpacity,
                ApplyBackgroundOpacity,
                (volume) => SoundManager.Volume = volume,
                () => TrayManager.UpdateLanguage(),
                GameManager,
                SaveManager
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
            _gameOver.StopTimer();
            _gameOver.CloseGameOverOverlay();
        }

        #endregion

        #region UI Update Methods

        public void UpdateUI()
        {
            if (GameManager == null) return;
            UpdateCoreUI();
            UpdateUpgradeCosts();
        }

        public void UpdateAllUI()
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

            if (InputCountText != null) InputCountText.Text = $"⌨️ {ViewModel.SessionInputCount}";
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
            int gold = GameManager.Gold;

            // 키보드 공격력
            int keyboardCost = GameManager.GetInGameStatUpgradeCost("keyboard_power");
            int keyboardLevel = GameManager.InGameStats.KeyboardPowerLevel;
            KeyboardCostText.Text = $"💰 {keyboardCost:N0}";
            KeyboardEffectText.Text = $"Lv.{keyboardLevel} (+{GameManager.KeyboardPower} 데미지)";
            bool canBuyKeyboard = gold >= keyboardCost;
            UpgradeKeyboardBtn.IsEnabled = canBuyKeyboard;
            KeyboardCostText.Foreground = new SolidColorBrush(
                canBuyKeyboard ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 100, 100));

            // 마우스 공격력
            int mouseCost = GameManager.GetInGameStatUpgradeCost("mouse_power");
            int mouseLevel = GameManager.InGameStats.MousePowerLevel;
            MouseCostText.Text = $"💰 {mouseCost:N0}";
            MouseEffectText.Text = $"Lv.{mouseLevel} (+{GameManager.MousePower} 데미지)";
            bool canBuyMouse = gold >= mouseCost;
            UpgradeMouseBtn.IsEnabled = canBuyMouse;
            MouseCostText.Foreground = new SolidColorBrush(
                canBuyMouse ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 100, 100));

            // 골드+
            int goldFlatCost = GameManager.GetInGameStatUpgradeCost("gold_flat");
            int goldFlatLevel = GameManager.InGameStats.GoldFlatLevel;
            GoldFlatCostText.Text = $"💰 {goldFlatCost:N0}";
            GoldFlatEffectText.Text = $"Lv.{goldFlatLevel} (+{GameManager.GoldFlat:N0} 골드)";
            bool canBuyGoldFlat = gold >= goldFlatCost;
            UpgradeGoldFlatBtn.IsEnabled = canBuyGoldFlat;
            GoldFlatCostText.Foreground = new SolidColorBrush(
                canBuyGoldFlat ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 100, 100));

            // 골드*
            int goldMultiCost = GameManager.GetInGameStatUpgradeCost("gold_multi");
            int goldMultiLevel = GameManager.InGameStats.GoldMultiLevel;
            GoldMultiCostText.Text = $"💰 {goldMultiCost:N0}";
            GoldMultiEffectText.Text = $"Lv.{goldMultiLevel} (+{GameManager.GoldMulti * 100:N0}%)";
            bool canBuyGoldMulti = gold >= goldMultiCost;
            UpgradeGoldMultiBtn.IsEnabled = canBuyGoldMulti;
            GoldMultiCostText.Foreground = new SolidColorBrush(
                canBuyGoldMulti ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 100, 100));

            // 시간 도둑
            int timeThiefCost = GameManager.GetInGameStatUpgradeCost("time_thief");
            int timeThiefLevel = GameManager.InGameStats.TimeThiefLevel;
            TimeThiefCostText.Text = $"💰 {timeThiefCost:N0}";
            TimeThiefEffectText.Text = $"Lv.{timeThiefLevel} (+{GameManager.TimeThief:N1}초)";
            bool canBuyTimeThief = gold >= timeThiefCost;
            UpgradeTimeThiefBtn.IsEnabled = canBuyTimeThief;
            TimeThiefCostText.Foreground = new SolidColorBrush(
                canBuyTimeThief ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 100, 100));

            // 콤보 유연성
            int comboFlexCost = GameManager.GetInGameStatUpgradeCost("combo_flex");
            int comboFlexLevel = GameManager.InGameStats.ComboFlexLevel;
            ComboFlexCostText.Text = $"💰 {comboFlexCost:N0}";
            ComboFlexEffectText.Text = $"Lv.{comboFlexLevel} (+{GameManager.ComboFlex:N3}초)";
            bool canBuyComboFlex = gold >= comboFlexCost;
            UpgradeComboFlexBtn.IsEnabled = canBuyComboFlex;
            ComboFlexCostText.Foreground = new SolidColorBrush(
                canBuyComboFlex ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 100, 100));

            // 콤보 데미지
            int comboDamageCost = GameManager.GetInGameStatUpgradeCost("combo_damage");
            int comboDamageLevel = GameManager.InGameStats.ComboDamageLevel;
            ComboDamageCostText.Text = $"💰 {comboDamageCost:N0}";
            ComboDamageEffectText.Text = $"Lv.{comboDamageLevel} (+{GameManager.ComboDamage * 100:N0}%)";
            bool canBuyComboDamage = gold >= comboDamageCost;
            UpgradeComboDamageBtn.IsEnabled = canBuyComboDamage;
            ComboDamageCostText.Foreground = new SolidColorBrush(
                canBuyComboDamage ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 100, 100));
        }

        private void UpdateManageModeUI(bool isManageMode)
        {
            // _windowInterop.IsManageMode is updated already
            if (isManageMode)
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
                _windowInterop.ForceShowModeButton();

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

                if (!_windowInterop.IsMouseOverWindow())
                {
                    ModeToggleBorder.Opacity = 0;
                    _windowInterop.ForceHideModeButton();
                }
            }

            ApplyBackgroundOpacity(SaveManager.CurrentSave.Settings.BackgroundOpacity);
        }

        private void UpdateLocalizedUI()
        {
            var loc = LocalizationManager.Instance;

            // Stat names (already in JSON config, no need to localize here)
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

        #region Settings

        private void ApplySettings()
        {
            var settings = SaveManager.CurrentSave.Settings;
            ApplyWindowOpacity(settings.WindowOpacity);
            ApplyBackgroundOpacity(settings.BackgroundOpacity);
            SoundManager.Volume = settings.Volume;
        }

        public void ApplyWindowOpacity(double opacity)
        {
            this.Opacity = opacity;
        }

        public void ApplyBackgroundOpacity(double opacity)
        {
            double effectiveOpacity = _windowInterop.IsManageMode ? Math.Max(opacity, 0.05) : opacity;
            double infoOpacity = Math.Clamp(effectiveOpacity, 0.0, 0.8);
            double upgradeOpacity = Math.Clamp(effectiveOpacity * 1.5, 0.0, 0.95);

            if (MainBackgroundBorder != null)
                MainBackgroundBorder.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x2e)) { Opacity = effectiveOpacity };
            if (EnemyInfoBorder != null)
                EnemyInfoBorder.Background = new SolidColorBrush(Colors.Black) { Opacity = infoOpacity };
            if (GoldInfoBar != null)
                GoldInfoBar.Background = new SolidColorBrush(Colors.Black) { Opacity = Math.Max(infoOpacity, 0.7) };
            if (TimerInfoBar != null)
                TimerInfoBar.Background = new SolidColorBrush(Colors.Black) { Opacity = Math.Max(infoOpacity, 0.7) };
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
    }
}
