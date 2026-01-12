using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using DeskWarrior.Managers;
using DeskWarrior.Models;

namespace DeskWarrior.Controls
{
    public partial class AchievementToast : UserControl
    {
        public event EventHandler? AnimationCompleted;

        public AchievementToast()
        {
            InitializeComponent();

            // 다국어 헤더 텍스트 적용
            HeaderText.Text = LocalizationManager.Instance["ui.toast.achievementUnlocked"];
        }

        public void Show(AchievementDefinition achievement)
        {
            IconText.Text = achievement.Icon;
            TitleText.Text = achievement.Name;
            DescriptionText.Text = string.IsNullOrEmpty(achievement.UnlockMessage)
                ? achievement.Description
                : achievement.UnlockMessage;

            // Play show animation
            var showStoryboard = (Storyboard)Resources["ShowAnimation"];
            showStoryboard.Begin(this);

            // Play hide animation after delay
            var hideStoryboard = (Storyboard)Resources["HideAnimation"];
            hideStoryboard.Completed += (s, e) => AnimationCompleted?.Invoke(this, EventArgs.Empty);
            hideStoryboard.Begin(this);
        }
    }
}
