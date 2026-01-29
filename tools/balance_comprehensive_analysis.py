#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
DeskWarrior 종합 밸런스 분석 스크립트
- 스탯별 투자 효율 비교
- 빌드 다양성 분석
- 필요 CPS 기준 밸런스 평가
"""

import json
import math
import sys
import io
from pathlib import Path

# Windows 콘솔 UTF-8 인코딩 설정
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

# 프로젝트 루트 경로 설정
PROJECT_ROOT = Path(__file__).parent.parent
sys.path.insert(0, str(PROJECT_ROOT / "tools"))

from stat_formulas_generated import (
    calc_upgrade_cost as upgrade_cost,
    calc_stat_effect as stat_effect,
    calc_combo_multiplier as combo_multiplier,
    calc_required_cps as required_cps,
    calc_monster_hp,
    calc_boss_hp,
    BASE_HP, HP_GROWTH, BOSS_HP_MULTI, BOSS_INTERVAL,
    BASE_TIME_LIMIT, BASE_CRIT_CHANCE, BASE_CRIT_MULTIPLIER,
    MAX_COMBO_STACK
)

# ===== 설정 로드 =====
def load_config(filename):
    """설정 파일 로드"""
    with open(PROJECT_ROOT / "config" / filename, 'r', encoding='utf-8') as f:
        return json.load(f)

perm_config = load_config("PermanentStatGrowth.json")
ingame_config = load_config("InGameStatGrowth.json")
formulas_config = load_config("StatFormulas.json")

# ===== 상수 =====
CONSTANTS = formulas_config["constants"]
BASE_HP = CONSTANTS["BASE_HP"]
HP_GROWTH = CONSTANTS["HP_GROWTH"]
BOSS_HP_MULTI = CONSTANTS["BOSS_HP_MULTI"]
BOSS_INTERVAL = CONSTANTS["BOSS_INTERVAL"]
BASE_TIME_LIMIT = CONSTANTS["BASE_TIME_LIMIT"]
BASE_CRIT_CHANCE = CONSTANTS["BASE_CRIT_CHANCE"]
BASE_CRIT_MULTIPLIER = CONSTANTS["BASE_CRIT_MULTIPLIER"]
MAX_COMBO_STACK = CONSTANTS["MAX_COMBO_STACK"]

# ===== 몬스터 HP 계산 =====
def get_monster_hp(stage):
    """일반 몬스터 HP"""
    return calc_monster_hp(stage)

def get_boss_hp(stage):
    """보스 몬스터 HP"""
    return calc_boss_hp(stage)

# ===== 인게임 스탯 계산 (골드 기반) =====
def calc_ingame_power(level):
    """인게임 KB+MS 파워 (레벨당 1씩 증가)"""
    kb_power = level
    ms_power = level
    return kb_power + ms_power

def calc_ingame_total_cost(level):
    """인게임 KB+MS를 level까지 올리는 총 비용"""
    params = ingame_config["stats"]["keyboard_power"]
    total = 0
    for lv in range(1, level + 1):
        cost = upgrade_cost(
            params["base_cost"],
            params["growth_rate"],
            params["multiplier"],
            params["softcap_interval"],
            lv
        )
        total += cost * 2  # KB + MS
    return total

# ===== 데미지 계산 =====
def calc_damage(base_power, base_attack=0, attack_percent=0,
                crit_chance=BASE_CRIT_CHANCE, crit_multi=BASE_CRIT_MULTIPLIER,
                multi_hit_chance=0, combo_stack=MAX_COMBO_STACK, combo_damage=0):
    """데미지 계산 (기대값 포함)"""
    # 1. 기본 + 가산 데미지
    base_dmg = base_power + base_attack

    # 2. 배수 데미지
    dmg_with_percent = base_dmg * (1 + attack_percent / 100)

    # 3. 크리티컬 기대값
    crit_expected = 1 + crit_chance * (crit_multi - 1)

    # 4. 멀티히트 기대값
    multi_expected = 1 + multi_hit_chance / 100

    # 5. 콤보 배율
    combo_multi = combo_multiplier(combo_damage, combo_stack)

    # 최종 데미지
    final_dmg = dmg_with_percent * crit_expected * multi_expected * combo_multi

    return final_dmg

# ===== 필요 CPS 계산 =====
def calc_required_cps(monster_hp, damage, time_limit=BASE_TIME_LIMIT):
    """몬스터를 클리어하는데 필요한 초당 클릭 수"""
    if damage <= 0:
        return float('inf')
    return required_cps(monster_hp, damage, time_limit)

# ===== 영구 스탯 투자 효율 분석 =====
def analyze_stat_efficiency(stat_name, level, stage=30):
    """특정 스탯을 level만큼 투자했을 때의 효율 분석"""
    stat_config = perm_config["stats"][stat_name]

    # 총 투자 비용 (크리스탈)
    total_cost = 0
    for lv in range(1, level + 1):
        cost = upgrade_cost(
            stat_config["base_cost"],
            stat_config["growth_rate"],
            stat_config["multiplier"],
            stat_config["softcap_interval"],
            lv
        )
        total_cost += cost

    # 스탯 효과
    effect = stat_effect(stat_config["effect_per_level"], level)

    # 스탯별 데미지 영향 계산 (기본 파워 30 가정)
    base_power = calc_ingame_power(30)  # Lv30 기준 인게임 파워

    damage_baseline = calc_damage(base_power)

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
        # 시간 연장은 데미지가 아닌 시간 효율
        boss_hp = get_boss_hp(stage)
        cps_baseline = calc_required_cps(boss_hp, damage_baseline, BASE_TIME_LIMIT)
        cps_with_stat = calc_required_cps(boss_hp, damage_baseline, BASE_TIME_LIMIT + effect)
        return {
            "stat_name": stat_name,
            "level": level,
            "total_cost": total_cost,
            "effect": effect,
            "effect_desc": f"+{effect:.1f}초",
            "damage_baseline": damage_baseline,
            "damage_with_stat": damage_baseline,
            "damage_increase": 0,
            "damage_multiplier": 1.0,
            "required_cps_baseline": cps_baseline,
            "required_cps_with_stat": cps_with_stat,
            "cps_reduction_percent": (1 - cps_with_stat / cps_baseline) * 100,
            "efficiency": (1 - cps_with_stat / cps_baseline) / total_cost if total_cost > 0 else 0
        }
    else:
        # 기타 스탯은 데미지 영향 없음
        return None

    # 데미지 증가율
    damage_increase = damage_with_stat - damage_baseline
    damage_multiplier = damage_with_stat / damage_baseline if damage_baseline > 0 else 0

    # 필요 CPS 감소
    boss_hp = get_boss_hp(stage)
    cps_baseline = calc_required_cps(boss_hp, damage_baseline)
    cps_with_stat = calc_required_cps(boss_hp, damage_with_stat)
    cps_reduction_percent = (1 - cps_with_stat / cps_baseline) * 100

    # 효율: 크리스탈 1당 필요 CPS 감소율
    efficiency = cps_reduction_percent / total_cost if total_cost > 0 else 0

    return {
        "stat_name": stat_name,
        "level": level,
        "total_cost": total_cost,
        "effect": effect,
        "effect_desc": f"+{effect:.1f}",
        "damage_baseline": damage_baseline,
        "damage_with_stat": damage_with_stat,
        "damage_increase": damage_increase,
        "damage_multiplier": damage_multiplier,
        "required_cps_baseline": cps_baseline,
        "required_cps_with_stat": cps_with_stat,
        "cps_reduction_percent": cps_reduction_percent,
        "efficiency": efficiency
    }

# ===== 빌드 조합 분석 =====
def analyze_build_combination(build_config, stage=30):
    """특정 빌드 조합의 효과 분석

    build_config: {stat_name: level, ...}
    """
    base_power = calc_ingame_power(30)

    # 스탯 누적
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
        effect = stat_effect(stat_config["effect_per_level"], level)

        # 비용 계산
        for lv in range(1, level + 1):
            cost = upgrade_cost(
                stat_config["base_cost"],
                stat_config["growth_rate"],
                stat_config["multiplier"],
                stat_config["softcap_interval"],
                lv
            )
            total_cost += cost

        # 효과 적용
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

    # 데미지 계산
    damage = calc_damage(
        base_power,
        base_attack=total_base_attack,
        attack_percent=total_attack_percent,
        crit_chance=total_crit_chance,
        crit_multi=total_crit_multi,
        multi_hit_chance=total_multi_hit
    )

    # 필요 CPS
    boss_hp = get_boss_hp(stage)
    time_limit = BASE_TIME_LIMIT + total_time_extend
    cps = calc_required_cps(boss_hp, damage, time_limit)

    return {
        "build": build_config,
        "total_cost": total_cost,
        "damage": damage,
        "required_cps": cps,
        "time_limit": time_limit,
        "stats_summary": {
            "base_attack": total_base_attack,
            "attack_percent": total_attack_percent,
            "crit_chance": total_crit_chance * 100,
            "crit_multi": total_crit_multi,
            "multi_hit": total_multi_hit,
            "time_extend": total_time_extend
        }
    }

# ===== CPS 난이도 판정 =====
def judge_cps_difficulty(cps):
    """CPS 기준 난이도 판정"""
    if cps < 3:
        return "✅ 매우 여유 (캐주얼)"
    elif cps < 5:
        return "✅ 여유 (일반)"
    elif cps < 8:
        return "⚠️ 도전적"
    elif cps < 12:
        return "⚠️ 어려움 (업글 권장)"
    elif cps < 15:
        return "❌ 극한 (최고 숙련도)"
    else:
        return "❌ 불가능 (입력 한계 초과)"

# ===== 메인 분석 함수 =====
def main_analysis():
    """종합 밸런스 분석 실행"""
    print("=" * 80)
    print("DeskWarrior 종합 밸런스 분석")
    print("=" * 80)
    print()

    # 1. 레벨별 필요 CPS 테이블 (업그레이드 없이)
    print("## 1. 기본 난이도 분석 (영구 업그레이드 0)")
    print()
    print("| 레벨 | 인게임 파워 | 보스 HP | 데미지/입력 | 필요 CPS | 판정 |")
    print("|------|-------------|---------|-------------|----------|------|")

    for level in [1, 10, 20, 30, 40, 50]:
        power = calc_ingame_power(level)
        boss_hp = get_boss_hp(level)
        damage = calc_damage(power)
        cps = calc_required_cps(boss_hp, damage)
        judgment = judge_cps_difficulty(cps)

        print(f"| {level:>4} | {power:>11} | {boss_hp:>7,} | {damage:>11.0f} | {cps:>8.2f} | {judgment} |")

    print()
    print()

    # 2. 스탯별 투자 효율 비교 (Lv20 투자 기준)
    print("## 2. 스탯별 투자 효율 비교 (각 스탯 Lv20 투자, Stage 30 기준)")
    print()

    combat_stats = ["base_attack", "attack_percent", "crit_chance", "crit_damage", "multi_hit", "time_extend"]
    efficiency_results = []

    for stat_name in combat_stats:
        result = analyze_stat_efficiency(stat_name, 20, stage=30)
        if result:
            efficiency_results.append(result)

    # 효율 정렬 (CPS 감소율/비용)
    efficiency_results.sort(key=lambda x: x["efficiency"], reverse=True)

    print("| 스탯 | 레벨 | 총 비용 | 효과 | 데미지 배율 | CPS 감소율 | 효율 (감소율/비용) |")
    print("|------|------|---------|------|------------|-----------|-------------------|")

    for r in efficiency_results:
        print(f"| {r['stat_name']:20} | {r['level']:>4} | {r['total_cost']:>7.0f} | "
              f"{r['effect_desc']:>8} | {r['damage_multiplier']:>10.2f}x | "
              f"{r['cps_reduction_percent']:>9.2f}% | {r['efficiency']:>17.4f} |")

    print()
    print()

    # 3. 빌드 다양성 분석 (총 비용 100 크리스탈 기준)
    print("## 3. 빌드 다양성 분석 (총 100 크리스탈 투자, Stage 30 기준)")
    print()

    # 다양한 빌드 전략
    builds = {
        "올인: base_attack": {"base_attack": 50, "attack_percent": 0, "crit_chance": 0, "crit_damage": 0, "multi_hit": 0, "time_extend": 0},
        "올인: attack_percent": {"base_attack": 0, "attack_percent": 30, "crit_chance": 0, "crit_damage": 0, "multi_hit": 0, "time_extend": 0},
        "올인: crit_damage": {"base_attack": 0, "attack_percent": 0, "crit_chance": 0, "crit_damage": 15, "multi_hit": 0, "time_extend": 0},
        "올인: multi_hit": {"base_attack": 0, "attack_percent": 0, "crit_chance": 0, "crit_damage": 0, "multi_hit": 15, "time_extend": 0},
        "올인: time_extend": {"base_attack": 0, "attack_percent": 0, "crit_chance": 0, "crit_damage": 0, "multi_hit": 0, "time_extend": 10},
        "균형: 모두 균등": {"base_attack": 15, "attack_percent": 10, "crit_chance": 5, "crit_damage": 5, "multi_hit": 5, "time_extend": 3},
        "크리티컬 특화": {"base_attack": 10, "attack_percent": 5, "crit_chance": 10, "crit_damage": 10, "multi_hit": 0, "time_extend": 0},
        "공격력 특화": {"base_attack": 30, "attack_percent": 15, "crit_chance": 0, "crit_damage": 0, "multi_hit": 0, "time_extend": 0},
    }

    build_results = []
    for build_name, build_config in builds.items():
        result = analyze_build_combination(build_config, stage=30)
        result["name"] = build_name
        build_results.append(result)

    # CPS 순으로 정렬 (낮을수록 좋음)
    build_results.sort(key=lambda x: x["required_cps"])

    print("| 빌드 | 총 비용 | 데미지 | 필요 CPS | 판정 |")
    print("|------|---------|--------|----------|------|")

    for r in build_results:
        judgment = judge_cps_difficulty(r["required_cps"])
        print(f"| {r['name']:20} | {r['total_cost']:>7.0f} | {r['damage']:>6.0f} | {r['required_cps']:>8.2f} | {judgment} |")

    print()
    print()

    # 4. 최적 효율 빌드 찾기
    print("## 4. 극단적 효율 격차 분석")
    print()

    best_build = build_results[0]
    worst_build = build_results[-1]

    efficiency_gap = worst_build["required_cps"] / best_build["required_cps"]

    print(f"최고 효율 빌드: {best_build['name']}")
    print(f"  - 필요 CPS: {best_build['required_cps']:.2f}")
    print(f"  - 총 비용: {best_build['total_cost']:.0f} 크리스탈")
    print(f"  - 데미지: {best_build['damage']:.0f}")
    print()
    print(f"최저 효율 빌드: {worst_build['name']}")
    print(f"  - 필요 CPS: {worst_build['required_cps']:.2f}")
    print(f"  - 총 비용: {worst_build['total_cost']:.0f} 크리스탈")
    print(f"  - 데미지: {worst_build['damage']:.0f}")
    print()
    print(f"**효율 격차: {efficiency_gap:.2f}배**")
    print()

    if efficiency_gap > 10:
        print("⚠️ **심각한 밸런스 문제**: 효율 격차 10배 이상")
        print("   → 특정 스탯 올인이 압도적으로 유리함")
        print("   → 빌드 다양성 부재")
    elif efficiency_gap > 5:
        print("⚠️ **밸런스 조정 권장**: 효율 격차 5배 이상")
        print("   → 일부 스탯이 과도하게 유리하거나 불리함")
    elif efficiency_gap > 3:
        print("⚠️ **경미한 불균형**: 효율 격차 3배 이상")
        print("   → 플레이 스타일에 따라 선택 가능한 수준")
    else:
        print("✅ **양호한 밸런스**: 효율 격차 3배 이하")
        print("   → 다양한 빌드 전략이 유효함")

    print()
    print()

    # 5. 결론 및 권장사항
    print("## 5. 결론 및 권장사항")
    print()

    # 효율 격차 기반 문제 진단
    top3_stats = efficiency_results[:3]
    bottom3_stats = efficiency_results[-3:]

    print("### 투자 효율 상위 3개 스탯:")
    for i, r in enumerate(top3_stats, 1):
        print(f"{i}. {r['stat_name']}: 효율 {r['efficiency']:.4f} (CPS 감소 {r['cps_reduction_percent']:.2f}%)")

    print()
    print("### 투자 효율 하위 3개 스탯:")
    for i, r in enumerate(bottom3_stats, 1):
        print(f"{i}. {r['stat_name']}: 효율 {r['efficiency']:.4f} (CPS 감소 {r['cps_reduction_percent']:.2f}%)")

    print()

    # 격차 분석
    if len(efficiency_results) > 1:
        top_eff = efficiency_results[0]["efficiency"]
        bottom_eff = efficiency_results[-1]["efficiency"]
        stat_efficiency_gap = top_eff / bottom_eff if bottom_eff > 0 else float('inf')

        print(f"### 스탯별 효율 격차: {stat_efficiency_gap:.2f}배")
        print()

        if stat_efficiency_gap > 10:
            print("⚠️ **문제 진단: 극단적 스탯 효율 격차**")
            print("   - 특정 스탯에만 투자하는 것이 압도적으로 유리")
            print("   - 하위 스탯은 투자 가치가 거의 없음")
            print("   - 빌드 다양성 심각하게 저해")
            print()
            print("### 개선 방안:")
            print(f"   1. 상위 스탯({efficiency_results[0]['stat_name']}) 비용 증가")
            print(f"      - multiplier 증가 (현재 → 1.5~1.8)")
            print(f"      - softcap_interval 감소 (더 자주 급등)")
            print(f"   2. 하위 스탯({efficiency_results[-1]['stat_name']}) 효과 증가")
            print(f"      - effect_per_level 2배 증가")
            print(f"      - 또는 비용 감소 (multiplier 1.2~1.3)")
        elif stat_efficiency_gap > 5:
            print("⚠️ **조정 권장: 스탯 효율 불균형**")
            print("   - 일부 스탯이 지나치게 유리/불리")
            print("   - 다양한 빌드가 가능하지만 최적 빌드가 명확함")
            print()
            print("### 개선 방안:")
            print(f"   1. 하위 스탯 비용 조정 또는 효과 증가")
            print(f"   2. 스탯 간 시너지 추가 고려")
        else:
            print("✅ **양호한 밸런스**")
            print("   - 스탯별 효율 격차가 허용 범위 내")
            print("   - 다양한 빌드 전략 가능")

    print()
    print("=" * 80)
    print("분석 완료")
    print("=" * 80)

if __name__ == "__main__":
    main_analysis()
