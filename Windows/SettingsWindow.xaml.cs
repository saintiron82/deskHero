using System;
using System.Windows;
using DeskWarrior.Models;

namespace DeskWarrior.Windows
{
    public partial class SettingsWindow : Window
    {
        private readonly UserSettings _settings;
        private readonly Action<double> _onOpacityChanged;
        private readonly Action<double> _onVolumeChanged;

        public SettingsWindow(UserSettings settings, Action<double> onOpacityChanged, Action<double> onVolumeChanged)
        {
            InitializeComponent();
            _settings = settings;
            _onOpacityChanged = onOpacityChanged;
            _onVolumeChanged = onVolumeChanged;

            // 초기값 설정
            OpacitySlider.Value = _settings.BackgroundOpacity;
            VolumeSlider.Value = _settings.Volume;
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityValueText == null) return;
            
            OpacityValueText.Text = $"{(int)(e.NewValue * 100)}%";
            _settings.BackgroundOpacity = e.NewValue;
            _onOpacityChanged?.Invoke(e.NewValue);
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VolumeValueText == null) return;

            VolumeValueText.Text = $"{(int)(e.NewValue * 100)}%";
            _settings.Volume = e.NewValue;
            _onVolumeChanged?.Invoke(e.NewValue);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
