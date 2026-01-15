using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DeskWarrior.Managers; // For SoundManager logic (via Window)
using DeskWarrior.Models;
using DeskWarrior.Interfaces; // For events

namespace DeskWarrior.ViewControllers
{
    public class VisualEffectController
    {
        private readonly MainWindow _window;
        private readonly Random _random = new();

        // Achievement Toast Queue
        private readonly Queue<AchievementDefinition> _toastQueue = new();
        private bool _isShowingToast;

        private const double MONSTER_SIZE = 80;
        private const double BOSS_SIZE = 130;

        public VisualEffectController(MainWindow window)
        {
            _window = window;
        }

        public void ShakeMonster(double shakePower)
        {
            // shakePower comes from GameManager via MainWindow usually, or passed in.
            // But here we might just take the value.
            
            double offsetX = (_random.NextDouble() - 0.5) * 2 * shakePower;
            double offsetY = (_random.NextDouble() - 0.5) * 2 * shakePower;

            var animX = new DoubleAnimation
            {
                From = offsetX, To = 0,
                Duration = TimeSpan.FromMilliseconds(50),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var animY = new DoubleAnimation
            {
                From = offsetY, To = 0,
                Duration = TimeSpan.FromMilliseconds(50),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            _window.MonsterShakeTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            _window.MonsterShakeTransform.BeginAnimation(TranslateTransform.YProperty, animY);

            var opacityFlash = new DoubleAnimation
            {
                From = 1.0, To = 0.5,
                Duration = TimeSpan.FromMilliseconds(80),
                AutoReverse = true
            };
            _window.MonsterImage.BeginAnimation(UIElement.OpacityProperty, opacityFlash);
        }

        public void ShowDamagePopup(int damage, bool isCritical)
        {
            var popup = new Controls.DamagePopup(damage, isCritical);
            double x = 30 + _random.NextDouble() * 40;
            double y = 30 + _random.NextDouble() * 30;

            Canvas.SetLeft(popup, x);
            Canvas.SetTop(popup, y);
            _window.DamagePopupCanvas.Children.Add(popup);

            popup.Animate(() => _window.DamagePopupCanvas.Children.Remove(popup));
        }

        public void FlashEffect(int goldReward)
        {
            var brush = new SolidColorBrush(Colors.Gold);
            _window.GoldText.Foreground = brush;

            var colorAnim = new ColorAnimation
            {
                From = Colors.White, To = Colors.Gold,
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = true
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
            _window.DebugText.Text = $"+{goldReward} 💰";
        }

        public void BossEntranceEffect()
        {
            _window.DebugText.Text = "⚠️ BOSS APPEARED!";
            _window.DebugText.Foreground = new SolidColorBrush(Colors.Purple);
            _window.MonsterImage.Width = BOSS_SIZE;
            _window.MonsterImage.Height = BOSS_SIZE;
        }

        public void OnAchievementUnlocked(AchievementDefinition achievement, SoundManager soundManager)
        {
            _toastQueue.Enqueue(achievement);
            if (!_isShowingToast)
            {
                ShowNextToast(soundManager);
            }
        }

        private void ShowNextToast(SoundManager soundManager)
        {
            if (_toastQueue.Count == 0)
            {
                _isShowingToast = false;
                return;
            }

            _isShowingToast = true;
            var achievement = _toastQueue.Dequeue();

            var toast = new Controls.AchievementToast();
            toast.HorizontalAlignment = HorizontalAlignment.Right;
            toast.VerticalAlignment = VerticalAlignment.Bottom;
            toast.Margin = new Thickness(0, 0, 10, 10);

            var mainGrid = _window.Content as Grid;
            if (mainGrid != null)
            {
                Panel.SetZIndex(toast, 999);
                mainGrid.Children.Add(toast);

                toast.AnimationCompleted += (s, args) =>
                {
                    mainGrid.Children.Remove(toast);
                    ShowNextToast(soundManager);
                };

                toast.Show(achievement);
                soundManager.Play(SoundType.Upgrade);
            }
            else
            {
                _isShowingToast = false;
            }
        }
    }
}
