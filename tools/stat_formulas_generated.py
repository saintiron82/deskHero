"""
DeskWarrior 스탯 공식 (자동 생성)
생성일: 2026-01-20 23:30:02
경고: 이 파일을 직접 수정하지 마세요!
      config/StatFormulas.json을 수정 후 generate_stat_code.py 실행
"""

import math

# ============================================================
# 상수
# ============================================================

BASE_CRIT_CHANCE = 0.1
BASE_CRIT_MULTIPLIER = 2.0
BASE_TIME_LIMIT = 30
COMBO_DURATION = 3.0
MAX_COMBO_STACK = 3
GOLD_TO_CRYSTAL_RATE = 1000


# ============================================================
# 공식 함수
# ============================================================

def calc_upgrade_cost(base_cost, growth_rate, multiplier, softcap_interval, level):
    """
    업그레이드 비용
    공식: base_cost * (1 + level * growth_rate) * pow(multiplier, level / softcap_interval)
    """
    return int(base_cost * (1 + level * growth_rate) * math.pow(multiplier, level / softcap_interval))


def calc_stat_effect(effect_per_level, level):
    """
    스탯 효과
    공식: effect_per_level * level
    """
    return effect_per_level * level


def calc_damage(base_power, base_attack, attack_percent, crit_multiplier, multi_hit_multiplier, combo_multiplier):
    """
    데미지 계산
    공식: (base_power + base_attack) * (1 + attack_percent) * crit_multiplier * multi_hit_multiplier * combo_multiplier
    """
    return int((base_power + base_attack) * (1 + attack_percent) * crit_multiplier * multi_hit_multiplier * combo_multiplier)


def calc_gold_earned(base_gold, gold_flat, gold_flat_perm, gold_multi, gold_multi_perm):
    """
    골드 획득
    공식: (base_gold + gold_flat + gold_flat_perm) * (1 + gold_multi + gold_multi_perm)
    """
    return int((base_gold + gold_flat + gold_flat_perm) * (1 + gold_multi + gold_multi_perm))


def calc_combo_multiplier(combo_damage, combo_stack):
    """
    콤보 배율
    공식: (1 + combo_damage / 100) * pow(2, combo_stack)
    """
    return (1 + combo_damage / 100) * math.pow(2, combo_stack)


def calc_time_thief(current_time, bonus_time, base_time):
    """
    시간 도둑
    최대 기본시간의 2배까지만 연장
    공식: min(current_time + bonus_time, base_time * 2)
    """
    return min(current_time + bonus_time, base_time * 2)


def calc_combo_tolerance(combo_flex):
    """
    콤보 허용 오차
    공식: 0.01 + combo_flex * 0.005
    """
    return 0.01 + combo_flex * 0.005


def calc_crystal_drop_chance(base_chance, crystal_multi, max_chance):
    """
    크리스탈 드롭 확률
    공식: min(base_chance + crystal_multi / 100, max_chance)
    """
    return min(base_chance + crystal_multi / 100, max_chance)


def calc_crystal_drop_amount(base_amount, crystal_flat):
    """
    크리스탈 드롭량
    공식: base_amount + crystal_flat
    """
    return int(base_amount + crystal_flat)


def calc_discounted_cost(original_cost, upgrade_discount):
    """
    할인된 비용
    공식: original_cost * (1 - upgrade_discount / 100)
    """
    return int(original_cost * (1 - upgrade_discount / 100))

