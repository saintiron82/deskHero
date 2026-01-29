# 밸런스 분석 결과 해설
**리포트 ID:** 327c313b
**분석 일시:** 2026-01-28
**분석 조건:** CPS 5, 크리스털 500, 목표 레벨 50

---

## 1. 한눈에 보는 결과

```
┌─────────────────────────────────────────────────────────┐
│  밸런스 등급: C (보통)                                    │
│  ─────────────────────────────────────────────────────  │
│  ✅ 지배 비율 1.11 - 단일 압도 전략 없음                   │
│  ⚠️ 다양성 0.30 - 상위 빌드들이 비슷한 스탯 사용           │
│  ❌ 2개 카테고리 완전 미사용 (utility, starting_bonus)    │
└─────────────────────────────────────────────────────────┘
```

**결론:** 심각한 밸런스 실패는 아니지만, 개선이 필요한 상태입니다.

---

## 2. 발견된 문제점

### 문제 1: attack_percent 독주 🔴

| 지표 | 값 | 의미 |
|------|-----|------|
| 단일 효율 순위 | 1위 | 이 스탯만 올려도 최고 효율 |
| 단일 투자 시 레벨 | 1492 | 2위보다 270 레벨 높음 |
| 상위 빌드 사용률 | 100% | 모든 좋은 빌드가 이 스탯 포함 |

**문제의 본질:**
- 플레이어가 "attack_percent만 올리면 됨"이라고 느낌
- 다른 스탯 선택의 의미가 없어짐
- 게임의 전략적 깊이 감소

**현재 상위 빌드 분석:**
```
1위: attack_percent 38% + base_attack 19% + crit_chance 18% + ...
2위: attack_percent 70% + gold_multi_perm 30%
3위: attack_percent 70% + gold_multi_perm 30%
4위: attack_percent 80% + gold_multi_perm 20%
5위: attack_percent 70% + gold_multi_perm 20% + crystal_multi 10%
```

모든 상위 빌드에 attack_percent가 38~80% 비중으로 포함되어 있습니다.

---

### 문제 2: utility 카테고리 전멸 🔴

| 스탯 | 효율 순위 | 상위 빌드 사용률 |
|------|-----------|------------------|
| time_extend | 6위 | 0% |
| upgrade_discount | 18위 | 0% |

**문제의 본질:**
- time_extend가 효율 6위인데도 아무도 안 씀
- 시간 연장보다 직접 공격력이 더 효과적
- "시간 벌기" 전략이 존재하지 않음

**원인 분석:**
```
time_extend 레벨 1 효과: +1초
attack_percent 레벨 1 효과: +?% 공격력

→ 1초 더 버티는 것보다 공격력 올려서 빨리 끝내는 게 나음
```

---

### 문제 3: starting_bonus 카테고리 전멸 🔴

| 스탯 | 효율 순위 | 역할 |
|------|-----------|------|
| start_level | 16위 | 시작 레벨 보너스 |
| start_gold | 17위 | 시작 골드 보너스 |
| start_keyboard | 19위 | 시작 키보드 레벨 |
| start_mouse | 14위 | 시작 마우스 레벨 |
| ... | ... | ... |

**문제의 본질:**
- 8개 스탯이 모두 쓸모없음
- "빠른 시작" 전략이 존재하지 않음
- 이 카테고리 전체가 함정

---

### 문제 4: crit_damage 최하위 🟡

| 지표 | 값 |
|------|-----|
| 효율 순위 | 15위 (D등급) |
| 같은 카테고리 내 순위 | base_stats 5개 중 5위 |

**원인 분석:**
```
크리티컬 데미지의 가치 = 크리티컬 확률 × 추가 데미지

크리티컬 확률이 낮으면 → 크리티컬 데미지 효과 미미
크리티컬 확률이 높아야 → 크리티컬 데미지 투자 의미 있음
```

현재 crit_chance를 많이 올려도 crit_damage 효율이 낮다면,
crit_damage 자체의 효과가 너무 약한 것입니다.

---

## 3. 권장 조치사항

### 🔴 우선순위: 높음

#### 조치 1: attack_percent 너프

**목표:** 다른 공격 스탯과의 격차 줄이기

**방법 A: 비용 증가**
```json
// config/PermanentStats.json
"attack_percent": {
  "base_cost": 10 → 15,      // 50% 증가
  "growth_rate": 0.5 → 0.6   // 고레벨 비용 더 빠르게 증가
}
```

**방법 B: 효과 감소**
```json
"attack_percent": {
  "effect_per_level": 5 → 3  // 레벨당 효과 40% 감소
}
```

**예상 결과:** attack_percent와 base_attack/crit_chance 간 선택이 의미 있어짐

---

#### 조치 2: utility 카테고리 버프

**목표:** "시간 벌기" 전략을 유효하게 만들기

**time_extend 버프:**
```json
"time_extend": {
  "base_cost": 5 → 3,           // 비용 40% 감소
  "effect_per_level": 1.0 → 2.0 // 효과 2배
}
```

**upgrade_discount 버프:**
```json
"upgrade_discount": {
  "effect_per_level": 1 → 3     // 효과 3배
}
```

**예상 결과:** "오래 버티며 업그레이드" 전략이 생김

---

### 🟡 우선순위: 중간

#### 조치 3: starting_bonus 카테고리 리워크

**목표:** "빠른 시작" 전략을 유효하게 만들기

**start_level 대폭 버프:**
```json
"start_level": {
  "effect_per_level": 1 → 3,    // 레벨당 시작 레벨 +3
  "base_cost": 10 → 5           // 비용 절반
}
```

**start_gold 대폭 버프:**
```json
"start_gold": {
  "effect_per_level": 10 → 50   // 레벨당 시작 골드 +50
}
```

**예상 결과:** 초반을 스킵하고 중반부터 시작하는 전략이 생김

---

#### 조치 4: crit_damage 시너지 개선

**목표:** crit_chance와 함께 사용 시 폭발적 효과

**방법:**
```json
"crit_damage": {
  "effect_per_level": 10 → 20   // 레벨당 크뎀 2배
}
```

또는 게임 로직에서 crit_chance가 높을수록 crit_damage 효과 증폭

---

## 4. 조정 후 목표 상태

### 이상적인 분석 결과

```
밸런스 등급: A 또는 B

상위 5개 빌드:
1위: 공격력 특화 빌드 (attack + crit)
2위: 골드 파밍 빌드 (gold + crystal)
3위: 생존 빌드 (time_extend + utility)     ← 새로 등장
4위: 빠른 시작 빌드 (starting_bonus)       ← 새로 등장
5위: 하이브리드 빌드

카테고리 사용률:
- base_stats: 60~80%
- currency_bonus: 40~60%
- utility: 20~40%           ← 0%에서 상승
- starting_bonus: 20~40%    ← 0%에서 상승
```

### 검증 방법

```bash
# 조정 전 저장
simulate --analyze --target 50 --cps 5 --crystals 500 \
  --output balanceDoc/2026-01-28_before

# PermanentStats.json 수정

# 조정 후 분석
simulate --analyze --target 50 --cps 5 --crystals 500 \
  --output balanceDoc/2026-01-28_after

# 비교
# - 등급이 C → B 이상으로 상승했는가?
# - 다양성 점수가 0.30 → 0.50 이상으로 상승했는가?
# - utility/starting_bonus 사용률이 0% → 20% 이상으로 상승했는가?
```

---

## 5. 요약: 액션 아이템 체크리스트

| 우선순위 | 항목 | 대상 | 액션 |
|----------|------|------|------|
| 🔴 높음 | attack_percent 독주 | attack_percent | 비용 ↑ 또는 효과 ↓ |
| 🔴 높음 | utility 미사용 | time_extend | 비용 ↓ + 효과 ↑ |
| 🔴 높음 | utility 미사용 | upgrade_discount | 효과 ↑ |
| 🟡 중간 | starting_bonus 미사용 | start_level | 대폭 버프 |
| 🟡 중간 | starting_bonus 미사용 | start_gold | 대폭 버프 |
| 🟡 중간 | crit_damage 최하위 | crit_damage | 효과 ↑ |
| 🟢 낮음 | 기타 D-tier 스탯 | start_keyboard 등 | 개별 검토 |

---

*이 문서는 report_2026-01-28_full.md 분석 결과를 바탕으로 작성되었습니다.*
