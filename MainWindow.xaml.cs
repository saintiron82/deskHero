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

namespace DeskWarrior
{
    /// <summary>
    /// 메인 윈도우 코드비하인드
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        // 캐릭터 크기 설정 (여기서 조절)
        private const double MONSTER_SIZE = 80;      // 일반 몬스터 크기
        private const double BOSS_SIZE = 130;        // 보스 크기
        private const double HERO_SIZE = 100;        // 히어로 크기

        private readonly IInputHandler _inputHandler;
        private readonly TrayManager _trayManager;
        private readonly SaveManager _saveManager;
        private readonly GameManager _gameManager;
        private readonly SoundManager _soundManager;
        private readonly AchievementManager _achievementManager;
        private readonly Random _random = new();

        private IntPtr _hwnd;
        private bool _isManageMode;
        private bool _isModeButtonVisible;
        private int _sessionInputCount;

        // Auto Restart
        private System.Windows.Threading.DispatcherTimer _autoRestartTimer;
        private int _autoRestartCountdown;

        // Achievement Toast Queue
        private readonly Queue<Models.AchievementDefinition> _toastQueue = new();
        private bool _isShowingToast;

        // Hero Sprite
        private HeroData? _currentHero;
        private System.Windows.Threading.DispatcherTimer? _heroAttackTimer;

        // Mode Button Hover Timer
        private System.Windows.Threading.DispatcherTimer? _hoverCheckTimer;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
            
            // 데이터 매니저 초기화
            _inputHandler = new GlobalInputManager();
            _trayManager = new TrayManager();
            _saveManager = new SaveManager();
            _gameManager = new GameManager();
            _soundManager = new SoundManager();
            _achievementManager = new AchievementManager(_saveManager);

            // 업적 해금 이벤트 구독
            _achievementManager.AchievementUnlocked += OnAchievementUnlocked;

            // Auto Restart Timer
            _autoRestartTimer = new System.Windows.Threading.DispatcherTimer();
            _autoRestartTimer.Interval = TimeSpan.FromSeconds(1);
            _autoRestartTimer.Tick += AutoRestartTimer_Tick;

            // 이벤트 구독
            _inputHandler.OnInput += OnInputReceived;
            _trayManager.ManageModeToggled += OnManageModeToggled;
            _trayManager.SettingsRequested += OnSettingsRequested;
            _trayManager.ExitRequested += OnExitRequested;

            _gameManager.DamageDealt += OnDamageDealt;
            _gameManager.MonsterDefeated += OnMonsterDefeated;
            _gameManager.TimerTick += OnTimerTick;
            _gameManager.StatsChanged += OnStatsChanged;
            _gameManager.GameOver += OnGameOver;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            LocationChanged += MainWindow_LocationChanged;

            // 게임 시작
            _gameManager.StartGame();
        }

        #endregion

        #region Event Handlers

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;

            // WndProc 훅 추가 (WM_NCHITTEST 처리용)
            HwndSource source = HwndSource.FromHwnd(_hwnd);
            source.AddHook(WndProc);

            // 저장 데이터 로드
            _saveManager.Load();

            // 다국어 초기화 (저장된 언어 설정 적용)
            LocalizationManager.Instance.Initialize(_saveManager.CurrentSave.Settings.Language);

            // 언어 변경 이벤트 구독
            LocalizationManager.Instance.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == "Item[]")
                {
                    Dispatcher.Invoke(UpdateLocalizedUI);
                }
            };

            // 초기 다국어 UI 적용
            UpdateLocalizedUI();

            // 저장된 위치 복원
            Left = _saveManager.CurrentSave.Position.X;
            Top = _saveManager.CurrentSave.Position.Y;

            // WM_NCHITTEST가 Click-through를 처리하므로 SetClickThrough 호출 불필요

            // 태스크바에서 숨기기
            Win32Helper.SetWindowToolWindow(_hwnd);

            // 트레이 아이콘 초기화
            _trayManager.Initialize();

            // 입력 감지 시작
            _inputHandler.ShouldBlockKey = (vkCode) =>
            {
                // F1(112) 키이고, 마우스가 창 위에 있으면 블로킹 (true 반환)
                if (vkCode == 112)
                {
                    return IsMouseOverWindow();
                }
                return false;
            };
            _inputHandler.Start();

            // 모드 버튼 호버 체크 타이머 초기화
            _hoverCheckTimer = new System.Windows.Threading.DispatcherTimer();
            _hoverCheckTimer.Interval = TimeSpan.FromMilliseconds(100);
            _hoverCheckTimer.Tick += HoverCheckTimer_Tick;
            _hoverCheckTimer.Start();

            // 게임 시작 및 저장된 업그레이드 로드
            _gameManager.StartGame();
            var upgrades = _saveManager.GetUpgrades();
            _gameManager.LoadUpgrades(upgrades.keyboard, upgrades.mouse);
            
            // 저장된 설정 적용
            ApplySettings();

            // 이미지 로드 (크로마 키 처리)
            LoadCharacterImages();

            // UI 초기화
            UpdateAllUI();
        }

        private void LoadCharacterImages()
        {
            // 히어로 이미지 로드
            try
            {
                // JSON에서 영웅 목록 가져와서 랜덤 선택
                var heroes = _gameManager.Heroes;
                if (heroes.Count > 0)
                {
                    _currentHero = heroes[_random.Next(heroes.Count)];
                    HeroImage.Source = ImageHelper.LoadWithChromaKey(
                        $"pack://application:,,,/Assets/Images/{_currentHero.IdleSprite}.png");
                }

                // 공격 스프라이트 복귀 타이머 초기화
                _heroAttackTimer = new System.Windows.Threading.DispatcherTimer();
                _heroAttackTimer.Interval = TimeSpan.FromMilliseconds(150);
                _heroAttackTimer.Tick += HeroAttackTimer_Tick;
            }
            catch (Exception ex)
            {
                DeskWarrior.Helpers.Logger.LogError("Hero image loading failed", ex);
            }
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

        private void ModeToggle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 모드 전환만 수행 (설정창 열지 않음)
            _trayManager.ToggleManageMode();
            e.Handled = true;
        }


        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            DeskWarrior.Helpers.Logger.Log("=== EXIT START ===");

            // 0. 위치 업데이트
            _saveManager.UpdateWindowPosition(Left, Top);

            // 1. 저장
            _saveManager.Save();
            DeskWarrior.Helpers.Logger.Log("SaveManager.Save() Completed");

            // 2. Hover Timer 정리
            _hoverCheckTimer?.Stop();

            // 3. InputHandler 정리
            _inputHandler.OnInput -= OnInputReceived;
            _inputHandler.Dispose();
            DeskWarrior.Helpers.Logger.Log("InputHandler Disposed");

            // 4. TrayManager 정리
            _trayManager.Dispose();
            DeskWarrior.Helpers.Logger.Log("TrayManager Disposed");

            // 5. SoundManager 정리
            _soundManager.Dispose();
            DeskWarrior.Helpers.Logger.Log("SoundManager Disposed");

            DeskWarrior.Helpers.Logger.Log("=== EXIT END ===");
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            _saveManager.UpdateWindowPosition(Left, Top);
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
                catch (InvalidOperationException)
                {
                    // 드래그 중 마우스 버튼 상태 변경 등으로 인한 예외 무시
                }
                catch (Exception ex)
                {
                     DeskWarrior.Helpers.Logger.LogError("DragMove Failed", ex);
                }
            }
        }

        private void GameElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 게임 요소(몬스터/히어로) 클릭 시 관리 모드가 아니면 자동 활성화
            if (!_isManageMode)
            {
                _trayManager.ToggleManageMode();
            }
            e.Handled = true;
        }

        private void OnInputReceived(object? sender, GameInputEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // F1 키로 관리 모드 토글 (VK_F1 = 112)
                if (e.Type == GameInputType.Keyboard && e.VirtualKeyCode == 112)
                {
                    // 마우스가 게임 창 위에 있을 때만 작동
                    if (IsMouseOverWindow())
                    {
                        _trayManager.ToggleManageMode();
                    }
                    return;
                }

                // 입력 카운트 증가
                _sessionInputCount++;

                // 게임 로직에 입력 전달 + 입력 타입별 통계 저장
                if (e.Type == GameInputType.Keyboard)
                {
                    _saveManager.AddKeyboardInput();
                    _gameManager.OnKeyboardInput();
                }
                else
                {
                    _saveManager.AddMouseInput();
                    _gameManager.OnMouseInput();
                }

                // 데미지 팝업 표시 (Event 기반으로 변경됨)
                // int damage = e.Type == GameInputType.Keyboard 
                //    ? _gameManager.KeyboardPower 
                //    : _gameManager.MousePower;
                // ShowDamagePopup(damage);

                // 공격 사운드
                _soundManager.Play(SoundType.Hit);

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

        private void OnManageModeToggled(object? sender, EventArgs e)
        {
            _isManageMode = _trayManager.IsManageMode;

            // UI 업데이트
            if (_isManageMode)
            {
                // 관리 모드 - 모든 정보 표시
                ManageModeBorder.Visibility = Visibility.Visible;
                UpgradePanel.Visibility = Visibility.Visible;
                PowerInfoBar.Visibility = Visibility.Visible;
                ExitButtonBorder.Visibility = Visibility.Visible;
                MaxLevelText.Visibility = Visibility.Visible;
                HpText.Visibility = Visibility.Visible;

                ModeIcon.Text = "✋";
                ModeIcon.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // Orange
                ModeToggleBorder.ToolTip = "👁️ 관전 모드로 전환 (F1)";

                // 관리 모드에서는 버튼 항상 표시
                ModeToggleBorder.Opacity = 1;
                _isModeButtonVisible = true;

                UpdateUpgradeCosts();
            }
            else
            {
                // 관전 모드 - 최소 UI
                ManageModeBorder.Visibility = Visibility.Collapsed;
                UpgradePanel.Visibility = Visibility.Collapsed;
                PowerInfoBar.Visibility = Visibility.Collapsed;
                ExitButtonBorder.Visibility = Visibility.Collapsed;
                MaxLevelText.Visibility = Visibility.Collapsed;
                HpText.Visibility = Visibility.Collapsed;

                ModeIcon.Text = "👁️";
                ModeIcon.Foreground = new SolidColorBrush(Color.FromRgb(0, 206, 209)); // Cyan
                ModeToggleBorder.ToolTip = "✋ 관리 모드로 전환 (F1)";

                // 관전 모드에서는 마우스가 창 위에 없으면 버튼 숨김
                if (!IsMouseOverWindow())
                {
                    ModeToggleBorder.Opacity = 0;
                    _isModeButtonVisible = false;
                }
            }

            // 모드 전환 시 배경 불투명도 재적용 (관리 모드 최소 5% 보장)
            ApplyBackgroundOpacity(_saveManager.CurrentSave.Settings.BackgroundOpacity);
        }

        private void UpgradeKeyboard_Click(object sender, RoutedEventArgs e)
        {
            if (_gameManager.UpgradeKeyboardPower())
            {
                _soundManager.Play(SoundType.Upgrade);
                SaveUpgrades();
                UpdateAllUI();
                UpdateUpgradeCosts();
            }
        }

        private void UpgradeMouse_Click(object sender, RoutedEventArgs e)
        {
            if (_gameManager.UpgradeMousePower())
            {
                _soundManager.Play(SoundType.Upgrade);
                SaveUpgrades();
                UpdateAllUI();
                UpdateUpgradeCosts();
            }
        }


        private void OnSettingsRequested(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                SettingsButton_Click(this, new RoutedEventArgs());
            });
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 설정 창 열기 (모달)
            var settingsWindow = new Windows.SettingsWindow(
                _saveManager.CurrentSave.Settings,
                (windowOpacity) => {
                    ApplyWindowOpacity(windowOpacity);
                },
                (opacity) => {
                    ApplyBackgroundOpacity(opacity);
                },
                (volume) => {
                    _soundManager.Volume = volume;
                },
                () => {
                    // 언어 변경 콜백 - 트레이 메뉴 업데이트
                    _trayManager.UpdateLanguage();
                }
            );
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();

            _saveManager.Save();
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            // 통계 창 열기
            var statsWindow = new Windows.StatisticsWindow(_saveManager, _achievementManager, _gameManager);
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

        private void ApplyWindowOpacity(double opacity)
        {
            this.Opacity = opacity;
        }

        private void ApplyBackgroundOpacity(double opacity)
        {
            // 관리 모드에서는 최소 5% 불투명도 보장
            double effectiveOpacity = _isManageMode ? Math.Max(opacity, 0.05) : opacity;

            // 각 패널마다 기본 투명도 비율이 다를 수 있음
            // 적 정보 / 타이머: 기본 0.4 (최대 0.8)
            double infoOpacity = Math.Clamp(effectiveOpacity, 0.0, 0.8);

            // 업그레이드 패널: 기본 0.6 (최대 0.9)
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

            // GameOverOverlay도 설정 따르도록 함
            if (GameOverOverlay != null)
            {
                 // 게임오버는 좀 더 어둡게
                 byte overlayAlpha = (byte)(Math.Max(opacity, 0.8) * 255);
                 GameOverOverlay.Background = new SolidColorBrush(Color.FromArgb(overlayAlpha, 0, 0, 0));
            }
        }

        private void ApplySettings()
        {
            var settings = _saveManager.CurrentSave.Settings;
            ApplyWindowOpacity(settings.WindowOpacity);
            ApplyBackgroundOpacity(settings.BackgroundOpacity);
            _soundManager.Volume = settings.Volume;
        }

        private void SaveUpgrades()
        {
            _saveManager.UpdateUpgrades(_gameManager.KeyboardPower, _gameManager.MousePower);
            _saveManager.Save();
        }

        private void UpdateUpgradeCosts()
        {
            var keyboardCost = _gameManager.CalculateUpgradeCost(_gameManager.KeyboardPower);
            var mouseCost = _gameManager.CalculateUpgradeCost(_gameManager.MousePower);
            int gold = _gameManager.Gold;

            KeyboardCostText.Text = $"💰 {keyboardCost}";
            MouseCostText.Text = $"💰 {mouseCost}";

            // 골드 부족 시 버튼 비활성화 및 비용 텍스트 색상 변경
            bool canBuyKeyboard = gold >= keyboardCost;
            bool canBuyMouse = gold >= mouseCost;

            UpgradeKeyboardBtn.IsEnabled = canBuyKeyboard;
            UpgradeMouseBtn.IsEnabled = canBuyMouse;

            // 비용 텍스트 색상: 구매 가능 시 금색, 불가 시 빨간색
            KeyboardCostText.Foreground = new SolidColorBrush(
                canBuyKeyboard ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 100, 100));
            MouseCostText.Foreground = new SolidColorBrush(
                canBuyMouse ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 100, 100));
        }

        private void OnExitRequested(object? sender, EventArgs e)
        {
            DeskWarrior.Helpers.Logger.Log("OnExitRequested: Before Close()");
            Close();
            DeskWarrior.Helpers.Logger.Log("OnExitRequested: After Close()");
        }

        private void OnStatsChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(UpdateAllUI);
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(UpdateTimerUI);
        }

        private void OnMonsterDefeated(object? sender, EventArgs e)
        {
            // 통계 업데이트
            _saveManager.AddKill();

            // 보스 처치 시 추적
            if (_gameManager.CurrentMonster?.IsBoss == true)
            {
                _saveManager.AddBossKill();
            }

            // 업적 체크
            _achievementManager.CheckAchievements("monster_kills");
            _achievementManager.CheckAchievements("bosses_defeated");
            _achievementManager.CheckAchievements("max_level");

            Dispatcher.Invoke(() =>
            {
                // 처치 사운드
                _soundManager.Play(SoundType.Defeat);

                // 최고 레벨 갱신 및 저장
                if (_gameManager.CurrentLevel > _saveManager.CurrentSave.Stats.MaxLevel)
                {
                    _saveManager.UpdateMaxLevel(_gameManager.CurrentLevel);
                    _saveManager.Save();
                }

                // 처치 효과 (간단한 플래시)
                FlashEffect();
            });
        }

        private void OnGameOver(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(StartGameOverSequence);
        }

        private void StartGameOverSequence()
        {
            // 1. 입력 차단 (게임 플레이 영역 비활성화)
            if (MainBackgroundBorder != null)
                MainBackgroundBorder.IsHitTestVisible = false;
            
            // 2. 몬스터 거대화 연출 (Smash Animation)
            // XAML에 ScaleTransform이 없어서 크기 조절로 대체
            var growAnim = new DoubleAnimation
            {
                To = 500, // 화면을 가득 채울 정도로 커짐
                Duration = TimeSpan.FromSeconds(1.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } // 점점 빠르게
            };

            var opacityAnim = new DoubleAnimation
            {
                To = 0,
                BeginTime = TimeSpan.FromSeconds(1.2), // 커지다가 사라짐 (암전)
                Duration = TimeSpan.FromSeconds(0.3)
            };

            MonsterImage.BeginAnimation(WidthProperty, growAnim);
            MonsterImage.BeginAnimation(HeightProperty, growAnim);
            
            // 흔들림 효과 증폭
            var shakeAnim = new DoubleAnimation
            {
                From = -5, To = 5,
                Duration = TimeSpan.FromMilliseconds(50),
                RepeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(1.5)),
                AutoReverse = true
            };
            MonsterShakeTransform.BeginAnimation(TranslateTransform.XProperty, shakeAnim);

            // 3. 암전 및 리포트 표시 (지연 실행)
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                ShowLifeReport();
            };
            timer.Start();
            
            _soundManager.Play(SoundType.GameOver); // 쿵 소리?
        }

        private void ShowLifeReport()
        {
            DeskWarrior.Helpers.Logger.Log("=== GAME OVER START ===");

            // 사망 타입 판단
            string? deathType = null;
            if (_gameManager.CurrentMonster != null && _gameManager.CurrentMonster.IsBoss)
            {
                deathType = "boss";
            }
            else if (_gameManager.RemainingTime <= 0)
            {
                deathType = "timeout";
            }
            else
            {
                deathType = "normal";
            }
            DeskWarrior.Helpers.Logger.Log($"DeathType: {deathType}");

            // 세션 저장
            var sessionStats = _gameManager.CreateSessionStats(deathType ?? "timeout");
            _saveManager.SaveSession(sessionStats);
            DeskWarrior.Helpers.Logger.Log("Session Saved");

            // 업적 체크 (세션 관련)
            _achievementManager.CheckAchievements("total_sessions");
            _achievementManager.CheckAchievements("total_gold_earned");
            _achievementManager.CheckAchievements("total_playtime_minutes");
            _achievementManager.CheckAchievements("keyboard_inputs");
            _achievementManager.CheckAchievements("mouse_inputs");
            _achievementManager.CheckAchievements("consecutive_days");

            // 게임 오버 메시지 선택
            GameOverMessageText.Text = _gameManager.GetGameOverMessage(deathType);

            // 데이터 바인딩
            ReportLevelText.Text = $"{_gameManager.CurrentLevel}";
            ReportGoldText.Text = $"{_gameManager.SessionTotalGold:N0}";
            ReportDamageText.Text = $"{_gameManager.SessionDamage:N0}";

            // 오버레이 표시
            GameOverOverlay.Opacity = 0;
            GameOverOverlay.Visibility = Visibility.Visible;
            GameOverOverlay.IsHitTestVisible = true;

            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };
            GameOverOverlay.BeginAnimation(OpacityProperty, fadeIn);

            // 배경 투명도 재적용 (로드 시점 문제 방지)
            ApplyBackgroundOpacity(_saveManager.CurrentSave.Settings.BackgroundOpacity);

            // 몬스터 크기/흔들림 초기화
            MonsterImage.BeginAnimation(WidthProperty, null);
            MonsterImage.BeginAnimation(HeightProperty, null);
            MonsterImage.Width = MONSTER_SIZE;
            MonsterImage.Height = MONSTER_SIZE;
            MonsterShakeTransform.BeginAnimation(TranslateTransform.XProperty, null);

            // 즉시 게임 재시작 (뒤에서 진행)
            _gameManager.RestartGame();

            // 10초 후 오버레이 자동 닫기 타이머 시작
            _autoRestartCountdown = 10;
            UpdateAutoCloseCountdown();
            _autoRestartTimer.Start();

            DeskWarrior.Helpers.Logger.Log("=== GAME OVER END ===");
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

        private void CloseOverlayButton_Click(object sender, RoutedEventArgs e)
        {
            _autoRestartTimer.Stop();
            CloseGameOverOverlay();
        }

        private void OnDamageDealt(object? sender, DamageEventArgs e)
        {
            // 통계 업데이트
            _saveManager.AddDamage(e.Damage);

            // 크리티컬 히트 추적
            if (e.IsCritical)
            {
                _saveManager.AddCriticalHit();
            }

            // 업적 체크 (데미지 관련)
            _achievementManager.CheckAchievements("total_damage");
            _achievementManager.CheckAchievements("max_damage");
            _achievementManager.CheckAchievements("critical_hits");

            Dispatcher.Invoke(() =>
            {
                ShowDamagePopup(e.Damage, e.IsCritical);
            });
        }

        #endregion

        #region Private Methods

        private bool IsMouseOverWindow()
        {
            if (Win32Helper.GetCursorPos(out var pt))
            {
                try
                {
                    // 스크린 좌표를 로컬 좌표로 변환
                    var localPoint = PointFromScreen(new System.Windows.Point(pt.x, pt.y));
                    // 윈도우 영역 내에 있는지 확인
                    return localPoint.X >= 0 && localPoint.X < ActualWidth &&
                           localPoint.Y >= 0 && localPoint.Y < ActualHeight;
                }
                catch
                {
                    return false;
                }
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
                // 관리 모드가 아닐 때만 Click-through 처리
                if (!_isManageMode)
                {
                    // 게임 오버 오버레이가 표시 중이면 클릭 허용
                    if (GameOverOverlay.Visibility == Visibility.Visible)
                    {
                        handled = true;
                        return new IntPtr(HTCLIENT);
                    }

                    // 마우스 좌표 가져오기
                    int x = (short)(lParam.ToInt32() & 0xFFFF);
                    int y = (short)(lParam.ToInt32() >> 16);
                    Point screenPoint = new Point(x, y);
                    Point clientPoint = PointFromScreen(screenPoint);

                    // 모드 전환 버튼 영역 확인
                    if (IsPointOverModeButton(clientPoint))
                    {
                        handled = true;
                        return new IntPtr(HTCLIENT);  // 클릭 받음
                    }

                    // 나머지 영역은 투과
                    handled = true;
                    return new IntPtr(HTTRANSPARENT);
                }
            }

            return IntPtr.Zero;
        }

        private void HoverCheckTimer_Tick(object? sender, EventArgs e)
        {
            // 관리 모드에서는 버튼 항상 표시
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
            // 관리 모드에서는 숨기지 않음
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
            catch
            {
                return false;
            }
        }

        private void UpdateAllUI()
        {
            UpdateLevelUI();
            UpdateGoldUI();
            UpdateInputCountUI();
            UpdatePowerUI();
            UpdateMonsterUI();
            UpdateTimerUI();
        }

        private void UpdateLevelUI()
        {
            LevelText.Text = $"Lv.{_gameManager.CurrentLevel}";
            MaxLevelText.Text = $"(Best: {Math.Max(_gameManager.CurrentLevel, _saveManager.CurrentSave.Stats.MaxLevel)})";
        }

        private void UpdateGoldUI()
        {
            GoldText.Text = $"💰 {_gameManager.Gold}";
        }

        private void UpdateInputCountUI()
        {
            InputCountText.Text = $"⌨️ {_sessionInputCount}";
        }

        private void UpdatePowerUI()
        {
            KeyboardPowerText.Text = $"⌨️ Atk: {_gameManager.KeyboardPower}";
            MousePowerText.Text = $"🖱️ Atk: {_gameManager.MousePower}";
        }

        private void UpdateMonsterUI()
        {
            var monster = _gameManager.CurrentMonster;
            if (monster == null) return;

            UpdateMonsterSpriteUI(monster);
            UpdateMonsterHpUI(monster);
        }

        private void UpdateMonsterSpriteUI(Models.Monster monster)
        {
            MonsterEmoji.Text = monster.Emoji;

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
                MonsterImage.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(needsFlip ? -1 : 1, 1));
                transformGroup.Children.Add(MonsterShakeTransform);
                MonsterImage.RenderTransform = transformGroup;
            }
            catch (Exception ex)
            {
                DeskWarrior.Helpers.Logger.Log($"Monster image load failed: {ex.Message}");
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

        private void UpdateMonsterHpUI(Models.Monster monster)
        {
            HpText.Text = $"{monster.CurrentHp}/{monster.MaxHp}";

            var hpRatio = monster.HpRatio;
            double targetWidth = hpRatio * 80;

            var widthAnim = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            HpBar.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);

            Color targetColor = hpRatio > 0.5 ? Color.FromRgb(0, 255, 0)
                              : hpRatio > 0.25 ? Color.FromRgb(255, 255, 0)
                              : Color.FromRgb(255, 0, 0);

            HpBar.Background = new SolidColorBrush(targetColor);
        }

        private void UpdateTimerUI()
        {
            int time = _gameManager.RemainingTime;
            TimerText.Text = time.ToString();

            // 타이머 색상 및 긴급 상태 애니메이션
            if (time > 20)
            {
                TimerText.BeginAnimation(OpacityProperty, null); // 깜빡임 중지
                TimerText.Opacity = 1.0;
                TimerText.Foreground = new SolidColorBrush(Color.FromRgb(135, 206, 235)); // 하늘색
            }
            else if (time > 10)
            {
                TimerText.BeginAnimation(OpacityProperty, null);
                TimerText.Opacity = 1.0;
                TimerText.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 0)); // 노란색
            }
            else
            {
                TimerText.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0)); // 빨간색

                // 5초 미만일 때 깜빡임 효과
                if (time <= 5 && time > 0)
                {
                    var blinkAnim = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.3,
                        Duration = TimeSpan.FromMilliseconds(300),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    TimerText.BeginAnimation(OpacityProperty, blinkAnim);
                }
            }
        }

        private void ShakeMonster()
        {
            double shakePower = _gameManager.Config.Visual.ShakePower;
            double offsetX = (_random.NextDouble() - 0.5) * 2 * shakePower;
            double offsetY = (_random.NextDouble() - 0.5) * 2 * shakePower;

            // 흔들림 애니메이션
            var animX = new DoubleAnimation
            {
                From = offsetX,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(50),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var animY = new DoubleAnimation
            {
                From = offsetY,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(50),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            MonsterShakeTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            MonsterShakeTransform.BeginAnimation(TranslateTransform.YProperty, animY);

            // 피격 시 Opacity 플래시 효과
            var opacityFlash = new DoubleAnimation
            {
                From = 1.0,
                To = 0.5,
                Duration = TimeSpan.FromMilliseconds(80),
                AutoReverse = true
            };
            MonsterImage.BeginAnimation(OpacityProperty, opacityFlash);
        }

        private void ShowDamagePopup(int damage, bool isCritical = false)
        {
            var popup = new Controls.DamagePopup(damage, isCritical);
            
            // 랜덤 위치
            double x = 30 + _random.NextDouble() * 40;
            double y = 30 + _random.NextDouble() * 30;
            
            Canvas.SetLeft(popup, x);
            Canvas.SetTop(popup, y);
            
            DamagePopupCanvas.Children.Add(popup);
            
            popup.Animate(() =>
            {
                DamagePopupCanvas.Children.Remove(popup);
            });
        }

        private void FlashEffect()
        {
            // 처치 시 골드 획득 강조 효과
            var goldReward = _gameManager.CurrentMonster?.GoldReward ?? 0;
            
            // 골드 텍스트 색상 애니메이션
            var brush = new SolidColorBrush(Colors.Gold);
            GoldText.Foreground = brush;
            
            var colorAnim = new ColorAnimation
            {
                From = Colors.White,
                To = Colors.Gold,
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = true
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
            
            // 골드 획득 팝업 (간단히 디버그로 표시)
            DebugText.Text = $"+{goldReward} 💰";
        }

        #endregion

        private void UpdateLocalizedUI()
        {
            var loc = LocalizationManager.Instance;

            // 업그레이드 버튼
            if (UpgradeKeyboardText != null) UpgradeKeyboardText.Text = loc["ui.main.upgradeKeyboard"];
            if (UpgradeMouseText != null) UpgradeMouseText.Text = loc["ui.main.upgradeMouse"];

            // 하단 버튼
            if (StatsBtn != null) StatsBtn.Content = loc["ui.main.stats"];
            if (SettingsBtn != null) SettingsBtn.Content = loc["ui.main.settings"];

            // 공격력 표시
            if (KeyboardPowerText != null)
                KeyboardPowerText.Text = $"{loc["ui.main.keyboardAtk"]}: {_gameManager?.KeyboardPower ?? 1:N0}";
            if (MousePowerText != null)
                MousePowerText.Text = $"{loc["ui.main.mouseAtk"]}: {_gameManager?.MousePower ?? 1:N0}";

            // 게임오버 화면
            if (GameOverTitleText != null) GameOverTitleText.Text = loc["ui.gameover.title"];
            if (ReportLevelLabel != null) ReportLevelLabel.Text = loc["ui.gameover.maxLevel"];
            if (ReportGoldLabel != null) ReportGoldLabel.Text = loc["ui.gameover.goldEarned"];
            if (ReportDamageLabel != null) ReportDamageLabel.Text = loc["ui.gameover.damageDealt"];
            if (CloseOverlayButton != null) CloseOverlayButton.Content = loc.CurrentLanguage == "ko-KR" ? "닫기" : "Close";

            // 툴팁
            if (UpgradeKeyboardBtn != null) UpgradeKeyboardBtn.ToolTip = loc["tooltips.upgradeKeyboard"];
            if (UpgradeMouseBtn != null) UpgradeMouseBtn.ToolTip = loc["tooltips.upgradeMouse"];
            if (StatsBtn != null) StatsBtn.ToolTip = loc["tooltips.stats"];
            if (SettingsBtn != null) SettingsBtn.ToolTip = loc["tooltips.settings"];
            if (ExitButtonBorder != null) ExitButtonBorder.ToolTip = loc["tooltips.exit"];
        }

        #region Achievement Toast

        private void OnAchievementUnlocked(object? sender, AchievementUnlockedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // 큐에 추가
                _toastQueue.Enqueue(e.Achievement);

                // 표시 중이 아니면 표시 시작
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

            // 토스트 생성
            var toast = new Controls.AchievementToast();
            toast.HorizontalAlignment = HorizontalAlignment.Right;
            toast.VerticalAlignment = VerticalAlignment.Bottom;
            toast.Margin = new Thickness(0, 0, 10, 10);

            // 토스트 표시 (메인 그리드에 추가)
            var mainGrid = Content as Grid;
            if (mainGrid != null)
            {
                Panel.SetZIndex(toast, 999);
                mainGrid.Children.Add(toast);

                toast.AnimationCompleted += (s, args) =>
                {
                    mainGrid.Children.Remove(toast);
                    ShowNextToast(); // 다음 토스트 표시
                };

                toast.Show(achievement);
                _soundManager.Play(SoundType.Upgrade); // 업적 해금 사운드
            }
            else
            {
                _isShowingToast = false;
            }
        }

        #endregion
    }
}
