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

        private void CloseButton_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 애니메이션 중단하고 즉시 숨김
            var storyboard = (Storyboard)Resources["ToastAnimation"];
            storyboard.Stop(this);
            this.Opacity = 0;
            AnimationCompleted?.Invoke(this, EventArgs.Empty);
        }

        public void Show(AchievementDefinition achievement)
        {
            IconText.Text = achievement.Icon;
            TitleText.Text = achievement.Name;
            DescriptionText.Text = string.IsNullOrEmpty(achievement.UnlockMessage)
                ? achievement.Description
                : achievement.UnlockMessage;

            // 통합된 애니메이션 실행 (Show + Hide를 하나로)
            var storyboard = (Storyboard)Resources["ToastAnimation"];
            storyboard.Completed += (s, e) => AnimationCompleted?.Invoke(this, EventArgs.Empty);
            storyboard.Begin(this);
        }
    }
}
