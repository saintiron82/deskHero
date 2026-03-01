using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WinFormsScreen = System.Windows.Forms.Screen;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DeskWarrior.Helpers;
using DeskWarrior.Interfaces;
using DeskWarrior.Managers;
using DeskWarrior.Managers.Services;
using DeskWarrior.Models;
using DeskWarrior.ViewModels;
using DeskWarrior.ViewControllers;
using DeskWarrior.Windows;

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

        // Services
        private OfflineRewardService _offlineRewardService;
        private System.Windows.Threading.DispatcherTimer _onlineTimeTimer;

        // ViewModel Property Shortcuts
        private GameManager GameManager => ViewModel.GameManager;
        private SaveManager SaveManager => ViewModel.SaveManager; // Accessed by GameOverController
        internal SoundManager SoundManager => ViewModel.SoundManager; // Accessed by Controllers
        private TrayManager TrayManager => ViewModel.TrayManager;
        private AchievementManager AchievementManager => ViewModel.AchievementManager;
        private IInputHandler InputHandler => ViewModel.InputHandler;

        private const double MONSTER_SIZE = 80;
        private const double BOSS_SIZE = 110;  // Used in UpdateMonsterImage
        private string DefaultBackgroundUri => ResourceManager.Instance.GetDefaultBackgroundUri().ToString();

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

            // HP 바 컨테이너 크기 변경 시 HP 바 업데이트
            HpBarContainer.SizeChanged += HpBarContainer_SizeChanged;

            // 배경 이미지 초기화 (ResourcePaths.json에서 로드)
            BackgroundImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                ResourceManager.Instance.GetDefaultBackgroundUri());

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
            ViewModel.GoldenGoblinSpawned += OnGoldenGoblinSpawned;
            ViewModel.GoldenGoblinDefeated += OnGoldenGoblinDefeated;
            ViewModel.GoldenGoblinEscaped += OnGoldenGoblinEscaped;

            // GameManager 이벤트 (UI 업데이트용)
            GameManager.TimerTick += OnTimerTick;
            GameManager.StatsChanged += OnStatsChanged;

            // AchievementManager 이벤트
            AchievementManager.AchievementUnlocked += OnAchievementUnlocked;

            // TrayManager 이벤트
            TrayManager.ExitRequested += OnExitRequested;
            TrayManager.MoveToScreenRequested += OnMoveToScreenRequested;
        }

        #endregion

        #region Window Event Handlers

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _windowInterop.InitializeWindow();

            // 저장 데이터 로드
            SaveManager.Load();

            // 오프라인 보상 체크
            CheckOfflineRewards();

            // 온라인 시간 업데이트 타이머 시작 (5분마다)
            StartOnlineTimeTimer();

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
            EnsureWindowOnScreen();

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

            // UI 초기화 (HP 바는 OnMonsterSpawned에서 이미 설정됨)
            UpdateCoreUI();
            UpdateTimerUI();
            UpdateUpgradeCosts();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.Log("=== EXIT START ===");

            // 온라인 타이머 중지
            _onlineTimeTimer?.Stop();

            // 마지막 온라인 시간 업데이트
            SaveManager.CurrentSave.LastOnlineTime = DateTime.Now;

            SaveManager.UpdateWindowPosition(Left, Top);
            ViewModel.SaveCurrentState();
            Logger.Log("SaveManager.Save() Completed");

            _windowInterop.Dispose();
            _heroAvatar.Dispose();
            _gameOver.Dispose();

            ViewModel.Dispose();
            Logger.Log("ViewModel Disposed");

            Logger.Log("=== EXIT END ===");
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            SaveManager.UpdateWindowPosition(Left, Top);
        }

        private void OnMoveToScreenRequested(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var primary = WinFormsScreen.PrimaryScreen;
                if (primary != null)
                {
                    Left = primary.WorkingArea.Left + 100;
                    Top = primary.WorkingArea.Top + 100;
                }
            });
        }

        private void EnsureWindowOnScreen()
        {
            var windowRect = new System.Drawing.Rectangle(
                (int)Left, (int)Top, (int)Width, (int)Height);

            bool isOnScreen = WinFormsScreen.AllScreens.Any(s =>
                s.WorkingArea.IntersectsWith(windowRect));

            if (!isOnScreen)
            {
                var primary = WinFormsScreen.PrimaryScreen;
                if (primary != null)
                {
                    Left = primary.WorkingArea.Left + 100;
                    Top = primary.WorkingArea.Top + 100;
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _windowInterop.HandleMouseLeftButtonDown(sender, e);
            Focus(); // 키보드 포커스 확보
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
                    SoundManager.Play(SoundType.KeyboardHit);
                }
                else
                {
                    SaveManager.AddMouseInput();
                    SoundManager.Play(SoundType.MouseClick);
                }
                _heroAvatar.ShowHeroAttackSprite();

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
                if (e.IsCritical)
                    SoundManager.Play(SoundType.Critical);

                _visualEffect.ShowDamagePopup(e.Damage, e.IsCritical);
                if (e.Damage > 0)
                {
                    _visualEffect.ShakeMonster(GameManager.Config.Visual.ShakePower);
                }
                // 몬스터가 살아있을 때만 HP 바 애니메이션 (죽으면 OnMonsterSpawned에서 처리)
                if (GameManager.CurrentMonster?.IsAlive == true)
                {
                    UpdateMonsterUI();
                }
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
                // 보스 처치 시 전용 사운드, 일반 몬스터는 Defeat 사운드
                if (GameManager.CurrentMonster?.IsBoss == true)
                    SoundManager.Play(SoundType.BossDefeat);
                else
                    SoundManager.Play(SoundType.Defeat);

                if (GameManager.CurrentLevel > SaveManager.CurrentSave.Stats.MaxLevel)
                {
                    SaveManager.UpdateMaxLevel(GameManager.CurrentLevel);
                    SaveManager.Save();
                }

                _visualEffect.FlashEffect(GameManager.CurrentMonster?.GoldReward ?? 0);
                // HP 바는 OnMonsterSpawned에서 업데이트하므로 여기서는 제외
                // (애니메이션 충돌 방지)
                UpdateCoreUI();
                UpdateTimerUI();
                UpdateUpgradeCosts();
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
            // HP 바는 OnDamageDealt/OnMonsterSpawned에서 처리하므로 여기서는 제외
            Dispatcher.Invoke(() =>
            {
                UpdateCoreUI();
                UpdateTimerUI();
                UpdateUpgradeCosts();
            });
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

        #region Golden Goblin UI

        private string? _savedBackgroundSource;

        private void OnGoldenGoblinSpawned(object? sender, GoldenGoblinSpawnEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // 배경 이미지 교체
                if (!string.IsNullOrEmpty(e.Background))
                {
                    _savedBackgroundSource = (BackgroundImage.Source as System.Windows.Media.Imaging.BitmapImage)?.UriSource?.ToString();
                    try
                    {
                        var bgUri = ResourceManager.Instance.GetImageUri(e.Background);
                        BackgroundImage.Source = new System.Windows.Media.Imaging.BitmapImage(bgUri);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Golden goblin background load failed: {ex.Message}");
                        BackgroundImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(DefaultBackgroundUri));
                    }
                }

                // 테두리 색상 변경
                if (!string.IsNullOrEmpty(e.BorderColor))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(e.BorderColor);
                        EnemyInfoBorder.BorderBrush = new SolidColorBrush(color);
                        EnemyInfoBorder.BorderThickness = new Thickness(2);
                    }
                    catch { }
                }

                // 이름 색상 변경
                if (!string.IsNullOrEmpty(e.NameColor))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(e.NameColor);
                        MonsterNameText.Foreground = new SolidColorBrush(color);
                    }
                    catch { }
                }

                // HP바 금색
                HpBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700"));

                // 사운드 재생
                SoundManager.Play(SoundType.GoldenGoblinAppear);

                // 등장 알림 표시
                ShowGoldenGoblinNotification(
                    LocalizationManager.Instance["ui.golden_goblin.appear"],
                    e.BorderColor ?? "#FFD700",
                    2.0);
            });
        }

        private void OnGoldenGoblinDefeated(object? sender, GoldenGoblinRewardEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // 사운드 재생
                SoundManager.Play(SoundType.GoldenGoblinDefeat);

                // 보상 알림
                string msg = string.Format(
                    LocalizationManager.Instance["ui.golden_goblin.reward"],
                    e.Multiplier, e.GoldReward.ToString("N0"));
                ShowGoldenGoblinNotification(msg, "#FFD700", 3.0);

                // UI 원래대로 복원
                RestoreNormalUI();
            });
        }

        private void OnGoldenGoblinEscaped(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // 사운드 재생
                SoundManager.Play(SoundType.GoldenGoblinEscape);

                // 도주 알림
                ShowGoldenGoblinNotification(
                    LocalizationManager.Instance["ui.golden_goblin.escaped"],
                    "#FF6666",
                    2.0);

                // UI 원래대로 복원
                RestoreNormalUI();
            });
        }

        private void RestoreNormalUI()
        {
            // 배경 복원
            if (_savedBackgroundSource != null)
            {
                try
                {
                    BackgroundImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(_savedBackgroundSource));
                }
                catch
                {
                    BackgroundImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(DefaultBackgroundUri));
                }
                _savedBackgroundSource = null;
            }

            // 테두리 복원
            EnemyInfoBorder.BorderBrush = null;
            EnemyInfoBorder.BorderThickness = new Thickness(0);

            // 이름 색상 복원
            MonsterNameText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#88ff88"));
        }

        private void ShowGoldenGoblinNotification(string text, string borderColorHex, double durationSeconds)
        {
            if (string.IsNullOrEmpty(text)) return;

            GoldenGoblinNotificationText.Text = text;

            try
            {
                var borderColor = (Color)ColorConverter.ConvertFromString(borderColorHex);
                GoldenGoblinNotification.BorderBrush = new SolidColorBrush(borderColor);
                GoldenGoblinNotificationText.Foreground = new SolidColorBrush(borderColor);
            }
            catch
            {
                GoldenGoblinNotification.BorderBrush = new SolidColorBrush(Colors.Gold);
                GoldenGoblinNotificationText.Foreground = new SolidColorBrush(Colors.Gold);
            }

            GoldenGoblinNotification.Visibility = Visibility.Visible;

            // 페이드 아웃 애니메이션
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                BeginTime = TimeSpan.FromSeconds(durationSeconds - 0.5),
                Duration = TimeSpan.FromSeconds(0.5),
            };
            fadeOut.Completed += (s, args) =>
            {
                GoldenGoblinNotification.Visibility = Visibility.Collapsed;
                GoldenGoblinNotification.Opacity = 1.0;
            };
            GoldenGoblinNotification.BeginAnimation(OpacityProperty, fadeOut);
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
                SaveManager,
                SoundManager
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

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Logger.Log($"[MainWindow] PreviewKeyDown: {e.Key}");

            // F12: 개발자 밸런스 테스트 창 (항상 사용 가능)
            if (e.Key == Key.F12)
            {
                Logger.Log("[MainWindow] F12 pressed, opening BalanceTestWindow");
                OpenBalanceTestWindow();
                e.Handled = true;
                return;
            }

            // 게임 오버 오버레이가 표시된 경우: 모든 키 입력 차단 (게임 재진입 방지)
            if (GameOverOverlayControl.Visibility == Visibility.Visible)
            {
                if (e.Key == Key.Space || e.Key == Key.Enter)
                {
                    // SPACE 또는 ENTER: 게임 재시작
                    CloseOverlayButton_Click(sender, e);
                }
                else if (e.Key == Key.S)
                {
                    // S: 상점 열기
                    ShopButton_Click(sender, e);
                }
                // 모든 키 입력 차단 (게임으로 전달 방지)
                e.Handled = true;
                return;
            }
        }

        private void OpenBalanceTestWindow()
        {
            try
            {
                var balanceWindow = new BalanceTestWindow(GameManager, SaveManager);
                balanceWindow.Owner = this;
                balanceWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.LogError("[MainWindow] Failed to open BalanceTestWindow", ex);
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

        /// <summary>
        /// 크리스탈 표시만 업데이트 (상점에서 호출)
        /// </summary>
        public void UpdateCrystalDisplay()
        {
            if (CrystalTextTop != null)
                CrystalTextTop.Text = $"{SaveManager.CurrentSave.PermanentCurrency.Crystals:N0}";
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

        private void UpdateMonsterUI(bool instantHpBar = false, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            var monster = GameManager.CurrentMonster;
            LogHpBar($"[UpdateMonsterUI] caller={caller}, instantHpBar={instantHpBar}, monster={monster?.Name ?? "null"}");
            if (monster == null) return;

            // MonsterEmoji, HpText는 ViewModel 바인딩으로 처리됨
            UpdateMonsterImage(monster);
            UpdateHpBar(monster, instantHpBar);
        }

        private void UpdateMonsterImage(Monster monster)
        {
            var previousSource = MonsterImage.Source;
            try
            {
                string spritePath = monster.SkinType;
                string imagePath = ResourceManager.Instance.GetImageUri(spritePath).ToString();

                var loaded = ImageHelper.LoadWithChromaKey(imagePath);
                if (loaded == null)
                {
                    Logger.Log($"Monster image load failed (null): {imagePath}");
                    MonsterImage.Source = previousSource;
                    return;
                }

                MonsterImage.Source = loaded;
                double size = monster.IsBoss ? BOSS_SIZE : MONSTER_SIZE;
                MonsterImage.Width = size;
                MonsterImage.Height = size;

                // 보스/일반 크기 변경 시 중심점 유지하도록 bottom margin 조정
                const double centerY = 54 + MONSTER_SIZE / 2;  // 일반 몬스터 기준 중심 Y
                double bottomMargin = centerY - size / 2;
                MonsterImage.Margin = new Thickness(0, 0, 20, Math.Max(0, bottomMargin));

                bool needsFlip = NeedsFlip(spritePath);
                MonsterImage.RenderTransformOrigin = new Point(0.5, 0.5);

                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(needsFlip ? -1 : 1, 1));
                transformGroup.Children.Add(MonsterShakeTransform);
                transformGroup.Children.Add(MonsterMoveTransform);
                transformGroup.Children.Add(MonsterScaleTransform);
                MonsterImage.RenderTransform = transformGroup;
            }
            catch (Exception ex)
            {
                Logger.Log($"Monster image load failed: {ex.Message}");
                MonsterImage.Source = previousSource;
            }
        }

        private static bool NeedsFlip(string spritePath)
        {
            // sprite-processor가 모든 이미지를 왼쪽(플레이어 방향) 기준으로 보정 완료
            return false;
        }

        private static void LogHpBar(string message)
        {
            var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hpbar_log.txt");
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            System.IO.File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
        }

        private void HpBarContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 컨테이너 크기 변경 시 HP 바 재계산
            var monster = GameManager?.CurrentMonster;
            if (monster != null && e.NewSize.Width > 0)
            {
                UpdateHpBar(monster, instant: true);
            }
        }

        private void UpdateHpBar(Monster monster, bool instant = false)
        {
            try
            {
                var hpRatio = monster.HpRatio;
                // HP 바 컨테이너의 실제 너비 사용 (레이아웃 전이면 0)
                double maxWidth = HpBarContainer?.ActualWidth ?? 0;
                if (maxWidth <= 0) return; // 레이아웃 완료 전이면 무시

                double targetWidth = hpRatio * maxWidth;
                double currentWidth = HpBar?.ActualWidth ?? 0;

                // 디버그 로그
                LogHpBar($"[HP바] instant={instant}, hpRatio={hpRatio:F2}, target={targetWidth:F1}, current={currentWidth:F1}, maxWidth={maxWidth:F1}, HP={monster.CurrentHp}/{monster.MaxHp}");

                if (HpBar == null) return;

                if (instant)
                {
                    // 새 몬스터 스폰 시: 즉시 완료되는 애니메이션으로 강제 설정
                    var instantAnim = new DoubleAnimation
                    {
                        To = targetWidth,
                        Duration = TimeSpan.Zero,
                        FillBehavior = FillBehavior.HoldEnd
                    };
                    HpBar.BeginAnimation(WidthProperty, instantAnim);
                    LogHpBar($"[HP바] 즉시 애니메이션 적용: target={targetWidth}");
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
            catch (Exception ex)
            {
                LogHpBar($"[HP바 오류] {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static Color GetHpBarColor(double hpRatio)
        {
            if (hpRatio > 0.5) return Color.FromRgb(0, 255, 0);
            if (hpRatio > 0.25) return Color.FromRgb(255, 255, 0);
            return Color.FromRgb(255, 0, 0);
        }

        private void UpdateTimerUI()
        {
            double time = GameManager.RemainingTime;
            TimerText.Text = time.ToString("F1");

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
            long gold = GameManager.Gold;
            Logger.Log($"[UpdateUpgradeCosts] Gold={gold}");

            // 키보드 공격력
            int keyboardCost = GameManager.GetInGameStatUpgradeCost("keyboard_power");
            Logger.Log($"[UpdateUpgradeCosts] KeyboardCost={keyboardCost}, CanBuy={gold >= keyboardCost}");
            int keyboardLevel = GameManager.InGameStats.KeyboardPowerLevel;
            KeyboardLevelText.Text = $"Lv.{keyboardLevel}";
            bool canBuyKeyboard = gold >= keyboardCost;
            UpgradeKeyboardBtn.IsEnabled = canBuyKeyboard;

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
            var loc = LocalizationManager.Instance;
            UpgradeKeyboardBtn.ToolTip = $"{loc["ui.main.tooltip.keyboardUpgrade"]}\nLv.{keyboardLevel} → Lv.{keyboardLevel + 1}\n{loc["ui.common.cost"]}: 💰{keyboardCost:N0}";


            // 마우스 공격력
            int mouseCost = GameManager.GetInGameStatUpgradeCost("mouse_power");
            int mouseLevel = GameManager.InGameStats.MousePowerLevel;
            MouseLevelText.Text = $"Lv.{mouseLevel}";
            bool canBuyMouse = gold >= mouseCost;
            UpgradeMouseBtn.IsEnabled = canBuyMouse;

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
            UpgradeMouseBtn.ToolTip = $"{loc["ui.main.tooltip.mouseUpgrade"]}\nLv.{mouseLevel} → Lv.{mouseLevel + 1}\n{loc["ui.common.cost"]}: 💰{mouseCost:N0}";
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

            // 버튼 텍스트 다국어 (TextBlock 직접 참조)
            if (StatsBtnText != null) StatsBtnText.Text = loc["ui.main.button.stats"];
            if (ShopBtnText != null) ShopBtnText.Text = loc["ui.main.button.shop"];
            if (SettingsBtnText != null) SettingsBtnText.Text = loc["ui.main.button.settings"];

            // 공격력 텍스트는 ViewModel 바인딩으로 처리됨 (KeyboardPowerDisplayText, MousePowerDisplayText)

            // 게임 오버 버튼 다국어 (UserControl)
            GameOverOverlayControl?.UpdateButtonTexts(
                loc["ui.gameover.button.shop"],
                loc["ui.gameover.button.game"]
            );

            // 툴팁 다국어
            if (UpgradeKeyboardBtn != null) UpgradeKeyboardBtn.ToolTip = loc["ui.main.tooltip.keyboardUpgrade"];
            if (UpgradeMouseBtn != null) UpgradeMouseBtn.ToolTip = loc["ui.main.tooltip.mouseUpgrade"];
            if (StatsBtn != null) StatsBtn.ToolTip = loc["ui.main.tooltip.stats"];
            if (PermanentShopBtn != null) PermanentShopBtn.ToolTip = loc["ui.main.tooltip.shop"];
            if (SettingsBtn != null) SettingsBtn.ToolTip = loc["ui.main.tooltip.settings"];
            if (ExitButtonBorderInline != null) ExitButtonBorderInline.ToolTip = loc["ui.main.tooltip.exit"];
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
            SoundManager.Enabled = settings.SoundEnabled;
            SoundManager.Volume = settings.Volume;

            // 카테고리별 사운드팩 적용
            SoundManager.ApplyCategorySettings(settings.CategorySoundPacks, settings.SoundPack);

            // 사운드 테마 적용
            SoundManager.ApplyThemeSettings(settings.SoundThemeId, settings.SoundThemeOverrides);
        }

        public void ApplyWindowOpacity(double opacity)
        {
            this.Opacity = opacity;
        }

        public void ApplyBackgroundOpacity(double opacity)
        {
            // 최소 0.01 유지: WPF에서 Opacity=0이면 hit-test 통과(클릭 관통)되므로 방지
            double effectiveOpacity = Math.Max(opacity, 0.01);
            double infoOpacity = Math.Clamp(effectiveOpacity, 0.01, 0.8);
            double upgradeOpacity = Math.Clamp(effectiveOpacity * 1.5, 0.01, 0.95);

            if (MainBackgroundBorder != null)
                MainBackgroundBorder.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x2e)) { Opacity = effectiveOpacity };
            if (EnemyInfoBorder != null)
                EnemyInfoBorder.Background = new SolidColorBrush(Colors.Black) { Opacity = infoOpacity };
            if (GoldInfoBarTop != null)
                GoldInfoBarTop.Background = new SolidColorBrush(Colors.Black) { Opacity = infoOpacity };
            if (UpgradePanel != null)
                UpgradePanel.Background = new SolidColorBrush(Colors.Black) { Opacity = upgradeOpacity };
            if (UtilityPanel != null)
                UtilityPanel.Background = new SolidColorBrush(Colors.Black) { Opacity = upgradeOpacity };
            // GameOverOverlay는 UserControl 내부에서 배경색 관리
        }

        #endregion

        #region Offline Rewards

        private void StartOnlineTimeTimer()
        {
            _onlineTimeTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _onlineTimeTimer.Tick += (s, e) =>
            {
                SaveManager.CurrentSave.LastOnlineTime = DateTime.Now;
                // 자동 저장은 하지 않음 (앱 종료 시 저장)
            };
            _onlineTimeTimer.Start();
        }

        private void CheckOfflineRewards()
        {
            try
            {
                _offlineRewardService = new OfflineRewardService();

                if (!_offlineRewardService.IsEnabled)
                    return;

                var lastOnlineTime = SaveManager.CurrentSave.LastOnlineTime;
                var result = _offlineRewardService.CalculateReward(lastOnlineTime);

                if (result.HasReward)
                {
                    ShowOfflineRewardPopup(result);
                }

                // 마지막 온라인 시간 업데이트
                SaveManager.CurrentSave.LastOnlineTime = DateTime.Now;
                SaveManager.Save();
            }
            catch (Exception ex)
            {
                Logger.LogError("[MainWindow] Failed to check offline rewards", ex);
            }
        }

        private void ShowOfflineRewardPopup(OfflineRewardResult result)
        {
            var popup = new Controls.OfflineRewardPopup();
            var loc = LocalizationManager.Instance;

            // 현재 언어로 메시지 생성
            string langCode = loc.CurrentLanguage;
            string headerText = langCode.StartsWith("ko") ? "돌아오셨군요!" : "WELCOME BACK!";
            string message = _offlineRewardService.GetPopupMessage(result, langCode);

            popup.SetReward(result, message, headerText);
            popup.HorizontalAlignment = HorizontalAlignment.Center;
            popup.VerticalAlignment = VerticalAlignment.Center;

            var mainGrid = this.Content as Grid;
            if (mainGrid != null)
            {
                Panel.SetZIndex(popup, 1000);
                mainGrid.Children.Add(popup);

                popup.ClaimClicked += (s, args) =>
                {
                    // 보상 지급
                    SaveManager.CurrentSave.LifetimeStats.TotalGoldEarned += result.Gold;
                    SaveManager.CurrentSave.PermanentCurrency.Crystals += result.Crystals;
                    SaveManager.CurrentSave.TotalOfflineRewardsClaimed += result.Gold + result.Crystals;
                    SaveManager.Save();

                    // 사운드 재생
                    SoundManager.Play(SoundType.OfflineReward);

                    // UI 업데이트
                    UpdateCrystalDisplay();

                    // 팝업 제거
                    mainGrid.Children.Remove(popup);

                    Logger.Log($"[OfflineReward] Claimed: {result.Gold} gold, {result.Crystals} crystals");
                };

                popup.Show();
            }
        }

        #endregion
    }
}
