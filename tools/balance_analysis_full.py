"""
DeskWarrior 종합 밸런스 분석 스크립트
- 비용 곡선 분석
- 투자 효율 계산
- CPS 관점 분석
- 골드/크리스탈 경제 분석
"""

import json
import math
from pathlib import Path
from stat_formulas_generated import *

# 경로 설정
ROOT = Path(__file__).parent.parent
PERM_STAT_PATH = ROOT / "config" / "PermanentStatGrowth.json"
INGAME_STAT_PATH = ROOT / "config" / "InGameStatGrowth.json"
FORMULA_PATH = ROOT / "config" / "StatFormulas.json"

# 데이터 로드
with open(PERM_STAT_PATH, encoding='utf-8') as f:
    perm_stats = json.load(f)['stats']
with open(INGAME_STAT_PATH, encoding='utf-8') as f:
    ingame_stats = json.load(f)['stats']
with open(FORMULA_PATH, encoding='utf-8') as f:
    formulas = json.load(f)

# 상수
CONSTANTS = formulas['constants']
BASE_HP = CONSTANTS['BASE_HP']
HP_GROWTH = CONSTANTS['HP_GROWTH']
BOSS_HP_MULTI = CONSTANTS['BOSS_HP_MULTI']
BASE_TIME_LIMIT = CONSTANTS['BASE_TIME_LIMIT']
MAX_COMBO_STACK = CONSTANTS['MAX_COMBO_STACK']

def calc_total_cost(stat_config, level):
    """레벨 1부터 level까지 업그레이드 총 비용"""
    total = 0
    for lv in range(1, level + 1):
        cost = calc_upgrade_cost(
            stat_config['base_cost'],
            stat_config['growth_rate'],
            stat_config['multiplier'],
            stat_config['softcap_interval'],
            lv
        )
        total += cost
    return total

def calc_cps_required(monster_hp, damage, time_limit=BASE_TIME_LIMIT):
    """필요 CPS 계산"""
    return calc_required_cps(monster_hp, damage, time_limit)

def calc_damage_with_stats(base_power, base_attack=0, attack_percent=0,
                           crit_chance=0.1, crit_damage=2.0,
                           multi_hit=0, combo_stack=3, combo_damage=0):
    """스탯을 고려한 데미지 계산"""
    # 크리티컬 기대값
    crit_multi = 1 + crit_chance * (crit_damage - 1)
    # 멀티히트 기대값
    multi_multi = 1 + (multi_hit / 100)
    # 콤보 배율
    combo_multi = calc_combo_multiplier(combo_damage, combo_stack)

    return calc_damage(base_power, base_attack, attack_percent / 100,
                  crit_multi, multi_multi, combo_multi)

def analyze_ingame_costs():
    """인게임 업그레이드 비용 곡선 분석"""
    print("=" * 60)
    print(" 1. 인게임 업그레이드 비용 곡선 (키보드 + 마우스)")
    print("=" * 60)

    kb = ingame_stats['keyboard_power']
    ms = ingame_stats['mouse_power']

    levels = [1, 5, 10, 20, 30, 40, 50]

    for lv in levels:
        kb_cost = calc_upgrade_cost(kb['base_cost'], kb['growth_rate'],
                               kb['multiplier'], kb['softcap_interval'], lv)
        ms_cost = calc_upgrade_cost(ms['base_cost'], ms['growth_rate'],
                               ms['multiplier'], ms['softcap_interval'], lv)
        kb_total = calc_total_cost(kb, lv)
        ms_total = calc_total_cost(ms, lv)

        print(f"Lv {lv:2d}: 다음 업글 비용: KB={kb_cost:5d}G MS={ms_cost:5d}G | "
              f"누적 비용: KB={kb_total:6d}G MS={ms_total:6d}G | "
              f"총 누적: {kb_total + ms_total:7d}G")

def analyze_perm_costs():
    """영구 업그레이드 비용 곡선 분석"""
    print("\n" + "=" * 60)
    print(" 2. 영구 업그레이드 비용 곡선 (주요 스탯)")
    print("=" * 60)

    key_stats = ['base_attack', 'attack_percent', 'crit_chance', 'crit_damage',
                 'multi_hit', 'time_extend']

    levels = [1, 5, 10, 20, 30, 40, 50]

    for stat_name in key_stats:
        stat = perm_stats[stat_name]
        print(f"\n[{stat['name']}] effect_per_level={stat['effect_per_level']}")
        print(f"  Lv | 다음 비용 | 누적 비용 | 총 효과 | 효율(효과/비용)")
        print("-" * 60)
        for lv in levels:
            cost = calc_upgrade_cost(stat['base_cost'], stat['growth_rate'],
                               stat['multiplier'], stat['softcap_interval'], lv)
            total = calc_total_cost(stat, lv)
            effect = calc_stat_effect(stat['effect_per_level'], lv)
            efficiency = effect / total if total > 0 else 0
            print(f"  {lv:2d} | {cost:9.1f}C | {total:9.1f}C | {effect:7.1f} | {efficiency:10.4f}")

def analyze_cps_required():
    """필요 CPS 분석"""
    print("\n" + "=" * 60)
    print(" 3. 필요 CPS 분석 (콤보 3스택, 30초 제한)")
    print("=" * 60)

    stages = [1, 10, 20, 30, 40, 50]

    # 파워 곡선 (인게임만, 영구 0)
    powers = {1: 2, 10: 20, 20: 40, 30: 60, 40: 80, 50: 100}

    print(f"\n{'Stage':<6} | {'몬스터HP':<10} | {'보스HP':<10} | {'파워':<5} | "
          f"{'데미지/입력':<10} | {'필요CPS(일반)':<12} | {'필요CPS(보스)':<12} | {'판정'}")
    print("-" * 120)

    for stage in stages:
        power = powers[stage]
        # 콤보 3스택 데미지
        dmg = calc_damage_with_stats(power, combo_stack=3)

        # 몬스터 HP
        normal_hp = calc_monster_hp(stage)
        boss_hp_val = calc_boss_hp(stage)

        # 필요 CPS
        cps_normal = calc_cps_required(normal_hp, dmg)
        cps_boss = calc_cps_required(boss_hp_val, dmg)

        # 판정
        if cps_boss < 3:
            grade = "[OK] 매우 여유"
        elif cps_boss < 5:
            grade = "[OK] 여유"
        elif cps_boss < 8:
            grade = "[WARN] 도전적"
        elif cps_boss < 12:
            grade = "[WARN] 어려움"
        elif cps_boss < 15:
            grade = "[FAIL] 극한"
        else:
            grade = "[FAIL] 업글필수"

        print(f"{stage:<6} | {normal_hp:>10,} | {boss_hp_val:>10,} | {power:<5} | "
              f"{dmg:>10,} | {cps_normal:>12.2f} | {cps_boss:>12.2f} | {grade}")

def analyze_investment_efficiency():
    """투자 효율 순위 분석"""
    print("\n" + "=" * 60)
    print(" 4. 투자 효율 순위 (Lv1→Lv10 기준)")
    print("=" * 60)

    # 주요 전투 스탯만 분석
    combat_stats = ['base_attack', 'attack_percent', 'crit_chance', 'crit_damage',
                    'multi_hit', 'time_extend']

    results = []

    for stat_name in combat_stats:
        stat = perm_stats[stat_name]

        # Lv10까지 비용
        total_cost = calc_total_cost(stat, 10)

        # Lv10 효과
        effect = calc_stat_effect(stat['effect_per_level'], 10)

        # CPS 감소율 추정
        if stat_name == 'base_attack':
            # 기본공격력 +10 → 데미지 증가
            dmg_before = calc_damage_with_stats(20)  # Lv20 기준
            dmg_after = calc_damage_with_stats(20, base_attack=10)
            cps_reduction = (dmg_after - dmg_before) / dmg_before
        elif stat_name == 'attack_percent':
            # 공격력% +5% (effect=0.5, lv10=5%)
            dmg_before = calc_damage_with_stats(20)
            dmg_after = calc_damage_with_stats(20, attack_percent=effect)
            cps_reduction = (dmg_after - dmg_before) / dmg_before
        elif stat_name == 'crit_chance':
            # 크리티컬 확률 +1% (0.1→0.11)
            dmg_before = calc_damage_with_stats(20, crit_chance=0.1)
            dmg_after = calc_damage_with_stats(20, crit_chance=0.1 + effect / 100)
            cps_reduction = (dmg_after - dmg_before) / dmg_before
        elif stat_name == 'crit_damage':
            # 크리티컬 배율 +1.0 (2.0→3.0)
            dmg_before = calc_damage_with_stats(20, crit_damage=2.0)
            dmg_after = calc_damage_with_stats(20, crit_damage=2.0 + effect)
            cps_reduction = (dmg_after - dmg_before) / dmg_before
        elif stat_name == 'multi_hit':
            # 멀티히트 +1%
            dmg_before = calc_damage_with_stats(20, multi_hit=0)
            dmg_after = calc_damage_with_stats(20, multi_hit=effect)
            cps_reduction = (dmg_after - dmg_before) / dmg_before
        elif stat_name == 'time_extend':
            # 시간 연장 (30초→31초) → 필요 CPS 감소
            cps_reduction = -effect / BASE_TIME_LIMIT
        else:
            cps_reduction = 0

        # 효율: (CPS 감소율) / (투자 비용)
        efficiency = cps_reduction / total_cost if total_cost > 0 else 0

        results.append({
            'name': stat['name'],
            'cost': total_cost,
            'effect': effect,
            'cps_reduction': cps_reduction * 100,
            'efficiency': efficiency * 1000  # 크리스탈 1000개당 CPS 감소율
        })

    # 효율순 정렬
    results.sort(key=lambda x: x['efficiency'], reverse=True)

    print(f"\n{'순위':<4} | {'스탯':<15} | {'Lv10 비용':<12} | {'효과':<10} | "
          f"{'CPS감소율':<12} | {'효율(C1000당)':<15}")
    print("-" * 90)

    for i, r in enumerate(results, 1):
        print(f"{i:<4} | {r['name']:<15} | {r['cost']:>11.1f}C | {r['effect']:>9.1f} | "
              f"{r['cps_reduction']:>10.2f}% | {r['efficiency']:>14.4f}%")

def analyze_gold_economy():
    """골드 경제 분석"""
    print("\n" + "=" * 60)
    print(" 5. 골드 이코노미 분석")
    print("=" * 60)

    stages = [1, 10, 20, 30, 40, 50]

    print(f"\n{'Stage':<6} | {'기본골드':<10} | {'몬스터처치필요':<15} | "
          f"{'Lv도달 누적비용':<15} | {'순수 몬스터수':<15}")
    print("-" * 80)

    for stage in stages:
        gold = calc_base_gold(stage)

        # 해당 레벨까지 KB+MS 업그레이드 비용
        kb = ingame_stats['keyboard_power']
        ms = ingame_stats['mouse_power']
        kb_total = calc_total_cost(kb, stage)
        ms_total = calc_total_cost(ms, stage)
        total_needed = kb_total + ms_total

        # 필요 몬스터 수
        monsters_needed = total_needed / gold if gold > 0 else 0

        print(f"{stage:<6} | {gold:>10.1f}G | {monsters_needed:>14.1f} | "
              f"{total_needed:>14.0f}G | {monsters_needed:>14.1f}")

def analyze_crystal_economy():
    """크리스탈 경제 분석"""
    print("\n" + "=" * 60)
    print(" 6. 크리스탈 이코노미 분석 (보스 드롭 기준)")
    print("=" * 60)

    # 가정: 보스당 1 크리스탈
    crystal_per_boss = 1
    boss_interval = CONSTANTS['BOSS_INTERVAL']

    key_stats = ['base_attack', 'attack_percent', 'crit_chance', 'crit_damage']

    print(f"\n{'스탯':<15} | {'Lv10 비용':<12} | {'필요 보스수':<12} | {'필요 스테이지':<15}")
    print("-" * 60)

    for stat_name in key_stats:
        stat = perm_stats[stat_name]
        cost = calc_total_cost(stat, 10)
        bosses = cost / crystal_per_boss
        stages = bosses * boss_interval

        print(f"{stat['name']:<15} | {cost:>11.1f}C | {bosses:>11.1f} | {stages:>14.1f}")

def analyze_balance_grade():
    """밸런스 품질 평가"""
    print("\n" + "=" * 60)
    print(" 7. 밸런스 품질 평가")
    print("=" * 60)

    # CPS 균형도 분석
    stages = [1, 10, 20, 30, 40, 50]
    powers = {1: 2, 10: 20, 20: 40, 30: 60, 40: 80, 50: 100}

    cps_values = []
    for stage in stages:
        power = powers[stage]
        dmg = calc_damage_with_stats(power, combo_stack=3)
        boss_hp_val = calc_boss_hp(stage)
        cps = calc_cps_required(boss_hp_val, dmg)
        cps_values.append(cps)

    # 변동계수 (CV) 계산
    avg_cps = sum(cps_values) / len(cps_values)
    variance = sum((x - avg_cps) ** 2 for x in cps_values) / len(cps_values)
    std_dev = math.sqrt(variance)
    cv = std_dev / avg_cps if avg_cps > 0 else 0

    print(f"\n필요 CPS 변동계수: {cv:.4f}")

    if cv < 0.5:
        grade = "A (우수)"
    elif cv < 1.0:
        grade = "B (양호)"
    elif cv < 1.5:
        grade = "C (보통)"
    else:
        grade = "D (불균형)"

    print(f"밸런스 등급: {grade}")

    # 초반 진입장벽 확인
    print(f"\n초반 진입성 (Lv1~20):")
    early_cps = [cps_values[i] for i in range(3)]  # Lv1, 10, 20
    if all(cps < 3 for cps in early_cps):
        print("  [OK] 매우 좋음 - 캐주얼 플레이어도 진행 가능")
    elif all(cps < 5 for cps in early_cps):
        print("  [OK] 좋음 - 일반 플레이어 클리어 가능")
    else:
        print("  [WARN] 높음 - 진입장벽 존재")

    # 후반 확장성 확인
    print(f"\n후반 확장성 (Lv40+):")
    late_cps = cps_values[-2:]  # Lv40, 50
    if any(cps > 15 for cps in late_cps):
        print("  [OK] 정상 - 영구 업그레이드 필요 (의도된 설계)")
    else:
        print("  [WARN] 너무 쉬움 - 영구 업그레이드 불필요")

def main():
    print("\n" + "=" * 60)
    print(" DeskWarrior 종합 밸런스 분석 리포트")
    print(" 분석일: 2026-01-27")
    print("=" * 60)

    analyze_ingame_costs()
    analyze_perm_costs()
    analyze_cps_required()
    analyze_investment_efficiency()
    analyze_gold_economy()
    analyze_crystal_economy()
    analyze_balance_grade()

    print("\n" + "=" * 60)
    print(" 분석 완료")
    print("=" * 60)

if __name__ == "__main__":
    main()
