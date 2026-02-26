# DeskWarrior Balance Analysis Report

**Report ID:** `09ab1db6`  
**Generated:** 2026-01-29 17:27:11  
**Analysis Duration:** 2.2s  
**Patterns Evaluated:** 3,068  
**Total Simulations:** 61,360  

## Analysis Configuration

| Parameter | Value |
|-----------|-------|
| Target Level | 50 |
| CPS | 5.0 |
| Crystal Budget | 0 |
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
| Dominance Ratio | 1.00 | :white_check_mark: PASS |
| Diversity Score | 1.00 | :white_check_mark: GOOD |
| Pattern Similarity | 1.00 | - |
| Active Categories | 2/4 | - |

### Level Statistics

- **Best Pattern:** Level 10.0
- **Worst Pattern:** Level 10.0
- **Average:** Level 10.0
- **Spread:** 0.0 levels

:warning: **Underused Categories:** utility, starting_bonus

---

## Top Patterns

### Top 10 Upgrade Strategies

| Rank | Pattern ID | Avg Level | Success | Main Stats | Diff |
|------|------------|-----------|---------|------------|------|
| 1 | `single_base_attack` | 10.0 | 0% | base_attack | - |
| 2 | `single_attack_percent` | 10.0 | 0% | attack_percent | - |
| 3 | `single_crit_chance` | 10.0 | 0% | crit_chance | - |
| 4 | `single_crit_damage` | 10.0 | 0% | crit_damage | - |
| 5 | `single_multi_hit` | 10.0 | 0% | multi_hit | - |
| 6 | `single_gold_flat_perm` | 10.0 | 0% | gold_flat_perm | - |
| 7 | `single_gold_multi_perm` | 10.0 | 0% | gold_multi_perm | - |
| 8 | `single_crystal_flat` | 10.0 | 0% | crystal_flat | - |
| 9 | `single_crystal_chance` | 10.0 | 0% | crystal_chance | - |
| 10 | `single_time_extend` | 10.0 | 0% | time_extend | - |

### Detailed Breakdown (Top 5)

#### #1: `single_base_attack`

- **Category:** base_stats
- **Average Level:** 10.0 (Min: 10, Max: 10)
- **Std Deviation:** 0.00
- **Success Rate:** 0.0%

**Allocation:**
- `base_attack`: 100% ████████████████████

#### #2: `single_attack_percent`

- **Category:** base_stats
- **Average Level:** 10.0 (Min: 10, Max: 10)
- **Std Deviation:** 0.00
- **Success Rate:** 0.0%

**Allocation:**
- `attack_percent`: 100% ████████████████████

#### #3: `single_crit_chance`

- **Category:** base_stats
- **Average Level:** 10.0 (Min: 10, Max: 10)
- **Std Deviation:** 0.00
- **Success Rate:** 0.0%

**Allocation:**
- `crit_chance`: 100% ████████████████████

#### #4: `single_crit_damage`

- **Category:** base_stats
- **Average Level:** 10.0 (Min: 10, Max: 10)
- **Std Deviation:** 0.00
- **Success Rate:** 0.0%

**Allocation:**
- `crit_damage`: 100% ████████████████████

#### #5: `single_multi_hit`

- **Category:** base_stats
- **Average Level:** 10.0 (Min: 10, Max: 10)
- **Std Deviation:** 0.00
- **Success Rate:** 0.0%

**Allocation:**
- `multi_hit`: 100% ████████████████████

---

## Category Analysis

| Category | Usage | Status | Best Pattern | Best Level |
|----------|-------|--------|--------------|------------|
| Base Stats (Attack/Crit) | 50% | :white_check_mark: Active | `single_base_attack` | 10.0 |
| Currency Bonus (Gold/Crystal) | 30% | :warning: Moderate | `single_gold_flat_perm` | 10.0 |
| Utility (Time/Discount) | 10% | :x: Underused | `single_time_extend` | 10.0 |
| Starting Bonus | 0% | :x: Underused | `single_start_level` | 10.0 |

### Base Stats (Attack/Crit)

**Stats:** `base_attack`, `attack_percent`, `crit_chance`, `crit_damage`, `multi_hit`

**Recommendation:** Currently well-balanced

### Currency Bonus (Gold/Crystal)

**Stats:** `gold_flat_perm`, `gold_multi_perm`, `crystal_flat`, `crystal_multi`

**Recommendation:** Monitor for potential improvements

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
| 1 | `base_attack` | base_stats | 10.0 | **S** | 10% | :arrow_down: Underused |
| 2 | `attack_percent` | base_stats | 10.0 | **S** | 10% | :arrow_down: Underused |
| 3 | `crit_chance` | base_stats | 10.0 | **S** | 10% | :arrow_down: Underused |
| 4 | `crit_damage` | base_stats | 10.0 | **A** | 10% | :arrow_down: Underused |
| 5 | `multi_hit` | base_stats | 10.0 | **A** | 10% | :arrow_down: Underused |
| 6 | `gold_flat_perm` | currency_bonus | 10.0 | **A** | 10% | :arrow_down: Underused |
| 7 | `gold_multi_perm` | currency_bonus | 10.0 | B | 10% | :arrow_down: Underused |
| 8 | `crystal_flat` | currency_bonus | 10.0 | B | 10% | :arrow_down: Underused |
| 9 | `crystal_chance` | unknown | 10.0 | B | 10% | :arrow_down: Underused |
| 10 | `time_extend` | utility | 10.0 | B | 10% | :arrow_down: Underused |
| 11 | `upgrade_discount` | utility | 10.0 | C | 0% | :arrow_down: Underused |
| 12 | `start_level` | starting_bonus | 10.0 | C | 0% | :arrow_down: Underused |
| 13 | `start_gold` | starting_bonus | 10.0 | C | 0% | :arrow_down: Underused |
| 14 | `start_keyboard` | starting_bonus | 10.0 | C | 0% | :arrow_down: Underused |
| 15 | `start_mouse` | starting_bonus | 10.0 | _D_ | 0% | :arrow_down: Underused |
| 16 | `start_gold_flat` | starting_bonus | 10.0 | _D_ | 0% | :arrow_down: Underused |
| 17 | `start_gold_multi` | starting_bonus | 10.0 | _D_ | 0% | :arrow_down: Underused |
| 18 | `start_combo_flex` | starting_bonus | 10.0 | _D_ | 0% | :arrow_down: Underused |
| 19 | `start_combo_damage` | starting_bonus | 10.0 | _D_ | 0% | :arrow_down: Underused |

:star: **S-Tier Stats:** `base_attack`, `attack_percent`, `crit_chance`

:warning: **D-Tier Stats (needs buff):** `start_mouse`, `start_gold_flat`, `start_gold_multi`, `start_combo_flex`, `start_combo_damage`

---

## Recommendations

### :bulb: Medium Priority

#### [Buff] Utility (Time/Discount)

- **Issue:** Category underutilized (10% usage)
- **Suggestion:** Consider buffing stats: time_extend, upgrade_discount
- **Expected Impact:** Increased build variety

#### [Buff] Starting Bonus

- **Issue:** Category underutilized (0% usage)
- **Suggestion:** Consider buffing stats: start_level, start_gold, start_keyboard
- **Expected Impact:** Increased build variety

### :information_source: Low Priority

#### [Buff] Start Gold Flat

- **Issue:** Stat underperforming (rank #16)
- **Suggestion:** Reduce cost or increase effect per level
- **Expected Impact:** Stat becomes viable in some builds

#### [Buff] Start Gold Multi

- **Issue:** Stat underperforming (rank #17)
- **Suggestion:** Reduce cost or increase effect per level
- **Expected Impact:** Stat becomes viable in some builds

#### [Buff] Start Combo Flex

- **Issue:** Stat underperforming (rank #18)
- **Suggestion:** Reduce cost or increase effect per level
- **Expected Impact:** Stat becomes viable in some builds

#### [Buff] Start Combo Damage

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
