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

            // Access ViewModel/Managers via Window properties (Assuming they will be exposed)
            var vm = _window.ViewModel; 
            var gameManager = vm.GameManager;
            var achievementManager = vm.AchievementManager;
            var saveManager = vm.SaveManager;

            string? deathType = gameManager.CurrentMonster?.IsBoss == true ? "boss"
                : gameManager.RemainingTime <= 0 ? "timeout" : "normal";

            vm.SaveSession();

            achievementManager.CheckAchievements("total_sessions");
            achievementManager.CheckAchievements("total_gold_earned");
            achievementManager.CheckAchievements("total_playtime_minutes");
            achievementManager.CheckAchievements("keyboard_inputs");
            achievementManager.CheckAchievements("mouse_inputs");
            achievementManager.CheckAchievements("consecutive_days");

            _window.GameOverMessageText.Text = gameManager.GetGameOverMessage(deathType);
            _window.ReportLevelText.Text = $"{gameManager.CurrentLevel}";
            _window.ReportGoldText.Text = $"{gameManager.SessionTotalGold:N0}";
            _window.ReportDamageText.Text = $"{gameManager.SessionDamage:N0}";

            _window.GameOverOverlay.Opacity = 0;
            _window.GameOverOverlay.Visibility = Visibility.Visible;
            _window.GameOverOverlay.IsHitTestVisible = true;

            var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.5) };
            _window.GameOverOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            // Apply Background Opacity (via internal method or duplicated logic)
            // For now, calling the public method on window or duplicating logic. 
            // Better to expose ApplyBackgroundOpacity as internal on MainWindow.
            _window.ApplyBackgroundOpacity(saveManager.CurrentSave.Settings.BackgroundOpacity);

            _window.MonsterImage.BeginAnimation(FrameworkElement.WidthProperty, null);
            _window.MonsterImage.BeginAnimation(FrameworkElement.HeightProperty, null);
            _window.MonsterImage.Width = MONSTER_SIZE;
            _window.MonsterImage.Height = MONSTER_SIZE;
            _window.MonsterShakeTransform.BeginAnimation(TranslateTransform.XProperty, null);

            gameManager.RestartGame();

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
