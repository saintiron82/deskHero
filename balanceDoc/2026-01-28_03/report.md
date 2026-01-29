# DeskWarrior Balance Analysis Report

**Report ID:** `d173d9f9`  
**Generated:** 2026-01-28 20:18:59  
**Analysis Duration:** 253.2s  
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
| Dominance Ratio | 1.09 | :white_check_mark: PASS |
| Diversity Score | 0.24 | :x: POOR |
| Pattern Similarity | 0.86 | - |
| Active Categories | 1/4 | - |

### Level Statistics

- **Best Pattern:** Level 1599.0
- **Worst Pattern:** Level 1408.5
- **Average:** Level 1430.6
- **Spread:** 190.5 levels

:warning: **Underused Categories:** currency_bonus, utility, starting_bonus

---

## Top Patterns

### Top 10 Upgrade Strategies

| Rank | Pattern ID | Avg Level | Success | Main Stats | Diff |
|------|------------|-----------|---------|------------|------|
| 1 | `ga_optimized` | 1599.0 | 100% | attack_percent, base_attack, gold_multi_perm | - |
| 2 | `db41461e` | 1471.0 | 100% | attack_percent, crit_chance, base_attack | -8.0% |
| 3 | `1426525d` | 1471.0 | 100% | attack_percent, base_attack, crit_chance | -8.0% |
| 4 | `02dc561c` | 1468.5 | 100% | attack_percent, base_attack, crit_chance | -8.2% |
| 5 | `e31f15e3` | 1468.0 | 100% | attack_percent, crit_chance, base_attack | -8.2% |
| 6 | `2fd84749` | 1467.0 | 100% | attack_percent, base_attack, crit_chance | -8.3% |
| 7 | `6c9b211d` | 1464.0 | 100% | attack_percent, crit_chance, base_attack | -8.4% |
| 8 | `26b206bb` | 1458.5 | 100% | attack_percent, crit_chance, base_attack | -8.8% |
| 9 | `4342de0d` | 1457.0 | 100% | attack_percent, crit_chance, base_attack | -8.9% |
| 10 | `7e73205b` | 1452.5 | 100% | base_attack, attack_percent, crit_chance | -9.2% |

### Detailed Breakdown (Top 5)

#### #1: `ga_optimized`

- **Category:** base_stats
- **Average Level:** 1599.0 (Min: 1530, Max: 1680)
- **Std Deviation:** 37.27
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 43% ████████
- `base_attack`: 25% █████
- `gold_multi_perm`: 11% ██
- `crit_chance`: 10% ██
- `multi_hit`: 10% █

#### #2: `db41461e`

- **Category:** base_stats
- **Average Level:** 1471.0 (Min: 1360, Max: 1540)
- **Std Deviation:** 40.24
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 50% ██████████
- `crit_chance`: 30% ██████
- `base_attack`: 20% ████

#### #3: `1426525d`

- **Category:** base_stats
- **Average Level:** 1471.0 (Min: 1370, Max: 1540)
- **Std Deviation:** 42.06
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 50% ██████████
- `base_attack`: 30% ██████
- `crit_chance`: 20% ████

#### #4: `02dc561c`

- **Category:** base_stats
- **Average Level:** 1468.5 (Min: 1370, Max: 1540)
- **Std Deviation:** 38.89
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 60% ████████████
- `base_attack`: 20% ████
- `crit_chance`: 20% ████

#### #5: `e31f15e3`

- **Category:** base_stats
- **Average Level:** 1468.0 (Min: 1320, Max: 1570)
- **Std Deviation:** 58.62
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 50% ██████████
- `crit_chance`: 40% ████████
- `base_attack`: 10% ██

---

## Category Analysis

| Category | Usage | Status | Best Pattern | Best Level |
|----------|-------|--------|--------------|------------|
| Base Stats (Attack/Crit) | 100% | :white_check_mark: Active | `ga_optimized` | 1599.0 |
| Currency Bonus (Gold/Crystal) | 10% | :x: Underused | `N/A` | 0.0 |
| Utility (Time/Discount) | 0% | :x: Underused | `N/A` | 0.0 |
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
| 1 | `attack_percent` | base_stats | 1315.0 | **S** | 100% | :arrow_up: Overused |
| 2 | `base_attack` | base_stats | 1223.0 | **S** | 100% | :arrow_up: Overused |
| 3 | `crit_chance` | base_stats | 1201.0 | **S** | 100% | :arrow_up: Overused |
| 4 | `gold_multi_perm` | currency_bonus | 1181.5 | **A** | 10% | :arrow_down: Underused |
| 5 | `multi_hit` | base_stats | 1178.0 | **A** | 0% | :arrow_down: Underused |
| 6 | `time_extend` | utility | 1095.0 | **A** | 0% | :arrow_down: Underused |
| 7 | `gold_flat_perm` | currency_bonus | 1037.0 | B | 10% | :arrow_down: Underused |
| 8 | `crystal_multi` | currency_bonus | 1021.5 | B | 0% | :arrow_down: Underused |
| 9 | `crystal_flat` | currency_bonus | 1016.0 | B | 0% | :arrow_down: Underused |
| 10 | `start_gold_flat` | starting_bonus | 1016.0 | B | 0% | :arrow_down: Underused |
| 11 | `start_gold_multi` | starting_bonus | 1016.0 | C | 0% | :arrow_down: Underused |
| 12 | `start_combo_flex` | starting_bonus | 1016.0 | C | 0% | :arrow_down: Underused |
| 13 | `start_combo_damage` | starting_bonus | 1016.0 | C | 0% | :arrow_down: Underused |
| 14 | `start_mouse` | starting_bonus | 1015.0 | C | 0% | :arrow_down: Underused |
| 15 | `crit_damage` | base_stats | 1014.5 | _D_ | 0% | :arrow_down: Underused |
| 16 | `start_level` | starting_bonus | 1014.5 | _D_ | 0% | :arrow_down: Underused |
| 17 | `upgrade_discount` | utility | 1013.5 | _D_ | 0% | :arrow_down: Underused |
| 18 | `start_gold` | starting_bonus | 1012.5 | _D_ | 0% | :arrow_down: Underused |
| 19 | `start_keyboard` | starting_bonus | 1001.5 | _D_ | 0% | :arrow_down: Underused |

:star: **S-Tier Stats:** `attack_percent`, `base_attack`, `crit_chance`

:warning: **D-Tier Stats (needs buff):** `crit_damage`, `start_level`, `upgrade_discount`, `start_gold`, `start_keyboard`

---

## Recommendations

### :warning: High Priority

#### [Design] Overall balance

- **Issue:** Low pattern diversity (score: 0.24)
- **Suggestion:** Review stat cost/effect ratios to create more viable combinations
- **Expected Impact:** More strategic depth and replay value

### :bulb: Medium Priority

#### [Buff] Currency Bonus (Gold/Crystal)

- **Issue:** Category underutilized (10% usage)
- **Suggestion:** Consider buffing stats: gold_flat_perm, gold_multi_perm, crystal_flat
- **Expected Impact:** Increased build variety

#### [Buff] Utility (Time/Discount)

- **Issue:** Category underutilized (0% usage)
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

- **Issue:** Stat overperforming (rank #3, 100% usage in top builds)
- **Suggestion:** Slightly increase cost or reduce effect
- **Expected Impact:** More balanced stat distribution

### :information_source: Low Priority

#### [Buff] Start Level

- **Issue:** Stat underperforming (rank #16)
- **Suggestion:** Reduce cost or increase effect per level
- **Expected Impact:** Stat becomes viable in some builds

#### [Buff] Upgrade Discount

- **Issue:** Stat underperforming (rank #17)
- **Suggestion:** Reduce cost or increase effect per level
- **Expected Impact:** Stat becomes viable in some builds

#### [Buff] Start Gold

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
