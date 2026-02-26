# AI-Driven Game Balance Verification System
### Portfolio Case Study: DeskWarrior

---

## Executive Summary

**Project:** Automated game balance verification using AI agent (Claude Opus 4)
**Domain:** Idle/Incremental Game Development
**Duration:** Single session (~2 hours)
**Result:** 2,360x improvement in strategy balance ratio

---

## The Challenge

DeskWarrior, an incremental idle game, suffered from severe balance issues:

| Issue | Impact |
|-------|--------|
| Level 450 progression wall | Players unable to progress |
| 3 of 6 strategies non-viable | Crystal, Economy, Survival stuck at level 2-102 |
| Crystal economy bug | Simulator showed 1 crystal/session vs 1 crystal/monster |

**Key Metric:** Dominance Ratio = Best Strategy / Worst Strategy
**Before:** 3,305:1 (severely imbalanced)
**Target:** < 1.3:1 (competitive strategies)

---

## AI-Powered Solution

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                 AI Balance System                        │
├─────────────────────────────────────────────────────────┤
│  Claude AI ──▶ Headless Simulator ──▶ Strategy Analyzer │
│      │              │                      │            │
│      ▼              ▼                      ▼            │
│  Hypothesis    50-hr Game Time       Balance Report     │
│  Generation    Simulation            & Recommendations  │
└─────────────────────────────────────────────────────────┘
```

### Key AI Techniques

#### 1. Iterative Hypothesis-Verification Loop
```
Iteration 1: "Add damage to economy strategies" → Still level 2 → Analyze budget
Iteration 2: "Increase Phase 1 budget to 70%" → Level 27 → Analyze stat costs
Iteration 3: "Prioritize cheap stats" → Level 38 → Analyze budget overflow
Iteration 4: "Use DamageFirst logic" → Level 6,621 → SUCCESS
```

#### 2. Parallel Simulation Testing
- 6 strategies tested simultaneously
- 50 game-hours per strategy
- 3 minutes total runtime (vs 15 minutes sequential)

#### 3. Root Cause Analysis
AI traced bug from symptom to source:
```
Symptom: "crystal_chance: 303 upgrades but max Lv.1"
    ↓
Code Analysis: Budget allocation logic
    ↓
Root Cause: 50% of 1-2 crystals = 0 budget for Phase 1
    ↓
Fix: Minimum budget guarantee + priority reordering
```

#### 4. Automated Code Generation
AI generated production-ready fixes:
```csharp
// AI-generated crystal tracking fix
public void ProcessStageClear()
{
    _stageCompletionCrystals += _config.StageCompletionCrystal;
}
```

---

## Results

### Strategy Balance Improvement

| Strategy | Before | After | Change |
|----------|--------|-------|--------|
| Greedy | 4,731 | 4,733 | +0.04% |
| Damage | 6,611 | 6,614 | +0.05% |
| **Survival** | **102** | **6,640** | **+6,410%** |
| **Crystal** | **2** | **6,621** | **+331,000%** |
| Balanced | 6,265 | 6,269 | +0.06% |
| **Economy** | **2** | **6,624** | **+331,100%** |

### Key Metrics

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Dominance Ratio | 3,305:1 | 1.40:1 | **2,360x better** |
| Viable Strategies | 3/6 | 6/6 | **100% viable** |
| Death Mechanism | Broken | Working | **Restored** |

---

## Technical Implementation

### Files Modified: 21
### Lines Changed: ~500

**Simulator Changes:**
- `CrystalTracker.cs` - Per-monster crystal accumulation
- `SimulationEngine.cs` - Utility bonus coefficient (0.003→0.01)
- `ProgressionSimulator.cs` - Strategy budget allocation logic

**Game Logic Changes:**
- `DamageCalculator.cs` - Utility bonus sync
- `PermanentProgressionManager.cs` - Config-based conversion rates
- `BossDropConfig.cs` - New config fields

**Balance Data:**
- `PermanentStats.json` - start_level, start_gold, crystal_flat tuning
- `BossDrops.json` - Gold conversion rate (1000→100)

---

## Key Learnings

### AI Strengths
1. **Rapid iteration** - Hypothesis→Test→Fix cycles in minutes
2. **System-wide understanding** - Traces issues across entire codebase
3. **Code generation** - Produces working fixes directly

### Human + AI Collaboration
- Human: Defines balance goals and constraints
- AI: Analyzes, diagnoses, and implements fixes
- Human: Validates and approves changes

---

## Reproducibility

```bash
# Run balance verification test
cd DeskWarrior.Simulator
dotnet run -- --progress --game-hours 50 --strategy [name] --cps 5

# Available strategies: greedy, damage, survival, crystal, balanced, economy
```

---

## Conclusion

This project demonstrates the power of **AI-assisted game development**:

- **Speed:** 2 hours vs days of manual testing
- **Accuracy:** Root cause identification vs trial-and-error
- **Quality:** 2,360x improvement in balance ratio
- **Reproducibility:** Automated tests for regression prevention

The combination of a headless simulation engine with AI analysis creates a powerful **continuous balance verification system** applicable to any game with quantifiable balance metrics.

---

*Built with Claude Opus 4 | January 2026*
