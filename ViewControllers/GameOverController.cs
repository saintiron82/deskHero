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

        private const double MONSTER_SIZE = 80;

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

        public void StartGameOverSequence(SoundManager soundManager)
        {
            if (_window.MainBackgroundBorder != null)
                _window.MainBackgroundBorder.IsHitTestVisible = false;

            var growAnim = new DoubleAnimation
            {
                To = 500,
                Duration = TimeSpan.FromSeconds(1.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            _window.MonsterImage.BeginAnimation(FrameworkElement.WidthProperty, growAnim);
            _window.MonsterImage.BeginAnimation(FrameworkElement.HeightProperty, growAnim);

            var shakeAnim = new DoubleAnimation
            {
                From = -5, To = 5,
                Duration = TimeSpan.FromMilliseconds(50),
                RepeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(1.5)),
                AutoReverse = true
            };
            _window.MonsterShakeTransform.BeginAnimation(TranslateTransform.XProperty, shakeAnim);

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
            int sessionGold = (int)gameManager.SessionTotalGold;
            long sessionDamage = gameManager.SessionDamage;
            int sessionLevel = gameManager.CurrentLevel;
            int sessionKills = gameManager.SessionKills;

            // 세션 중 획득한 크리스탈 (보스 드롭, 업적 보상)
            int bossDropCrystals = gameManager.SessionBossDropCrystals;
            int achievementCrystals = gameManager.SessionAchievementCrystals;

            // 골드 → 크리스탈 변환 (1000:1)
            int convertedCrystals = sessionGold / 1000;

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
            int totalEarned = convertedCrystals + bossDropCrystals + achievementCrystals;

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
            _window.MonsterImage.BeginAnimation(FrameworkElement.WidthProperty, null);
            _window.MonsterImage.BeginAnimation(FrameworkElement.HeightProperty, null);
            _window.MonsterImage.Width = MONSTER_SIZE;
            _window.MonsterImage.Height = MONSTER_SIZE;
            _window.MonsterShakeTransform.BeginAnimation(TranslateTransform.XProperty, null);

            // 게임 재시작
            gameManager.RestartGame();

            // 자동 닫기 타이머 시작
            _autoRestartCountdown = 10;
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
            _window.GameOverOverlayControl.Visibility = Visibility.Collapsed;
            if (_window.MainBackgroundBorder != null)
                _window.MainBackgroundBorder.IsHitTestVisible = true;

            // Trigger UI update
            _window.UpdateAllUI();
        }

        public void Dispose()
        {
            _autoRestartTimer?.Stop();
        }
    }
}
