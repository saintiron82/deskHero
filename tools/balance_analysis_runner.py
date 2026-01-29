#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
밸런스 분석 실행 및 결과 저장
"""

import json
import sys
from pathlib import Path
from datetime import datetime

# 프로젝트 루트 경로
PROJECT_ROOT = Path(__file__).parent.parent
sys.path.insert(0, str(PROJECT_ROOT / "tools"))

from stat_formulas_generated import (
    calc_upgrade_cost, calc_stat_effect, calc_combo_multiplier,
    calc_required_cps, calc_monster_hp, calc_boss_hp,
    BASE_HP, HP_GROWTH, BOSS_HP_MULTI, BOSS_INTERVAL,
    BASE_TIME_LIMIT, BASE_CRIT_CHANCE, BASE_CRIT_MULTIPLIER,
    MAX_COMBO_STACK
)

# 설정 로드
def load_config(filename):
    with open(PROJECT_ROOT / "config" / filename, 'r', encoding='utf-8') as f:
        return json.load(f)

perm_config = load_config("PermanentStatGrowth.json")
ingame_config = load_config("InGameStatGrowth.json")

# 데미지 계산
def calc_damage(base_power, base_attack=0, attack_percent=0,
                crit_chance=BASE_CRIT_CHANCE, crit_multi=BASE_CRIT_MULTIPLIER,
                multi_hit_chance=0, combo_stack=MAX_COMBO_STACK, combo_damage=0):
    base_dmg = base_power + base_attack
    dmg_with_percent = base_dmg * (1 + attack_percent / 100)
    crit_expected = 1 + crit_chance * (crit_multi - 1)
    multi_expected = 1 + multi_hit_chance / 100
    combo_multi = calc_combo_multiplier(combo_damage, combo_stack)
    return dmg_with_percent * crit_expected * multi_expected * combo_multi

# 인게임 파워
def calc_ingame_power(level):
    return level * 2  # KB + MS

# CPS 판정
def judge_cps(cps):
    if cps < 3:
        return "EASY"
    elif cps < 5:
        return "NORMAL"
    elif cps < 8:
        return "HARD"
    elif cps < 12:
        return "VERY_HARD"
    elif cps < 15:
        return "EXTREME"
    else:
        return "IMPOSSIBLE"

# 스탯 효율 분석
def analyze_stat_efficiency(stat_name, level, stage=30):
    stat_config = perm_config["stats"][stat_name]

    # 총 비용
    total_cost = sum(
        calc_upgrade_cost(
            stat_config["base_cost"],
            stat_config["growth_rate"],
            stat_config["multiplier"],
            stat_config["softcap_interval"],
            lv
        )
        for lv in range(1, level + 1)
    )

    # 효과
    effect = calc_stat_effect(stat_config["effect_per_level"], level)

    # 기본 데미지
    base_power = calc_ingame_power(30)
    damage_baseline = calc_damage(base_power)

    # 스탯 적용 데미지
    if stat_name == "base_attack":
        damage_with_stat = calc_damage(base_power, base_attack=effect)
    elif stat_name == "attack_percent":
        damage_with_stat = calc_damage(base_power, attack_percent=effect)
    elif stat_name == "crit_chance":
        damage_with_stat = calc_damage(base_power, crit_chance=BASE_CRIT_CHANCE + effect/100)
    elif stat_name == "crit_damage":
        damage_with_stat = calc_damage(base_power, crit_multi=BASE_CRIT_MULTIPLIER + effect)
    elif stat_name == "multi_hit":
        damage_with_stat = calc_damage(base_power, multi_hit_chance=effect)
    elif stat_name == "time_extend":
        boss_hp = calc_boss_hp(stage)
        cps_baseline = calc_required_cps(boss_hp, damage_baseline, BASE_TIME_LIMIT)
        cps_with_stat = calc_required_cps(boss_hp, damage_baseline, BASE_TIME_LIMIT + effect)
        return {
            "stat_name": stat_name,
            "level": level,
            "total_cost": total_cost,
            "effect": effect,
            "damage_multiplier": 1.0,
            "cps_reduction_percent": (1 - cps_with_stat / cps_baseline) * 100,
            "efficiency": (1 - cps_with_stat / cps_baseline) / total_cost if total_cost > 0 else 0
        }
    else:
        return None

    # CPS 계산
    boss_hp = calc_boss_hp(stage)
    cps_baseline = calc_required_cps(boss_hp, damage_baseline, BASE_TIME_LIMIT)
    cps_with_stat = calc_required_cps(boss_hp, damage_with_stat, BASE_TIME_LIMIT)

    return {
        "stat_name": stat_name,
        "level": level,
        "total_cost": total_cost,
        "effect": effect,
        "damage_multiplier": damage_with_stat / damage_baseline if damage_baseline > 0 else 0,
        "cps_reduction_percent": (1 - cps_with_stat / cps_baseline) * 100,
        "efficiency": (1 - cps_with_stat / cps_baseline) / total_cost if total_cost > 0 else 0
    }

# 빌드 분석
def analyze_build(build_config, stage=30):
    base_power = calc_ingame_power(30)

    total_base_attack = 0
    total_attack_percent = 0
    total_crit_chance = BASE_CRIT_CHANCE
    total_crit_multi = BASE_CRIT_MULTIPLIER
    total_multi_hit = 0
    total_time_extend = 0
    total_cost = 0

    for stat_name, level in build_config.items():
        if level == 0:
            continue

        stat_config = perm_config["stats"][stat_name]
        effect = calc_stat_effect(stat_config["effect_per_level"], level)

        for lv in range(1, level + 1):
            cost = calc_upgrade_cost(
                stat_config["base_cost"],
                stat_config["growth_rate"],
                stat_config["multiplier"],
                stat_config["softcap_interval"],
                lv
            )
            total_cost += cost

        if stat_name == "base_attack":
            total_base_attack += effect
        elif stat_name == "attack_percent":
            total_attack_percent += effect
        elif stat_name == "crit_chance":
            total_crit_chance += effect / 100
        elif stat_name == "crit_damage":
            total_crit_multi += effect
        elif stat_name == "multi_hit":
            total_multi_hit += effect
        elif stat_name == "time_extend":
            total_time_extend += effect

    damage = calc_damage(
        base_power,
        base_attack=total_base_attack,
        attack_percent=total_attack_percent,
        crit_chance=total_crit_chance,
        crit_multi=total_crit_multi,
        multi_hit_chance=total_multi_hit
    )

    boss_hp = calc_boss_hp(stage)
    time_limit = BASE_TIME_LIMIT + total_time_extend
    cps = calc_required_cps(boss_hp, damage, time_limit)

    return {
        "total_cost": total_cost,
        "damage": damage,
        "required_cps": cps,
        "difficulty": judge_cps(cps)
    }

# 메인 분석
def main():
    results = {
        "timestamp": datetime.now().isoformat(),
        "baseline_difficulty": [],
        "stat_efficiency": [],
        "build_diversity": []
    }

    # 1. 기본 난이도 (업그레이드 없이)
    for level in [1, 10, 20, 30, 40, 50]:
        power = calc_ingame_power(level)
        boss_hp = calc_boss_hp(level)
        damage = calc_damage(power)
        cps = calc_required_cps(boss_hp, damage, BASE_TIME_LIMIT)

        results["baseline_difficulty"].append({
            "level": level,
            "power": power,
            "boss_hp": boss_hp,
            "damage": damage,
            "required_cps": cps,
            "difficulty": judge_cps(cps)
        })

    # 2. 스탯 효율
    combat_stats = ["base_attack", "attack_percent", "crit_chance", "crit_damage", "multi_hit", "time_extend"]
    for stat_name in combat_stats:
        result = analyze_stat_efficiency(stat_name, 20, stage=30)
        if result:
            results["stat_efficiency"].append(result)

    # 효율 정렬
    results["stat_efficiency"].sort(key=lambda x: x["efficiency"], reverse=True)

    # 3. 빌드 다양성
    builds = {
        "all_base_attack": {"base_attack": 50, "attack_percent": 0, "crit_chance": 0, "crit_damage": 0, "multi_hit": 0, "time_extend": 0},
        "all_attack_percent": {"base_attack": 0, "attack_percent": 30, "crit_chance": 0, "crit_damage": 0, "multi_hit": 0, "time_extend": 0},
        "all_crit_damage": {"base_attack": 0, "attack_percent": 0, "crit_chance": 0, "crit_damage": 15, "multi_hit": 0, "time_extend": 0},
        "all_multi_hit": {"base_attack": 0, "attack_percent": 0, "crit_chance": 0, "crit_damage": 0, "multi_hit": 15, "time_extend": 0},
        "all_time_extend": {"base_attack": 0, "attack_percent": 0, "crit_chance": 0, "crit_damage": 0, "multi_hit": 0, "time_extend": 10},
        "balanced": {"base_attack": 15, "attack_percent": 10, "crit_chance": 5, "crit_damage": 5, "multi_hit": 5, "time_extend": 3},
        "crit_focused": {"base_attack": 10, "attack_percent": 5, "crit_chance": 10, "crit_damage": 10, "multi_hit": 0, "time_extend": 0},
        "attack_focused": {"base_attack": 30, "attack_percent": 15, "crit_chance": 0, "crit_damage": 0, "multi_hit": 0, "time_extend": 0},
    }

    for build_name, build_config in builds.items():
        result = analyze_build(build_config, stage=30)
        result["name"] = build_name
        results["build_diversity"].append(result)

    # 빌드 정렬
    results["build_diversity"].sort(key=lambda x: x["required_cps"])

    # 결과 저장
    output_path = PROJECT_ROOT / "balanceDoc" / "analysis_results.json"
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(results, f, indent=2, ensure_ascii=False)

    print(f"Analysis complete. Results saved to: {output_path}")

    return results

if __name__ == "__main__":
    results = main()
