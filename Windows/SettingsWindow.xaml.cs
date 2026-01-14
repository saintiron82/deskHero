using System;
using System.Windows;
using System.Windows.Controls;
using DeskWarrior.Managers;
using DeskWarrior.Models;

namespace DeskWarrior.Windows
{
    public partial class SettingsWindow : Window
    {
        private readonly UserSettings _settings;
        private readonly Action<double> _onWindowOpacityChanged;
        private readonly Action<double> _onOpacityChanged;
        private readonly Action<double> _onVolumeChanged;
        private readonly Action? _onLanguageChanged;
        private bool _isInitializing = true;

        public SettingsWindow(UserSettings settings, Action<double> onWindowOpacityChanged, Action<double> onOpacityChanged, Action<double> onVolumeChanged, Action? onLanguageChanged = null)
        {
            InitializeComponent();
            _settings = settings;
            _onWindowOpacityChanged = onWindowOpacityChanged;
            _onOpacityChanged = onOpacityChanged;
            _onVolumeChanged = onVolumeChanged;
            _onLanguageChanged = onLanguageChanged;

            // 초기값 설정
            WindowOpacitySlider.Value = _settings.WindowOpacity;
            OpacitySlider.Value = _settings.BackgroundOpacity;
            VolumeSlider.Value = _settings.Volume;

            // 언어 선택 초기화
            InitializeLanguageSelection();

            // UI 텍스트 업데이트
            UpdateUIText();

            // 언어 변경 이벤트 구독
            LocalizationManager.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Item[]")
                {
                    Dispatcher.Invoke(UpdateUIText);
                }
            };

            _isInitializing = false;
        }

        private void InitializeLanguageSelection()
        {
            string currentLanguage = LocalizationManager.Instance.CurrentLanguage;

            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == currentLanguage)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void UpdateUIText()
        {
            var loc = LocalizationManager.Instance;
            TitleText.Text = loc["ui.settings.title"];
            WindowOpacityLabel.Text = loc["ui.settings.windowOpacity"];
            OpacityLabel.Text = loc["ui.settings.opacity"];
            VolumeLabel.Text = loc["ui.settings.volume"];
            LanguageLabel.Text = loc["ui.settings.language"];
            CloseBtn.Content = loc["ui.settings.close"];
        }

        private void WindowOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (WindowOpacityValueText == null) return;

            WindowOpacityValueText.Text = $"{(int)(e.NewValue * 100)}%";
            _settings.WindowOpacity = e.NewValue;
            _onWindowOpacityChanged?.Invoke(e.NewValue);
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

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string languageCode = selectedItem.Tag?.ToString() ?? "ko-KR";

                // 언어 변경
                LocalizationManager.Instance.SetLanguage(languageCode);

                // 설정에 저장
                _settings.Language = languageCode;

                // 콜백 호출
                _onLanguageChanged?.Invoke();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
