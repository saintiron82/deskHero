using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DeskWarrior.Helpers;
using DeskWarrior.Interfaces;
using DeskWarrior.Managers;
using DeskWarrior.Models;

namespace DeskWarrior.Windows
{
    public partial class SoundSettingsWindow : Window
    {
        private readonly ISoundManager _soundManager;
        private readonly UserSettings _settings;
        private bool _isInitializing = true;

        // SoundType key → UI 요소 참조
        private readonly Dictionary<string, CheckBox> _checkBoxes = new();
        private readonly Dictionary<string, Slider> _sliders = new();
        private readonly Dictionary<string, TextBlock> _volumeTexts = new();

        // 카테고리별 팩 ComboBox
        private readonly Dictionary<string, ComboBox> _categoryComboBoxes = new();

        public SoundSettingsWindow(ISoundManager soundManager, UserSettings settings)
        {
            InitializeComponent();
            _soundManager = soundManager;
            _settings = settings;

            BuildSoundTypeRows();
            BuildCategoryPackRows();
            PopulateThemeComboBox();
            UpdateUIText();

            LocalizationManager.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Item[]")
                    Dispatcher.Invoke(UpdateUIText);
            };

            _isInitializing = false;
        }

        #region Build UI

        private void BuildSoundTypeRows()
        {
            SoundTypesList.Children.Clear();
            _checkBoxes.Clear();
            _sliders.Clear();
            _volumeTexts.Clear();

            foreach (var category in SoundCategory.AllCategories)
            {
                if (!SoundCategory.CategorySoundTypes.TryGetValue(category, out var types))
                    continue;

                foreach (var type in types)
                {
                    string key = type.ToString();
                    var row = CreateSoundTypeRow(key, type);
                    SoundTypesList.Children.Add(row);
                }
            }
        }

        private Grid CreateSoundTypeRow(string key, SoundType type)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 3) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });

            // CheckBox
            var checkBox = new CheckBox
            {
                Tag = key,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            checkBox.Checked += SoundType_CheckChanged;
            checkBox.Unchecked += SoundType_CheckChanged;
            Grid.SetColumn(checkBox, 0);
            grid.Children.Add(checkBox);
            _checkBoxes[key] = checkBox;

            // Label
            var label = new TextBlock
            {
                Name = $"Label_{key}",
                Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            Grid.SetColumn(label, 1);
            grid.Children.Add(label);

            // Slider
            var slider = new Slider
            {
                Tag = key,
                Minimum = 0,
                Maximum = 1,
                SmallChange = 0.05,
                LargeChange = 0.1,
                IsSnapToTickEnabled = true,
                TickFrequency = 0.05,
                VerticalAlignment = VerticalAlignment.Center
            };
            slider.ValueChanged += SoundType_VolumeChanged;
            Grid.SetColumn(slider, 2);
            grid.Children.Add(slider);
            _sliders[key] = slider;

            // Volume %
            var volText = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right
            };
            Grid.SetColumn(volText, 3);
            grid.Children.Add(volText);
            _volumeTexts[key] = volText;

            // Preview button
            var previewBtn = new Button
            {
                Content = "\u25B6",
                Tag = key,
                Width = 22,
                Height = 22,
                FontSize = 9,
                Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            previewBtn.Click += PreviewSoundType_Click;
            Grid.SetColumn(previewBtn, 4);
            grid.Children.Add(previewBtn);

            return grid;
        }

        private void BuildCategoryPackRows()
        {
            CategoryPackList.Children.Clear();
            _categoryComboBoxes.Clear();

            foreach (var category in SoundCategory.AllCategories)
            {
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });

                var label = new TextBlock
                {
                    Name = $"CatLabel_{category}",
                    Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                var comboBox = new ComboBox
                {
                    Tag = category,
                    Height = 24,
                    Margin = new Thickness(6, 0, 6, 0),
                    Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                    Foreground = Brushes.Gray,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x2E, 0x2E, 0x2E)),
                    BorderThickness = new Thickness(1)
                };
                ApplyDarkComboBoxStyle(comboBox);
                comboBox.SelectionChanged += CategoryPack_SelectionChanged;
                Grid.SetColumn(comboBox, 1);
                grid.Children.Add(comboBox);
                _categoryComboBoxes[category] = comboBox;

                var previewBtn = new Button
                {
                    Content = "\u25B6",
                    Tag = category,
                    Width = 22,
                    Height = 22,
                    FontSize = 9,
                    Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                previewBtn.Click += PreviewCategory_Click;
                Grid.SetColumn(previewBtn, 2);
                grid.Children.Add(previewBtn);

                CategoryPackList.Children.Add(grid);
            }

            PopulateCategoryComboBoxes();
        }

        #endregion

        #region Populate

        private void PopulateThemeComboBox()
        {
            ThemeComboBox.Items.Clear();
            var loc = LocalizationManager.Instance;

            foreach (var theme in _soundManager.AvailableSoundThemes)
            {
                string displayName = loc[theme.NameKey];
                if (string.IsNullOrEmpty(displayName) || displayName == theme.NameKey)
                    displayName = theme.Id;

                var item = new ComboBoxItem
                {
                    Content = displayName,
                    Tag = theme.Id,
                    Foreground = Brushes.White
                };
                ThemeComboBox.Items.Add(item);

                if (theme.Id == _settings.SoundThemeId)
                    ThemeComboBox.SelectedItem = item;
            }

            if (ThemeComboBox.SelectedItem == null && ThemeComboBox.Items.Count > 0)
                ThemeComboBox.SelectedIndex = 0;

            // 현재 테마 값으로 UI 채우기
            PopulateSoundTypeValues();
        }

        private void PopulateSoundTypeValues()
        {
            _isInitializing = true;

            // 현재 활성 테마 찾기
            SoundThemeData? theme = null;
            foreach (var t in _soundManager.AvailableSoundThemes)
            {
                if (t.Id == _soundManager.CurrentThemeId)
                {
                    theme = t;
                    break;
                }
            }

            var loc = LocalizationManager.Instance;

            foreach (var kvp in _checkBoxes)
            {
                string key = kvp.Key;
                var cb = kvp.Value;
                var slider = _sliders[key];
                var volText = _volumeTexts[key];

                // Override 먼저, 없으면 테마 기본값
                double vol;
                if (_settings.SoundThemeOverrides.TryGetValue(key, out var overrideVol))
                    vol = overrideVol;
                else if (theme != null)
                    vol = theme.GetVolume(key);
                else
                    vol = 1.0;

                bool enabled = vol > 0;
                cb.IsChecked = enabled;
                slider.Value = enabled ? vol : 1.0;
                slider.IsEnabled = enabled;
                slider.Opacity = enabled ? 1.0 : 0.4;
                volText.Text = enabled ? $"{(int)(vol * 100)}%" : "--";

                // 라벨 업데이트
                string locKey = $"sound.type.{key}";
                string labelText = loc[locKey];
                if (string.IsNullOrEmpty(labelText) || labelText == locKey)
                    labelText = key;

                // StackPanel의 children에서 라벨 찾기
                var parent = cb.Parent as Grid;
                if (parent != null && parent.Children.Count > 1 && parent.Children[1] is TextBlock tb)
                    tb.Text = labelText;
            }

            _isInitializing = false;
        }

        private void PopulateCategoryComboBoxes()
        {
            _isInitializing = true;

            var loc = LocalizationManager.Instance;
            string currentLang = loc.CurrentLanguage;
            var packs = _soundManager.AvailableSoundPacks;
            var currentCategoryPacks = _soundManager.CategorySoundPackIds;

            foreach (var kvp in _categoryComboBoxes)
            {
                string category = kvp.Key;
                var comboBox = kvp.Value;
                comboBox.Items.Clear();

                string selectedPackId = currentCategoryPacks.TryGetValue(category, out var id)
                    ? id
                    : _soundManager.CurrentSoundPackId;

                foreach (var pack in packs)
                {
                    string displayName = currentLang.StartsWith("ko") && !string.IsNullOrEmpty(pack.NameLocalized)
                        ? pack.NameLocalized
                        : pack.Name;

                    if (pack.IsCustom)
                        displayName = $"★ {displayName}";

                    var item = new ComboBoxItem
                    {
                        Content = displayName,
                        Tag = pack.Id,
                        Foreground = Brushes.White
                    };

                    comboBox.Items.Add(item);

                    if (pack.Id == selectedPackId)
                        comboBox.SelectedItem = item;
                }

                if (comboBox.SelectedItem == null && comboBox.Items.Count > 0)
                    comboBox.SelectedIndex = 0;

                // 카테고리 라벨 업데이트
                var parent = comboBox.Parent as Grid;
                if (parent != null && parent.Children[0] is TextBlock label)
                {
                    string catKey = $"ui.soundSettings.{category.ToLowerInvariant()}";
                    string catText = loc[catKey];
                    label.Text = string.IsNullOrEmpty(catText) || catText == catKey ? category : catText;
                }
            }

            _isInitializing = false;
        }

        #endregion

        #region Event Handlers

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            if (ThemeComboBox.SelectedItem is not ComboBoxItem selected) return;

            string themeId = selected.Tag?.ToString() ?? "default";

            if (_soundManager.ApplySoundTheme(themeId))
            {
                _settings.SoundThemeId = themeId;
                _settings.SoundThemeOverrides.Clear();
                PopulateSoundTypeValues();
            }

            UpdateDeleteButtonState(themeId);
        }

        private void UpdateDeleteButtonState(string themeId)
        {
            bool isCustom = false;
            foreach (var t in _soundManager.AvailableSoundThemes)
            {
                if (t.Id == themeId && !t.IsBuiltin)
                {
                    isCustom = true;
                    break;
                }
            }
            DeleteThemeBtn.IsEnabled = isCustom;
        }

        private void SaveThemeButton_Click(object sender, RoutedEventArgs e)
        {
            var loc = LocalizationManager.Instance;
            string title = loc["ui.soundSettings.saveThemeTitle"];
            string prompt = loc["ui.soundSettings.saveThemePrompt"];
            if (string.IsNullOrEmpty(title) || title == "ui.soundSettings.saveThemeTitle")
                title = "Save Theme";
            if (string.IsNullOrEmpty(prompt) || prompt == "ui.soundSettings.saveThemePrompt")
                prompt = "Enter theme name:";

            var dialog = new Window
            {
                Title = title,
                Width = 320, Height = 150,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
                AllowsTransparency = true,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var panel = new StackPanel { Margin = new Thickness(16) };
            var label = new TextBlock { Text = prompt, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8) };
            var textBox = new TextBox
            {
                Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 13
            };
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var okBtn = new Button { Content = "OK", Width = 70, Height = 28, Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x5A, 0x6A)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
            var cancelBtn = new Button { Content = "Cancel", Width = 70, Height = 28, Margin = new Thickness(8, 0, 0, 0), Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };

            okBtn.Click += (s, args) => { dialog.DialogResult = true; dialog.Close(); };
            cancelBtn.Click += (s, args) => { dialog.DialogResult = false; dialog.Close(); };
            textBox.KeyDown += (s, args) => { if (args.Key == Key.Enter) { dialog.DialogResult = true; dialog.Close(); } };

            btnPanel.Children.Add(okBtn);
            btnPanel.Children.Add(cancelBtn);
            panel.Children.Add(label);
            panel.Children.Add(textBox);
            panel.Children.Add(btnPanel);
            dialog.Content = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
                Child = panel
            };

            textBox.Focus();
            if (dialog.ShowDialog() != true) return;

            string name = textBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;

            // 현재 설정 병합: 베이스 테마 + 오버라이드
            var sounds = new Dictionary<string, double>();
            var activeTheme = _soundManager.AvailableSoundThemes
                .FirstOrDefault(t => t.Id == _soundManager.CurrentThemeId);
            if (activeTheme != null)
            {
                foreach (var kvp in activeTheme.Sounds)
                    sounds[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in _settings.SoundThemeOverrides)
                sounds[kvp.Key] = kvp.Value;

            var newTheme = new SoundThemeData
            {
                Id = $"custom_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                NameKey = name,
                IsBuiltin = false,
                Sounds = sounds
            };

            if (_soundManager.AddCustomTheme(newTheme))
            {
                _soundManager.ApplySoundTheme(newTheme.Id);
                _settings.SoundThemeId = newTheme.Id;
                _settings.SoundThemeOverrides.Clear();
                PopulateThemeComboBox();
                // 새 테마 선택
                foreach (ComboBoxItem item in ThemeComboBox.Items)
                {
                    if (item.Tag?.ToString() == newTheme.Id)
                    {
                        ThemeComboBox.SelectedItem = item;
                        break;
                    }
                }
                PopulateSoundTypeValues();
            }
        }

        private void DeleteThemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is not ComboBoxItem selected) return;
            string themeId = selected.Tag?.ToString() ?? "";

            if (_soundManager.DeleteCustomTheme(themeId))
            {
                _soundManager.ApplySoundTheme("minimal");
                _settings.SoundThemeId = "minimal";
                _settings.SoundThemeOverrides.Clear();
                PopulateThemeComboBox();
                PopulateSoundTypeValues();
            }
        }

        private void SoundType_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            if (sender is not CheckBox cb) return;

            string key = cb.Tag?.ToString() ?? "";
            bool enabled = cb.IsChecked == true;

            var slider = _sliders.GetValueOrDefault(key);
            var volText = _volumeTexts.GetValueOrDefault(key);

            if (enabled)
            {
                double vol = slider?.Value > 0 ? slider.Value : 1.0;
                if (slider != null)
                {
                    slider.IsEnabled = true;
                    slider.Opacity = 1.0;
                    if (slider.Value <= 0) slider.Value = 1.0;
                    vol = slider.Value;
                }
                if (volText != null) volText.Text = $"{(int)(vol * 100)}%";

                _soundManager.SetSoundTypeOverride(key, vol);
                _settings.SoundThemeOverrides[key] = vol;
            }
            else
            {
                if (slider != null)
                {
                    slider.IsEnabled = false;
                    slider.Opacity = 0.4;
                }
                if (volText != null) volText.Text = "--";

                _soundManager.SetSoundTypeOverride(key, 0);
                _settings.SoundThemeOverrides[key] = 0;
            }
        }

        private void SoundType_VolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            if (sender is not Slider slider) return;

            string key = slider.Tag?.ToString() ?? "";
            double vol = e.NewValue;

            var volText = _volumeTexts.GetValueOrDefault(key);
            if (volText != null)
                volText.Text = $"{(int)(vol * 100)}%";

            _soundManager.SetSoundTypeOverride(key, vol);
            _settings.SoundThemeOverrides[key] = vol;
        }

        private void PreviewSoundType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            string key = btn.Tag?.ToString() ?? "";

            if (Enum.TryParse<SoundType>(key, out var type))
                _soundManager.Play(type);
        }

        private void CategoryPack_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            if (sender is not ComboBox comboBox) return;
            if (comboBox.SelectedItem is not ComboBoxItem selectedItem) return;

            string category = comboBox.Tag?.ToString() ?? "";
            string packId = selectedItem.Tag?.ToString() ?? "default";

            if (_soundManager.ChangeCategorySoundPack(category, packId))
            {
                _settings.CategorySoundPacks[category] = packId;
                var previewSound = SoundCategory.GetPreviewSound(category);
                _soundManager.Play(previewSound);
            }
        }

        private void PreviewCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            string category = btn.Tag?.ToString() ?? "";
            var previewSound = SoundCategory.GetPreviewSound(category);
            _soundManager.Play(previewSound);
        }

        #endregion

        #region UI Text

        private void UpdateUIText()
        {
            var loc = LocalizationManager.Instance;
            TitleText.Text = loc["ui.soundSettings.title"];
            ThemeLabel.Text = loc["ui.soundSettings.themeLabel"];
            SoundTypesHeader.Text = loc["ui.soundSettings.soundTypesSection"];
            PackSectionHeader.Text = loc["ui.soundSettings.packSection"];
            CloseBtn.Content = loc["ui.settings.close"];

            if (!_isInitializing)
            {
                PopulateSoundTypeValues();
                PopulateCategoryComboBoxes();
                PopulateThemeComboBox();
            }
        }

        #endregion

        #region Helpers

        private void ApplyDarkComboBoxStyle(ComboBox comboBox)
        {
            comboBox.Resources[SystemColors.WindowBrushKey] = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));
            comboBox.Resources[SystemColors.HighlightBrushKey] = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
            comboBox.Resources[SystemColors.HighlightTextBrushKey] = new SolidColorBrush(Colors.White);
            comboBox.Resources[SystemColors.ControlTextBrushKey] = new SolidColorBrush(Colors.White);
            comboBox.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));

            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44))));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, Brushes.White));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(8, 5, 8, 5)));

            var highlightTrigger = new Trigger { Property = ComboBoxItem.IsHighlightedProperty, Value = true };
            highlightTrigger.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66))));
            itemStyle.Triggers.Add(highlightTrigger);

            var selectedTrigger = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))));
            selectedTrigger.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x00, 0xCE, 0xD1))));
            itemStyle.Triggers.Add(selectedTrigger);

            comboBox.ItemContainerStyle = itemStyle;
        }

        #endregion

        #region Window Events

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void CloseButton_RoutedClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }
}
