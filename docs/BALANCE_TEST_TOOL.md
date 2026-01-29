# Balance Test Tool Documentation

## Overview

The Balance Test Tool is a developer-only debugging UI that allows real-time testing and verification of game balance. It provides comprehensive tools for testing stat formulas, simulating progression, and debugging game state.

## Access

**Location**: Settings Window → "🔧 Balance Test Tool (DEV)" button

**Requirements**:
- Available in all builds (production and development)
- Requires GameManager and SaveManager instances

## Features

### 1. 💰 Cost Calculator

Calculate upgrade costs and effects for any stat at any level.

**Usage**:
1. Select stat type (In-Game or Permanent)
2. Choose specific stat from dropdown
3. Enter target level
4. View calculated cost and effect

**Purpose**:
- Verify cost formulas are working correctly
- Test stat scaling at different levels
- Balance progression curves

**Formula Applied**:
```
cost = base × (1 + level × growth_rate) × multiplier^(level / softcap_interval)
```

---

### 2. 🎮 Session Simulator

Estimate crystal accumulation over multiple sessions.

**Inputs**:
- Number of sessions
- Average level per session
- Average boss kills per session

**Outputs**:
- Total crystals earned
- Average crystals per session

**Purpose**:
- Project long-term progression
- Balance crystal economy
- Estimate time-to-goal for upgrades

**Current Model**:
- 10% drop rate per boss kill
- 10 crystals average per drop
- Linear scaling (can be refined)

---

### 3. 📊 Current In-Game Stats

Real-time display of all in-game stats (session-based).

**Displayed Info**:
- Stat name
- Current level
- Current effect value

**Stats Tracked**:
- Keyboard Power (damage)
- Mouse Power (damage)
- Gold+ (flat bonus)
- Gold* (percentage bonus)
- Time Thief (time added per kill)
- Combo Flex (rhythm tolerance)
- Combo Damage (combo bonus percentage)

**Auto-Updates**: Yes (when stats change)

---

### 4. 💎 Current Permanent Stats

Real-time display of all permanent stats (meta progression).

**Displayed Info**:
- Stat name
- Current level
- Current effect value

**Categories**:
- **Base Stats**: Attack, Crit, Multi-hit
- **Currency**: Gold bonuses, Crystal bonuses
- **Utility**: Time extension, Upgrade discount
- **Starting Bonuses**: Initial levels, gold, stats

**Scrollable**: Yes (for long list)

---

### 5. ⚠️ Cheat Mode (Test Only)

Direct manipulation of game state for testing purposes.

#### Add Gold
- Input amount → Click "Add"
- Instantly adds gold to current game state
- Shows confirmation with new total

#### Add Crystals
- Input amount → Click "Add"
- Adds crystals to permanent currency
- Updates save file
- Shows confirmation

#### Set Stat Level
- Select stat (in-game or permanent)
- Input desired level
- Click "Set"
- Directly modifies stat level

**⚠️ Warning**:
- Cheat mode bypasses all cost checks
- Changes to permanent stats are saved immediately
- Use only for testing, not for normal gameplay

---

## Technical Implementation

### Architecture

```
BalanceTestWindow.xaml     - UI layout
BalanceTestWindow.xaml.cs  - Business logic
StatGrowthManager          - Formula calculations
GameManager                - In-game state
SaveManager                - Permanent state
```

### Key Dependencies

- **StatGrowthManager**: Reads from `InGameStatGrowth.json` and `PermanentStats.json`
- **GameManager**: Provides current in-game stats via `InGameStats` property
- **SaveManager**: Provides permanent stats via `CurrentSave.PermanentStats`
- **PermanentProgressionManager**: Handles crystal economy

### Data Flow

```
User Input → Cost Calculator
    ↓
StatGrowthManager.GetUpgradeCost(statId, level)
    ↓
Display Result

User Input → Simulator
    ↓
Simple calculation (boss kills × drop rate × crystals)
    ↓
Display projection

Cheat Mode → Direct State Modification
    ↓
Reflection-based property setting
    ↓
SaveManager.Save() (if permanent)
```

---

## Use Cases

### 1. Testing New Stat Formulas

**Scenario**: You've modified `InGameStatGrowth.json` and want to verify costs.

**Steps**:
1. Open Balance Test Tool
2. Select "In-Game Stat (Gold)"
3. Choose the modified stat
4. Test multiple levels (1, 10, 50, 100)
5. Verify costs match expected curve

---

### 2. Balancing Crystal Economy

**Scenario**: Players report crystals accumulate too slowly.

**Steps**:
1. Open Session Simulator
2. Input realistic session count (e.g., 20 sessions)
3. Input average level reached (e.g., 50)
4. Calculate avg boss kills (level ÷ boss_interval)
5. Review total crystals
6. Compare with permanent upgrade costs
7. Adjust drop rates or costs as needed

---

### 3. Debugging Stat Issues

**Scenario**: Player reports "Keyboard Power level 10 shows wrong effect".

**Steps**:
1. Open Balance Test Tool
2. Use Cheat Mode to set Keyboard Power to level 10
3. Check "Current In-Game Stats" panel
4. Verify effect matches formula
5. If mismatch, check `StatGrowthManager` logic

---

### 4. Testing Starting Bonuses

**Scenario**: You want to test high-level permanent stats.

**Steps**:
1. Open Cheat Mode
2. Set multiple permanent stats (e.g., Start Level = 50)
3. Close tool
4. Restart game session
5. Verify starting state matches stat effects

---

## Performance Considerations

- **Memory**: Negligible (~5MB additional for window)
- **CPU**: <1% (only during calculations)
- **Update Frequency**: Manual refresh only
- **Thread Safety**: All operations on UI thread

---

## Security Notes

### Production Considerations

**Current State**: Cheat mode is accessible in all builds.

**Recommendations for Production**:
```csharp
#if DEBUG
    BalanceTestBtn.Visibility = Visibility.Visible;
#else
    BalanceTestBtn.Visibility = Visibility.Collapsed;
#endif
```

**Or** use a developer flag in settings:
```csharp
if (!SaveManager.CurrentSave.Settings.DeveloperMode)
{
    CheatPanel.Visibility = Visibility.Collapsed;
}
```

### Cheat Detection

If implementing anti-cheat:
- Log all cheat mode usage
- Add telemetry for abnormal stat values
- Disable leaderboards if cheats detected

---

## Future Enhancements

### Recommended Additions

1. **Damage Simulator**
   - Input: Monster HP, Player stats
   - Output: Hits to kill, DPS, Time to kill

2. **Progression Graph**
   - Visual chart of cost curves
   - Compare multiple stats
   - Export to CSV

3. **Auto-Balance Suggestions**
   - Analyze current curves
   - Suggest cost adjustments
   - Detect balance issues

4. **Save/Load Test Profiles**
   - Store test configurations
   - Quick switching between test scenarios
   - Share test cases with team

5. **Formula Tester**
   - Live formula editor
   - Test custom formulas
   - Compare before/after

---

## Troubleshooting

### "Cost Calculator shows NaN"
**Cause**: Invalid stat ID or missing config
**Fix**: Check `InGameStatGrowth.json` and `PermanentStats.json`

### "Cheat mode doesn't update display"
**Cause**: Stats display not refreshing
**Fix**: Call `UpdateStatsDisplay()` after cheat operations

### "Simulator shows 0 crystals"
**Cause**: Invalid input values
**Fix**: Verify all inputs are positive integers

### "Window doesn't open"
**Cause**: GameManager or SaveManager is null
**Fix**: Ensure both are passed from MainWindow

---

## Code Examples

### Adding a New Stat to Calculator

```csharp
// In PopulateStatCombo method
if (type == "ingame")
{
    AddStatItem("new_stat_id", "New Stat Name");
}
```

### Modifying Simulator Formula

```csharp
// In SimulateButton_Click method
double dropRate = 0.15; // Increase from 0.10
int avgCrystalsPerDrop = 12; // Increase from 10
```

### Adding Custom Cheat Function

```csharp
private void CheatMaxLevel_Click(object sender, RoutedEventArgs e)
{
    var levelProperty = _gameManager.GetType().GetProperty("CurrentLevel");
    levelProperty?.SetValue(_gameManager, 999);
    UpdateStatsDisplay();
}
```

---

## Testing Checklist

Before deploying balance changes:

- [ ] Test all stat cost curves in calculator
- [ ] Verify simulator projections are realistic
- [ ] Check in-game stats display correctly
- [ ] Confirm permanent stats persist after save/load
- [ ] Test cheat mode doesn't crash game
- [ ] Verify all formulas match JSON configs
- [ ] Test edge cases (level 0, max level)
- [ ] Confirm UI updates after cheat operations

---

## Maintenance

### When Adding New Stats

1. Add to `InGameStatGrowth.json` or `PermanentStats.json`
2. Update `InGameStats.cs` or `PermanentStats.cs` model
3. Add to dropdown in `PopulateStatCombo()`
4. Add to cheat combo in `PopulateCheatStatCombo()`
5. Add to stats display in `UpdateInGameStatsDisplay()` or `UpdatePermanentStatsDisplay()`

### When Changing Formulas

1. Update JSON config files
2. Test in Cost Calculator
3. Verify existing saves still load
4. Update documentation

---

## Support

For issues or questions:
- Check this documentation
- Review `StatGrowthManager.cs` for formula logic
- Inspect JSON config files for stat definitions
- Use debugger to trace calculation flow

---

**Last Updated**: 2026-01-16
**Version**: 1.0
**Author**: lily (AI Code Implementation Agent)
