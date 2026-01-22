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
            ViewModel.InputReceived += OnInputReceived;
            ViewModel.SettingsRequested += OnSettingsRequested;
            ViewModel.StatsRequested += OnStatsRequested;

            // GameManager 이벤트 (UI 업데이트용)
            GameManager.TimerTick += OnTimerTick;
            GameManager.StatsChanged += OnStatsChanged;

            // AchievementManager 이벤트
            AchievementManager.AchievementUnlocked += OnAchievementUnlocked;

            // TrayManager 이벤트
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
                // F1 키는 무시 (기능 제거됨)
                if (e.Type == GameInputType.Keyboard && e.VirtualKeyCode == 112)
                    return;

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
                UpdateMonsterUI(instantHpBar: true);
            });
        }

        private void OnGameOver(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() => _gameOver.StartGameOverSequence(SoundManager));
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

        private void InfoBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 상단 바를 잡고 창 이동
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            }
            catch (InvalidOperationException) { }
            e.Handled = true;
        }

        private void GameElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 게임 요소 클릭 시 아무 동작 안 함 (관리 모드 제거됨)
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

        private void UpgradePanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 패널 영역 클릭 시 이벤트 전달 허용 (버튼이 자체 클릭 이벤트 처리)
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            GameManager.PauseTimer();
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
            settingsWindow.Closed += (s, args) =>
            {
                SaveManager.Save();
                GameManager.ResumeTimer();
            };
            settingsWindow.Show();
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            GameManager.PauseTimer();
            var statsWindow = new Windows.StatisticsWindow(SaveManager, AchievementManager, GameManager);
            statsWindow.Owner = this;
            statsWindow.Closed += (s, args) => GameManager.ResumeTimer();
            statsWindow.Show();
        }

        private void PermanentShopButton_Click(object sender, RoutedEventArgs e)
        {
            OpenPermanentUpgradeShop();
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

        private void ShopButton_Click(object sender, RoutedEventArgs e)
        {
            _gameOver.StopTimer();
            _gameOver.CloseGameOverOverlay();
            OpenPermanentUpgradeShop();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // 게임 오버 오버레이가 표시된 경우에만 키보드 단축키 처리
            if (GameOverOverlayControl.Visibility == Visibility.Visible)
            {
                if (e.Key == Key.Space || e.Key == Key.Enter)
                {
                    // SPACE 또는 ENTER: 게임 재시작
                    CloseOverlayButton_Click(sender, e);
                    e.Handled = true;
                }
                else if (e.Key == Key.S)
                {
                    // S: 상점 열기
                    ShopButton_Click(sender, e);
                    e.Handled = true;
                }
            }
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
            UpdateUpgradeCosts();
        }

        private void UpdateCoreUI()
        {
            // 대부분의 UI는 ViewModel 바인딩으로 자동 업데이트됨
            // 여기서는 바인딩되지 않은 요소만 직접 업데이트

            // 크리스탈 (ViewModel에 아직 없음)
            if (CrystalTextTop != null)
                CrystalTextTop.Text = $"{SaveManager.CurrentSave.PermanentCurrency.Crystals:N0}";

            // 입력 카운트 (디버그용)
            if (InputCountText != null)
                InputCountText.Text = $"⌨️ {ViewModel.SessionInputCount}";
        }

        private void UpdateMonsterUI(bool instantHpBar = false)
        {
            var monster = GameManager.CurrentMonster;
            if (monster == null) return;

            // MonsterEmoji, HpText는 ViewModel 바인딩으로 처리됨
            UpdateMonsterImage(monster);
            UpdateHpBar(monster, instantHpBar);
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

        private void UpdateHpBar(Monster monster, bool instant = false)
        {
            var hpRatio = monster.HpRatio;
            double targetWidth = hpRatio * 80;

            if (instant)
            {
                // 새 몬스터 스폰 시: 애니메이션 없이 즉시 설정
                HpBar.BeginAnimation(WidthProperty, null);
                HpBar.Width = targetWidth;
            }
            else
            {
                // 데미지 시: 애니메이션으로 부드럽게 감소
                var widthAnim = new DoubleAnimation
                {
                    To = targetWidth,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                HpBar.BeginAnimation(WidthProperty, widthAnim);
            }
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
            Logger.Log($"[UpdateUpgradeCosts] Gold={gold}");

            // 키보드 공격력
            int keyboardCost = GameManager.GetInGameStatUpgradeCost("keyboard_power");
            Logger.Log($"[UpdateUpgradeCosts] KeyboardCost={keyboardCost}, CanBuy={gold >= keyboardCost}");
            int keyboardLevel = GameManager.InGameStats.KeyboardPowerLevel;
            KeyboardCostText.Text = $"{keyboardCost:N0}";
            KeyboardLevelText.Text = $"Lv.{keyboardLevel}";
            bool canBuyKeyboard = gold >= keyboardCost;
            UpgradeKeyboardBtn.IsEnabled = canBuyKeyboard;
            KeyboardCostText.Foreground = new SolidColorBrush(
                canBuyKeyboard ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 100, 100));

            // 관찰 모드: 구매 가능 여부에 따라 색상 변경
            if (canBuyKeyboard)
            {
                // 구매 가능: 금색 계열
                KeyboardIconText.Foreground = new SolidColorBrush(Color.FromRgb(232, 208, 63));
                KeyboardLevelText.Foreground = new SolidColorBrush(Color.FromRgb(232, 208, 63));
                UpgradeKeyboardBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(232, 208, 63));
                UpgradeKeyboardBtn.BorderThickness = new Thickness(2);
            }
            else
            {
                // 구매 불가: 회색
                KeyboardIconText.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                KeyboardLevelText.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                UpgradeKeyboardBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235));
                UpgradeKeyboardBtn.BorderThickness = new Thickness(1);
            }

            // 툴팁 업데이트 (관찰 모드에서 비용 확인용)
            UpgradeKeyboardBtn.ToolTip = $"⌨️ 키보드 공격력 증가\nLv.{keyboardLevel} → Lv.{keyboardLevel + 1}\n비용: 💰{keyboardCost:N0}";


            // 마우스 공격력
            int mouseCost = GameManager.GetInGameStatUpgradeCost("mouse_power");
            int mouseLevel = GameManager.InGameStats.MousePowerLevel;
            MouseCostText.Text = $"{mouseCost:N0}";
            MouseLevelText.Text = $"Lv.{mouseLevel}";
            bool canBuyMouse = gold >= mouseCost;
            UpgradeMouseBtn.IsEnabled = canBuyMouse;
            MouseCostText.Foreground = new SolidColorBrush(
                canBuyMouse ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 100, 100));

            // 관찰 모드: 구매 가능 여부에 따라 색상 변경
            if (canBuyMouse)
            {
                // 구매 가능: 금색 계열
                MouseIconText.Foreground = new SolidColorBrush(Color.FromRgb(232, 208, 63));
                MouseLevelText.Foreground = new SolidColorBrush(Color.FromRgb(232, 208, 63));
                UpgradeMouseBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(232, 208, 63));
                UpgradeMouseBtn.BorderThickness = new Thickness(2);
            }
            else
            {
                // 구매 불가: 회색
                MouseIconText.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                MouseLevelText.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                UpgradeMouseBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235));
                UpgradeMouseBtn.BorderThickness = new Thickness(1);
            }

            // 툴팁 업데이트
            UpgradeMouseBtn.ToolTip = $"🖱️ 마우스 공격력 증가\nLv.{mouseLevel} → Lv.{mouseLevel + 1}\n비용: 💰{mouseCost:N0}";
        }

        private void SetUpgradeButtonsOpacity(double opacity)
        {
            if (double.IsNaN(opacity))
            {
                // 기본값으로 복원 (ClearValue 사용)
                UpgradeKeyboardBtn?.ClearValue(OpacityProperty);
                UpgradeMouseBtn?.ClearValue(OpacityProperty);
            }
            else
            {
                // 명시적으로 Opacity 설정
                if (UpgradeKeyboardBtn != null) UpgradeKeyboardBtn.Opacity = opacity;
                if (UpgradeMouseBtn != null) UpgradeMouseBtn.Opacity = opacity;
            }
        }

        private void UpdateLocalizedUI()
        {
            var loc = LocalizationManager.Instance;

            // 버튼 텍스트 다국어
            if (StatsBtn != null) StatsBtn.Content = loc["ui.main.stats"];
            if (SettingsBtn != null) SettingsBtn.Content = loc["ui.main.settings"];

            // 공격력 텍스트는 ViewModel 바인딩으로 처리됨 (KeyboardPowerDisplayText, MousePowerDisplayText)

            // 게임 오버 버튼 다국어 (UserControl)
            GameOverOverlayControl?.UpdateButtonTexts(
                loc.CurrentLanguage == "ko-KR" ? "🛒 상점 (S)" : "🛒 Shop (S)",
                loc.CurrentLanguage == "ko-KR" ? "▶️ 게임 (SPACE)" : "▶️ Game (SPACE)"
            );

            // 툴팁 다국어
            if (UpgradeKeyboardBtn != null) UpgradeKeyboardBtn.ToolTip = loc["tooltips.upgradeKeyboard"];
            if (UpgradeMouseBtn != null) UpgradeMouseBtn.ToolTip = loc["tooltips.upgradeMouse"];
            if (StatsBtn != null) StatsBtn.ToolTip = loc["tooltips.stats"];
            if (SettingsBtn != null) SettingsBtn.ToolTip = loc["tooltips.settings"];
            if (ExitButtonBorderInline != null) ExitButtonBorderInline.ToolTip = loc["tooltips.exit"];
        }

        #endregion

        #region Shop Management

        private void OpenPermanentUpgradeShop()
        {
            var permanentProgression = GameManager.PermanentProgression;
            if (permanentProgression == null)
            {
                Logger.Log("PermanentProgressionManager not initialized");
                return;
            }

            GameManager.PauseTimer();
            var shopWindow = new PermanentUpgradeShop(permanentProgression, SaveManager);
            shopWindow.Owner = this;
            shopWindow.Closed += (s, args) =>
            {
                UpdateAllUI();
                GameManager.ResumeTimer();
            };
            shopWindow.Show();
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
            double effectiveOpacity = opacity;
            double infoOpacity = Math.Clamp(effectiveOpacity, 0.0, 0.8);
            double upgradeOpacity = Math.Clamp(effectiveOpacity * 1.5, 0.0, 0.95);

            if (MainBackgroundBorder != null)
                MainBackgroundBorder.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x2e)) { Opacity = effectiveOpacity };
            if (EnemyInfoBorder != null)
                EnemyInfoBorder.Background = new SolidColorBrush(Colors.Black) { Opacity = infoOpacity };
            if (GoldInfoBarTop != null)
                GoldInfoBarTop.Background = new SolidColorBrush(Colors.Black) { Opacity = infoOpacity };
            if (PowerInfoBar != null)
                PowerInfoBar.Background = new SolidColorBrush(Colors.Black) { Opacity = infoOpacity };
            if (UpgradePanel != null)
                UpgradePanel.Background = new SolidColorBrush(Colors.Black) { Opacity = upgradeOpacity };
            if (UtilityPanel != null)
                UtilityPanel.Background = new SolidColorBrush(Colors.Black) { Opacity = upgradeOpacity };
            // GameOverOverlay는 UserControl 내부에서 배경색 관리
        }

        #endregion
    }
}
