using System;
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

        private readonly IInputHandler _inputHandler;
        private readonly TrayManager _trayManager;
        private readonly SaveManager _saveManager;
        private readonly GameManager _gameManager;
        private readonly SoundManager _soundManager;
        private readonly Random _random = new();
        
        private IntPtr _hwnd;
        private bool _isDragMode;
        private int _sessionInputCount;

        // Auto Restart
        private System.Windows.Threading.DispatcherTimer _autoRestartTimer;
        private int _autoRestartCountdown;

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

            // Auto Restart Timer
            _autoRestartTimer = new System.Windows.Threading.DispatcherTimer();
            _autoRestartTimer.Interval = TimeSpan.FromSeconds(1);
            _autoRestartTimer.Tick += AutoRestartTimer_Tick;

            // 이벤트 구독
            _inputHandler.OnInput += OnInputReceived;
            _trayManager.DragModeToggled += OnDragModeToggled;
            _trayManager.ExitRequested += OnExitRequested;

            _gameManager.DamageDealt += OnDamageDealt;
            _gameManager.MonsterDefeated += OnMonsterDefeated;
            _gameManager.TimerTick += OnTimerTick;
            _gameManager.StatsChanged += OnStatsChanged;
            _gameManager.GameOver += OnGameOver;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            LocationChanged += MainWindow_LocationChanged;

            // 초기 UI 업데이트
            UpdateUI();
            
            // 게임 시작
            _gameManager.StartGame();
        }

        #endregion

        #region Event Handlers

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;

            // 저장 데이터 로드
            _saveManager.Load();
            
            // 저장된 위치 복원
            Left = _saveManager.CurrentSave.Position.X;
            Top = _saveManager.CurrentSave.Position.Y;

            // Click-through 설정 (초기 상태)
            SetClickThrough(true);

            // 태스크바에서 숨기기
            Win32Helper.SetWindowToolWindow(_hwnd);

            // 트레이 아이콘 초기화
            _trayManager.Initialize();

            // 입력 감지 시작
            _inputHandler.Start();

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
            // 히어로 이미지 크로마 키 처리
            try
            {
                HeroImage.Source = ImageHelper.LoadWithChromaKey(
                    "pack://application:,,,/Assets/Images/hero.png");
            }
            catch { }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 위치 저장
            _saveManager.UpdateWindowPosition(Left, Top);
            _saveManager.Save();

            // 리소스 정리
            _inputHandler.OnInput -= OnInputReceived;
            _inputHandler.Dispose();
            _trayManager.Dispose();
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            _saveManager.UpdateWindowPosition(Left, Top);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDragMode)
            {
                DragMove();
            }
        }

        private void OnInputReceived(object? sender, GameInputEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // F1 키로 드래그 모드 토글 (VK_F1 = 112)
                if (e.Type == GameInputType.Keyboard && e.VirtualKeyCode == 112)
                {
                    // 마우스가 게임 창 위에 있을 때만 작동
                    if (IsMouseOverWindow())
                    {
                        _trayManager.ToggleDragMode();
                    }
                    return;
                }

                // 드래그 모드일 때는 게임 입력 무시
                if (_isDragMode) return;

                // 입력 카운트 증가
                _sessionInputCount++;
                _saveManager.AddInput();

                // 게임 로직에 입력 전달
                if (e.Type == GameInputType.Keyboard)
                {
                    _gameManager.OnKeyboardInput();
                }
                else
                {
                    _gameManager.OnMouseInput();
                }

                // 데미지 팝업 표시 (Event 기반으로 변경됨)
                // int damage = e.Type == GameInputType.Keyboard 
                //    ? _gameManager.KeyboardPower 
                //    : _gameManager.MousePower;
                // ShowDamagePopup(damage);

                // 공격 사운드
                _soundManager.Play(SoundType.Hit);

                // 몬스터 흔들림 효과
                ShakeMonster();

                // 디버그 텍스트
                string inputInfo = e.Type == GameInputType.Keyboard
                    ? $"⌨️ Key:{e.VirtualKeyCode}"
                    : $"🖱️ {e.MouseButton}";
                DebugText.Text = inputInfo;
            });
        }

        private void OnDragModeToggled(object? sender, EventArgs e)
        {
            _isDragMode = _trayManager.IsDragMode;
            SetClickThrough(!_isDragMode);
            DragModeBorder.Visibility = _isDragMode ? Visibility.Visible : Visibility.Collapsed;
            UpgradePanel.Visibility = _isDragMode ? Visibility.Visible : Visibility.Collapsed;
            
            // 업그레이드 비용 업데이트
            if (_isDragMode) UpdateUpgradeCosts();
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

        private void ToggleDragMode()
        {
            _isDragMode = !_isDragMode;
            if (_isDragMode)
            {
                DragModeBorder.Visibility = Visibility.Visible;
                UpgradePanel.Visibility = Visibility.Visible;
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
            }
            else
            {
                DragModeBorder.Visibility = Visibility.Collapsed;
                UpgradePanel.Visibility = Visibility.Collapsed;
                this.ResizeMode = ResizeMode.NoResize;
                
                // 위치 저장
                _saveManager.UpdateWindowPosition(Left, Top); // Changed from UpdatePosition to UpdateWindowPosition
                _saveManager.Save();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 설정 창 열기 (모달)
            var settingsWindow = new Windows.SettingsWindow(
                _saveManager.CurrentSave.Settings,
                (opacity) => {
                    ApplyBackgroundOpacity(opacity);
                },
                (volume) => {
                    _soundManager.Volume = volume;
                }
            );
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
            
            _saveManager.Save();
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            // 통계 창 열기
            var statsWindow = new Windows.StatisticsWindow(_saveManager.CurrentSave.Stats);
            statsWindow.Owner = this;
            statsWindow.ShowDialog();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ApplyBackgroundOpacity(double opacity)
        {
            // 각 패널마다 기본 투명도 비율이 다를 수 있음
            // 적 정보 / 타이머: 기본 0.4 (최대 0.8)
            double infoOpacity = Math.Clamp(opacity, 0.0, 0.8);
            
            // 업그레이드 패널: 기본 0.6 (최대 0.9)
            double upgradeOpacity = Math.Clamp(opacity * 1.5, 0.0, 0.95);

            if (MainBackgroundBorder != null)
                MainBackgroundBorder.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x2e)) { Opacity = opacity };

            if (EnemyInfoBorder != null) 
                EnemyInfoBorder.Background = new SolidColorBrush(Colors.Black) { Opacity = infoOpacity };
            
            if (TimerBorder != null) 
                TimerBorder.Background = new SolidColorBrush(Colors.Black) { Opacity = infoOpacity };
            
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
            ApplyBackgroundOpacity(settings.BackgroundOpacity);
            _soundManager.Volume = settings.Volume;
            
            // Auto Restart 설정 로드
            AutoRestartCheckBox.IsChecked = settings.AutoRestart;
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
            Close();
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

            // 게임 오버 메시지 선택
            GameOverMessageText.Text = _gameManager.GetGameOverMessage(deathType);

            // 데이터 바인딩
            ReportLevelText.Text = $"{_gameManager.CurrentLevel}";
            ReportGoldText.Text = $"{_gameManager.SessionTotalGold:N0}";
            ReportDamageText.Text = $"{_gameManager.SessionDamage:N0}";

            // 오버레이 표시
            GameOverOverlay.Opacity = 0;
            GameOverOverlay.Visibility = Visibility.Visible;
            GameOverOverlay.IsHitTestVisible = true; // 반응 즉시 가능하도록 명시
            
            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };
            GameOverOverlay.BeginAnimation(OpacityProperty, fadeIn);
            
            // 배경 투명도 재적용 (로드 시점 문제 방지)
            ApplyBackgroundOpacity(_saveManager.CurrentSave.Settings.BackgroundOpacity);

            // Auto Restart 시작 확인
            if (AutoRestartCheckBox.IsChecked == true)
            {
                _autoRestartCountdown = 10;
                UpdateAutoRestartText();
                _autoRestartTimer.Start();
            }
            else
            {
                _autoRestartTimer.Stop();
                AutoRestartCheckBox.Content = "Auto Restart in 10s";
            }
        }

        private void UpdateAutoRestartText()
        {
            AutoRestartCheckBox.Content = $"Auto Restart in {_autoRestartCountdown}s";
        }

        private void AutoRestartTimer_Tick(object? sender, EventArgs e)
        {
            _autoRestartCountdown--;
            UpdateAutoRestartText();

            if (_autoRestartCountdown <= 0)
            {
                _autoRestartTimer.Stop();
                NewLifeButton_Click(this, new RoutedEventArgs());
            }
        }

        private void AutoRestart_Checked(object sender, RoutedEventArgs e)
        {
            // 설정 저장
            _saveManager.CurrentSave.Settings.AutoRestart = true;
            _saveManager.Save();
            
            // 이미 게임오버 상태라면 타이머 시작
            if (GameOverOverlay.Visibility == Visibility.Visible)
            {
                _autoRestartCountdown = 10;
                UpdateAutoRestartText();
                _autoRestartTimer.Start();
            }
        }

        private void AutoRestart_Unchecked(object sender, RoutedEventArgs e)
        {
            // 설정 저장
             _saveManager.CurrentSave.Settings.AutoRestart = false;
            _saveManager.Save();
            
            // 타이머 중지
            _autoRestartTimer.Stop();
            AutoRestartCheckBox.Content = "Auto Restart in 10s";
        }

        private void NewLifeButton_Click(object sender, RoutedEventArgs e)
        {
            // 타이머 중지 확인
            _autoRestartTimer.Stop();
            
            // 오버레이 숨기기
            GameOverOverlay.Visibility = Visibility.Collapsed;
            
            // 게임 플레이 영역 활성화
            if (MainBackgroundBorder != null)
                MainBackgroundBorder.IsHitTestVisible = true;
            
            // 몬스터 크기 초기화
            MonsterImage.BeginAnimation(WidthProperty, null);
            MonsterImage.BeginAnimation(HeightProperty, null);
            MonsterImage.Width = 100;
            MonsterImage.Height = 100;
            
            // 흔들림 초기화
            MonsterShakeTransform.BeginAnimation(TranslateTransform.XProperty, null);

            // 게임 리스타트
            _gameManager.RestartGame();
        }

        private void OnDamageDealt(object? sender, DamageEventArgs e)
        {
            // 통계 업데이트
            _saveManager.AddDamage(e.Damage);

            Dispatcher.Invoke(() =>
            {
                ShowDamagePopup(e.Damage, e.IsCritical);
            });
        }

        #endregion

        #region Private Methods

        private void SetClickThrough(bool enabled)
        {
            if (enabled)
            {
                Win32Helper.SetWindowClickThrough(_hwnd);
            }
            else
            {
                int extendedStyle = Win32Helper.GetWindowLong(_hwnd, Win32Helper.GWL_EXSTYLE);
                Win32Helper.SetWindowLong(_hwnd, Win32Helper.GWL_EXSTYLE, 
                    extendedStyle & ~Win32Helper.WS_EX_TRANSPARENT);
            }
        }

        private bool IsMouseOverWindow()
        {
            if (!Win32Helper.GetCursorPos(out var pt)) return false;

            // 현재 창의 화면 좌표 범위 계산
            Point topLeft = PointToScreen(new Point(0, 0));
            Point bottomRight = PointToScreen(new Point(ActualWidth, ActualHeight));

            // 마우스 좌표가 범위 내에 있는지 확인
            return pt.x >= topLeft.X && pt.x <= bottomRight.X &&
                   pt.y >= topLeft.Y && pt.y <= bottomRight.Y;
        }

        private void UpdateAllUI()
        {
            var monster = _gameManager.CurrentMonster;
            
            // 레벨 표시
            LevelText.Text = $"Lv.{_gameManager.CurrentLevel}";
            MaxLevelText.Text = $"(Best: {Math.Max(_gameManager.CurrentLevel, _saveManager.CurrentSave.Stats.MaxLevel)})";
            
            // 골드 표시
            GoldText.Text = $"💰 {_gameManager.Gold}";
            
            // 입력 수 표시
            InputCountText.Text = $"⌨️ {_sessionInputCount}";
            
            // 공격력 표시
            KeyboardPowerText.Text = $"⌨️ Atk: {_gameManager.KeyboardPower}";
            MousePowerText.Text = $"🖱️ Atk: {_gameManager.MousePower}";
            
            // 몬스터 정보
            if (monster != null)
            {
                // 이모지 업데이트 (보스 vs 일반)
                MonsterEmoji.Text = monster.IsBoss ? " 👿" : " 👹";
                
                // 이미지 업데이트 (보스 vs 일반) - 크로마 키 처리
                try
                {
                    string imagePath = monster.IsBoss 
                        ? "pack://application:,,,/Assets/Images/boss.png" 
                        : "pack://application:,,,/Assets/Images/monster.png";
                    MonsterImage.Source = ImageHelper.LoadWithChromaKey(imagePath);
                    
                    // 보스는 더 크게
                    MonsterImage.Width = monster.IsBoss ? 120 : 100;
                    MonsterImage.Height = monster.IsBoss ? 120 : 100;
                }
                catch { }
                
                // HP 텍스트
                HpText.Text = $"{monster.CurrentHp}/{monster.MaxHp}";

                // HP 바 애니메이션 (80px 기준)
                var hpRatio = monster.HpRatio;
                double targetWidth = hpRatio * 80;

                var widthAnim = new DoubleAnimation
                {
                    To = targetWidth,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                HpBar.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);

                // HP 바 색상 애니메이션 (초록 → 노랑 → 빨강)
                Color targetColor;
                if (hpRatio > 0.5)
                    targetColor = Color.FromRgb(0, 255, 0);
                else if (hpRatio > 0.25)
                    targetColor = Color.FromRgb(255, 255, 0);
                else
                    targetColor = Color.FromRgb(255, 0, 0);

                var hpBrush = new SolidColorBrush(targetColor);
                HpBar.Background = hpBrush;
            }
            
            UpdateTimerUI();
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
                TimerText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                TimerBorder.Background = new SolidColorBrush(Color.FromArgb(0x44, 0, 0, 0));
            }
            else if (time > 10)
            {
                TimerText.BeginAnimation(OpacityProperty, null);
                TimerText.Opacity = 1.0;
                TimerText.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                TimerBorder.Background = new SolidColorBrush(Color.FromArgb(0x66, 255, 165, 0));
            }
            else
            {
                TimerText.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                TimerBorder.Background = new SolidColorBrush(Color.FromArgb(0x88, 255, 0, 0));

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

        private void GameOverEffect()
        {
            // Hard Reset 시 화면 붉은 플래시 효과
            DebugText.Text = "⚠️ TIME OVER - RESET!";
            DebugText.Foreground = new SolidColorBrush(Colors.Red);
            
            // 타이머 색상 깜빡임
            var flashAnim = new ColorAnimation
            {
                From = Colors.Red,
                To = Colors.DarkRed,
                Duration = TimeSpan.FromMilliseconds(100),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };
            
            var brush = new SolidColorBrush(Colors.Red);
            TimerText.Foreground = brush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, flashAnim);
        }

        private void BossEntranceEffect()
        {
            // 보스 등장 연출
            DebugText.Text = "⚠️ BOSS APPEARED!";
            DebugText.Foreground = new SolidColorBrush(Colors.Purple);
            
            // 몬스터 크기 확대 애니메이션
            MonsterImage.Width = 120;
            MonsterImage.Height = 120;
        }

        #endregion
        private void UpdateUI()
        {
            if (_gameManager == null) return;

            // 레벨, 골드 업데이트
            if (LevelText != null) LevelText.Text = $"Lv.{_gameManager.CurrentLevel}";
            if (MaxLevelText != null) MaxLevelText.Text = $"(Best: {_saveManager.CurrentSave.Stats.MaxLevel})";
            if (GoldText != null) GoldText.Text = $"💰 {_gameManager.Gold:N0}";
            
            // HP 업데이트
            if (_gameManager.CurrentMonster != null && HpText != null)
            {
                HpText.Text = $"{_gameManager.CurrentMonster.CurrentHp:N0}/{_gameManager.CurrentMonster.MaxHp:N0}";
            }

            // 입력 카운트
            if (InputCountText != null) InputCountText.Text = $"⌨️ {_sessionInputCount}";

            // 공격력 업데이트
            if (KeyboardPowerText != null) KeyboardPowerText.Text = $"⌨️ Atk: {_gameManager.KeyboardPower:N0}";
            if (MousePowerText != null) MousePowerText.Text = $"🖱️ Atk: {_gameManager.MousePower:N0}";

            // 업그레이드 비용 업데이트
            UpdateUpgradeCosts();
        }
    }
}
