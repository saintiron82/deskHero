using System;
using System.Windows;
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
        private readonly Random _random = new();
        
        private IntPtr _hwnd;
        private bool _isDragMode;
        private int _sessionInputCount;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            // 매니저 초기화
            _inputHandler = new GlobalInputManager();
            _trayManager = new TrayManager();
            _saveManager = new SaveManager();
            _gameManager = new GameManager();

            // 이벤트 연결
            _inputHandler.OnInput += OnInputReceived;
            _trayManager.DragModeToggled += OnDragModeToggled;
            _trayManager.ExitRequested += OnExitRequested;
            
            // 게임 이벤트 연결
            _gameManager.StatsChanged += OnStatsChanged;
            _gameManager.TimerTick += OnTimerTick;
            _gameManager.MonsterDefeated += OnMonsterDefeated;
            _gameManager.GameOver += OnGameOver;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            LocationChanged += MainWindow_LocationChanged;
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

            // 게임 시작
            _gameManager.StartGame();

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
                    _trayManager.ToggleDragMode();
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
            Dispatcher.Invoke(() =>
            {
                // 처치 효과 (간단한 플래시)
                FlashEffect();
            });
        }

        private void OnGameOver(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Hard Reset 효과
                GameOverEffect();
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

        private void UpdateAllUI()
        {
            var monster = _gameManager.CurrentMonster;
            
            // 레벨 표시
            LevelText.Text = $"Lv.{_gameManager.CurrentLevel}";
            
            // 골드 표시
            GoldText.Text = $"💰 {_gameManager.Gold}";
            
            // 입력 수 표시
            InputCountText.Text = $"⌨️ {_sessionInputCount}";
            
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
                }
                catch { }
                
                // HP 텍스트
                HpText.Text = $"{monster.CurrentHp}/{monster.MaxHp}";
                
                // HP 바 너비 (80px 기준)
                var hpRatio = monster.HpRatio;
                HpBar.Width = hpRatio * 80;
                
                // HP 바 색상 (초록 → 노랑 → 빨강)
                if (hpRatio > 0.5)
                    HpBar.Background = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                else if (hpRatio > 0.25)
                    HpBar.Background = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                else
                    HpBar.Background = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            }
            
            UpdateTimerUI();
        }

        private void UpdateTimerUI()
        {
            int time = _gameManager.RemainingTime;
            TimerText.Text = time.ToString();
            
            // 타이머 색상
            if (time > 20)
                TimerText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            else if (time > 10)
                TimerText.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 0));
            else
                TimerText.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        }

        private void ShakeMonster()
        {
            double shakePower = _gameManager.Config.Visual.ShakePower;
            double offsetX = (_random.NextDouble() - 0.5) * 2 * shakePower;
            double offsetY = (_random.NextDouble() - 0.5) * 2 * shakePower;

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

            MonsterShakeTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, animX);
            MonsterShakeTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, animY);
        }

        private void FlashEffect()
        {
            // 처치 시 간단한 효과 (골드 텍스트 강조)
            var anim = new ColorAnimation
            {
                From = Colors.Yellow,
                To = Colors.Gold,
                Duration = TimeSpan.FromMilliseconds(200),
                AutoReverse = true
            };
            GoldText.Foreground = new SolidColorBrush(Colors.Gold);
        }

        private void GameOverEffect()
        {
            // Hard Reset 시 효과
            DebugText.Text = "⚠️ TIME OVER - RESET!";
        }

        #endregion
    }
}
