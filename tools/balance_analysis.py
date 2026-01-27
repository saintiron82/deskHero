#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
DeskWarrior 밸런스 종합 분석 스크립트
"""

import json
import math
from pathlib import Path
from typing import Dict, List, Tuple

# 프로젝트 루트 경로
ROOT = Path(__file__).parent.parent
CONFIG_DIR = ROOT / "config"

# 공식 로드
from stat_formulas_generated import (
    calc_upgrade_cost,
    calc_stat_effect,
    calc_damage,
    calc_combo_multiplier,
)


class BalanceAnalyzer:
    """밸런스 분석기"""

    def __init__(self):
        """설정 파일 로드"""
        self.stat_formulas = self._load_json("StatFormulas.json")
        self.perm_stat_growth = self._load_json("PermanentStatGrowth.json")
        self.ingame_stat_growth = self._load_json("InGameStatGrowth.json")
        self.player_levels = self._load_json("PlayerLevels.json")
        self.character_data = self._load_json("CharacterData.json")
        self.game_data = self._load_json("GameData.json")

        # 상수
        self.BASE_CRIT_CHANCE = 0.1
        self.BASE_CRIT_MULTIPLIER = 2.0
        self.MAX_COMBO_STACK = 3

    def _load_json(self, filename: str) -> dict:
        """JSON 파일 로드"""
        with open(CONFIG_DIR / filename, 'r', encoding='utf-8') as f:
            return json.load(f)

    def calculate_monster_hp(self, base_hp: int, hp_growth: int, level: int) -> int:
        """몬스터 HP 계산"""
        return base_hp + (level - 1) * hp_growth

    def calculate_monster_gold(self, base_gold: int, gold_growth: int, level: int) -> int:
        """몬스터 골드 보상 계산"""
        return base_gold + level * gold_growth

    def calculate_player_power(self, perm_stats: dict, ingame_keyboard: int,
                               ingame_mouse: int, combo_stack: int = 0) -> dict:
        """플레이어 파워 계산"""
        # 영구 스탯 효과
        base_attack = perm_stats.get('base_attack', 0)
        attack_percent = perm_stats.get('attack_percent', 0) * 5 / 100  # 5%씩
        crit_chance = self.BASE_CRIT_CHANCE + perm_stats.get('crit_chance', 0) / 100
        crit_damage = self.BASE_CRIT_MULTIPLIER + perm_stats.get('crit_damage', 0) * 0.1
        multi_hit = perm_stats.get('multi_hit', 0) / 100
        combo_damage = perm_stats.get('start_combo_damage', 0) * 5 / 100

        # 키보드/마우스 평균 파워
        avg_power = (ingame_keyboard + ingame_mouse) / 2

        # 데미지 계산 (평균)
        combo_mult = calc_combo_multiplier(combo_damage * 100, combo_stack)

        # 기본 데미지
        base_dmg = calc_damage(
            base_power=avg_power,
            base_attack=base_attack,
            attack_percent=attack_percent,
            crit_multiplier=1.0,  # 크리 없음
            multi_hit_multiplier=1.0,  # 멀티히트 없음
            combo_multiplier=combo_mult
        )

        # 크리티컬 데미지
        crit_dmg = calc_damage(
            base_power=avg_power,
            base_attack=base_attack,
            attack_percent=attack_percent,
            crit_multiplier=crit_damage,
            multi_hit_multiplier=1.0,
            combo_multiplier=combo_mult
        )

        # 멀티히트 데미지
        multi_dmg = crit_dmg * 2

        # 기댓값 데미지
        expected_dmg = (
            base_dmg * (1 - crit_chance) * (1 - multi_hit) +
            crit_dmg * crit_chance * (1 - multi_hit) +
            multi_dmg * multi_hit
        )

        return {
            'base_damage': base_dmg,
            'crit_damage': crit_dmg,
            'multi_damage': multi_dmg,
            'expected_damage': expected_dmg,
            'crit_chance': crit_chance,
            'multi_hit_chance': multi_hit,
            'avg_power': avg_power,
            'base_attack': base_attack,
            'attack_percent': attack_percent,
        }

    def calculate_ingame_upgrade_cost(self, stat_name: str, level: int) -> int:
        """인게임 업그레이드 비용 계산"""
        stat = self.ingame_stat_growth['stats'][stat_name]
        return calc_upgrade_cost(
            base_cost=stat['base_cost'],
            growth_rate=stat['growth_rate'],
            multiplier=stat['multiplier'],
            softcap_interval=stat['softcap_interval'],
            level=level
        )

    def calculate_perm_upgrade_cost(self, stat_name: str, level: int) -> int:
        """영구 업그레이드 비용 계산"""
        stat = self.perm_stat_growth['stats'][stat_name]
        return calc_upgrade_cost(
            base_cost=stat['base_cost'],
            growth_rate=stat['growth_rate'],
            multiplier=stat['multiplier'],
            softcap_interval=stat['softcap_interval'],
            level=level
        )

    def analyze_level_progression(self, max_level: int = 50) -> List[dict]:
        """레벨별 진행 분석"""
        results = []

        # 현재 영구 스탯 (모두 0으로 시작)
        perm_stats = {k: 0 for k in self.perm_stat_growth['stats'].keys()}

        for level in range(1, max_level + 1):
            # 몬스터 선택 (순환)
            monsters = self.character_data['Monsters']
            monster_idx = (level - 1) % len(monsters)
            monster = monsters[monster_idx]

            # 보스 여부 (10레벨마다)
            is_boss = level % 10 == 0
            if is_boss:
                bosses = self.character_data['Bosses']
                boss_idx = ((level // 10) - 1) % len(bosses)
                monster = bosses[boss_idx]

            # 몬스터 HP
            monster_hp = self.calculate_monster_hp(
                monster['BaseHp'],
                monster['HpGrowth'],
                level
            )

            # 골드 보상
            monster_gold = self.calculate_monster_gold(
                monster['BaseGold'],
                monster['GoldGrowth'],
                level
            )

            # 플레이어 파워 (인게임 업그레이드 없이)
            player_power = self.calculate_player_power(
                perm_stats=perm_stats,
                ingame_keyboard=1,  # 기본값
                ingame_mouse=1,
                combo_stack=0
            )

            # Time to Kill (초 단위, 1초당 2회 공격 가정)
            attacks_per_second = 2
            expected_dmg = player_power['expected_damage']
            ttk = monster_hp / (expected_dmg * attacks_per_second) if expected_dmg > 0 else float('inf')

            results.append({
                'level': level,
                'is_boss': is_boss,
                'monster_name': monster['Name'],
                'monster_hp': monster_hp,
                'monster_gold': monster_gold,
                'player_expected_dmg': round(expected_dmg, 2),
                'ttk_seconds': round(ttk, 2),
                'hits_to_kill': math.ceil(monster_hp / expected_dmg) if expected_dmg > 0 else float('inf'),
            })

        return results

    def analyze_upgrade_economy(self) -> dict:
        """업그레이드 경제 분석"""
        # 키보드/마우스 레벨별 비용
        keyboard_costs = []
        mouse_costs = []
        for level in [1, 5, 10, 20, 30, 40, 50]:
            kb_cost = self.calculate_ingame_upgrade_cost('keyboard_power', level)
            ms_cost = self.calculate_ingame_upgrade_cost('mouse_power', level)
            keyboard_costs.append({'level': level, 'cost': kb_cost})
            mouse_costs.append({'level': level, 'cost': ms_cost})

        # 영구 업그레이드 주요 스탯 비용
        perm_costs = {}
        key_stats = ['base_attack', 'attack_percent', 'crit_chance', 'crit_damage']
        for stat_name in key_stats:
            costs = []
            for level in [1, 5, 10, 20, 30, 40, 50]:
                cost = self.calculate_perm_upgrade_cost(stat_name, level)
                costs.append({'level': level, 'cost': cost})
            perm_costs[stat_name] = costs

        return {
            'ingame_keyboard': keyboard_costs,
            'ingame_mouse': mouse_costs,
            'permanent': perm_costs,
        }

    def analyze_combo_system(self) -> dict:
        """콤보 시스템 분석"""
        # 콤보 스택별 배율
        combo_multipliers = []
        for stack in range(0, self.MAX_COMBO_STACK + 1):
            # combo_damage = 0 가정
            mult = calc_combo_multiplier(combo_damage=0, combo_stack=stack)
            combo_multipliers.append({
                'stack': stack,
                'multiplier': mult,
                'damage_increase_pct': round((mult - 1) * 100, 1)
            })

        # combo_damage 보너스 효과
        combo_damage_effects = []
        for combo_dmg_pct in [0, 25, 50, 75, 100]:
            mult_base = calc_combo_multiplier(combo_dmg_pct, 0)
            mult_stack1 = calc_combo_multiplier(combo_dmg_pct, 1)
            mult_stack3 = calc_combo_multiplier(combo_dmg_pct, 3)
            combo_damage_effects.append({
                'combo_damage_pct': combo_dmg_pct,
                'base_mult': round(mult_base, 2),
                'stack1_mult': round(mult_stack1, 2),
                'stack3_mult': round(mult_stack3, 2),
            })

        return {
            'stack_multipliers': combo_multipliers,
            'damage_bonus_effects': combo_damage_effects,
        }

    def identify_balance_issues(self, progression: List[dict]) -> List[dict]:
        """밸런스 이슈 식별"""
        issues = []

        for i, data in enumerate(progression):
            level = data['level']
            ttk = data['ttk_seconds']

            # 이슈 1: TTK가 너무 긴 경우 (30초 제한시간 초과)
            if ttk > 30:
                issues.append({
                    'level': level,
                    'severity': 'CRITICAL',
                    'type': 'TTK_TOO_HIGH',
                    'description': f"레벨 {level}: TTK {ttk:.1f}초 (제한시간 30초 초과)",
                    'value': ttk,
                })
            elif ttk > 20:
                issues.append({
                    'level': level,
                    'severity': 'HIGH',
                    'type': 'TTK_HIGH',
                    'description': f"레벨 {level}: TTK {ttk:.1f}초 (여유시간 10초 미만)",
                    'value': ttk,
                })

            # 이슈 2: TTK가 너무 짧은 경우 (1초 미만)
            if ttk < 1:
                issues.append({
                    'level': level,
                    'severity': 'MEDIUM',
                    'type': 'TTK_TOO_LOW',
                    'description': f"레벨 {level}: TTK {ttk:.1f}초 (너무 쉬움)",
                    'value': ttk,
                })

            # 이슈 3: TTK 급증 (이전 레벨 대비 2배 이상)
            if i > 0:
                prev_ttk = progression[i-1]['ttk_seconds']
                if ttk / prev_ttk > 2.0 and prev_ttk > 0:
                    issues.append({
                        'level': level,
                        'severity': 'HIGH',
                        'type': 'TTK_SPIKE',
                        'description': f"레벨 {level}: TTK 급증 ({prev_ttk:.1f}s → {ttk:.1f}s, {ttk/prev_ttk:.1f}배)",
                        'value': ttk / prev_ttk,
                    })

        return issues


def print_section(title: str, width: int = 80):
    """섹션 헤더 출력"""
    print("\n" + "=" * width)
    print(f" {title}")
    print("=" * width + "\n")


def print_table(headers: List[str], rows: List[List], col_widths: List[int] = None):
    """테이블 출력"""
    if col_widths is None:
        col_widths = [max(len(str(row[i])) for row in [headers] + rows) + 2 for i in range(len(headers))]

    # 헤더
    header_row = "".join(str(headers[i]).ljust(col_widths[i]) for i in range(len(headers)))
    print(header_row)
    print("-" * sum(col_widths))

    # 데이터 행
    for row in rows:
        data_row = "".join(str(row[i]).ljust(col_widths[i]) for i in range(len(row)))
        print(data_row)


def main():
    """메인 분석 실행"""
    print("\n" + "=" * 80)
    print("=" + " " * 78 + "=")
    print("=" + " DeskWarrior 밸런스 종합 분석 리포트".center(78) + "=")
    print("=" + " " * 78 + "=")
    print("=" * 80)

    analyzer = BalanceAnalyzer()

    # 1. 레벨별 진행 분석
    print_section("1. 레벨별 플레이어 파워 vs 몬스터 난이도 곡선")
    progression = analyzer.analyze_level_progression(max_level=50)

    # 샘플 데이터 출력 (1, 5, 10, 20, 30, 40, 50)
    sample_levels = [1, 5, 10, 20, 30, 40, 50]
    sample_data = [p for p in progression if p['level'] in sample_levels]

    headers = ["레벨", "몬스터", "HP", "골드", "플레이어 DPS", "TTK(초)", "타수"]
    rows = []
    for data in sample_data:
        boss_mark = "[BOSS]" if data['is_boss'] else ""
        rows.append([
            f"{data['level']}{boss_mark}",
            data['monster_name'][:8],
            data['monster_hp'],
            data['monster_gold'],
            f"{data['player_expected_dmg']:.1f}",
            f"{data['ttk_seconds']:.1f}",
            data['hits_to_kill'],
        ])

    print_table(headers, rows)

    # 통계 요약
    avg_ttk = sum(p['ttk_seconds'] for p in progression if p['ttk_seconds'] != float('inf')) / len(progression)
    max_ttk = max(p['ttk_seconds'] for p in progression if p['ttk_seconds'] != float('inf'))
    min_ttk = min(p['ttk_seconds'] for p in progression if p['ttk_seconds'] != float('inf'))

    print(f"\n[STATS] TTK 통계:")
    print(f"  - 평균 TTK: {avg_ttk:.2f}초")
    print(f"  - 최대 TTK: {max_ttk:.2f}초 (제한시간: 30초)")
    print(f"  - 최소 TTK: {min_ttk:.2f}초")

    # 2. 업그레이드 경제 분석
    print_section("2. 업그레이드 비용 곡선")
    economy = analyzer.analyze_upgrade_economy()

    print("2-1. 인게임 업그레이드 (골드)")
    headers = ["레벨", "키보드 비용", "마우스 비용", "누적 비용"]
    rows = []
    cumulative = 0
    for kb, ms in zip(economy['ingame_keyboard'], economy['ingame_mouse']):
        cumulative += kb['cost'] + ms['cost']
        rows.append([
            kb['level'],
            f"{kb['cost']:,}",
            f"{ms['cost']:,}",
            f"{cumulative:,}",
        ])
    print_table(headers, rows)

    print("\n2-2. 영구 업그레이드 (크리스탈)")
    for stat_name, costs in economy['permanent'].items():
        print(f"\n{stat_name}:")
        print("  " + "  ".join(f"Lv{c['level']}: {c['cost']}" for c in costs))

    # 3. 콤보 시스템 분석
    print_section("3. 콤보 시스템 배율")
    combo_analysis = analyzer.analyze_combo_system()

    print("3-1. 콤보 스택별 배율")
    headers = ["스택", "배율", "데미지 증가"]
    rows = []
    for data in combo_analysis['stack_multipliers']:
        rows.append([
            f"x{data['stack']}",
            f"{data['multiplier']:.1f}x",
            f"+{data['damage_increase_pct']:.0f}%",
        ])
    print_table(headers, rows)

    print("\n3-2. 콤보 데미지 보너스 효과")
    headers = ["콤보데미지", "기본", "스택1", "스택3"]
    rows = []
    for data in combo_analysis['damage_bonus_effects']:
        rows.append([
            f"{data['combo_damage_pct']}%",
            f"{data['base_mult']:.2f}x",
            f"{data['stack1_mult']:.2f}x",
            f"{data['stack3_mult']:.2f}x",
        ])
    print_table(headers, rows)

    # 4. 밸런스 이슈 식별
    print_section("4. 잠재적 밸런스 이슈")
    issues = analyzer.identify_balance_issues(progression)

    if not issues:
        print("[OK] 발견된 심각한 밸런스 이슈가 없습니다.")
    else:
        # 심각도별 정렬
        issues.sort(key=lambda x: {'CRITICAL': 0, 'HIGH': 1, 'MEDIUM': 2, 'LOW': 3}[x['severity']])

        for issue in issues:
            severity_emoji = {
                'CRITICAL': '[CRITICAL]',
                'HIGH': '[HIGH]',
                'MEDIUM': '[MEDIUM]',
                'LOW': '[INFO]',
            }[issue['severity']]

            print(f"{severity_emoji} [{issue['severity']}] {issue['description']}")

    # 5. 권장사항
    print_section("5. 개선 권장사항")

    recommendations = []

    # 권장사항 생성
    if max_ttk > 30:
        recommendations.append({
            'priority': 'CRITICAL',
            'title': '초기 플레이어 파워 부족',
            'description': '영구 업그레이드 없이는 게임 진행이 불가능합니다.',
            'suggestions': [
                '- base_attack 기본값 상향 (0 → 5)',
                '- 시작 키보드/마우스 파워 증가 (1 → 3)',
                '- 초반 몬스터 HP 하향 (BaseHp -30%)',
            ],
            'confidence': 10,
        })

    if avg_ttk > 15:
        recommendations.append({
            'priority': 'HIGH',
            'title': 'TTK가 전반적으로 높음',
            'description': '평균 TTK가 15초를 초과하여 게임 템포가 느립니다.',
            'suggestions': [
                '- attack_percent 효과 증가 (5% → 7%)',
                '- 크리티컬 기본 확률 증가 (10% → 15%)',
                '- 몬스터 HpGrowth 감소 (5 → 4)',
            ],
            'confidence': 8,
        })

    if not recommendations:
        print("[OK] 현재 밸런스 상태가 양호합니다.")
    else:
        for i, rec in enumerate(recommendations, 1):
            priority_emoji = {
                'CRITICAL': '[CRITICAL]',
                'HIGH': '[HIGH]',
                'MEDIUM': '[MEDIUM]',
                'LOW': '[INFO]',
            }[rec['priority']]

            print(f"\n{i}. {priority_emoji} [{rec['priority']}] {rec['title']}")
            print(f"   신뢰도: {rec['confidence']}/10")
            print(f"   설명: {rec['description']}")
            print(f"   제안:")
            for suggestion in rec['suggestions']:
                print(f"     {suggestion}")

    # 6. 적용 방법 안내
    print_section("6. 밸런스 조정 적용 방법")
    print("""
[HIGH] 중요: 모든 밸런스 조정은 반드시 다음 절차를 따르세요.

[1] config/StatFormulas.json 또는 config/*.json 파일 수정
   - 공식 변경 시: StatFormulas.json
   - 몬스터 스탯: CharacterData.json
   - 성장 곡선: PermanentStatGrowth.json, InGameStatGrowth.json

[2] 코드 생성기 실행 (공식 변경 시만)
   > python tools/generate_stat_code.py

[3] 검증
   > python tools/test_stat_formulas.py
   > python tools/balance_analysis.py

[4] C# 빌드 확인
   > dotnet build

[X] 절대 금지: .Generated.cs 파일 직접 수정
    """)

    print("\n" + "=" * 80)
    print("=" + " 분석 완료".center(78) + "=")
    print("=" * 80 + "\n")


if __name__ == '__main__':
    main()
