# DeskWarrior Balance Analysis Report

**Report ID:** `327c313b`  
**Generated:** 2026-01-28 19:03:00  
**Analysis Duration:** 227.1s  
**Patterns Evaluated:** 1,051  
**Total Simulations:** 21,020  

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
| Dominance Ratio | 1.11 | :white_check_mark: PASS |
| Diversity Score | 0.30 | :warning: MODERATE |
| Pattern Similarity | 0.82 | - |
| Active Categories | 2/4 | - |

### Level Statistics

- **Best Pattern:** Level 1738.0
- **Worst Pattern:** Level 1436.0
- **Average:** Level 1477.7
- **Spread:** 302.0 levels

:warning: **Underused Categories:** utility, starting_bonus

---

## Top Patterns

### Top 10 Upgrade Strategies

| Rank | Pattern ID | Avg Level | Success | Main Stats | Diff |
|------|------------|-----------|---------|------------|------|
| 1 | `ga_optimized` | 1738.0 | 100% | attack_percent, base_attack, crit_chance | - |
| 2 | `5bc43d9b` | 1560.5 | 100% | attack_percent, gold_multi_perm | -10.2% |
| 3 | `duo_gold_multi_perm_attack_percent_30` | 1560.5 | 100% | attack_percent, gold_multi_perm | -10.2% |
| 4 | `8fcb7683` | 1554.0 | 100% | attack_percent, gold_multi_perm | -10.6% |
| 5 | `47cee7c5` | 1544.5 | 100% | attack_percent, gold_multi_perm, crystal_multi | -11.1% |
| 6 | `064eedb6` | 1540.5 | 100% | attack_percent, gold_multi_perm | -11.4% |
| 7 | `6c6dc003` | 1536.0 | 100% | attack_percent, gold_multi_perm, start_gold | -11.6% |
| 8 | `bdfc2925` | 1534.0 | 100% | attack_percent, gold_multi_perm, gold_flat_perm | -11.7% |
| 9 | `135ce38d` | 1528.5 | 100% | attack_percent, gold_multi_perm | -12.1% |
| 10 | `a8524b60` | 1527.5 | 100% | attack_percent, gold_multi_perm, gold_flat_perm | -12.1% |

### Detailed Breakdown (Top 5)

#### #1: `ga_optimized`

- **Category:** base_stats
- **Average Level:** 1738.0 (Min: 1650, Max: 1820)
- **Std Deviation:** 39.95
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 38% ███████
- `base_attack`: 19% ███
- `crit_chance`: 18% ███
- `multi_hit`: 12% ██
- `gold_multi_perm`: 12% ██

#### #2: `5bc43d9b`

- **Category:** base_stats
- **Average Level:** 1560.5 (Min: 1490, Max: 1630)
- **Std Deviation:** 41.65
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 70% ██████████████
- `gold_multi_perm`: 30% ██████

#### #3: `duo_gold_multi_perm_attack_percent_30`

- **Category:** base_stats
- **Average Level:** 1560.5 (Min: 1490, Max: 1630)
- **Std Deviation:** 41.65
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 70% ██████████████
- `gold_multi_perm`: 30% ██████

#### #4: `8fcb7683`

- **Category:** base_stats
- **Average Level:** 1554.0 (Min: 1460, Max: 1610)
- **Std Deviation:** 44.54
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 80% ████████████████
- `gold_multi_perm`: 20% ████

#### #5: `47cee7c5`

- **Category:** base_stats
- **Average Level:** 1544.5 (Min: 1490, Max: 1590)
- **Std Deviation:** 27.29
- **Success Rate:** 100.0%

**Allocation:**
- `attack_percent`: 70% ██████████████
- `gold_multi_perm`: 20% ████
- `crystal_multi`: 10% ██

---

## Category Analysis

| Category | Usage | Status | Best Pattern | Best Level |
|----------|-------|--------|--------------|------------|
| Base Stats (Attack/Crit) | 100% | :white_check_mark: Active | `ga_optimized` | 1738.0 |
| Currency Bonus (Gold/Crystal) | 80% | :white_check_mark: Active | `5bc43d9b` | 1560.5 |
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
| 1 | `attack_percent` | base_stats | 1492.5 | **S** | 100% | :arrow_up: Overused |
| 2 | `base_attack` | base_stats | 1223.0 | **S** | 10% | :arrow_down: Underused |
| 3 | `crit_chance` | base_stats | 1201.0 | **S** | 10% | :arrow_down: Underused |
| 4 | `gold_multi_perm` | currency_bonus | 1190.5 | **A** | 100% | :arrow_up: Overused |
| 5 | `multi_hit` | base_stats | 1178.0 | **A** | 10% | :arrow_down: Underused |
| 6 | `time_extend` | utility | 1095.0 | **A** | 0% | :arrow_down: Underused |
| 7 | `gold_flat_perm` | currency_bonus | 1037.0 | B | 20% | :heavy_minus_sign: Balanced |
| 8 | `crystal_multi` | currency_bonus | 1021.5 | B | 20% | :heavy_minus_sign: Balanced |
| 9 | `crystal_flat` | currency_bonus | 1016.0 | B | 0% | :arrow_down: Underused |
| 10 | `start_gold_flat` | starting_bonus | 1016.0 | B | 0% | :arrow_down: Underused |
| 11 | `start_gold_multi` | starting_bonus | 1016.0 | C | 0% | :arrow_down: Underused |
| 12 | `start_combo_flex` | starting_bonus | 1016.0 | C | 0% | :arrow_down: Underused |
| 13 | `start_combo_damage` | starting_bonus | 1016.0 | C | 0% | :arrow_down: Underused |
| 14 | `start_mouse` | starting_bonus | 1015.0 | C | 0% | :arrow_down: Underused |
| 15 | `crit_damage` | base_stats | 1014.5 | _D_ | 0% | :arrow_down: Underused |
| 16 | `start_level` | starting_bonus | 1014.5 | _D_ | 0% | :arrow_down: Underused |
| 17 | `start_gold` | starting_bonus | 1012.5 | _D_ | 10% | :arrow_down: Underused |
| 18 | `upgrade_discount` | utility | 1007.0 | _D_ | 0% | :arrow_down: Underused |
| 19 | `start_keyboard` | starting_bonus | 1001.5 | _D_ | 0% | :arrow_down: Underused |

:star: **S-Tier Stats:** `attack_percent`, `base_attack`, `crit_chance`

:warning: **D-Tier Stats (needs buff):** `crit_damage`, `start_level`, `start_gold`, `upgrade_discount`, `start_keyboard`

---

## Recommendations

### :bulb: Medium Priority

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
