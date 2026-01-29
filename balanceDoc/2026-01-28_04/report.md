# DeskWarrior Balance Analysis Report

**Report ID:** `735547f8`  
**Generated:** 2026-01-28 20:57:15  
**Analysis Duration:** 202.9s  
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

### Balance Grade: **D** :orange_circle:

> Poor balance - significant path preference.

### Key Metrics

| Metric | Value | Status |
|--------|-------|--------|
| Dominance Ratio | 1.06 | :white_check_mark: PASS |
| Diversity Score | 0.11 | :x: POOR |
| Pattern Similarity | 0.81 | - |
| Active Categories | 2/4 | - |

### Level Statistics

- **Best Pattern:** Level 1574.0
- **Worst Pattern:** Level 1415.0
- **Average:** Level 1436.1
- **Spread:** 159.0 levels

:warning: **Underused Categories:** utility, starting_bonus

---

## Top Patterns

### Top 10 Upgrade Strategies

| Rank | Pattern ID | Avg Level | Success | Main Stats | Diff |
|------|------------|-----------|---------|------------|------|
| 1 | `ga_optimized` | 1574.0 | 100% | attack_percent, gold_multi_perm, crit_chance | - |
| 2 | `53043e4e` | 1482.5 | 100% | attack_percent, gold_multi_perm, multi_hit | -5.8% |
| 3 | `6a561d13` | 1479.0 | 100% | multi_hit, attack_percent, gold_multi_perm | -6.0% |
| 4 | `b2bb77ab` | 1478.0 | 100% | gold_multi_perm, multi_hit, attack_percent | -6.1% |
| 5 | `400c9210` | 1474.0 | 100% | attack_percent, gold_multi_perm, multi_hit | -6.4% |
| 6 | `0ebe63b9` | 1470.5 | 100% | multi_hit, attack_percent, gold_multi_perm | -6.6% |
| 7 | `d392e3c2` | 1468.0 | 100% | multi_hit, attack_percent, gold_multi_perm | -6.7% |
| 8 | `e28323df` | 1467.5 | 100% | attack_percent, multi_hit, gold_multi_perm | -6.8% |
| 9 | `6634428e` | 1464.5 | 100% | multi_hit, attack_percent, gold_multi_perm | -7.0% |
| 10 | `b858ac9f` | 1462.5 | 100% | multi_hit, attack_percent, gold_multi_perm | -7.1% |

### Detailed Breakdown (Top 5)

#### #1: `ga_optimized`

- **Category:** base_stats
- **Average Level:** 1574.0 (Min: 1480, Max: 1650)
- **Std Deviation:** 50.83
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 30% █████
- `gold_multi_perm`: 25% ████
- `crit_chance`: 21% ████
- `multi_hit`: 15% ███
- `time_extend`: 4% 
- `start_combo_flex`: 3% 
- `upgrade_discount`: 2% 

#### #2: `53043e4e`

- **Category:** base_stats
- **Average Level:** 1482.5 (Min: 1390, Max: 1530)
- **Std Deviation:** 36.04
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 50% ██████████
- `gold_multi_perm`: 30% ██████
- `multi_hit`: 20% ████

#### #3: `6a561d13`

- **Category:** base_stats
- **Average Level:** 1479.0 (Min: 1390, Max: 1560)
- **Std Deviation:** 48.36
- **Success Rate:** 100.0%

**Allocation:**
- `multi_hit`: 40% ████████
- `attack_percent`: 30% ██████
- `gold_multi_perm`: 30% ██████

#### #4: `b2bb77ab`

- **Category:** currency_bonus
- **Average Level:** 1478.0 (Min: 1370, Max: 1560)
- **Std Deviation:** 49.05
- **Success Rate:** 100.0%

**Allocation:**
- `gold_multi_perm`: 40% ████████
- `multi_hit`: 30% ██████
- `attack_percent`: 30% ██████

#### #5: `400c9210`

- **Category:** base_stats
- **Average Level:** 1474.0 (Min: 1370, Max: 1560)
- **Std Deviation:** 43.63
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 40% ████████
- `gold_multi_perm`: 40% ████████
- `multi_hit`: 20% ████

---

## Category Analysis

| Category | Usage | Status | Best Pattern | Best Level |
|----------|-------|--------|--------------|------------|
| Base Stats (Attack/Crit) | 100% | :white_check_mark: Active | `53043e4e` | 1482.5 |
| Currency Bonus (Gold/Crystal) | 90% | :white_check_mark: Active | `53043e4e` | 1482.5 |
| Utility (Time/Discount) | 0% | :x: Underused | `N/A` | 0.0 |
| Starting Bonus | 0% | :x: Underused | `N/A` | 0.0 |

### Base Stats (Attack/Crit)

**Stats:** `base_attack`, `attack_percent`, `crit_chance`, `crit_damage`, `multi_hit`

**Recommendation:** Currently well-balanced

### Currency Bonus (Gold/Crystal)

**Stats:** `gold_flat_perm`, `gold_multi_perm`, `crystal_flat`, `crystal_multi`

**Recommendation:** Currently well-balanced

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
| 1 | `multi_hit` | base_stats | 1277.5 | **S** | 100% | :arrow_up: Overused |
| 2 | `attack_percent` | base_stats | 1220.0 | **S** | 100% | :arrow_up: Overused |
| 3 | `gold_multi_perm` | currency_bonus | 1203.5 | **S** | 100% | :arrow_up: Overused |
| 4 | `crit_chance` | base_stats | 1201.0 | **A** | 10% | :arrow_down: Underused |
| 5 | `base_attack` | base_stats | 1177.5 | **A** | 0% | :arrow_down: Underused |
| 6 | `time_extend` | utility | 1135.0 | **A** | 0% | :arrow_down: Underused |
| 7 | `gold_flat_perm` | currency_bonus | 1037.0 | B | 0% | :arrow_down: Underused |
| 8 | `crit_damage` | base_stats | 1026.5 | B | 20% | :heavy_minus_sign: Balanced |
| 9 | `upgrade_discount` | utility | 1022.5 | B | 0% | :arrow_down: Underused |
| 10 | `crystal_multi` | currency_bonus | 1021.5 | B | 0% | :arrow_down: Underused |
| 11 | `crystal_flat` | currency_bonus | 1016.0 | C | 0% | :arrow_down: Underused |
| 12 | `start_gold_flat` | starting_bonus | 1016.0 | C | 0% | :arrow_down: Underused |
| 13 | `start_gold_multi` | starting_bonus | 1016.0 | C | 0% | :arrow_down: Underused |
| 14 | `start_combo_flex` | starting_bonus | 1016.0 | C | 0% | :arrow_down: Underused |
| 15 | `start_combo_damage` | starting_bonus | 1016.0 | _D_ | 0% | :arrow_down: Underused |
| 16 | `start_mouse` | starting_bonus | 1015.0 | _D_ | 0% | :arrow_down: Underused |
| 17 | `start_gold` | starting_bonus | 1012.5 | _D_ | 0% | :arrow_down: Underused |
| 18 | `start_keyboard` | starting_bonus | 1001.5 | _D_ | 0% | :arrow_down: Underused |
| 19 | `start_level` | starting_bonus | 867.5 | _D_ | 0% | :arrow_down: Underused |

:star: **S-Tier Stats:** `multi_hit`, `attack_percent`, `gold_multi_perm`

:warning: **D-Tier Stats (needs buff):** `start_combo_damage`, `start_mouse`, `start_gold`, `start_keyboard`, `start_level`

---

## Recommendations

### :warning: High Priority

#### [Design] Overall balance

- **Issue:** Low pattern diversity (score: 0.11)
- **Suggestion:** Review stat cost/effect ratios to create more viable combinations
- **Expected Impact:** More strategic depth and replay value

### :bulb: Medium Priority

#### [Buff] Utility (Time/Discount)

- **Issue:** Category underutilized (0% usage)
- **Suggestion:** Consider buffing stats: time_extend, upgrade_discount
- **Expected Impact:** Increased build variety

#### [Buff] Starting Bonus

- **Issue:** Category underutilized (0% usage)
- **Suggestion:** Consider buffing stats: start_level, start_gold, start_keyboard
- **Expected Impact:** Increased build variety

#### [Nerf] Multi Hit

- **Issue:** Stat overperforming (rank #1, 100% usage in top builds)
- **Suggestion:** Slightly increase cost or reduce effect
- **Expected Impact:** More balanced stat distribution

#### [Nerf] Attack Percent

- **Issue:** Stat overperforming (rank #2, 100% usage in top builds)
- **Suggestion:** Slightly increase cost or reduce effect
- **Expected Impact:** More balanced stat distribution

#### [Nerf] Gold Multi Perm

- **Issue:** Stat overperforming (rank #3, 100% usage in top builds)
- **Suggestion:** Slightly increase cost or reduce effect
- **Expected Impact:** More balanced stat distribution

### :information_source: Low Priority

#### [Buff] Start Mouse

- **Issue:** Stat underperforming (rank #16)
- **Suggestion:** Reduce cost or increase effect per level
- **Expected Impact:** Stat becomes viable in some builds

#### [Buff] Start Gold

- **Issue:** Stat underperforming (rank #17)
- **Suggestion:** Reduce cost or increase effect per level
- **Expected Impact:** Stat becomes viable in some builds

#### [Buff] Start Keyboard

- **Issue:** Stat underperforming (rank #18)
- **Suggestion:** Reduce cost or increase effect per level
- **Expected Impact:** Stat becomes viable in some builds

#### [Buff] Start Level

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
