using System.Windows;
using System.Windows.Input;

namespace DeskWarrior.Windows
{
    /// <summary>
    /// 공용 도움말 팝업
    /// </summary>
    public partial class HelpPopup : Window
    {
        public HelpPopup(string title, string content)
        {
            InitializeComponent();

            TitleText.Text = $"❓ {title}";
            ContentText.Text = content;
        }

        /// <summary>
        /// 헤더 드래그로 창 이동
        /// </summary>
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// 닫기 버튼
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
