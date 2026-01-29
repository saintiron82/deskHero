"""
밸런스 분석 그래프 데이터 생성
matplotlib을 사용한 시각화
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
    calc_monster_hp,
    calc_boss_hp,
    calc_required_cps,
    calc_combo_multiplier,
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

# ============================================================
# 그래프 데이터 생성
# ============================================================

def generate_cost_curve_data():
    """비용 곡선 그래프 데이터"""
    levels = list(range(1, 51))

    data = {
        'levels': levels,
        'stats': {}
    }

    # 주요 스탯만 선택
    key_stats = {
        'base_attack': '기본 공격력',
        'attack_percent': '공격력 배수',
        'crit_chance': '크리티컬 확률',
        'crit_damage': '크리티컬 배율',
        'time_extend': '시간 연장'
    }

    for stat_id, stat_name in key_stats.items():
        config = permanent_stats['stats'][stat_id]
        costs = []

        for level in levels:
            cost = calc_upgrade_cost(
                config['base_cost'],
                config['growth_rate'],
                config['multiplier'],
                config['softcap_interval'],
                level
            )
            costs.append(cost)

        data['stats'][stat_name] = costs

    # 인게임 스탯
    for stat_id, config in ingame_stats['stats'].items():
        costs = []
        for level in levels:
            cost = calc_upgrade_cost(
                config['base_cost'],
                config['growth_rate'],
                config['multiplier'],
                config['softcap_interval'],
                level
            )
            costs.append(cost)

        data['stats'][config['name']] = costs

    return data

def generate_required_cps_data():
    """필요 CPS 그래프 데이터"""
    levels = list(range(1, 51))
    combo_multi = calc_combo_multiplier(0, MAX_COMBO_STACK)

    data = {
        'levels': levels,
        'normal_monster': [],
        'boss': []
    }

    for level in levels:
        # 파워 (KB + MS)
        power = level
        damage = power * combo_multi

        # 일반 몬스터
        normal_hp = calc_monster_hp(level)
        normal_cps = calc_required_cps(normal_hp, damage, BASE_TIME_LIMIT)
        data['normal_monster'].append(normal_cps)

        # 보스
        boss_hp = calc_boss_hp(level)
        boss_cps = calc_required_cps(boss_hp, damage, BASE_TIME_LIMIT)
        data['boss'].append(boss_cps)

    return data

def generate_efficiency_comparison():
    """효율 비교 데이터"""
    level = 30

    stats = []

    for stat_id, config in permanent_stats['stats'].items():
        # 카테고리 필터 (주요 스탯만)
        if config['category'] not in ['base', 'utility']:
            continue

        total_cost = sum(
            calc_upgrade_cost(
                config['base_cost'],
                config['growth_rate'],
                config['multiplier'],
                config['softcap_interval'],
                lv
            )
            for lv in range(1, level + 1)
        )

        total_effect = config['effect_per_level'] * level
        efficiency = total_effect / total_cost if total_cost > 0 else 0

        stats.append({
            'name': config['name'],
            'cost': total_cost,
            'effect': total_effect,
            'efficiency': efficiency
        })

    # 효율 기준 정렬
    stats.sort(key=lambda x: x['efficiency'], reverse=True)

    return {
        'labels': [s['name'] for s in stats],
        'costs': [s['cost'] for s in stats],
        'effects': [s['effect'] for s in stats],
        'efficiency': [s['efficiency'] for s in stats]
    }

def generate_cps_contribution():
    """업그레이드별 CPS 기여도 데이터"""
    stage = 30
    boss_hp = calc_boss_hp(stage)
    base_power = 60
    combo_multi = calc_combo_multiplier(0, MAX_COMBO_STACK)
    time_limit = BASE_TIME_LIMIT

    scenarios = []

    # 업그레이드 없음
    damage_base = base_power * combo_multi
    cps_base = calc_required_cps(boss_hp, damage_base, time_limit)
    scenarios.append(('업그레이드 없음', cps_base, 0))

    # base_attack +30
    damage = (base_power + 30) * combo_multi
    cps = calc_required_cps(boss_hp, damage, time_limit)
    improvement = (cps_base - cps) / cps_base * 100
    scenarios.append(('base_attack +30', cps, improvement))

    # attack_percent +15%
    damage = base_power * 1.15 * combo_multi
    cps = calc_required_cps(boss_hp, damage, time_limit)
    improvement = (cps_base - cps) / cps_base * 100
    scenarios.append(('attack_percent +15%', cps, improvement))

    # crit_chance +3%
    crit_expected = 1 + 0.13 * (2.0 - 1)
    damage = base_power * crit_expected * combo_multi
    cps = calc_required_cps(boss_hp, damage, time_limit)
    improvement = (cps_base - cps) / cps_base * 100
    scenarios.append(('crit_chance +3%', cps, improvement))

    # crit_damage +3.0
    crit_expected = 1 + 0.1 * (5.0 - 1)
    damage = base_power * crit_expected * combo_multi
    cps = calc_required_cps(boss_hp, damage, time_limit)
    improvement = (cps_base - cps) / cps_base * 100
    scenarios.append(('crit_damage +3.0', cps, improvement))

    # time_extend +3초
    cps = calc_required_cps(boss_hp, damage_base, time_limit + 3)
    improvement = (cps_base - cps) / cps_base * 100
    scenarios.append(('time_extend +3초', cps, improvement))

    # 정렬 (개선율 높은 순)
    scenarios.sort(key=lambda x: x[2], reverse=True)

    return {
        'labels': [s[0] for s in scenarios],
        'required_cps': [s[1] for s in scenarios],
        'improvement': [s[2] for s in scenarios]
    }

# ============================================================
# 메인 실행
# ============================================================

if __name__ == "__main__":
    print("밸런스 그래프 데이터 생성 중...")

    output_dir = ROOT_DIR / "balanceDoc"
    output_dir.mkdir(exist_ok=True)

    # 1. 비용 곡선
    print("1. 비용 곡선 데이터...")
    cost_data = generate_cost_curve_data()
    with open(output_dir / "graph_cost_curves.json", "w", encoding='utf-8') as f:
        json.dump(cost_data, f, indent=2, ensure_ascii=False)

    # 2. 필요 CPS
    print("2. 필요 CPS 데이터...")
    cps_data = generate_required_cps_data()
    with open(output_dir / "graph_required_cps.json", "w", encoding='utf-8') as f:
        json.dump(cps_data, f, indent=2, ensure_ascii=False)

    # 3. 효율 비교
    print("3. 효율 비교 데이터...")
    efficiency_data = generate_efficiency_comparison()
    with open(output_dir / "graph_efficiency.json", "w", encoding='utf-8') as f:
        json.dump(efficiency_data, f, indent=2, ensure_ascii=False)

    # 4. CPS 기여도
    print("4. CPS 기여도 데이터...")
    contribution_data = generate_cps_contribution()
    with open(output_dir / "graph_cps_contribution.json", "w", encoding='utf-8') as f:
        json.dump(contribution_data, f, indent=2, ensure_ascii=False)

    print("\n완료! 생성된 파일:")
    print("  - balanceDoc/graph_cost_curves.json")
    print("  - balanceDoc/graph_required_cps.json")
    print("  - balanceDoc/graph_efficiency.json")
    print("  - balanceDoc/graph_cps_contribution.json")
    print("\n이 JSON 파일들은 웹 대시보드나 Excel에서 시각화할 수 있습니다.")
