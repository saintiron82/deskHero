# 밸런스 분석 보고서 (테스트 5/5)

- 분석일: 2026-01-28
- Record ID: 76c0190c
- 목적: B 등급 설정 검증 테스트

---

## 결과 요약

| 지표 | 값 | 평가 |
|------|------|------|
| **밸런스 등급** | D | ❌ |
| **다양성 점수** | 0.12 | ❌ Low |
| **지배 비율** | 1.08 | ✅ 양호 |
| **최고 레벨** | 1594.5 | - |

---

## 카테고리 사용률

| 카테고리 | 사용률 | 상태 |
|----------|--------|------|
| base_stats | 100% | ✅ Active |
| currency_bonus | 90% | ✅ Active |
| utility | 0% | ❌ Underused |
| starting_bonus | 0% | ❌ Underused |

---

## 상위 5 패턴

1. **ga_optimized** - Lv.1594.5
   - attack_percent: 38%, multi_hit: 20%, crit_chance: 15%

2. **9cff5f05** - Lv.1482.5
   - attack_percent: 50%, gold_multi_perm: 30%, multi_hit: 20%

3. **d6b599cc** - Lv.1479.0
   - multi_hit: 40%, attack_percent: 30%, gold_multi_perm: 30%

4. **c3899940** - Lv.1478.0
   - gold_multi_perm: 40%, multi_hit: 30%, attack_percent: 30%

5. **8a870fa5** - Lv.1474.0
   - attack_percent: 40%, gold_multi_perm: 40%, multi_hit: 20%

---

## 스탯 분류

**과다 사용:** attack_percent, multi_hit, gold_multi_perm

**과소 사용:** base_attack, crit_damage, gold_flat_perm, crystal_flat, crystal_multi, time_extend, upgrade_discount, start_level, start_gold, start_keyboard, start_mouse, start_gold_flat, start_gold_multi, start_combo_flex, start_combo_damage

---

**작성:** Balance Analyzer
**테스트:** 5/5
