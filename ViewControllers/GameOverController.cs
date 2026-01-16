using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DeskWarrior.Helpers;
using DeskWarrior.Managers;
using DeskWarrior.ViewModels;
using DeskWarrior.Interfaces;

namespace DeskWarrior.ViewControllers
{
    public class GameOverController : IDisposable
    {
        private readonly MainWindow _window;
        private System.Windows.Threading.DispatcherTimer? _autoRestartTimer;
        private int _autoRestartCountdown;
        
        private const double MONSTER_SIZE = 80;

        public GameOverController(MainWindow window)
        {
            _window = window;
            InitializeTimer();
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
            var sessionTracker = gameManager.GetType().GetField("_sessionTracker",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(gameManager) as SessionTracker;

            int bossDropCrystals = sessionTracker?.SessionBossDropCrystals ?? 0;
            int achievementCrystals = sessionTracker?.SessionAchievementCrystals ?? 0;

            // 골드 → 크리스탈 변환 (1000:1)
            int convertedCrystals = sessionGold / 1000;

            // 세션 저장 (크리스탈이 자동으로 지급됨)
            long crystalsBeforeSession = saveManager.CurrentSave.PermanentCurrency.Crystals;
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

            // UI 업데이트 - 세션 통계
            _window.GameOverMessageText.Text = gameManager.GetGameOverMessage(deathType);
            _window.ReportLevelText.Text = $"{sessionLevel}";
            _window.ReportKillsText.Text = $"{sessionKills}";
            _window.ReportGoldText.Text = $"{sessionGold:N0}";
            _window.ReportDamageText.Text = $"{sessionDamage:N0}";

            // UI 업데이트 - 크리스탈 획득 정보
            _window.SessionGoldRun.Text = $"{sessionGold:N0}";
            _window.ConvertedCrystalsRun.Text = $"{convertedCrystals}";

            // 보스 드롭 표시 (있는 경우만)
            if (bossDropCrystals > 0)
            {
                _window.BossDropLine.Visibility = Visibility.Visible;
                _window.BossDropCrystalsRun.Text = $"{bossDropCrystals}";
            }
            else
            {
                _window.BossDropLine.Visibility = Visibility.Collapsed;
            }

            // 업적 보상 표시 (있는 경우만)
            if (achievementCrystals > 0)
            {
                _window.AchievementRewardLine.Visibility = Visibility.Visible;
                _window.AchievementCrystalsRun.Text = $"{achievementCrystals}";
            }
            else
            {
                _window.AchievementRewardLine.Visibility = Visibility.Collapsed;
            }

            // 총 획득 및 현재 보유
            _window.TotalCrystalsEarnedRun.Text = $"{totalEarned}";
            _window.CurrentCrystalBalanceRun.Text = $"{crystalsAfterSession:N0}";

            // 오버레이 표시
            _window.GameOverOverlay.Opacity = 0;
            _window.GameOverOverlay.Visibility = Visibility.Visible;
            _window.GameOverOverlay.IsHitTestVisible = true;

            var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.5) };
            _window.GameOverOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);

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
            _window.AutoCloseCountdownText.Text = loc.CurrentLanguage == "ko-KR"
                ? $"{_autoRestartCountdown}초후 닫힘"
                : $"Closes in {_autoRestartCountdown}s";
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
            _window.GameOverOverlay.Visibility = Visibility.Collapsed;
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
