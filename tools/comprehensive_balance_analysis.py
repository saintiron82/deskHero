"""
DeskWarrior 종합 밸런스 분석 스크립트
"""

import json
import math
import sys
from pathlib import Path

# 프로젝트 루트 경로
ROOT_DIR = Path(__file__).parent.parent
sys.path.insert(0, str(ROOT_DIR / "tools"))

from stat_formulas_generated import (
    calc_upgrade_cost,
    calc_stat_effect,
    calc_monster_hp,
    calc_boss_hp,
    calc_base_gold,
    calc_combo_multiplier,
    calc_required_cps,
    BASE_TIME_LIMIT,
    MAX_COMBO_STACK
)

# ============================================================
# 데이터 로드
# ============================================================

def load_json(filename):
    with open(ROOT_DIR / "config" / filename, encoding='utf-8') as f:
        return json.load(f)

permanent_stats = load_json("PermanentStatGrowth.json")
ingame_stats = load_json("InGameStatGrowth.json")
game_data = load_json("GameData.json")

# ============================================================
# 1. 비용 곡선 분석
# ============================================================

def analyze_cost_curves():
    print("=" * 80)
    print("1. 비용 곡선 분석 (Lv 1~50)")
    print("=" * 80)

    levels = [1, 5, 10, 20, 30, 40, 50]

    # 영구 스탯
    print("\n[영구 스탯 (크리스탈)]")
    print(f"{'스탯':<20} {'Lv1':<10} {'Lv10':<10} {'Lv20':<10} {'Lv30':<10} {'Lv50':<10} {'총비용(Lv30)':<15}")
    print("-" * 100)

    for stat_id, config in permanent_stats['stats'].items():
        costs = []
        total_cost = 0
        for level in levels:
            cost = calc_upgrade_cost(
                config['base_cost'],
                config['growth_rate'],
                config['multiplier'],
                config['softcap_interval'],
                level
            )
            costs.append(cost)
            if level <= 30:
                total_cost += cost

        print(f"{config['name']:<20} {costs[0]:<10} {costs[2]:<10} {costs[3]:<10} {costs[4]:<10} {costs[6]:<10} {total_cost:<15}")

    # 인게임 스탯
    print("\n[인게임 스탯 (골드)]")
    print(f"{'스탯':<20} {'Lv1':<10} {'Lv10':<10} {'Lv20':<10} {'Lv30':<10} {'Lv50':<10} {'총비용(Lv30)':<15}")
    print("-" * 100)

    for stat_id, config in ingame_stats['stats'].items():
        costs = []
        total_cost = 0
        for level in levels:
            cost = calc_upgrade_cost(
                config['base_cost'],
                config['growth_rate'],
                config['multiplier'],
                config['softcap_interval'],
                level
            )
            costs.append(cost)
            if level <= 30:
                total_cost += cost

        print(f"{config['name']:<20} {costs[0]:<10} {costs[2]:<10} {costs[3]:<10} {costs[4]:<10} {costs[6]:<10} {total_cost:<15}")

# ============================================================
# 2. 효율 분석 (effect_per_level 대비 비용)
# ============================================================

def analyze_efficiency():
    print("\n" + "=" * 80)
    print("2. 효율 분석 (Lv30 기준 - 투자 가치 순위)")
    print("=" * 80)

    efficiency_data = []

    # 영구 스탯 효율 계산
    for stat_id, config in permanent_stats['stats'].items():
        total_cost = sum(
            calc_upgrade_cost(
                config['base_cost'],
                config['growth_rate'],
                config['multiplier'],
                config['softcap_interval'],
                level
            )
            for level in range(1, 31)
        )

        total_effect = calc_stat_effect(config['effect_per_level'], 30)

        # 효율 = 효과 / 비용 (클수록 효율적)
        efficiency = total_effect / total_cost if total_cost > 0 else 0

        efficiency_data.append({
            'name': config['name'],
            'category': config['category'],
            'type': '영구',
            'total_cost': total_cost,
            'total_effect': total_effect,
            'efficiency': efficiency,
            'effect_per_level': config['effect_per_level']
        })

    # 정렬 (효율 높은 순)
    efficiency_data.sort(key=lambda x: x['efficiency'], reverse=True)

    print(f"\n{'순위':<5} {'스탯':<20} {'타입':<8} {'총비용(Lv30)':<15} {'총효과':<12} {'효율':<12} {'판정':<10}")
    print("-" * 90)

    for i, data in enumerate(efficiency_data, 1):
        # 판정 기준
        if data['efficiency'] > 5:
            grade = "[우수]"
        elif data['efficiency'] > 1:
            grade = "[보통]"
        else:
            grade = "[낮음]"

        print(f"{i:<5} {data['name']:<20} {data['type']:<8} {data['total_cost']:<15.0f} "
              f"{data['total_effect']:<12.1f} {data['efficiency']:<12.4f} {grade:<10}")

# ============================================================
# 3. CPS 관점 분석 (필요 CPS 감소 기여도)
# ============================================================

def analyze_cps_contribution():
    print("\n" + "=" * 80)
    print("3. CPS 관점 분석 - 필요 CPS 감소 기여도 (Lv30 보스 기준)")
    print("=" * 80)

    # 기준 상황
    stage = 30
    boss_hp = calc_boss_hp(stage)
    time_limit = BASE_TIME_LIMIT

    # 기본 파워 (인게임 Lv30: KB=30, MS=30 → Total=60)
    base_power = 60
    combo_multi = calc_combo_multiplier(0, MAX_COMBO_STACK)  # 콤보 3스택 = 8배

    print(f"\n기준 상황:")
    print(f"  - 스테이지: {stage}")
    print(f"  - 보스 HP: {boss_hp:,}")
    print(f"  - 기본 파워: {base_power} (KB+MS Lv30)")
    print(f"  - 콤보 배율: {combo_multi:.1f}x (3스택)")
    print(f"  - 제한 시간: {time_limit}초")

    # 업그레이드 시나리오
    scenarios = []

    # 1. 업그레이드 없음
    damage_base = base_power * combo_multi
    cps_base = calc_required_cps(boss_hp, damage_base, time_limit)
    scenarios.append({
        'name': '업그레이드 없음',
        'damage': damage_base,
        'cps': cps_base,
        'improvement': 0
    })

    # 2. base_attack +30 (Lv30)
    base_attack_bonus = 30
    damage_with_base_attack = (base_power + base_attack_bonus) * combo_multi
    cps_with_base_attack = calc_required_cps(boss_hp, damage_with_base_attack, time_limit)
    scenarios.append({
        'name': 'base_attack +30',
        'damage': damage_with_base_attack,
        'cps': cps_with_base_attack,
        'improvement': (cps_base - cps_with_base_attack) / cps_base * 100
    })

    # 3. attack_percent +15% (Lv30)
    attack_percent = 0.15
    damage_with_percent = base_power * (1 + attack_percent) * combo_multi
    cps_with_percent = calc_required_cps(boss_hp, damage_with_percent, time_limit)
    scenarios.append({
        'name': 'attack_percent +15%',
        'damage': damage_with_percent,
        'cps': cps_with_percent,
        'improvement': (cps_base - cps_with_percent) / cps_base * 100
    })

    # 4. crit_chance +3% (Lv30) → 기대값 증가
    crit_chance_bonus = 0.03
    crit_expected = 1 + (0.1 + crit_chance_bonus) * (2.0 - 1)  # 기본 10% + 3% = 13%
    damage_with_crit = base_power * crit_expected * combo_multi
    cps_with_crit = calc_required_cps(boss_hp, damage_with_crit, time_limit)
    scenarios.append({
        'name': 'crit_chance +3%',
        'damage': damage_with_crit,
        'cps': cps_with_crit,
        'improvement': (cps_base - cps_with_crit) / cps_base * 100
    })

    # 5. crit_damage +3.0 (5x 배율)
    crit_damage_bonus = 3.0
    crit_expected_5x = 1 + 0.1 * (2.0 + crit_damage_bonus - 1)
    damage_with_crit_dmg = base_power * crit_expected_5x * combo_multi
    cps_with_crit_dmg = calc_required_cps(boss_hp, damage_with_crit_dmg, time_limit)
    scenarios.append({
        'name': 'crit_damage +3.0 (5x)',
        'damage': damage_with_crit_dmg,
        'cps': cps_with_crit_dmg,
        'improvement': (cps_base - cps_with_crit_dmg) / cps_base * 100
    })

    # 6. multi_hit +3%
    multi_hit_bonus = 0.03
    multi_expected = 1 + multi_hit_bonus
    damage_with_multi = base_power * multi_expected * combo_multi
    cps_with_multi = calc_required_cps(boss_hp, damage_with_multi, time_limit)
    scenarios.append({
        'name': 'multi_hit +3%',
        'damage': damage_with_multi,
        'cps': cps_with_multi,
        'improvement': (cps_base - cps_with_multi) / cps_base * 100
    })

    # 7. time_extend +3초
    time_bonus = 3
    cps_with_time = calc_required_cps(boss_hp, damage_base, time_limit + time_bonus)
    scenarios.append({
        'name': 'time_extend +3초',
        'damage': damage_base,
        'cps': cps_with_time,
        'improvement': (cps_base - cps_with_time) / cps_base * 100
    })

    # 정렬 (개선율 높은 순)
    scenarios.sort(key=lambda x: x['improvement'], reverse=True)

    print(f"\n{'순위':<5} {'업그레이드':<25} {'데미지/입력':<15} {'필요CPS':<12} {'개선율':<12} {'판정':<10}")
    print("-" * 90)

    for i, scenario in enumerate(scenarios, 1):
        # 판정 기준
        if scenario['cps'] < 3:
            grade = "[여유]"
        elif scenario['cps'] < 5:
            grade = "[적정]"
        elif scenario['cps'] < 8:
            grade = "[도전적]"
        elif scenario['cps'] < 12:
            grade = "[어려움]"
        else:
            grade = "[업글필수]"

        improvement_str = f"{scenario['improvement']:.1f}%" if scenario['improvement'] > 0 else "-"

        print(f"{i:<5} {scenario['name']:<25} {scenario['damage']:<15.0f} "
              f"{scenario['cps']:<12.2f} {improvement_str:<12} {grade:<10}")

# ============================================================
# 4. 경제 밸런스 분석
# ============================================================

def analyze_economy():
    print("\n" + "=" * 80)
    print("4. 골드 이코노미 분석")
    print("=" * 80)

    print("\n[레벨별 골드 필요량 vs 수급]")
    print(f"{'레벨':<8} {'KB+MS 누적비용':<18} {'스테이지 골드':<15} {'필요 처치 수':<15} {'판정':<10}")
    print("-" * 80)

    levels = [10, 20, 30, 40, 50]

    for level in levels:
        # KB + MS 누적 비용
        total_cost = sum(
            calc_upgrade_cost(
                ingame_stats['stats']['keyboard_power']['base_cost'],
                ingame_stats['stats']['keyboard_power']['growth_rate'],
                ingame_stats['stats']['keyboard_power']['multiplier'],
                ingame_stats['stats']['keyboard_power']['softcap_interval'],
                lv
            ) * 2  # KB + MS
            for lv in range(1, level + 1)
        )

        # 해당 스테이지 골드 (기본값)
        stage_gold = calc_base_gold(level)

        # 필요 처치 수
        kills_needed = total_cost / stage_gold if stage_gold > 0 else float('inf')

        # 판정
        if kills_needed < 50:
            grade = "[원활]"
        elif kills_needed < 200:
            grade = "[보통]"
        else:
            grade = "[과다]"

        print(f"Lv{level:<6} {total_cost:<18,.0f} {stage_gold:<15,.0f} {kills_needed:<15.1f} {grade:<10}")

    print("\n[크리스탈 환산]")
    crystal_rate = 1000  # 골드 1000 = 크리스탈 1
    print(f"골드 1,000 = 크리스탈 1")
    print(f"Lv30 KB+MS 총비용: {sum(calc_upgrade_cost(ingame_stats['stats']['keyboard_power']['base_cost'], ingame_stats['stats']['keyboard_power']['growth_rate'], ingame_stats['stats']['keyboard_power']['multiplier'], ingame_stats['stats']['keyboard_power']['softcap_interval'], lv) * 2 for lv in range(1, 31)):,.0f} 골드 = {sum(calc_upgrade_cost(ingame_stats['stats']['keyboard_power']['base_cost'], ingame_stats['stats']['keyboard_power']['growth_rate'], ingame_stats['stats']['keyboard_power']['multiplier'], ingame_stats['stats']['keyboard_power']['softcap_interval'], lv) * 2 for lv in range(1, 31)) / crystal_rate:.1f} 크리스탈")

# ============================================================
# 5. 문제점 및 개선 제안
# ============================================================

def suggest_improvements():
    print("\n" + "=" * 80)
    print("5. 밸런스 문제점 및 개선 제안")
    print("=" * 80)

    print("\n[현재 밸런스 상태]")

    # Lv30 보스 필요 CPS 체크
    stage = 30
    boss_hp = calc_boss_hp(stage)
    base_power = 60
    combo_multi = calc_combo_multiplier(0, MAX_COMBO_STACK)
    damage = base_power * combo_multi
    required_cps = calc_required_cps(boss_hp, damage, BASE_TIME_LIMIT)

    print(f"[OK] Lv30 보스 필요 CPS: {required_cps:.2f} (도전적이지만 적정)")

    # Lv50 체크
    stage = 50
    boss_hp = calc_boss_hp(stage)
    base_power = 100
    damage = base_power * combo_multi
    required_cps = calc_required_cps(boss_hp, damage, BASE_TIME_LIMIT)

    print(f"[HIGH] Lv50 보스 필요 CPS: {required_cps:.2f} (영구 업그레이드 필수)")

    print("\n[문제점]")
    print("1. [주의] 후반 골드 수급: Lv40+ 진행 시 필요 처치 수 과다")
    print("2. [주의] 영구 스탯 가성비: 일부 스탯 효율이 낮음 (crit_damage, multi_hit)")
    print("3. [양호] 콤보 의존도: 콤보 3스택(8배)이 핵심이지만 적절함")

    print("\n[개선 제안]")
    print("\n1. 골드 수급 개선 (후반 진행 완화)")
    print("   - 옵션 A: 스테이지 골드 배율 증가 (BASE_GOLD_MULTI: 1.5 → 2.0)")
    print("   - 옵션 B: gold_multi_perm 효과 증가 (effect_per_level: 3 → 5)")
    print("   - 옵션 C: 보스 골드 보너스 추가 (보스 처치 시 5배 골드)")

    print("\n2. 영구 스탯 효율 개선")
    print("   - crit_damage: softcap_interval 5 → 8 (급등 완화)")
    print("   - multi_hit: softcap_interval 5 → 8 (급등 완화)")
    print("   - 또는: effect_per_level 증가로 보상 강화")

    print("\n3. 엔드게임 확장 (Lv50+ 진행 가능하게)")
    print("   - base_attack: effect_per_level 1 → 2")
    print("   - attack_percent: effect_per_level 0.5 → 1.0")
    print("   - 목표: Lv50 보스 필요 CPS를 15 이하로")

    print("\n4. 난이도 조절 옵션 (선택적)")
    print("   - 캐주얼 모드: HP_GROWTH 1.2 → 1.15, BOSS_HP_MULTI 5.0 → 3.0")
    print("   - 하드코어 모드: HP_GROWTH 1.2 → 1.25, TIME_LIMIT 30 → 25")

# ============================================================
# 메인 실행
# ============================================================

if __name__ == "__main__":
    print("\n" + "=" * 80)
    print("DeskWarrior 종합 밸런스 분석 리포트")
    print("=" * 80)
    print(f"분석 기준: balance-knowledge.md 공식 및 상수")
    print(f"핵심 지표: 필요 CPS (Clicks Per Second)")
    print(f"기본 전제: 일반 플레이어 CPS=5, 콤보 3스택 배율=8x")
    print("=" * 80)

    analyze_cost_curves()
    analyze_efficiency()
    analyze_cps_contribution()
    analyze_economy()
    suggest_improvements()

    print("\n" + "=" * 80)
    print("분석 완료")
    print("=" * 80)
