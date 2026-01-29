"""
게임 공식 계산 (stat_formulas_generated.py 래핑)

주의: 이 클래스는 자동 생성된 공식 모듈(SF)을 래핑합니다.
공식 변경 시 config/StatFormulas.json 수정 후 코드 생성기 실행.
"""

from .config_loader import SF


class GameFormulas:
    """
    게임 공식 계산 - stat_formulas_generated.py 사용
    """

    # 상수는 생성된 모듈에서 가져옴 (Single Source of Truth)
    BASE_HP = SF.BASE_HP
    HP_GROWTH = SF.HP_GROWTH
    BOSS_INTERVAL = SF.BOSS_INTERVAL
    BOSS_HP_MULTI = SF.BOSS_HP_MULTI
    BASE_GOLD_MULTI = SF.BASE_GOLD_MULTI
    TIME_LIMIT = SF.BASE_TIME_LIMIT
    BASE_CRIT_CHANCE = SF.BASE_CRIT_CHANCE
    BASE_CRIT_MULTI = SF.BASE_CRIT_MULTIPLIER

    @staticmethod
    def monster_hp(stage: int) -> int:
        """스테이지별 몬스터 HP (보스 포함)"""
        if GameFormulas.is_boss(stage):
            return SF.calc_boss_hp(stage)
        return SF.calc_monster_hp(stage)

    @staticmethod
    def is_boss(stage: int) -> bool:
        """보스 스테이지인지"""
        return stage > 0 and stage % SF.BOSS_INTERVAL == 0

    @staticmethod
    def monster_gold(stage: int, gold_flat: int = 0, gold_multi: float = 0) -> int:
        """몬스터 처치 골드"""
        base = SF.calc_base_gold(stage)
        return int((base + gold_flat) * (1 + gold_multi / 100))

    @staticmethod
    def calc_damage(base_power: int, base_attack: int, attack_percent: float,
                    crit_chance: float, crit_multi: float,
                    multi_hit_chance: float, combo_stack: int, combo_damage: float) -> dict:
        """데미지 계산 (상세 정보 포함)"""
        # 기본 데미지
        raw = base_power + base_attack
        after_percent = raw * (1 + attack_percent / 100)

        # 크리티컬 기대값 계산
        total_crit_chance = min(SF.BASE_CRIT_CHANCE + crit_chance / 100, 1.0)
        total_crit_multi = SF.BASE_CRIT_MULTIPLIER + crit_multi
        crit_expected = 1 + total_crit_chance * (total_crit_multi - 1)

        # 멀티히트 기대값
        multi_expected = 1 + multi_hit_chance / 100

        # 콤보 배율 (생성된 공식 사용)
        combo_multi = SF.calc_combo_multiplier(combo_damage, combo_stack)

        # 최종 기대 데미지
        expected = after_percent * crit_expected * multi_expected * combo_multi

        return {
            'raw': raw,
            'after_percent': after_percent,
            'crit_chance': total_crit_chance,
            'crit_multi': total_crit_multi,
            'crit_expected': crit_expected,
            'multi_expected': multi_expected,
            'combo_multi': combo_multi,
            'expected': expected,
            'min': int(after_percent),  # 논크리티컬
            'max': int(after_percent * total_crit_multi * 2 * combo_multi)  # 풀버프
        }

    @staticmethod
    def upgrade_cost(base: float, growth: float, multi: float, softcap: int, level: int) -> int:
        """업그레이드 비용 (생성된 공식 사용)"""
        return SF.calc_upgrade_cost(base, growth, multi, softcap, level)

    @staticmethod
    def total_cost(base: float, growth: float, multi: float, softcap: int,
                   from_lv: int, to_lv: int) -> int:
        """총 업그레이드 비용"""
        return sum(
            SF.calc_upgrade_cost(base, growth, multi, softcap, lv)
            for lv in range(from_lv, to_lv)
        )
