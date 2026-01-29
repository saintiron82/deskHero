# 밸런스 분석 보고서 (테스트 4/5)

- 분석일: 2026-01-28
- Record ID: b2a8638a
- 목적: B 등급 설정 검증 테스트

---

## 결과 요약

| 지표 | 값 | 평가 |
|------|------|------|
| **밸런스 등급** | C | ⚠️ |
| **다양성 점수** | 0.29 | ❌ Low |
| **지배 비율** | 1.06 | ✅ 양호 |
| **최고 레벨** | 1595.5 | - |

---

## 카테고리 사용률

| 카테고리 | 사용률 | 상태 |
|----------|--------|------|
| base_stats | 100% | ✅ Active |
| currency_bonus | 60% | ✅ Active |
| utility | 0% | ❌ Underused |
| starting_bonus | 0% | ❌ Underused |

---

## 상위 5 패턴

1. **ga_optimized** - Lv.1595.5
   - attack_percent: 31%, multi_hit: 25%, base_attack: 17%

2. **82b58aff** - Lv.1503.5
   - attack_percent: 30%, base_attack: 30%, multi_hit: 20%

3. **c213383e** - Lv.1503.5
   - multi_hit: 30%, base_attack: 30%, attack_percent: 20%

4. **eff024b9** - Lv.1498.0
   - base_attack: 40%, attack_percent: 30%, multi_hit: 20%

5. **fee20fb1** - Lv.1497.5
   - multi_hit: 40%, base_attack: 30%, gold_multi_perm: 20%

---

## 스탯 분류

**과다 사용:** base_attack, attack_percent, multi_hit, gold_multi_perm

**과소 사용:** crit_chance, crit_damage, gold_flat_perm, crystal_flat, crystal_multi, time_extend, upgrade_discount, start_level, start_gold, start_keyboard, start_mouse, start_gold_flat, start_gold_multi, start_combo_flex, start_combo_damage

---

## 분석

- utility와 starting_bonus 여전히 미사용
- 다양성 점수가 이전보다 낮음 (0.29)
- 패턴 유사도 감소 (0.73)

---

**작성:** Balance Analyzer
**테스트:** 4/5
