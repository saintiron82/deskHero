using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DeskWarrior.Helpers;
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
        private readonly ISoundManager? _soundManager;
        private bool _isInitializing = true;

        public SettingsWindow(
            UserSettings settings,
            Action<double> onWindowOpacityChanged,
            Action<double> onOpacityChanged,
            Action<double> onVolumeChanged,
            Action? onLanguageChanged = null,
            IGameManager? gameManager = null,
            SaveManager? saveManager = null,
            ISoundManager? soundManager = null)
        {
            InitializeComponent();
            _settings = settings;
            _onWindowOpacityChanged = onWindowOpacityChanged;
            _onOpacityChanged = onOpacityChanged;
            _onVolumeChanged = onVolumeChanged;
            _onLanguageChanged = onLanguageChanged;
            _gameManager = gameManager;
            _saveManager = saveManager;
            _soundManager = soundManager;

            // 초기값 설정
            WindowOpacitySlider.Value = _settings.WindowOpacity;
            OpacitySlider.Value = _settings.BackgroundOpacity;
            VolumeSlider.Value = _settings.Volume;
            SoundEnabledCheckBox.IsChecked = _settings.SoundEnabled;

            // 언어 선택 초기화
            InitializeLanguageSelection();

            // 사운드팩 선택 초기화
            InitializeSoundPackSelection();

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

        private void InitializeSoundPackSelection()
        {
            if (_soundManager == null) return;

            SoundPackComboBox.Items.Clear();

            var loc = LocalizationManager.Instance;
            string currentLang = loc.CurrentLanguage;

            foreach (var pack in _soundManager.AvailableSoundPacks)
            {
                // 로컬라이즈된 이름 사용 (한국어면 NameLocalized, 아니면 Name)
                string displayName = currentLang.StartsWith("ko") && !string.IsNullOrEmpty(pack.NameLocalized)
                    ? pack.NameLocalized
                    : pack.Name;

                // 커스텀 팩 표시
                if (pack.IsCustom)
                {
                    displayName = $"★ {displayName}";
                }

                var item = new ComboBoxItem
                {
                    Content = displayName,
                    Tag = pack.Id,
                    Foreground = System.Windows.Media.Brushes.White
                };

                SoundPackComboBox.Items.Add(item);

                // 현재 선택된 팩 설정
                if (pack.Id == _settings.SoundPack || pack.Id == _soundManager.CurrentSoundPackId)
                {
                    SoundPackComboBox.SelectedItem = item;
                }
            }

            // 선택된 항목이 없으면 첫 번째 선택
            if (SoundPackComboBox.SelectedItem == null && SoundPackComboBox.Items.Count > 0)
            {
                SoundPackComboBox.SelectedIndex = 0;
            }
        }

        private void UpdateUIText()
        {
            var loc = LocalizationManager.Instance;
            TitleText.Text = loc["ui.settings.title"];
            WindowOpacityLabel.Text = loc["ui.settings.windowOpacity"];
            OpacityLabel.Text = loc["ui.settings.opacity"];
            VolumeLabel.Text = loc["ui.settings.volume"];
            SoundPackLabel.Text = loc["ui.settings.soundPack"];
            LanguageLabel.Text = loc["ui.settings.language"];
            ResetGameBtn.Content = loc["ui.settings.resetGame"];
            CloseBtn.Content = loc["ui.settings.close"];
            SoundDetailBtn.Content = loc["ui.settings.soundDetail"];

            // 사운드팩 목록 갱신 (언어 변경 시 이름 업데이트)
            if (!_isInitializing)
            {
                InitializeSoundPackSelection();
            }
        }

        private void WindowOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => HandleSliderChanged(WindowOpacityValueText, e.NewValue, v => { _settings.WindowOpacity = v; _onWindowOpacityChanged?.Invoke(v); });

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => HandleSliderChanged(OpacityValueText, e.NewValue, v => { _settings.BackgroundOpacity = v; _onOpacityChanged?.Invoke(v); });

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => HandleSliderChanged(VolumeValueText, e.NewValue, v => { _settings.Volume = v; _onVolumeChanged?.Invoke(v); });

        private void SoundEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _soundManager == null) return;
            bool enabled = SoundEnabledCheckBox.IsChecked == true;
            _settings.SoundEnabled = enabled;
            _soundManager.Enabled = enabled;
        }

        private static void HandleSliderChanged(System.Windows.Controls.TextBlock? textBlock, double value, Action<double> updateAction)
        {
            if (textBlock == null) return;
            textBlock.Text = $"{(int)(value * 100)}%";
            updateAction(value);
        }

        private void SoundPackComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _soundManager == null) return;

            if (SoundPackComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string packId = selectedItem.Tag?.ToString() ?? "default";

                if (_soundManager.ChangeSoundPack(packId))
                {
                    _settings.SoundPack = packId;

                    // 테스트 사운드 재생
                    _soundManager.Play(SoundType.KeyboardHit);
                }
            }
        }

        private void OpenSoundFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var customPath = Path.Combine(baseDir, "Assets", "Sounds", "Custom");

                // 폴더가 없으면 생성
                Directory.CreateDirectory(customPath);

                // 폴더 열기
                Process.Start(new ProcessStartInfo
                {
                    FileName = customPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.LogError("[SettingsWindow] Failed to open custom sound folder", ex);
            }
        }

        private void SoundDetailButton_Click(object sender, RoutedEventArgs e)
        {
            if (_soundManager == null) return;

            try
            {
                var soundSettingsWindow = new SoundSettingsWindow(_soundManager, _settings);
                soundSettingsWindow.Owner = this;
                soundSettingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.LogError("[SettingsWindow] Failed to open SoundSettingsWindow", ex);
            }
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

        private void ResetGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (_saveManager == null)
            {
                return;
            }

            var loc = LocalizationManager.Instance;

            // 확인 대화상자 표시
            var result = MessageBox.Show(
                loc["ui.settings.resetConfirmMessage"],
                loc["ui.settings.resetConfirmTitle"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // 모든 데이터 초기화
                _saveManager.ResetAllData();

                // 게임 재시작을 위해 애플리케이션 재시작
                System.Diagnostics.Process.Start(Environment.ProcessPath ?? "");
                Application.Current.Shutdown();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // F12: 개발자 밸런스 테스트 창
            if (e.Key == Key.F12 && _gameManager != null && _saveManager != null)
            {
                try
                {
                    var balanceWindow = new BalanceTestWindow(_gameManager, _saveManager);
                    balanceWindow.Owner = this;
                    balanceWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    Logger.LogError("[SettingsWindow] Failed to open BalanceTestWindow", ex);
                }
                e.Handled = true;
            }

            // F5: 사운드팩 새로고침
            if (e.Key == Key.F5 && _soundManager != null)
            {
                _soundManager.RefreshSoundPacks();
                InitializeSoundPackSelection();
                e.Handled = true;
            }
        }
    }
}
