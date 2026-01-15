using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DeskWarrior.Interfaces;
using DeskWarrior.Managers;
using DeskWarrior.Models;

namespace DeskWarrior.Windows
{
    /// <summary>
    /// Balance Test Tool - Developer debugging and testing UI
    /// </summary>
    public partial class BalanceTestWindow : Window
    {
        #region Fields

        private readonly IGameManager _gameManager;
        private readonly SaveManager _saveManager;
        private readonly StatGrowthManager _statGrowth;
        private readonly PermanentProgressionManager? _permanentProgression;

        #endregion

        #region Constructor

        public BalanceTestWindow(IGameManager gameManager, SaveManager saveManager)
        {
            InitializeComponent();

            _gameManager = gameManager;
            _saveManager = saveManager;
            _statGrowth = new StatGrowthManager();
            _permanentProgression = new PermanentProgressionManager(saveManager);

            InitializeUI();
            UpdateStatsDisplay();
        }

        #endregion

        #region Initialization

        private void InitializeUI()
        {
            // Populate In-Game Stats combo
            PopulateStatCombo("ingame");

            // Populate Cheat Stat combo with all stats
            PopulateCheatStatCombo();

            // Initial calculation
            CalculateButton_Click(null, null);
        }

        private void PopulateStatCombo(string type)
        {
            StatCombo.Items.Clear();

            if (type == "ingame")
            {
                AddStatItem("keyboard_power", "Keyboard Power");
                AddStatItem("mouse_power", "Mouse Power");
                AddStatItem("gold_flat", "Gold+");
                AddStatItem("gold_multi", "Gold*");
                AddStatItem("time_thief", "Time Thief");
                AddStatItem("combo_flex", "Combo Flex");
                AddStatItem("combo_damage", "Combo Damage");
            }
            else // permanent
            {
                AddStatItem("base_attack", "Base Attack");
                AddStatItem("attack_percent", "Attack Percent");
                AddStatItem("crit_chance", "Crit Chance");
                AddStatItem("crit_damage", "Crit Damage");
                AddStatItem("multi_hit", "Multi Hit");
                AddStatItem("gold_flat_perm", "Gold+ (Permanent)");
                AddStatItem("gold_multi_perm", "Gold* (Permanent)");
                AddStatItem("crystal_flat", "Crystal+");
                AddStatItem("crystal_multi", "Crystal*");
                AddStatItem("time_extend", "Time Extend");
                AddStatItem("upgrade_discount", "Upgrade Discount");
                AddStatItem("start_level", "Start Level");
                AddStatItem("start_gold", "Start Gold");
                AddStatItem("start_keyboard", "Start Keyboard");
                AddStatItem("start_mouse", "Start Mouse");
                AddStatItem("start_gold_flat", "Start Gold+");
                AddStatItem("start_gold_multi", "Start Gold*");
                AddStatItem("start_combo_flex", "Start Combo Flex");
                AddStatItem("start_combo_damage", "Start Combo Damage");
            }

            if (StatCombo.Items.Count > 0)
                StatCombo.SelectedIndex = 0;
        }

        private void AddStatItem(string statId, string displayName)
        {
            var item = new ComboBoxItem
            {
                Content = displayName,
                Tag = statId
            };
            StatCombo.Items.Add(item);
        }

        private void PopulateCheatStatCombo()
        {
            CheatStatCombo.Items.Clear();

            // In-Game Stats
            AddCheatStatItem("keyboard_power", "[In-Game] Keyboard Power");
            AddCheatStatItem("mouse_power", "[In-Game] Mouse Power");
            AddCheatStatItem("gold_flat", "[In-Game] Gold+");
            AddCheatStatItem("gold_multi", "[In-Game] Gold*");
            AddCheatStatItem("time_thief", "[In-Game] Time Thief");
            AddCheatStatItem("combo_flex", "[In-Game] Combo Flex");
            AddCheatStatItem("combo_damage", "[In-Game] Combo Damage");

            // Permanent Stats (most useful ones)
            AddCheatStatItem("base_attack", "[Permanent] Base Attack");
            AddCheatStatItem("crit_chance", "[Permanent] Crit Chance");
            AddCheatStatItem("start_level", "[Permanent] Start Level");
            AddCheatStatItem("start_gold", "[Permanent] Start Gold");

            if (CheatStatCombo.Items.Count > 0)
                CheatStatCombo.SelectedIndex = 0;
        }

        private void AddCheatStatItem(string statId, string displayName)
        {
            var item = new ComboBoxItem
            {
                Content = displayName,
                Tag = statId
            };
            CheatStatCombo.Items.Add(item);
        }

        #endregion

        #region Stats Display

        private void UpdateStatsDisplay()
        {
            UpdateInGameStatsDisplay();
            UpdatePermanentStatsDisplay();
        }

        private void UpdateInGameStatsDisplay()
        {
            InGameStatsGrid.Children.Clear();
            InGameStatsGrid.RowDefinitions.Clear();

            var stats = _gameManager.InGameStats;
            int row = 0;

            AddStatRow(ref row, "Keyboard Power", stats.KeyboardPowerLevel, _gameManager.KeyboardPower);
            AddStatRow(ref row, "Mouse Power", stats.MousePowerLevel, _gameManager.MousePower);
            AddStatRow(ref row, "Gold+", stats.GoldFlatLevel, _gameManager.GoldFlat);
            AddStatRow(ref row, "Gold*", stats.GoldMultiLevel, _gameManager.GoldMulti * 100, "%");
            AddStatRow(ref row, "Time Thief", stats.TimeThiefLevel, _gameManager.TimeThief, "s");
            AddStatRow(ref row, "Combo Flex", stats.ComboFlexLevel, _gameManager.ComboFlex, "s");
            AddStatRow(ref row, "Combo Damage", stats.ComboDamageLevel, _gameManager.ComboDamage * 100, "%");
        }

        private void UpdatePermanentStatsDisplay()
        {
            PermanentStatsGrid.Children.Clear();
            PermanentStatsGrid.RowDefinitions.Clear();

            var stats = _saveManager.CurrentSave.PermanentStats;
            int row = 0;

            // Base Stats
            AddPermanentStatRow(ref row, "Base Attack", stats.BaseAttackLevel, "base_attack");
            AddPermanentStatRow(ref row, "Attack %", stats.AttackPercentLevel, "attack_percent");
            AddPermanentStatRow(ref row, "Crit Chance", stats.CritChanceLevel, "crit_chance");
            AddPermanentStatRow(ref row, "Crit Damage", stats.CritDamageLevel, "crit_damage");
            AddPermanentStatRow(ref row, "Multi Hit", stats.MultiHitLevel, "multi_hit");

            // Currency
            AddPermanentStatRow(ref row, "Gold+ (Perm)", stats.GoldFlatPermLevel, "gold_flat_perm");
            AddPermanentStatRow(ref row, "Gold* (Perm)", stats.GoldMultiPermLevel, "gold_multi_perm");
            AddPermanentStatRow(ref row, "Crystal+", stats.CrystalFlatLevel, "crystal_flat");
            AddPermanentStatRow(ref row, "Crystal*", stats.CrystalMultiLevel, "crystal_multi");

            // Utility
            AddPermanentStatRow(ref row, "Time Extend", stats.TimeExtendLevel, "time_extend");
            AddPermanentStatRow(ref row, "Upgrade Discount", stats.UpgradeDiscountLevel, "upgrade_discount");

            // Starting
            AddPermanentStatRow(ref row, "Start Level", stats.StartLevelLevel, "start_level");
            AddPermanentStatRow(ref row, "Start Gold", stats.StartGoldLevel, "start_gold");
            AddPermanentStatRow(ref row, "Start Keyboard", stats.StartKeyboardLevel, "start_keyboard");
            AddPermanentStatRow(ref row, "Start Mouse", stats.StartMouseLevel, "start_mouse");
        }

        private void AddStatRow(ref int row, string name, int level, double effect, string suffix = "")
        {
            InGameStatsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var nameText = new TextBlock
            {
                Text = name,
                Foreground = System.Windows.Media.Brushes.LightGray,
                FontSize = 11,
                Margin = new Thickness(0, 2, 10, 2)
            };
            Grid.SetRow(nameText, row);
            Grid.SetColumn(nameText, 0);
            InGameStatsGrid.Children.Add(nameText);

            var levelText = new TextBlock
            {
                Text = $"Lv.{level}",
                Foreground = System.Windows.Media.Brushes.Yellow,
                FontSize = 11,
                Margin = new Thickness(0, 2, 10, 2),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(levelText, row);
            Grid.SetColumn(levelText, 1);
            InGameStatsGrid.Children.Add(levelText);

            var effectText = new TextBlock
            {
                Text = $"{effect:F1}{suffix}",
                Foreground = System.Windows.Media.Brushes.Cyan,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 2, 0, 2),
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 60
            };
            Grid.SetRow(effectText, row);
            Grid.SetColumn(effectText, 2);
            InGameStatsGrid.Children.Add(effectText);

            row++;
        }

        private void AddPermanentStatRow(ref int row, string name, int level, string statId)
        {
            PermanentStatsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var nameText = new TextBlock
            {
                Text = name,
                Foreground = System.Windows.Media.Brushes.LightGray,
                FontSize = 11,
                Margin = new Thickness(0, 2, 10, 2)
            };
            Grid.SetRow(nameText, row);
            Grid.SetColumn(nameText, 0);
            PermanentStatsGrid.Children.Add(nameText);

            var levelText = new TextBlock
            {
                Text = $"Lv.{level}",
                Foreground = System.Windows.Media.Brushes.Gold,
                FontSize = 11,
                Margin = new Thickness(0, 2, 10, 2),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(levelText, row);
            Grid.SetColumn(levelText, 1);
            PermanentStatsGrid.Children.Add(levelText);

            var effect = _statGrowth.GetPermanentStatEffect(statId, level);
            var effectText = new TextBlock
            {
                Text = $"{effect:F1}",
                Foreground = System.Windows.Media.Brushes.Cyan,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 2, 0, 2),
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 60
            };
            Grid.SetRow(effectText, row);
            Grid.SetColumn(effectText, 2);
            PermanentStatsGrid.Children.Add(effectText);

            row++;
        }

        #endregion

        #region Cost Calculator

        private void StatTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatTypeCombo.SelectedItem is ComboBoxItem item)
            {
                string type = item.Tag?.ToString() ?? "ingame";
                PopulateStatCombo(type);
                CalculateButton_Click(null, null);
            }
        }

        private void StatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateButton_Click(null, null);
        }

        private void CalculateButton_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                if (StatCombo.SelectedItem is not ComboBoxItem statItem) return;
                if (StatTypeCombo.SelectedItem is not ComboBoxItem typeItem) return;

                string statId = statItem.Tag?.ToString() ?? "";
                string type = typeItem.Tag?.ToString() ?? "ingame";

                if (!int.TryParse(TargetLevelInput.Text, out int targetLevel) || targetLevel < 0)
                {
                    CostResult.Text = "Invalid level";
                    EffectResult.Text = "";
                    return;
                }

                int cost;
                double effect;

                if (type == "ingame")
                {
                    var discountPercent = _saveManager.CurrentSave.PermanentStats.UpgradeCostReduction;
                    cost = _statGrowth.GetInGameUpgradeCost(statId, targetLevel - 1, discountPercent);
                    effect = _statGrowth.GetInGameStatEffect(statId, targetLevel);
                    CostResult.Text = $"💰 Cost: {cost:N0} Gold";
                }
                else // permanent
                {
                    cost = _statGrowth.GetPermanentUpgradeCost(statId, targetLevel - 1, null);
                    effect = _statGrowth.GetPermanentStatEffect(statId, targetLevel);
                    CostResult.Text = $"💎 Cost: {cost:N0} Crystal";
                }

                EffectResult.Text = $"Effect: {effect:F2}";
            }
            catch (Exception ex)
            {
                CostResult.Text = $"Error: {ex.Message}";
                EffectResult.Text = "";
            }
        }

        #endregion

        #region Simulator

        private void SimulateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(SessionCountInput.Text, out int sessions) || sessions <= 0)
                {
                    SimTotalCrystals.Text = "Invalid session count";
                    return;
                }

                if (!int.TryParse(AvgLevelInput.Text, out int avgLevel) || avgLevel <= 0)
                {
                    SimTotalCrystals.Text = "Invalid avg level";
                    return;
                }

                if (!int.TryParse(AvgBossKillsInput.Text, out int avgBossKills) || avgBossKills < 0)
                {
                    SimTotalCrystals.Text = "Invalid avg boss kills";
                    return;
                }

                // Simple simulation based on boss kill rate
                // Assuming 10% drop rate and 10 crystals per drop on average
                double dropRate = 0.10;
                int avgCrystalsPerDrop = 10;

                double crystalsPerSession = avgBossKills * dropRate * avgCrystalsPerDrop;
                double totalCrystals = crystalsPerSession * sessions;

                SimTotalCrystals.Text = $"💎 Total Crystals: {totalCrystals:F0}";
                SimAvgCrystalsPerSession.Text = $"Avg per Session: {crystalsPerSession:F1}";
            }
            catch (Exception ex)
            {
                SimTotalCrystals.Text = $"Error: {ex.Message}";
                SimAvgCrystalsPerSession.Text = "";
            }
        }

        #endregion

        #region Cheat Mode

        private void CheatAddGold_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(CheatGoldInput.Text, out int gold) || gold <= 0)
                {
                    MessageBox.Show("Invalid gold amount", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Use reflection to add gold to GameManager
                var goldProperty = _gameManager.GetType().GetProperty("Gold");
                if (goldProperty != null)
                {
                    int currentGold = (int)(goldProperty.GetValue(_gameManager) ?? 0);
                    goldProperty.SetValue(_gameManager, currentGold + gold);

                    MessageBox.Show($"Added {gold:N0} gold!\nNew Total: {currentGold + gold:N0}",
                        "Cheat Applied", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheatAddCrystal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(CheatCrystalInput.Text, out int crystals) || crystals <= 0)
                {
                    MessageBox.Show("Invalid crystal amount", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _permanentProgression?.AddCrystals(crystals, "cheat_mode");
                var currentCrystals = _saveManager.CurrentSave.PermanentCurrency.Crystals;

                MessageBox.Show($"Added {crystals:N0} crystals!\nNew Total: {currentCrystals:N0}",
                    "Cheat Applied", MessageBoxButton.OK, MessageBoxImage.Information);

                UpdateStatsDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheatSetStat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheatStatCombo.SelectedItem is not ComboBoxItem item)
                {
                    MessageBox.Show("Select a stat", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(CheatStatLevelInput.Text, out int level) || level < 0)
                {
                    MessageBox.Show("Invalid level", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string statId = item.Tag?.ToString() ?? "";
                string displayName = item.Content?.ToString() ?? statId;

                // Determine if it's in-game or permanent stat
                if (displayName.Contains("[In-Game]"))
                {
                    SetInGameStatLevel(statId, level);
                }
                else if (displayName.Contains("[Permanent]"))
                {
                    SetPermanentStatLevel(statId, level);
                }

                MessageBox.Show($"Set {displayName} to level {level}",
                    "Cheat Applied", MessageBoxButton.OK, MessageBoxImage.Information);

                UpdateStatsDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetInGameStatLevel(string statId, int level)
        {
            var stats = _gameManager.InGameStats;
            var property = statId switch
            {
                "keyboard_power" => typeof(InGameStats).GetProperty("KeyboardPowerLevel"),
                "mouse_power" => typeof(InGameStats).GetProperty("MousePowerLevel"),
                "gold_flat" => typeof(InGameStats).GetProperty("GoldFlatLevel"),
                "gold_multi" => typeof(InGameStats).GetProperty("GoldMultiLevel"),
                "time_thief" => typeof(InGameStats).GetProperty("TimeThiefLevel"),
                "combo_flex" => typeof(InGameStats).GetProperty("ComboFlexLevel"),
                "combo_damage" => typeof(InGameStats).GetProperty("ComboDamageLevel"),
                _ => null
            };

            property?.SetValue(stats, level);
        }

        private void SetPermanentStatLevel(string statId, int level)
        {
            var stats = _saveManager.CurrentSave.PermanentStats;
            var property = statId switch
            {
                "base_attack" => typeof(PermanentStats).GetProperty("BaseAttackLevel"),
                "attack_percent" => typeof(PermanentStats).GetProperty("AttackPercentLevel"),
                "crit_chance" => typeof(PermanentStats).GetProperty("CritChanceLevel"),
                "crit_damage" => typeof(PermanentStats).GetProperty("CritDamageLevel"),
                "multi_hit" => typeof(PermanentStats).GetProperty("MultiHitLevel"),
                "gold_flat_perm" => typeof(PermanentStats).GetProperty("GoldFlatPermLevel"),
                "gold_multi_perm" => typeof(PermanentStats).GetProperty("GoldMultiPermLevel"),
                "crystal_flat" => typeof(PermanentStats).GetProperty("CrystalFlatLevel"),
                "crystal_multi" => typeof(PermanentStats).GetProperty("CrystalMultiLevel"),
                "time_extend" => typeof(PermanentStats).GetProperty("TimeExtendLevel"),
                "upgrade_discount" => typeof(PermanentStats).GetProperty("UpgradeDiscountLevel"),
                "start_level" => typeof(PermanentStats).GetProperty("StartLevelLevel"),
                "start_gold" => typeof(PermanentStats).GetProperty("StartGoldLevel"),
                "start_keyboard" => typeof(PermanentStats).GetProperty("StartKeyboardLevel"),
                "start_mouse" => typeof(PermanentStats).GetProperty("StartMouseLevel"),
                "start_gold_flat" => typeof(PermanentStats).GetProperty("StartGoldFlatLevel"),
                "start_gold_multi" => typeof(PermanentStats).GetProperty("StartGoldMultiLevel"),
                "start_combo_flex" => typeof(PermanentStats).GetProperty("StartComboFlexLevel"),
                "start_combo_damage" => typeof(PermanentStats).GetProperty("StartComboDamageLevel"),
                _ => null
            };

            property?.SetValue(stats, level);
            _saveManager.Save();
        }

        #endregion

        #region Window Events

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

        #endregion
    }
}
