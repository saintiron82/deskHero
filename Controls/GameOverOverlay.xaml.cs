using System.Windows;
using System.Windows.Controls;
using DeskWarrior.ViewModels;

namespace DeskWarrior.Controls
{
    /// <summary>
    /// GameOverOverlay UserControl
    /// </summary>
    public partial class GameOverOverlay : UserControl
    {
        public GameOverOverlay()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ViewModel 접근자
        /// </summary>
        public GameOverViewModel? ViewModel => DataContext as GameOverViewModel;

        /// <summary>
        /// 버튼 텍스트 업데이트 (다국어 지원)
        /// </summary>
        public void UpdateButtonTexts(string shopText, string closeText)
        {
            ShopButton.Content = shopText;
            CloseOverlayButton.Content = closeText;
        }
    }
}
