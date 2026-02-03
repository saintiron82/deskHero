# Balance Test Tool - Quick Start Guide

## 🚀 How to Open

1. Launch DeskWarrior
2. Press `F1` or click mode toggle to enter **Manage Mode**
3. Click **⚙️ Settings** button
4. Click **🔧 Balance Test Tool (DEV)** button at the bottom

## 📋 Quick Reference

### Cost Calculator
```
Purpose: Calculate upgrade costs at any level
Input:   Stat type → Stat name → Target level
Output:  Cost (gold/crystal) + Effect value
```

### Session Simulator
```
Purpose: Estimate crystal earnings over time
Input:   Sessions, Avg level, Boss kills
Output:  Total crystals + Per-session average
```

### Stats Viewer
```
Purpose: Monitor current stat values
Display: All in-game and permanent stats
Updates: Real-time (on change)
```

### Cheat Mode
```
Purpose: Test scenarios instantly
Actions: Add gold, Add crystals, Set stat level
Warning: Changes permanent stats instantly
```

---

## 🎯 Common Tasks

### Test New Stat Formula
1. Open Cost Calculator
2. Select your stat
3. Try levels: 1, 10, 50, 100
4. Verify costs look balanced

### Balance Crystal Economy
1. Open Session Simulator
2. Input: 20 sessions, level 50, 5 bosses
3. Check total crystals (~100)
4. Compare to permanent upgrade costs
5. Adjust drop rates if needed

### Debug Stat Issue
1. Open Cheat Mode
2. Set problematic stat to reported level
3. Check Stats Viewer for actual effect
4. Compare to expected value

### Test High-Level Start
1. Cheat Mode → Set permanent stats
   - Start Level = 50
   - Start Gold = 10000
2. Close tool
3. Restart game session
4. Verify starting bonuses applied

---

## ⚠️ Safety Tips

✅ **Safe to Use**:
- Cost Calculator (read-only)
- Session Simulator (read-only)
- Stats Viewer (read-only)

⚠️ **Use with Caution**:
- Cheat Mode → Add Gold (temporary, lost on game over)
- Cheat Mode → Add Crystals (permanent, saved)
- Cheat Mode → Set Stat Level (permanent if permanent stat)

🔴 **Before Using Cheat Mode**:
1. Backup your save file (`user_save.json`)
2. Consider creating a test profile
3. Remember: Crystal/permanent stat changes are saved immediately

---

## 🔍 Example Workflow

### Scenario: Testing Keyboard Power Balance

**Step 1: Check Current Cost Curve**
```
Cost Calculator
→ Stat Type: In-Game Stat (Gold)
→ Select: Keyboard Power
→ Test Levels: 1, 10, 20, 50
→ Record costs
```

**Step 2: Test In-Game**
```
Cheat Mode
→ Add Gold: 10000
→ Close tool
→ Buy keyboard upgrades manually
→ Verify costs match calculator
```

**Step 3: Simulate Progression**
```
Session Simulator
→ Sessions: 10
→ Avg Level: 50 (estimate gold earned)
→ Compare gold needed for upgrades
```

**Step 4: Adjust if Needed**
```
Edit InGameStatGrowth.json
→ Modify keyboard_power base_cost
→ Reload game
→ Verify in Cost Calculator
```

---

## 📊 Reading Stats Display

### In-Game Stats Format
```
Keyboard Power    Lv.5    6
    ↑              ↑      ↑
  Name           Level  Effect
```

### Permanent Stats Format
```
Base Attack      Lv.10   10.0
    ↑              ↑      ↑
  Name           Level  Effect
```

**Color Coding**:
- 🟡 Yellow: Levels
- 🔵 Cyan: Effects
- ⚪ Gray: Names

---

## 🐛 Troubleshooting

**"Button not visible in Settings"**
→ Check that GameManager and SaveManager are passed to SettingsWindow

**"Cost Calculator shows weird numbers"**
→ Verify JSON config files are valid
→ Check stat ID matches exactly

**"Cheat doesn't work"**
→ Ensure you clicked "Add" or "Set" button
→ Check input is valid number
→ Look for error message box

**"Stats not updating"**
→ Close and reopen window
→ Stats auto-refresh on next change

---

## 📈 Interpreting Results

### Good Cost Curve
```
Level 1:    100 gold
Level 10:   300 gold    (3x)
Level 20:   900 gold    (3x)
Level 50:   5000 gold   (5x)
```
Smooth exponential growth

### Bad Cost Curve
```
Level 1:    100 gold
Level 10:   150 gold    (1.5x) TOO CHEAP
Level 20:   10000 gold  (66x)  TOO EXPENSIVE
```
Irregular jumps

### Healthy Economy
```
10 sessions → 100 crystals
Useful upgrade = 50 crystals
Player can afford 2 upgrades
```
Balanced progression

### Broken Economy
```
10 sessions → 10 crystals
Useful upgrade = 500 crystals
Player needs 50+ sessions
```
Too grindy

---

## 🎓 Pro Tips

1. **Always test extreme values**
   - Level 1 (starting experience)
   - Level 100 (late game)
   - Level 999 (max cap)

2. **Compare similar stats**
   - Keyboard vs Mouse power
   - Gold+ vs Gold*
   - Should have similar curves

3. **Use simulator for reality checks**
   - Input your actual average gameplay
   - If crystals seem too low/high → adjust

4. **Document your tests**
   - Screenshot costs before/after changes
   - Record simulator results
   - Track player feedback

5. **Cheat mode for edge cases**
   - Test level 100 without grinding
   - Verify caps work correctly
   - Reproduce reported bugs

---

## 🔗 Related Documentation

- **Full Documentation**: `docs/BALANCE_TEST_TOOL.md`
- **Stat System**: `docs/STAT_SYSTEM.md`
- **JSON Configs**: `config/InGameStatGrowth.json`, `config/PermanentStats.json`
- **Implementation**: `BALANCE_TEST_IMPLEMENTATION.md`

---

**Quick Start Version**: 1.0
**Last Updated**: 2026-01-16
