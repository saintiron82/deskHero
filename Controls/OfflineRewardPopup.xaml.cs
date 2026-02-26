using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using DeskWarrior.Models;

namespace DeskWarrior.Controls
{
    /// <summary>
    /// 오프라인 보상 팝업 컨트롤
    /// </summary>
    public partial class OfflineRewardPopup : UserControl
    {
        public event EventHandler? ClaimClicked;

        private OfflineRewardResult _result;

        public OfflineRewardPopup()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 보상 정보 설정
        /// </summary>
        public void SetReward(OfflineRewardResult result, string message, string headerText = "WELCOME BACK!")
        {
            _result = result;
            HeaderText.Text = headerText;
            MessageText.Text = message;
            GoldText.Text = $"+{result.Gold:N0}";
            CrystalText.Text = $"+{result.Crystals:N0}";
        }

        /// <summary>
        /// 팝업 표시
        /// </summary>
        public void Show()
        {
            Visibility = Visibility.Visible;
            var showAnimation = FindResource("ShowAnimation") as Storyboard;
            showAnimation?.Begin(this);
        }

        /// <summary>
        /// 팝업 숨기기
        /// </summary>
        public void Hide()
        {
            var hideAnimation = FindResource("HideAnimation") as Storyboard;
            if (hideAnimation != null)
            {
                hideAnimation.Completed += (s, e) => Visibility = Visibility.Collapsed;
                hideAnimation.Begin(this);
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }
        }

        private void ClaimButton_Click(object sender, RoutedEventArgs e)
        {
            ClaimClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }
    }
}
