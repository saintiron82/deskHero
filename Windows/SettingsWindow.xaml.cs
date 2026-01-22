using System;
using System.Windows;
using System.Windows.Controls;
using DeskWarrior.Interfaces;
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
        private readonly IGameManager? _gameManager;
        private readonly SaveManager? _saveManager;
        private bool _isInitializing = true;

        public SettingsWindow(UserSettings settings, Action<double> onWindowOpacityChanged, Action<double> onOpacityChanged, Action<double> onVolumeChanged, Action? onLanguageChanged = null, IGameManager? gameManager = null, SaveManager? saveManager = null)
        {
            InitializeComponent();
            _settings = settings;
            _onWindowOpacityChanged = onWindowOpacityChanged;
            _onOpacityChanged = onOpacityChanged;
            _onVolumeChanged = onVolumeChanged;
            _onLanguageChanged = onLanguageChanged;
            _gameManager = gameManager;
            _saveManager = saveManager;

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

            // DEBUG 빌드에서만 Balance Test Tool 버튼 표시
#if DEBUG
            BalanceTestBtn.Visibility = Visibility.Visible;
#else
            BalanceTestBtn.Visibility = Visibility.Collapsed;
#endif

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
            => HandleSliderChanged(WindowOpacityValueText, e.NewValue, v => { _settings.WindowOpacity = v; _onWindowOpacityChanged?.Invoke(v); });

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => HandleSliderChanged(OpacityValueText, e.NewValue, v => { _settings.BackgroundOpacity = v; _onOpacityChanged?.Invoke(v); });

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => HandleSliderChanged(VolumeValueText, e.NewValue, v => { _settings.Volume = v; _onVolumeChanged?.Invoke(v); });

        private static void HandleSliderChanged(System.Windows.Controls.TextBlock? textBlock, double value, Action<double> updateAction)
        {
            if (textBlock == null) return;
            textBlock.Text = $"{(int)(value * 100)}%";
            updateAction(value);
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

        private void BalanceTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (_gameManager == null || _saveManager == null)
            {
                MessageBox.Show("Balance Test Tool requires GameManager and SaveManager.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var balanceTestWindow = new BalanceTestWindow(_gameManager, _saveManager);
            balanceTestWindow.Owner = this;
            balanceTestWindow.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
