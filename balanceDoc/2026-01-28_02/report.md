# DeskWarrior Balance Analysis Report

**Report ID:** `a299442c`  
**Generated:** 2026-01-28 20:07:12  
**Analysis Duration:** 304.4s  
**Patterns Evaluated:** 3,068  
**Total Simulations:** 61,360  

## Analysis Configuration

| Parameter | Value |
|-----------|-------|
| Target Level | 50 |
| CPS | 5.0 |
| Crystal Budget | 500 |
| Analysis Mode | Full |
| Simulations/Pattern | 20 |
| GA Generations | 100 |
| GA Population | 50 |

---

## Executive Summary

### Balance Grade: **C** :yellow_circle:

> Moderate balance issues - some paths underutilized.

### Key Metrics

| Metric | Value | Status |
|--------|-------|--------|
| Dominance Ratio | 1.03 | :white_check_mark: PASS |
| Diversity Score | 0.42 | :warning: MODERATE |
| Pattern Similarity | 0.54 | - |
| Active Categories | 1/4 | - |

### Level Statistics

- **Best Pattern:** Level 1702.0
- **Worst Pattern:** Level 1562.0
- **Average:** Level 1585.4
- **Spread:** 140.0 levels

:warning: **Underused Categories:** currency_bonus, utility, starting_bonus

---

## Top Patterns

### Top 10 Upgrade Strategies

| Rank | Pattern ID | Avg Level | Success | Main Stats | Diff |
|------|------------|-----------|---------|------------|------|
| 1 | `ga_optimized` | 1702.0 | 100% | attack_percent, gold_multi_perm, base_attack | - |
| 2 | `9c8e6a95` | 1650.5 | 100% | attack_percent, crit_chance, base_attack | -3.0% |
| 3 | `0cda3a84` | 1630.5 | 100% | attack_percent, base_attack, crit_chance | -4.2% |
| 4 | `a7ee0a1c` | 1628.0 | 100% | attack_percent, crit_chance, base_attack | -4.3% |
| 5 | `c40ec11a` | 1628.0 | 100% | attack_percent, crit_chance, base_attack | -4.3% |
| 6 | `681a7226` | 1624.0 | 100% | attack_percent, crit_chance, base_attack | -4.6% |
| 7 | `a09108d4` | 1620.0 | 100% | attack_percent, base_attack, crit_chance | -4.8% |
| 8 | `2aaa0a34` | 1619.5 | 100% | attack_percent, base_attack, crit_chance | -4.8% |
| 9 | `ca0e9d83` | 1618.0 | 100% | attack_percent, base_attack, crit_chance | -4.9% |
| 10 | `90e0d5a0` | 1616.0 | 100% | attack_percent, base_attack, crit_chance | -5.1% |

### Detailed Breakdown (Top 5)

#### #1: `ga_optimized`

- **Category:** base_stats
- **Average Level:** 1702.0 (Min: 1590, Max: 1770)
- **Std Deviation:** 42.61
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 63% ████████████
- `gold_multi_perm`: 13% ██
- `base_attack`: 10% ██
- `crit_chance`: 10% █
- `time_extend`: 3% 

#### #2: `9c8e6a95`

- **Category:** base_stats
- **Average Level:** 1650.5 (Min: 1530, Max: 1740)
- **Std Deviation:** 42.72
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 70% ██████████████
- `crit_chance`: 20% ████
- `base_attack`: 10% ██

#### #3: `0cda3a84`

- **Category:** base_stats
- **Average Level:** 1630.5 (Min: 1570, Max: 1720)
- **Std Deviation:** 45.44
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 70% ██████████████
- `base_attack`: 10% ██
- `crit_chance`: 10% ██
- `time_extend`: 10% ██

#### #4: `a7ee0a1c`

- **Category:** base_stats
- **Average Level:** 1628.0 (Min: 1570, Max: 1690)
- **Std Deviation:** 32.95
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 50% ██████████
- `crit_chance`: 30% ██████
- `base_attack`: 20% ████

#### #5: `c40ec11a`

- **Category:** base_stats
- **Average Level:** 1628.0 (Min: 1550, Max: 1710)
- **Std Deviation:** 36.28
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 60% ████████████
- `crit_chance`: 20% ████
- `base_attack`: 10% ██
- `time_extend`: 10% ██

---

## Category Analysis

| Category | Usage | Status | Best Pattern | Best Level |
|----------|-------|--------|--------------|------------|
| Base Stats (Attack/Crit) | 100% | :white_check_mark: Active | `ga_optimized` | 1702.0 |
| Currency Bonus (Gold/Crystal) | 10% | :x: Underused | `N/A` | 0.0 |
| Utility (Time/Discount) | 10% | :x: Underused | `N/A` | 0.0 |
| Starting Bonus | 0% | :x: Underused | `N/A` | 0.0 |

### Base Stats (Attack/Crit)

**Stats:** `base_attack`, `attack_percent`, `crit_chance`, `crit_damage`, `multi_hit`

**Recommendation:** Currently well-balanced

### Currency Bonus (Gold/Crystal)

**Stats:** `gold_flat_perm`, `gold_multi_perm`, `crystal_flat`, `crystal_multi`

**Recommendation:** Consider buffing stats in this category or reducing costs

### Utility (Time/Discount)

**Stats:** `time_extend`, `upgrade_discount`

**Recommendation:** Consider buffing stats in this category or reducing costs

### Starting Bonus

**Stats:** `start_level`, `start_gold`, `start_keyboard`, `start_mouse`, `start_gold_flat`, `start_gold_multi`, `start_combo_flex`, `start_combo_damage`

**Recommendation:** Consider buffing stats in this category or reducing costs

---

## Stat Analysis

### Single-Stat Efficiency Ranking

| Rank | Stat | Category | Level | Rating | Top10 Usage | Status |
|------|------|----------|-------|--------|-------------|--------|
| 1 | `attack_percent` | base_stats | 1492.5 | **S** | 100% | :arrow_up: Overused |
| 2 | `base_attack` | base_stats | 1223.0 | **S** | 100% | :arrow_up: Overused |
| 3 | `crit_chance` | base_stats | 1201.0 | **S** | 90% | :arrow_up: Overused |
| 4 | `gold_multi_perm` | currency_bonus | 1190.5 | **A** | 10% | :arrow_down: Underused |
| 5 | `multi_hit` | base_stats | 1178.0 | **A** | 0% | :arrow_down: Underused |
| 6 | `time_extend` | utility | 1095.0 | **A** | 60% | :arrow_up: Overused |
| 7 | `gold_flat_perm` | currency_bonus | 1037.0 | B | 0% | :arrow_down: Underused |
| 8 | `crystal_multi` | currency_bonus | 1021.5 | B | 0% | :arrow_down: Underused |
| 9 | `crystal_flat` | currency_bonus | 1016.0 | B | 0% | :arrow_down: Underused |
| 10 | `start_gold_flat` | starting_bonus | 1016.0 | B | 0% | :arrow_down: Underused |
| 11 | `start_gold_multi` | starting_bonus | 1016.0 | C | 0% | :arrow_down: Underused |
| 12 | `start_combo_flex` | starting_bonus | 1016.0 | C | 0% | :arrow_down: Underused |
| 13 | `start_combo_damage` | starting_bonus | 1016.0 | C | 0% | :arrow_down: Underused |
| 14 | `start_mouse` | starting_bonus | 1015.0 | C | 0% | :arrow_down: Underused |
| 15 | `crit_damage` | base_stats | 1014.5 | _D_ | 0% | :arrow_down: Underused |
| 16 | `start_level` | starting_bonus | 1014.5 | _D_ | 0% | :arrow_down: Underused |
| 17 | `start_gold` | starting_bonus | 1012.5 | _D_ | 0% | :arrow_down: Underused |
| 18 | `upgrade_discount` | utility | 1007.0 | _D_ | 0% | :arrow_down: Underused |
| 19 | `start_keyboard` | starting_bonus | 1001.5 | _D_ | 0% | :arrow_down: Underused |

:star: **S-Tier Stats:** `attack_percent`, `base_attack`, `crit_chance`

:warning: **D-Tier Stats (needs buff):** `crit_damage`, `start_level`, `start_gold`, `upgrade_discount`, `start_keyboard`

---

## Recommendations

### :bulb: Medium Priority

#### [Buff] Currency Bonus (Gold/Crystal)

- **Issue:** Category underutilized (10% usage)
- **Suggestion:** Consider buffing stats: gold_flat_perm, gold_multi_perm, crystal_flat
- **Expected Impact:** Increased build variety

#### [Buff] Utility (Time/Discount)

- **Issue:** Category underutilized (10% usage)
- **Suggestion:** Consider buffing stats: time_extend, upgrade_discount
- **Expected Impact:** Increased build variety

#### [Buff] Starting Bonus

- **Issue:** Category underutilized (0% usage)
- **Suggestion:** Consider buffing stats: start_level, start_gold, start_keyboard
- **Expected Impact:** Increased build variety

#### [Nerf] Attack Percent

- **Issue:** Stat overperforming (rank #1, 100% usage in top builds)
- **Suggestion:** Slightly increase cost or reduce effect
- **Expected Impact:** More balanced stat distribution

#### [Nerf] Base Attack

- **Issue:** Stat overperforming (rank #2, 100% usage in top builds)
- **Suggestion:** Slightly increase cost or reduce effect
- **Expected Impact:** More balanced stat distribution

#### [Nerf] Crit Chance

- **Issue:** Stat overperforming (rank #3, 90% usage in top builds)
- **Suggestion:** Slightly increase cost or reduce effect
- **Expected Impact:** More balanced stat distribution

### :information_source: Low Priority

#### [Buff] Start Level

- **Issue:** Stat underperforming (rank #16)
- **Suggestion:** Reduce cost or increase effect per level
- **Expected Impact:** Stat becomes viable in some builds

#### [Buff] Start Gold

- **Issue:** Stat underperforming (rank #17)
- **Suggestion:** Reduce cost or increase effect per level
- **Expected Impact:** Stat becomes viable in some builds

#### [Buff] Upgrade Discount

- **Issue:** Stat underperforming (rank #18)
- **Suggestion:** Reduce cost or increase effect per level
- **Expected Impact:** Stat becomes viable in some builds

#### [Buff] Start Keyboard

- **Issue:** Stat underperforming (rank #19)
- **Suggestion:** Reduce cost or increase effect per level
- **Expected Impact:** Stat becomes viable in some builds

---

## Appendix

### Glossary

| Term | Definition |
|------|------------|
| Dominance Ratio | 1st place / 2nd place average level. >1.3 indicates dominant route |
| Diversity Score | Jaccard distance between top patterns (0-1). Higher = more diverse |
| Pattern Similarity | Cosine similarity between top 3 patterns (0-1). Lower = more diverse |
| Usage Rate | % of top patterns using this category/stat significantly (>10%) |

### Grade Scale

| Grade | Meaning |
|-------|---------|
| A | Excellent - Multiple viable routes, high diversity |
| B | Good - Minor dominance tendency |
| C | Moderate - Some paths underutilized |
| D | Poor - Significant path preference |
| F | Fail - Single dominant route detected |

---

*Generated by DeskWarrior.Simulator v1.0*
