"""
DeskWarrior Balance CSV Generator
Generates CSV files for monster HP, player stats, and upgrade costs.
Uses the same formulas as the actual game (config-driven).
"""
import csv
import math
import json
import os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
CONFIG_DIR = os.path.join(SCRIPT_DIR, "..", "config")
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "..", "balanceDoc")

MAX_LEVEL = 100


# ============================================================
# Tier HP System (Monster.CalculateTierBasedHp)
# ============================================================
def calculate_tier_hp(base_hp, level, cfg):
    tier = (level - 1) // cfg["tier_interval"]
    tier_index = float(tier)

    exp = cfg.get("tier_curve_exponent", 1.0)
    if exp > 0 and exp != 1.0 and tier_index > 0:
        tier_index = math.pow(tier_index, exp)

    tier_mult = math.pow(cfg["tier_multiplier"], tier_index)

    decay_per_tier = cfg.get("tier_multiplier_decay_per_tier", 1.0)
    if decay_per_tier != 1.0:
        decay = math.pow(decay_per_tier, tier_index * (tier_index - 1) / 2.0)
        tier_mult *= decay

    min_mult = cfg.get("min_tier_multiplier", 0)
    if min_mult > 0 and tier_mult < min_mult:
        tier_mult = min_mult

    tier_base_hp = int(base_hp * tier_mult)

    level_in_tier = (level - 1) % cfg["tier_interval"]
    growth_rate = cfg["linear_growth_per_level"] * math.pow(
        cfg.get("growth_decrease_per_tier", 1.0), tier_index
    )
    min_growth = cfg.get("min_linear_growth_per_level", 0)
    if min_growth > 0 and growth_rate < min_growth:
        growth_rate = min_growth

    linear_inc = int(level_in_tier * growth_rate)
    hp = tier_base_hp + linear_inc

    # Late game
    late_start = cfg.get("late_start_level", 0)
    if late_start > 0 and level >= late_start:
        late_interval = cfg.get("late_tier_interval", cfg["tier_interval"])
        late_tier = (level - late_start) // max(1, late_interval)
        max_late = cfg.get("max_late_tiers", 0)
        if max_late > 0 and late_tier > max_late:
            late_tier = max_late
        late_mult = cfg.get("late_tier_multiplier", 1.0)
        if late_mult != 1.0:
            hp = int(hp * math.pow(late_mult, late_tier))

    return hp, tier, growth_rate


# ============================================================
# Stat Cost Formula (StatGrowthConfig.CalculateCost)
# ============================================================
def calculate_cost(base_cost, growth_rate, multiplier, softcap_interval, level):
    if level <= 0:
        return 0
    linear = 1.0 + level * growth_rate
    exp = math.pow(multiplier, level / softcap_interval) if multiplier > 0 else 1.0
    return math.ceil(base_cost * linear * exp)


# ============================================================
# Stat Effect Formula (StatGrowthConfig.CalculateEffect)
# ============================================================
def calculate_effect(effect_per_level, level, max_effect=0, tier_config=None):
    if level <= 0:
        return 0

    if tier_config and (
        tier_config.get("effect_multiplier_per_tier", 1.0) != 1.0
        or tier_config.get("effect_add_per_tier", 0.0) != 0.0
    ):
        interval = max(1, tier_config.get("tier_interval", 1000))
        remaining = level
        tier = 0
        total = 0.0
        while remaining > 0:
            in_tier = min(remaining, interval)
            per_lv = effect_per_level * math.pow(
                tier_config.get("effect_multiplier_per_tier", 1.0), tier
            ) + (tier_config.get("effect_add_per_tier", 0.0) * tier)
            total += in_tier * per_lv
            remaining -= in_tier
            tier += 1
        effect = total
    else:
        effect = level * effect_per_level

    if max_effect > 0 and effect > max_effect:
        return max_effect
    return effect


def format_num(n):
    """숫자 포맷: 정수면 정수, 소수면 소수점 표시"""
    if isinstance(n, float):
        if n == int(n) and abs(n) < 1e15:
            return str(int(n))
        return f"{n:.4f}".rstrip('0').rstrip('.')
    return str(n)


def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    # Load configs
    with open(os.path.join(CONFIG_DIR, "GameData.json"), "r", encoding="utf-8") as f:
        game_data = json.load(f)

    with open(os.path.join(CONFIG_DIR, "InGameStatGrowth.json"), "r", encoding="utf-8") as f:
        ingame_stats = json.load(f)

    with open(os.path.join(CONFIG_DIR, "PermanentStats.json"), "r", encoding="utf-8") as f:
        perm_stats = json.load(f)

    with open(os.path.join(CONFIG_DIR, "BossDrops.json"), "r", encoding="utf-8") as f:
        boss_drops = json.load(f)

    tier_cfg = game_data["balance"]["tier_hp_system"]
    base_hp = game_data["balance"]["base_hp"]
    boss_mult = game_data["balance"]["boss_hp_multiplier"]
    boss_interval = game_data["balance"]["boss_interval"]

    # ========================================
    # 1. Monster HP Table (1-100)
    # ========================================
    hp_file = os.path.join(OUTPUT_DIR, "monster_hp_table.csv")
    with open(hp_file, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f)

        # Formula header
        w.writerow(["# Monster HP Formulas"])
        w.writerow([f"# base_hp = {base_hp} (config/GameData.json)"])
        w.writerow([f"# tier_interval = {tier_cfg['tier_interval']}"])
        w.writerow([f"# tier_multiplier = {tier_cfg['tier_multiplier']}"])
        w.writerow([f"# linear_growth_per_level = {tier_cfg['linear_growth_per_level']}"])
        w.writerow([f"# decay_per_tier = {tier_cfg.get('tier_multiplier_decay_per_tier', 1.0)}"])
        w.writerow([f"# boss_hp_multiplier = {boss_mult}"])
        w.writerow([f"# boss_interval = {boss_interval}"])
        w.writerow(["#"])
        w.writerow(["# HP = base_hp * tier_multiplier^tier + level_in_tier * linear_growth"])
        w.writerow(["# tier = (level - 1) // tier_interval"])
        w.writerow(["# level_in_tier = (level - 1) % tier_interval"])
        w.writerow(["# Boss HP = Normal HP * boss_hp_multiplier"])
        w.writerow([])

        w.writerow([
            "Level", "Normal_HP", "Boss_HP", "Tier",
            "HP_Delta", "Growth_Rate", "Is_Boss", "Gold_Reward",
            "Boss_Crystal"
        ])

        prev_hp = 0
        base_crystal = boss_drops.get("base_crystal_amount", 10)
        crystal_per_lv = boss_drops.get("crystal_per_level", 5)

        for lv in range(1, MAX_LEVEL + 1):
            hp, tier, growth = calculate_tier_hp(base_hp, lv, tier_cfg)
            boss_hp = int(hp * boss_mult)
            is_boss = (lv % boss_interval == 0)
            delta = hp - prev_hp
            gold = 10 + lv * 2  # base_gold + level * gold_growth (batch_01 average)
            boss_crystal = int(base_crystal + crystal_per_lv * lv) if is_boss else ""

            w.writerow([
                lv, hp, boss_hp, tier, delta,
                f"{growth:.1f}", "Y" if is_boss else "",
                gold, boss_crystal
            ])
            prev_hp = hp
    print(f"[1/3] Monster HP table: {hp_file}")

    # ========================================
    # 2. InGame Stats Table (Keyboard/Mouse)
    # ========================================
    ingame_file = os.path.join(OUTPUT_DIR, "ingame_stats_table.csv")
    kb = ingame_stats["stats"]["keyboard_power"]
    with open(ingame_file, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f)

        # Formula header
        w.writerow(["# InGame Stat Upgrade Formulas (Keyboard/Mouse Power)"])
        w.writerow([f"# base_cost = {kb['base_cost']}"])
        w.writerow([f"# growth_rate = {kb['growth_rate']}"])
        w.writerow([f"# multiplier = {kb['multiplier']}"])
        w.writerow([f"# softcap_interval = {kb['softcap_interval']}"])
        w.writerow([f"# effect_per_level = {kb['effect_per_level']}"])
        w.writerow(["#"])
        w.writerow(["# Cost = base_cost * (1 + level * growth_rate) * multiplier^(level / softcap_interval)"])
        w.writerow(["# Effect = level * effect_per_level"])
        w.writerow(["# Total Damage = 1 (base) + effect"])
        w.writerow([])

        w.writerow([
            "Upgrade_Level", "Cost", "Cumulative_Cost",
            "Effect", "Total_Damage", "DPS_at_5CPS"
        ])

        cum_cost = 0
        for lv in range(1, MAX_LEVEL + 1):
            cost = calculate_cost(
                kb["base_cost"], kb["growth_rate"],
                kb["multiplier"], kb["softcap_interval"], lv
            )
            cum_cost += cost
            effect = calculate_effect(kb["effect_per_level"], lv)
            total_dmg = 1 + int(effect)
            dps = total_dmg * 5.0
            w.writerow([lv, f"{cost:,}", f"{cum_cost:,}", format_num(effect), total_dmg, f"{dps:.0f}"])
    print(f"[2/3] InGame stats table: {ingame_file}")

    # ========================================
    # 3. Permanent Stats Table (All stats)
    # ========================================
    perm_file = os.path.join(OUTPUT_DIR, "permanent_stats_table.csv")
    stat_keys = [
        "base_attack", "attack_percent", "crit_chance", "crit_damage",
        "multi_hit", "time_extend", "upgrade_discount",
        "gold_flat_perm", "gold_multi_perm",
        "crystal_flat", "crystal_chance",
        "start_level", "start_gold", "start_keyboard", "start_mouse"
    ]

    with open(perm_file, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f)

        # Formula header
        w.writerow(["# Permanent Stat Upgrade Formulas (Crystal Currency)"])
        w.writerow(["# Cost = base_cost * (1 + level * growth_rate) * multiplier^(level / softcap_interval)"])
        w.writerow(["# Effect = level * effect_per_level  (capped at max_effect if set)"])
        w.writerow(["# Tier Effect: effect_per_level * effect_multiplier_per_tier^tier  (if tier_config exists)"])
        w.writerow(["#"])

        # Per-stat formula summary
        w.writerow(["# === Per-Stat Parameters ==="])
        for key in stat_keys:
            s = perm_stats["stats"].get(key, {})
            name = s.get("localization", {}).get("ko-KR", {}).get("name", key)
            bc = s.get("base_cost", 1)
            gr = s.get("growth_rate", 0.5)
            ml = s.get("multiplier", 1.5)
            sc = s.get("softcap_interval", 10)
            epl = s.get("effect_per_level", 1)
            maxlv = s.get("max_level", 0)
            maxeff = s.get("max_effect", 0)
            tc = s.get("tier_config")
            emt = tc.get("effect_multiplier_per_tier", 1.0) if tc else 1.0

            parts = [f"# {key} ({name})"]
            parts.append(f"base_cost={bc}")
            parts.append(f"growth={gr}")
            parts.append(f"mult={ml}")
            parts.append(f"softcap={sc}")
            parts.append(f"effect/lv={epl}")
            if maxlv > 0:
                parts.append(f"max_lv={maxlv}")
            if maxeff > 0:
                parts.append(f"max_effect={maxeff}")
            if emt != 1.0:
                parts.append(f"effect_mult_tier={emt}")
            dmg_bonus = s.get("damage_bonus_per_level")
            if dmg_bonus:
                parts.append(f"dmg_bonus/lv={dmg_bonus}%")
            w.writerow([", ".join(parts)])

        w.writerow([])

        # Data header
        header = ["Level"]
        for key in stat_keys:
            header.extend([f"{key}_cost", f"{key}_cum_cost", f"{key}_effect"])
        w.writerow(header)

        cum_costs = {k: 0 for k in stat_keys}
        for lv in range(1, MAX_LEVEL + 1):
            row = [lv]
            for key in stat_keys:
                s = perm_stats["stats"].get(key, {})
                max_lv = s.get("max_level", 0)

                if max_lv > 0 and lv > max_lv:
                    row.extend(["MAX", f"{cum_costs[key]:,}", "MAX"])
                    continue

                cost = calculate_cost(
                    s.get("base_cost", 1),
                    s.get("growth_rate", 0.5),
                    s.get("multiplier", 1.5),
                    s.get("softcap_interval", 10),
                    lv
                )
                cum_costs[key] += cost
                effect = calculate_effect(
                    s.get("effect_per_level", 1),
                    lv,
                    max_effect=s.get("max_effect", 0),
                    tier_config=s.get("tier_config")
                )
                row.extend([f"{cost:,}", f"{cum_costs[key]:,}", format_num(effect)])
            w.writerow(row)
    print(f"[3/3] Permanent stats table: {perm_file}")

    # ========================================
    # Summary
    # ========================================
    print(f"\n=== Balance Summary (Level 1-{MAX_LEVEL}) ===")

    print(f"\nMonster HP (base_hp={base_hp}, tier_mult={tier_cfg['tier_multiplier']}, linear_growth={tier_cfg['linear_growth_per_level']}):")
    for lv in [1, 5, 10, 20, 50, 100]:
        hp, tier, growth = calculate_tier_hp(base_hp, lv, tier_cfg)
        boss_hp = int(hp * boss_mult)
        print(f"  Lv.{lv:>3}: HP={hp:>8,}  BossHP={boss_hp:>10,}  (Tier {tier})")

    print(f"\nKeyboard Power (base_cost={kb['base_cost']}, growth={kb['growth_rate']}, mult={kb['multiplier']}, softcap={kb['softcap_interval']}):")
    for lv in [1, 5, 10, 20, 50, 100]:
        c = calculate_cost(kb["base_cost"], kb["growth_rate"], kb["multiplier"], kb["softcap_interval"], lv)
        cum = sum(calculate_cost(kb["base_cost"], kb["growth_rate"], kb["multiplier"], kb["softcap_interval"], l) for l in range(1, lv + 1))
        eff = calculate_effect(kb["effect_per_level"], lv)
        dmg = 1 + int(eff)
        print(f"  Lv.{lv:>3}: Cost={c:>10,}  CumCost={cum:>12,}  Damage={dmg}")

    print(f"\nPermanent Stat Costs (Crystal) at Lv.10/50/100:")
    for key in stat_keys:
        s = perm_stats["stats"].get(key, {})
        costs = []
        for lv in [10, 50, 100]:
            max_lv = s.get("max_level", 0)
            if max_lv > 0 and lv > max_lv:
                costs.append("MAX")
            else:
                c = calculate_cost(s.get("base_cost", 1), s.get("growth_rate", 0.5), s.get("multiplier", 1.5), s.get("softcap_interval", 10), lv)
                costs.append(f"{c:>10,}")
        effects = []
        for lv in [10, 50, 100]:
            max_lv = s.get("max_level", 0)
            if max_lv > 0 and lv > max_lv:
                effects.append("MAX")
            else:
                e = calculate_effect(s.get("effect_per_level", 1), lv, max_effect=s.get("max_effect", 0), tier_config=s.get("tier_config"))
                effects.append(format_num(e))
        name = s.get("localization", {}).get("ko-KR", {}).get("name", key)
        print(f"  {key:>20} ({name:>10}): Cost@10={costs[0]}  @50={costs[1]}  @100={costs[2]}  | Effect@10={effects[0]}  @50={effects[1]}  @100={effects[2]}")


if __name__ == "__main__":
    main()
