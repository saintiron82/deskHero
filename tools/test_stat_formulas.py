"""
DeskWarrior 스탯 공식 검증 테스트
생성일: 2026-01-29 11:12:19
"""

from stat_formulas_generated import *

def test_upgrade_cost():
    """업그레이드 비용 테스트"""
    # 레벨 1, base=100, growth=0.5, multi=1.5, interval=10
    result = calc_upgrade_cost(100, 0.5, 1.5, 10, 1)
    expected = 150  # 100 * (1 + 1*0.5) * 1.5^(1/10) = 100 * 1.5 * 1.04 = 156
    print(f"upgrade_cost(lv1): {result} (expected ~150)")
    
    # 레벨 10
    result = calc_upgrade_cost(100, 0.5, 1.5, 10, 10)
    print(f"upgrade_cost(lv10): {result}")
    
    # 레벨 50
    result = calc_upgrade_cost(100, 0.5, 1.5, 10, 50)
    print(f"upgrade_cost(lv50): {result}")


def test_damage():
    """데미지 계산 테스트"""
    # base=10, base_attack=5, attack_percent=0.2, crit=1, multi=1, combo=1
    result = calc_damage(10, 5, 0.2, 1.0, 1.0, 1.0)
    expected = 18  # (10+5) * 1.2 * 1 * 1 * 1 = 18
    print(f"damage(no crit): {result} (expected {expected})")
    
    # 크리티컬 발동 시
    result = calc_damage(10, 5, 0.2, 2.0, 1.0, 1.0)
    expected = 36  # 18 * 2 = 36
    print(f"damage(crit): {result} (expected {expected})")
    
    # 콤보 스택 3
    combo_mult = calc_combo_multiplier(0, 3)  # 0% bonus, stack 3 = 8x
    result = calc_damage(10, 5, 0.2, 1.0, 1.0, combo_mult)
    expected = 144  # 18 * 8 = 144
    print(f"damage(combo3): {result} (expected {expected})")


def test_combo():
    """콤보 시스템 테스트"""
    # 콤보 배율
    for stack in range(4):
        mult = calc_combo_multiplier(0, stack)
        print(f"combo stack {stack}: x{mult}")
    
    # 콤보 허용 오차
    for flex in range(0, 21, 5):
        tolerance = calc_combo_tolerance(flex)
        print(f"combo_flex {flex}: +/-{tolerance:.3f}s")


def test_gold():
    """골드 획득 테스트"""
    # 기본
    result = calc_gold_earned(100, 0, 0, 0, 0)
    print(f"gold(base): {result}")
    
    # 보너스 적용
    result = calc_gold_earned(100, 10, 5, 0.2, 0.1)
    expected = int((100 + 10 + 5) * 1.3)  # 115 * 1.3 = 149.5
    print(f"gold(bonus): {result} (expected {expected})")


if __name__ == "__main__":
    print("=" * 50)
    print(" 스탯 공식 검증 테스트")
    print("=" * 50)
    print()
    
    test_upgrade_cost()
    print()
    test_damage()
    print()
    test_combo()
    print()
    test_gold()
    
    print()
    print("=" * 50)
    print(" 테스트 완료")
    print("=" * 50)