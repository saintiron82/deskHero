"""
DeskWarrior 밸런스 계산기
- 비용 예측
- 성장 곡선 시각화
- 플레이 시뮬레이션
"""

import json
import math
import os
from dataclasses import dataclass
from typing import Dict, List, Optional

# ============================================================
# 데이터 클래스
# ============================================================

@dataclass
class StatConfig:
    """스탯 설정"""
    name: str
    base_cost: float
    growth_rate: float
    multiplier: float
    softcap_interval: int
    effect_per_level: float
    max_level: int = 0

    def calculate_cost(self, level: int) -> int:
        """특정 레벨 업그레이드 비용 계산"""
        if self.max_level > 0 and level > self.max_level:
            return -1  # 최대 레벨 초과

        linear = 1 + level * self.growth_rate
        exponential = self.multiplier ** (level / self.softcap_interval)
        return int(self.base_cost * linear * exponential)

    def calculate_total_cost(self, from_level: int, to_level: int) -> int:
        """from_level에서 to_level까지 총 비용"""
        total = 0
        for lv in range(from_level, to_level):
            cost = self.calculate_cost(lv)
            if cost < 0:
                break
            total += cost
        return total

    def calculate_effect(self, level: int) -> float:
        """특정 레벨에서의 효과"""
        return self.effect_per_level * level


# ============================================================
# 설정 로더
# ============================================================

def load_config(filepath: str) -> Dict[str, StatConfig]:
    """JSON 설정 파일 로드"""
    with open(filepath, 'r', encoding='utf-8') as f:
        data = json.load(f)

    stats = {}
    for stat_id, config in data.get('stats', {}).items():
        if stat_id.startswith('_'):  # 주석 스킵
            continue
        stats[stat_id] = StatConfig(
            name=config.get('name', stat_id),
            base_cost=config.get('base_cost', 100),
            growth_rate=config.get('growth_rate', 0.5),
            multiplier=config.get('multiplier', 1.5),
            softcap_interval=config.get('softcap_interval', 10),
            effect_per_level=config.get('effect_per_level', 1),
            max_level=config.get('max_level', 0)
        )
    return stats


# ============================================================
# 비용 계산기
# ============================================================

def print_cost_table(stat: StatConfig, max_level: int = 50):
    """비용 테이블 출력"""
    print(f"\n{'='*60}")
    print(f" {stat.name} 비용 테이블")
    print(f"{'='*60}")
    print(f" base={stat.base_cost}, growth={stat.growth_rate}, "
          f"multi={stat.multiplier}, interval={stat.softcap_interval}")
    print(f"{'='*60}")
    print(f" {'레벨':>6} | {'업그레이드 비용':>15} | {'누적 비용':>15} | {'효과':>10}")
    print(f"{'-'*60}")

    total = 0
    for lv in range(1, max_level + 1):
        cost = stat.calculate_cost(lv)
        if cost < 0:
            print(f" {'최대 레벨 도달':^56}")
            break
        total += cost
        effect = stat.calculate_effect(lv)
        print(f" {lv:>6} | {cost:>15,} | {total:>15,} | {effect:>10.2f}")

        # 주요 구간만 출력
        if lv > 10 and lv % 10 != 0 and lv < max_level:
            continue


def print_milestone_summary(stat: StatConfig, milestones: List[int] = None):
    """주요 마일스톤 요약"""
    if milestones is None:
        milestones = [1, 5, 10, 20, 30, 50, 100]

    print(f"\n{'='*50}")
    print(f" {stat.name} 마일스톤 요약")
    print(f"{'='*50}")
    print(f" {'목표 레벨':>10} | {'총 비용':>15} | {'효과':>10}")
    print(f"{'-'*50}")

    for target in milestones:
        if stat.max_level > 0 and target > stat.max_level:
            target = stat.max_level
        total_cost = stat.calculate_total_cost(1, target + 1)
        effect = stat.calculate_effect(target)
        print(f" {target:>10} | {total_cost:>15,} | {effect:>10.2f}")
        if stat.max_level > 0 and target >= stat.max_level:
            break


# ============================================================
# 성장 곡선 시각화 (ASCII)
# ============================================================

def print_growth_curve(stat: StatConfig, max_level: int = 50, width: int = 60, height: int = 20):
    """ASCII 그래프로 성장 곡선 시각화"""
    costs = []
    for lv in range(1, max_level + 1):
        cost = stat.calculate_cost(lv)
        if cost < 0:
            break
        costs.append(cost)

    if not costs:
        print("데이터 없음")
        return

    max_cost = max(costs)
    min_cost = min(costs)

    print(f"\n{'='*70}")
    print(f" {stat.name} 성장 곡선 (레벨 vs 비용)")
    print(f"{'='*70}")

    # 그래프 그리기
    for row in range(height, -1, -1):
        threshold = min_cost + (max_cost - min_cost) * row / height
        line = ""
        for i, cost in enumerate(costs):
            if cost >= threshold:
                line += "*"
            else:
                line += " "

        # Y축 레이블
        if row == height:
            label = f"{max_cost:>10,}"
        elif row == 0:
            label = f"{min_cost:>10,}"
        elif row == height // 2:
            mid = (max_cost + min_cost) // 2
            label = f"{mid:>10,}"
        else:
            label = " " * 10

        print(f"{label} |{line}")

    # X축
    print(f"{' '*10} +{'-'*len(costs)}")
    print(f"{' '*10}  1{' '*(len(costs)-2)}{len(costs)}")
    print(f"{' '*15}레벨")


# ============================================================
# 플레이 시뮬레이터
# ============================================================

@dataclass
class PlaySession:
    """플레이 세션 결과"""
    max_level: int
    gold_earned: int
    crystals_earned: int
    upgrades: Dict[str, int]


def simulate_session(
    ingame_stats: Dict[str, StatConfig],
    perm_stats: Dict[str, StatConfig],
    perm_levels: Dict[str, int],
    target_level: int = 20
) -> PlaySession:
    """단일 세션 시뮬레이션"""

    # 영구 스탯 보너스 계산
    start_gold = perm_levels.get('start_gold', 0) * perm_stats.get('start_gold', StatConfig('', 30, 0.3, 1.2, 20, 50)).effect_per_level
    gold_multi_perm = perm_levels.get('gold_multi_perm', 0) * perm_stats.get('gold_multi_perm', StatConfig('', 40, 0.6, 1.5, 10, 3)).effect_per_level / 100

    # 세션 시작
    gold = int(start_gold)
    upgrades = {stat_id: 0 for stat_id in ingame_stats}

    # 레벨별 골드 획득 (간단한 모델)
    for lv in range(1, target_level + 1):
        base_gold = 10 + lv * 2  # 몬스터 기본 골드
        gold += int(base_gold * (1 + gold_multi_perm))

        # 업그레이드 시도
        for stat_id, stat in ingame_stats.items():
            current_lv = upgrades[stat_id]
            cost = stat.calculate_cost(current_lv + 1)
            if cost > 0 and gold >= cost:
                gold -= cost
                upgrades[stat_id] += 1

    # 크리스탈 획득 (보스 처치 기준)
    bosses_killed = target_level // 10
    crystals = bosses_killed * 10  # 간단한 모델

    return PlaySession(
        max_level=target_level,
        gold_earned=gold,
        crystals_earned=crystals,
        upgrades=upgrades
    )


def simulate_multiple_sessions(
    ingame_stats: Dict[str, StatConfig],
    perm_stats: Dict[str, StatConfig],
    num_sessions: int = 10,
    avg_level_per_session: int = 20
):
    """여러 세션 시뮬레이션"""
    print(f"\n{'='*70}")
    print(f" {num_sessions}회 플레이 시뮬레이션")
    print(f"{'='*70}")

    perm_levels = {stat_id: 0 for stat_id in perm_stats}
    total_crystals = 0

    print(f" {'세션':>5} | {'레벨':>6} | {'크리스탈':>10} | {'누적 크리스탈':>12} | 영구 업그레이드")
    print(f"{'-'*70}")

    for session in range(1, num_sessions + 1):
        result = simulate_session(ingame_stats, perm_stats, perm_levels, avg_level_per_session)
        total_crystals += result.crystals_earned

        # 영구 업그레이드 시도 (base_attack 우선)
        upgraded = []
        for stat_id in ['base_attack', 'attack_percent', 'gold_multi_perm']:
            if stat_id not in perm_stats:
                continue
            stat = perm_stats[stat_id]
            current_lv = perm_levels.get(stat_id, 0)
            cost = stat.calculate_cost(current_lv + 1)
            if cost > 0 and total_crystals >= cost:
                total_crystals -= cost
                perm_levels[stat_id] = current_lv + 1
                upgraded.append(f"{stat_id}+1")

        upgrade_str = ", ".join(upgraded) if upgraded else "-"
        print(f" {session:>5} | {result.max_level:>6} | {result.crystals_earned:>10} | {total_crystals:>12,} | {upgrade_str}")


# ============================================================
# 파라미터 비교 도구
# ============================================================

def compare_parameters(stat: StatConfig, new_params: Dict, max_level: int = 30):
    """파라미터 변경 전/후 비교"""

    # 새 설정 생성
    new_stat = StatConfig(
        name=stat.name + " (변경후)",
        base_cost=new_params.get('base_cost', stat.base_cost),
        growth_rate=new_params.get('growth_rate', stat.growth_rate),
        multiplier=new_params.get('multiplier', stat.multiplier),
        softcap_interval=new_params.get('softcap_interval', stat.softcap_interval),
        effect_per_level=new_params.get('effect_per_level', stat.effect_per_level),
        max_level=new_params.get('max_level', stat.max_level)
    )

    print(f"\n{'='*70}")
    print(f" 파라미터 비교: {stat.name}")
    print(f"{'='*70}")
    print(f" {'':>15} | {'기존':>20} | {'변경후':>20}")
    print(f"{'-'*70}")
    print(f" {'base_cost':>15} | {stat.base_cost:>20} | {new_stat.base_cost:>20}")
    print(f" {'growth_rate':>15} | {stat.growth_rate:>20} | {new_stat.growth_rate:>20}")
    print(f" {'multiplier':>15} | {stat.multiplier:>20} | {new_stat.multiplier:>20}")
    print(f" {'softcap_interval':>15} | {stat.softcap_interval:>20} | {new_stat.softcap_interval:>20}")
    print(f"{'='*70}")
    print(f" {'레벨':>6} | {'기존 비용':>15} | {'변경후 비용':>15} | {'차이':>12}")
    print(f"{'-'*70}")

    milestones = [1, 5, 10, 20, 30, 50]
    for lv in milestones:
        if lv > max_level:
            break
        old_total = stat.calculate_total_cost(1, lv + 1)
        new_total = new_stat.calculate_total_cost(1, lv + 1)
        diff = new_total - old_total
        diff_str = f"+{diff:,}" if diff > 0 else f"{diff:,}"
        print(f" {lv:>6} | {old_total:>15,} | {new_total:>15,} | {diff_str:>12}")


# ============================================================
# 메인 인터페이스
# ============================================================

def main():
    """메인 함수"""
    # 설정 파일 경로
    config_dir = os.path.dirname(os.path.abspath(__file__))
    config_dir = os.path.join(os.path.dirname(config_dir), 'config')

    ingame_path = os.path.join(config_dir, 'InGameStatGrowth.json')
    perm_path = os.path.join(config_dir, 'PermanentStatGrowth.json')

    print("\n" + "="*70)
    print(" DeskWarrior 밸런스 계산기")
    print("="*70)

    # 설정 로드
    try:
        ingame_stats = load_config(ingame_path)
        perm_stats = load_config(perm_path)
        print(f" 인게임 스탯 {len(ingame_stats)}종 로드")
        print(f" 영구 스탯 {len(perm_stats)}종 로드")
    except Exception as e:
        print(f" 설정 로드 실패: {e}")
        return

    while True:
        print("\n" + "-"*50)
        print(" 메뉴:")
        print("  1. 비용 테이블 보기")
        print("  2. 마일스톤 요약")
        print("  3. 성장 곡선 그래프")
        print("  4. 플레이 시뮬레이션")
        print("  5. 파라미터 비교")
        print("  6. 전체 스탯 요약")
        print("  0. 종료")
        print("-"*50)

        choice = input(" 선택: ").strip()

        if choice == '0':
            print(" 종료합니다.")
            break

        elif choice == '1':
            print("\n 스탯 선택:")
            all_stats = {**ingame_stats, **perm_stats}
            for i, (stat_id, stat) in enumerate(all_stats.items(), 1):
                print(f"  {i}. {stat.name} ({stat_id})")

            try:
                idx = int(input(" 번호: ")) - 1
                stat_id = list(all_stats.keys())[idx]
                stat = all_stats[stat_id]
                max_lv = int(input(" 최대 레벨 (기본 50): ") or "50")
                print_cost_table(stat, max_lv)
            except (ValueError, IndexError):
                print(" 잘못된 입력")

        elif choice == '2':
            print("\n 스탯 선택:")
            all_stats = {**ingame_stats, **perm_stats}
            for i, (stat_id, stat) in enumerate(all_stats.items(), 1):
                print(f"  {i}. {stat.name} ({stat_id})")

            try:
                idx = int(input(" 번호: ")) - 1
                stat_id = list(all_stats.keys())[idx]
                stat = all_stats[stat_id]
                print_milestone_summary(stat)
            except (ValueError, IndexError):
                print(" 잘못된 입력")

        elif choice == '3':
            print("\n 스탯 선택:")
            all_stats = {**ingame_stats, **perm_stats}
            for i, (stat_id, stat) in enumerate(all_stats.items(), 1):
                print(f"  {i}. {stat.name} ({stat_id})")

            try:
                idx = int(input(" 번호: ")) - 1
                stat_id = list(all_stats.keys())[idx]
                stat = all_stats[stat_id]
                max_lv = int(input(" 최대 레벨 (기본 30): ") or "30")
                print_growth_curve(stat, max_lv)
            except (ValueError, IndexError):
                print(" 잘못된 입력")

        elif choice == '4':
            try:
                num = int(input(" 시뮬레이션 횟수 (기본 10): ") or "10")
                avg_lv = int(input(" 세션당 평균 레벨 (기본 20): ") or "20")
                simulate_multiple_sessions(ingame_stats, perm_stats, num, avg_lv)
            except ValueError:
                print(" 잘못된 입력")

        elif choice == '5':
            print("\n 스탯 선택:")
            all_stats = {**ingame_stats, **perm_stats}
            for i, (stat_id, stat) in enumerate(all_stats.items(), 1):
                print(f"  {i}. {stat.name} ({stat_id})")

            try:
                idx = int(input(" 번호: ")) - 1
                stat_id = list(all_stats.keys())[idx]
                stat = all_stats[stat_id]

                print(f"\n 현재 값: base={stat.base_cost}, growth={stat.growth_rate}, "
                      f"multi={stat.multiplier}, interval={stat.softcap_interval}")
                print(" 새 값 입력 (Enter=유지):")

                new_params = {}
                val = input(f"  base_cost [{stat.base_cost}]: ").strip()
                if val: new_params['base_cost'] = float(val)

                val = input(f"  growth_rate [{stat.growth_rate}]: ").strip()
                if val: new_params['growth_rate'] = float(val)

                val = input(f"  multiplier [{stat.multiplier}]: ").strip()
                if val: new_params['multiplier'] = float(val)

                val = input(f"  softcap_interval [{stat.softcap_interval}]: ").strip()
                if val: new_params['softcap_interval'] = int(val)

                compare_parameters(stat, new_params)
            except (ValueError, IndexError):
                print(" 잘못된 입력")

        elif choice == '6':
            print("\n" + "="*80)
            print(" 전체 스탯 요약 (레벨 10, 30, 50 기준)")
            print("="*80)
            print(f" {'스탯':>20} | {'Lv10 비용':>12} | {'Lv30 비용':>12} | {'Lv50 비용':>12}")
            print("-"*80)

            for stat_id, stat in {**ingame_stats, **perm_stats}.items():
                c10 = stat.calculate_total_cost(1, 11)
                c30 = stat.calculate_total_cost(1, 31)
                c50 = stat.calculate_total_cost(1, 51)
                print(f" {stat.name:>20} | {c10:>12,} | {c30:>12,} | {c50:>12,}")


if __name__ == "__main__":
    main()
