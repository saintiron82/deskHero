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

            // 통합된 애니메이션 실행 (Show + Hide를 하나로)
            var storyboard = (Storyboard)Resources["ToastAnimation"];
            storyboard.Completed += (s, e) => AnimationCompleted?.Invoke(this, EventArgs.Empty);
            storyboard.Begin(this);
        }
    }
}
