# HP Curve Adjustment Summary
**Date**: 2026-02-05
**Goal**: Reduce 1-hour progression from 1,674 levels to ~800 levels
**Status**: In Progress

## Key Findings

### 1. Critical Discovery: Positive Feedback Loop
**Root Cause Identified by ba_ma Agent**:
- Higher HP → More timeouts → More time_extend investment (30% of crystals)
- More time_extend → Longer sessions → More levels per session
- This creates a **positive feedback loop** where increasing HP paradoxically speeds up progression

### 2. Test Results Summary

| Config | base_hp | tier_interval | tier_mult | linear_growth | timeout_invest | 1h Level |
|--------|---------|---------------|-----------|---------------|----------------|----------|
| Original | 20 | - | - | - | 30% | 1,674 |
| Test 1 | 45 | 300 | 1.5 | 17 | 30% | 1,792 |
| Test 2 | 100 | 150 | 2.0 | 100 | 30% | 1,681 |
| Test 3 | 10,000 | 100 | 2.5 | 400 | 30% | **1,818** ⬆️ |
| Test 4 | 500 | 300 | 1.5 | 8 | 10% | 1,726 |
| Test 5 | 2,000 | 300 | 1.5 | 20 | 10% | 1,530 |
| Test 6 | 4,000 | 300 | 1.5 | 40 | 10% | **1,826** ⬆️ |
| Test 7 | 100 | - | - | hp_growth=50 | 10% | **89** ⬇️ |
| Test 8 | 40 | 300 | 1.5 | 8 | 10% | 1,676 |

**Observation**: Increasing HP without fixing the timeout investment loop actually **increases** progression!

### 3. Implemented Changes

#### Code Changes
**File**: `DeskWarrior.Core/Simulation/ProgressionSimulator.cs:415`
```csharp
// Before
long timeExtendBudget = (long)(crystals * 0.3);  // 30%

// After
long timeExtendBudget = (long)(crystals * 0.1);  // 10%
```

**File**: `Models/Monster.cs:300-310`
- Added `growth_decrease_per_tier` logic to match simulator behavior
- Fixed discrepancy between game code and simulator

#### Configuration Changes
**Current Settings** (`config/GameData.json`):
```json
{
  "balance": {
    "base_hp": 40,
    "hp_growth": 10,
    "tier_hp_system": {
      "enabled": true,
      "tier_interval": 300,
      "tier_multiplier": 1.5,
      "linear_growth_per_level": 8,
      "growth_decrease_per_tier": 0.85
    }
  }
}
```

**Result**: 1,676 levels (still 2.1x target)

## Problem Analysis

### Why Adjustments Don't Work as Expected

1. **Tier System Amplification**
   - Tier 0→1 transition at level 301
   - Each tier multiplies base HP by 1.5x
   - Creates exponential growth that feedback loop exploits

2. **Timeout Investment Strategy**
   - Even at 10%, timeout-triggered time_extend investment is significant
   - Longer sessions = more gold = more upgrades = faster progression

3. **Gold Economy Scaling**
   - Higher HP monsters give proportionally more gold
   - More gold enables more aggressive permanent stat upgrades
   - Creates runaway scaling

### Test 7 Insight: Linear HP Works!
- **Disabled tier system**, used `hp_growth = 50`
- Result: **89 levels** in 1 hour
- **Too slow**, but proves tier system is the main acceleration factor

## Recommendations

### Option A: Conservative Tier System (Recommended)
Target the middle ground between Test 7 (89 levels) and current (1,676 levels):

```json
{
  "balance": {
    "base_hp": 100,
    "hp_growth": 15,
    "tier_hp_system": {
      "enabled": true,
      "tier_interval": 400,
      "tier_multiplier": 1.3,
      "linear_growth_per_level": 5,
      "growth_decrease_per_tier": 0.85
    }
  }
}
```

**Rationale**:
- Tier interval 400 delays first tier transition
- Multiplier 1.3 provides gentle exponential curve
- Linear growth 5 keeps intra-tier progression slow
- Should achieve ~800-1,000 levels

### Option B: Hybrid Linear + Tier
Use higher linear growth with very gentle tier scaling:

```json
{
  "balance": {
    "base_hp": 50,
    "hp_growth": 20,
    "tier_hp_system": {
      "enabled": true,
      "tier_interval": 500,
      "tier_multiplier": 1.2,
      "linear_growth_per_level": 3,
      "growth_decrease_per_tier": 0.9
    }
  }
}
```

### Option C: Further Reduce Timeout Investment
Additional code change to `ProgressionSimulator.cs:415`:

```csharp
long timeExtendBudget = (long)(crystals * 0.05);  // 10% → 5%
```

**And/Or** reduce max investment limit (Line 440):
```csharp
if (_costCalculator.GetStatLevel(stats, "time_extend") - currentLevel >= 3)  // 5 → 3
```

## Next Steps

1. **Test Option A** with 1-hour simulation
2. **If still too fast**: Apply Option C (5% timeout investment)
3. **If too slow**: Increase tier_multiplier to 1.4
4. **Final validation**: 50-hour simulation to check long-term curve

## Success Criteria

- ✅ 1 hour: 750-850 levels (target 800±50)
- ✅ 10 hours: 2,000-2,500 levels
- ✅ 50 hours: 3,000-3,500 levels
- ✅ Session time: 80-90 seconds maintained
- ✅ Curve shape: Fast early, gradual slowdown

## References

- ba_ma Agent Analysis: Agent ID ad7eae6 (2026-02-05)
- ba_ma Agent Analysis: Agent ID aabcfba (2026-02-05)
- Balance Reference: `balanceDoc/balance_reference.md`
- Original Test Results: `balanceDoc/2026-02-05/balance_test_summary.md`
