using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DeskWarrior.Helpers;
using DeskWarrior.Interfaces;
using DeskWarrior.Managers;
using DeskWarrior.ViewModels;

namespace DeskWarrior.ViewControllers
{
    public class GameOverController : IDisposable
    {
        private readonly MainWindow _window;
        private readonly GameOverViewModel _viewModel;
        private System.Windows.Threading.DispatcherTimer? _autoRestartTimer;
        private int _autoRestartCountdown;
        private bool _isShowingGameOver;

        public GameOverController(MainWindow window)
        {
            _window = window;
            _viewModel = new GameOverViewModel();

            // ViewModel 커맨드 설정
            _viewModel.ShopCommand = new RelayCommand(_ => OnShopCommand());
            _viewModel.CloseCommand = new RelayCommand(_ => OnCloseCommand());

            // UserControl에 ViewModel 바인딩
            _window.GameOverOverlayControl.DataContext = _viewModel;

            InitializeTimer();
        }

        private void OnShopCommand()
        {
            StopTimer();
            CloseGameOverOverlay();
            // MainWindow의 OpenPermanentUpgradeShop 호출은 MainWindow에서 처리
        }

        private void OnCloseCommand()
        {
            StopTimer();
            CloseGameOverOverlay();
        }

        private void InitializeTimer()
        {
            _autoRestartTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _autoRestartTimer.Tick += AutoRestartTimer_Tick;
        }

        public bool IsShowingGameOver => _isShowingGameOver;

        public void StartGameOverSequence(SoundManager soundManager)
        {
            // 이미 게임 오버 화면이 표시 중이면 무시 (재진입 방지)
            if (_isShowingGameOver)
                return;

            _isShowingGameOver = true;

            if (_window.MainBackgroundBorder != null)
                _window.MainBackgroundBorder.IsHitTestVisible = false;

            // Phase 1 (0~0.7s): 몬스터 돌진 - 가속하며 히어로에 도달
            var moveX = new DoubleAnimation
            {
                From = 0, To = -60,
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            var moveY = new DoubleAnimation
            {
                From = 0, To = 50,
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            _window.MonsterMoveTransform.BeginAnimation(TranslateTransform.XProperty, moveX);
            _window.MonsterMoveTransform.BeginAnimation(TranslateTransform.YProperty, moveY);

            var scaleUp = new DoubleAnimation
            {
                From = 1.0, To = 1.25,
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            _window.MonsterScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
            _window.MonsterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);

            var shakeAnim = new DoubleAnimation
            {
                From = -4, To = 4,
                Duration = TimeSpan.FromMilliseconds(40),
                RepeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(1.5)),
                AutoReverse = true
            };
            _window.MonsterShakeTransform.BeginAnimation(TranslateTransform.XProperty, shakeAnim);

            // Phase 2 (0.7s~): 충돌 후 히어로가 날아감
            var heroRotateAnim = new DoubleAnimation
            {
                From = 0,
                To = -540,
                BeginTime = TimeSpan.FromSeconds(0.7),
                Duration = TimeSpan.FromSeconds(0.8),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            _window.HeroRotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, heroRotateAnim);

            var heroFlyX = new DoubleAnimation
            {
                From = 0,
                To = -180,
                BeginTime = TimeSpan.FromSeconds(0.7),
                Duration = TimeSpan.FromSeconds(0.8),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            _window.HeroTranslateTransform.BeginAnimation(TranslateTransform.XProperty, heroFlyX);

            var heroFlyY = new DoubleAnimation
            {
                From = 0,
                To = -100,
                BeginTime = TimeSpan.FromSeconds(0.7),
                Duration = TimeSpan.FromSeconds(0.8),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            _window.HeroTranslateTransform.BeginAnimation(TranslateTransform.YProperty, heroFlyY);

            var heroFadeAnim = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                BeginTime = TimeSpan.FromSeconds(0.8),
                Duration = TimeSpan.FromSeconds(0.5)
            };
            _window.HeroImage.BeginAnimation(UIElement.OpacityProperty, heroFadeAnim);

            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                ShowLifeReport();
            };
            timer.Start();

            soundManager.Play(SoundType.GameOver);
        }

        private void ShowLifeReport()
        {
            Logger.Log("=== GAME OVER START ===");

            // Access ViewModel/Managers via Window properties
            var vm = _window.ViewModel;
            var gameManager = vm.GameManager;
            var achievementManager = vm.AchievementManager;
            var saveManager = vm.SaveManager;

            string? deathType = gameManager.CurrentMonster?.IsBoss == true ? "boss"
                : gameManager.RemainingTime <= 0 ? "timeout" : "normal";

            // 세션 통계 수집 (크리스탈 변환 전)
            long sessionGold = gameManager.SessionTotalGold;
            long sessionDamage = gameManager.SessionDamage;
            int sessionLevel = gameManager.CurrentLevel;
            int sessionKills = gameManager.SessionKills;

            // 세션 중 획득한 크리스탈 (보스 드롭, 업적 보상, 스테이지 클리어)
            int bossDropCrystals = gameManager.SessionBossDropCrystals;
            int achievementCrystals = gameManager.SessionAchievementCrystals;
            int stageClearCrystals = gameManager.SessionStageClearCrystals;

            // 골드 → 크리스탈 변환 (1000:1)
            int convertedCrystals = (int)(sessionGold / 1000);

            // 세션 저장 (크리스탈이 자동으로 지급됨)
            vm.SaveSession();

            // 업적 체크
            achievementManager.CheckAchievements("total_sessions");
            achievementManager.CheckAchievements("total_gold_earned");
            achievementManager.CheckAchievements("total_playtime_minutes");
            achievementManager.CheckAchievements("keyboard_inputs");
            achievementManager.CheckAchievements("mouse_inputs");
            achievementManager.CheckAchievements("consecutive_days");

            // 세션 후 크리스탈 잔액
            long crystalsAfterSession = saveManager.CurrentSave.PermanentCurrency.Crystals;
            int totalEarned = stageClearCrystals + bossDropCrystals + achievementCrystals + convertedCrystals;

            // ViewModel 업데이트 - 세션 통계
            _viewModel.GameOverMessage = gameManager.GetGameOverMessage(deathType);
            _viewModel.LevelText = $"{sessionLevel}";
            _viewModel.KillsText = $"{sessionKills}";
            _viewModel.GoldText = $"{sessionGold:N0}";
            _viewModel.DamageText = $"{sessionDamage:N0}";

            // ViewModel 업데이트 - 크리스탈
            _viewModel.BossDropCrystals = bossDropCrystals;
            _viewModel.AchievementCrystals = achievementCrystals;
            _viewModel.TotalCrystalsEarned = totalEarned;
            _viewModel.CurrentCrystalBalance = crystalsAfterSession;

            // 오버레이 표시
            _window.GameOverOverlayControl.Opacity = 0;
            _window.GameOverOverlayControl.Visibility = Visibility.Visible;
            _window.GameOverOverlayControl.IsHitTestVisible = true;

            var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.5) };
            _window.GameOverOverlayControl.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            _window.ApplyBackgroundOpacity(saveManager.CurrentSave.Settings.BackgroundOpacity);

            // 몬스터 애니메이션 리셋
            _window.MonsterShakeTransform.BeginAnimation(TranslateTransform.XProperty, null);
            _window.MonsterMoveTransform.BeginAnimation(TranslateTransform.XProperty, null);
            _window.MonsterMoveTransform.BeginAnimation(TranslateTransform.YProperty, null);
            _window.MonsterScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            _window.MonsterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            _window.MonsterMoveTransform.X = 0;
            _window.MonsterMoveTransform.Y = 0;
            _window.MonsterScaleTransform.ScaleX = 1;
            _window.MonsterScaleTransform.ScaleY = 1;

            // 히어로 애니메이션 리셋
            _window.HeroImage.BeginAnimation(UIElement.OpacityProperty, null);
            _window.HeroRotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null);
            _window.HeroTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
            _window.HeroTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
            _window.HeroImage.Opacity = 1.0;

            // 자동 닫기 타이머 시작 (게임 재시작은 오버레이 닫힐 때 수행)
            _autoRestartCountdown = 7;
            UpdateAutoCloseCountdown();
            _autoRestartTimer?.Start();

            Logger.Log("=== GAME OVER END ===");
        }

        public void UpdateAutoCloseCountdown()
        {
            var loc = LocalizationManager.Instance;
            _viewModel.CountdownText = loc.Format("ui.gameover.closesIn", _autoRestartCountdown);
        }

        public void StopTimer()
        {
            _autoRestartTimer?.Stop();
        }

        private void AutoRestartTimer_Tick(object? sender, EventArgs e)
        {
            _autoRestartCountdown--;
            UpdateAutoCloseCountdown();
            if (_autoRestartCountdown <= 0)
            {
                _autoRestartTimer?.Stop();
                CloseGameOverOverlay();
            }
        }

        public void CloseGameOverOverlay()
        {
            // 오버레이 애니메이션 정리 후 숨기기
            _window.GameOverOverlayControl.BeginAnimation(UIElement.OpacityProperty, null);
            _window.GameOverOverlayControl.Visibility = Visibility.Collapsed;
            if (_window.MainBackgroundBorder != null)
                _window.MainBackgroundBorder.IsHitTestVisible = true;

            _isShowingGameOver = false;

            // 게임 재시작 (오버레이가 닫힌 후 수행)
            _window.ViewModel.GameManager.RestartGame();

            // Trigger UI update
            _window.UpdateAllUI();
        }

        public void Dispose()
        {
            _autoRestartTimer?.Stop();
        }
    }
}
