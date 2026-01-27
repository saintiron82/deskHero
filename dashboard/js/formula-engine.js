/**
 * DeskWarrior 스탯 공식 (자동 생성)
 * 생성일: 2026-01-26 18:57:40
 * 경고: 이 파일을 직접 수정하지 마세요!
 *        config/StatFormulas.json을 수정 후 generate_stat_code.py 실행
 */

const FormulaEngine = {

    // ============================================================
    // 상수
    // ============================================================

    BASE_CRIT_CHANCE: 0.1,
    BASE_CRIT_MULTIPLIER: 2.0,
    BASE_TIME_LIMIT: 30,
    COMBO_DURATION: 3.0,
    MAX_COMBO_STACK: 3,
    GOLD_TO_CRYSTAL_RATE: 1000,
    BASE_HP: 100,
    HP_GROWTH: 1.2,
    BOSS_INTERVAL: 10,
    BOSS_HP_MULTI: 5.0,
    BASE_GOLD_MULTI: 1.5,

    // ============================================================
    // 공식 함수
    // ============================================================

    /**
     * 업그레이드 비용
     * 공식: base_cost * (1 + level * growth_rate) * pow(multiplier, level / softcap_interval)
     */
    calcUpgradeCost(base_cost, growth_rate, multiplier, softcap_interval, level) {
        return Math.floor(base_cost * (1 + level * growth_rate) * Math.pow(multiplier, level / softcap_interval));
    },

    /**
     * 스탯 효과
     * 공식: effect_per_level * level
     */
    calcStatEffect(effect_per_level, level) {
        return effect_per_level * level;
    },

    /**
     * 데미지 계산
     * 공식: (base_power + base_attack) * (1 + attack_percent) * crit_multiplier * multi_hit_multiplier * combo_multiplier
     */
    calcDamage(base_power, base_attack, attack_percent, crit_multiplier, multi_hit_multiplier, combo_multiplier) {
        return Math.floor((base_power + base_attack) * (1 + attack_percent) * crit_multiplier * multi_hit_multiplier * combo_multiplier);
    },

    /**
     * 골드 획득
     * 공식: (base_gold + gold_flat + gold_flat_perm) * (1 + gold_multi + gold_multi_perm)
     */
    calcGoldEarned(base_gold, gold_flat, gold_flat_perm, gold_multi, gold_multi_perm) {
        return Math.floor((base_gold + gold_flat + gold_flat_perm) * (1 + gold_multi + gold_multi_perm));
    },

    /**
     * 콤보 배율
     * 공식: (1 + combo_damage / 100) * pow(2, combo_stack)
     */
    calcComboMultiplier(combo_damage, combo_stack) {
        return (1 + combo_damage / 100) * Math.pow(2, combo_stack);
    },

    /**
     * 시간 도둑
     * 최대 기본시간의 2배까지만 연장
     * 공식: min(current_time + bonus_time, base_time * 2)
     */
    calcTimeThief(current_time, bonus_time, base_time) {
        return Math.min(current_time + bonus_time, base_time * 2);
    },

    /**
     * 콤보 허용 오차
     * 공식: 0.01 + combo_flex * 0.005
     */
    calcComboTolerance(combo_flex) {
        return 0.01 + combo_flex * 0.005;
    },

    /**
     * 크리스탈 드롭 확률
     * 공식: min(base_chance + crystal_multi / 100, max_chance)
     */
    calcCrystalDropChance(base_chance, crystal_multi, max_chance) {
        return Math.min(base_chance + crystal_multi / 100, max_chance);
    },

    /**
     * 크리스탈 드롭량
     * 공식: base_amount + crystal_flat
     */
    calcCrystalDropAmount(base_amount, crystal_flat) {
        return Math.floor(base_amount + crystal_flat);
    },

    /**
     * 할인된 비용
     * 공식: original_cost * (1 - upgrade_discount / 100)
     */
    calcDiscountedCost(original_cost, upgrade_discount) {
        return Math.floor(original_cost * (1 - upgrade_discount / 100));
    },

    /**
     * 몬스터 HP
     * 스테이지별 일반 몬스터 HP
     * 공식: BASE_HP * pow(HP_GROWTH, stage)
     */
    calcMonsterHp(stage) {
        return Math.floor(BASE_HP * Math.pow(HP_GROWTH, stage));
    },

    /**
     * 보스 HP
     * 보스 몬스터 HP (BOSS_INTERVAL 스테이지마다 등장)
     * 공식: BASE_HP * pow(HP_GROWTH, stage) * BOSS_HP_MULTI
     */
    calcBossHp(stage) {
        return Math.floor(BASE_HP * Math.pow(HP_GROWTH, stage) * BOSS_HP_MULTI);
    },

    /**
     * 기본 골드
     * 스테이지별 기본 골드 획득량
     * 공식: stage * BASE_GOLD_MULTI
     */
    calcBaseGold(stage) {
        return Math.floor(stage * BASE_GOLD_MULTI);
    },

    /**
     * 필요 CPS
     * 해당 스테이지 클리어에 필요한 초당 클릭 수
     * 공식: monster_hp / damage / time_limit
     */
    calcRequiredCps(monster_hp, damage, time_limit) {
        return monster_hp / damage / time_limit;
    },

    // ============================================================
    // 헬퍼 함수
    // ============================================================

    /**
     * 누적 비용 계산 (fromLevel ~ toLevel)
     */
    calcTotalCost(baseCost, growthRate, multiplier, softcapInterval, fromLevel, toLevel) {
        let total = 0;
        for (let lv = fromLevel; lv < toLevel; lv++) {
            total += this.calcUpgradeCost(baseCost, growthRate, multiplier, softcapInterval, lv);
        }
        return total;
    },

    /**
     * 데미지 단계별 분해
     */
    calcDamageSteps(basePower, baseAttack, attackPercent, critMultiplier, multiHitMultiplier, comboMultiplier) {
        const step1 = basePower;
        const step2 = basePower + baseAttack;
        const step3 = step2 * (1 + attackPercent);
        const step4 = step3 * critMultiplier;
        const step5 = step4 * multiHitMultiplier;
        const step6 = step5 * comboMultiplier;

        return [
            { name: "기본", formula: "base_power", value: Math.floor(step1) },
            { name: "가산", formula: `${Math.floor(step1)} + ${baseAttack}`, value: Math.floor(step2) },
            { name: "배수", formula: `${Math.floor(step2)} × (1 + ${(attackPercent * 100).toFixed(0)}%)`, value: Math.floor(step3) },
            { name: "크리티컬", formula: `${Math.floor(step3)} × ${critMultiplier}`, value: Math.floor(step4) },
            { name: "멀티히트", formula: `${Math.floor(step4)} × ${multiHitMultiplier}`, value: Math.floor(step5) },
            { name: "콤보", formula: `${Math.floor(step5)} × ${comboMultiplier.toFixed(2)}`, value: Math.floor(step6) }
        ];
    },

    /**
     * 기대 DPS 계산 (확률 가중 평균)
     */
    calcExpectedDamage(basePower, baseAttack, attackPercent, critChance, critDamage, multiHitChance) {
        const baseDamage = (basePower + baseAttack) * (1 + attackPercent);
        const avgCritMult = 1 + critChance * (critDamage - 1);
        const avgMultiMult = 1 + multiHitChance;
        return Math.floor(baseDamage * avgCritMult * avgMultiMult);
    }
};

// Export for use in other modules
window.FormulaEngine = FormulaEngine;