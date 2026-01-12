using System;
using System.Windows;

namespace DeskWarrior.Windows
{
    public partial class DragToggleWindow : Window
    {
        public event EventHandler? ToggleRequested;

        public DragToggleWindow()
        {
            InitializeComponent();
        }

        public void UpdateIcon(bool isDragMode)
        {
            DragModeIcon.Text = isDragMode ? "🔒" : "🔓";
            ToggleButton.Background = isDragMode 
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xCC, 0x55, 0x44, 0x00))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xCC, 0x33, 0x33, 0x33));
        }

        public void UpdatePosition(double mainWindowLeft, double mainWindowTop, double mainWindowWidth)
        {
            Left = mainWindowLeft + mainWindowWidth - Width - 5;
            Top = mainWindowTop + 5;
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
