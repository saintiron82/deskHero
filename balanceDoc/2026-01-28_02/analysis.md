# 밸런스 분석 결과 해설

**리포트 ID:** a299442c
**분석 일시:** 2026-01-28 20:07
**분석 조건:** CPS 5, 크리스털 500, 목표 레벨 50

---

## 1. 한눈에 보는 결과

```
┌─────────────────────────────────────────────────────────┐
│  밸런스 등급: C (보통)                                    │
│  ─────────────────────────────────────────────────────  │
│  ✅ 지배 비율 1.03 - 단일 압도 전략 없음                   │
│  ⚠️ 다양성 0.42 - 상위 빌드들이 비슷한 스탯 사용           │
│  ❌ 3개 카테고리 완전 미사용 (currency, utility, start)   │
└─────────────────────────────────────────────────────────┘
```

**결론:** 심각한 밸런스 실패는 아니지만, 게임 깊이가 부족한 상태입니다.

---

## 2. 발견된 문제점

### 문제 1: attack_percent 독주 체제 유지 🔴

| 지표 | 값 | 의미 |
|------|-----|------|
| 단일 효율 순위 | 1위 | 이 스탯만 올려도 최고 효율 |
| 단일 투자 시 레벨 | 1492 | 2위보다 270 레벨 높음 |
| 상위 빌드 사용률 | 100% | 모든 좋은 빌드가 이 스탯 포함 |
| 평균 투자 비중 | 50~70% | 크리스탈 절반 이상을 여기 투자 |

**현재 상위 빌드 분석:**

```
1위: attack_percent 63% + gold_multi_perm 13% + base_attack 10% + ...
2위: attack_percent 70% + crit_chance 20% + base_attack 10%
3위: attack_percent 70% + base_attack 10% + crit_chance 10% + time_extend 10%
4위: attack_percent 50% + crit_chance 30% + base_attack 20%
5위: attack_percent 60% + crit_chance 20% + base_attack 10% + time_extend 10%
```

**특이사항:** 1위 빌드가 gold_multi_perm을 13% 포함하고 있습니다. 이는 이전 분석(2026-01-28_01)에서도 관찰되었으며, 재화 스탯이 완전히 쓸모없지는 않다는 신호입니다.

**문제의 본질:**
- 플레이어가 "attack_percent만 올리면 됨"이라고 느낌
- 다른 선택지가 의미 없어짐
- 게임이 단순해짐

---

### 문제 2: 재화(currency_bonus) 카테고리 사장 🔴

| 스탯 | 효율 순위 | 단일 투자 레벨 | 상위 빌드 사용률 |
|------|-----------|---------------|------------------|
| gold_multi_perm | 4위 (A등급) | 1190 | 10% |
| gold_flat_perm | 7위 (B등급) | 1037 | 0% |
| crystal_multi | 8위 (B등급) | 1021 | 0% |
| crystal_flat | 9위 (B등급) | 1016 | 0% |

**역설적 상황:**
- gold_multi_perm이 효율 4위인데도 10%만 사용
- 단독 투자 시 레벨 1190 (상당히 높음)
- 1위 빌드에서만 13% 사용, 나머지는 무시

**원인 분석:**
```
공격 스탯의 직접성:
  attack_percent 투자 → 즉시 데미지 증가 → 레벨 상승

재화 스탯의 간접성:
  gold_multi_perm 투자 → 골드 증가 → 업그레이드 증가 → 공격력 증가 → 레벨 상승

→ 간접 효과는 플레이어가 체감하기 어려움
→ "공격력이 답"이라는 직관이 강함
```

**개선 여지:**
- gold_multi_perm이 4위인 것은 잠재력이 있다는 증거
- 효과를 더 극적으로 만들면 채택률 상승 가능

---

### 문제 3: utility 카테고리 절반만 살아남음 🟡

| 스탯 | 효율 순위 | 단일 투자 레벨 | 상위 빌드 사용률 |
|------|-----------|---------------|------------------|
| time_extend | 6위 (A등급) | 1095 | 60% |
| upgrade_discount | 18위 (D등급) | 1007 | 0% |

**흥미로운 발견:**
- time_extend가 이전 분석에서는 0% 사용이었는데, 이번엔 60% 사용
- 단독 효율은 6위로 높음
- 상위 5개 빌드 중 3, 5번 빌드가 10% 투자

**왜 time_extend가 부활했는가?**
```
이전 분석 (2026-01-28_01):
  time_extend: 6위 효율, 0% 사용 → GA가 발견 못함

현재 분석 (2026-01-28_02):
  time_extend: 6위 효율, 60% 사용 → GA가 발견함
  Focus 스탯에 포함됨 (히스토리 시스템)
```

**히스토리 시스템의 성과:**
- 과거에 간과된 스탯을 재탐색
- 숨겨진 시너지 발견

**upgrade_discount는 여전히 최악:**
- 18위 (하위 2위)
- 할인 효과가 너무 약함

---

### 문제 4: starting_bonus 카테고리 전멸 🔴

| 스탯 | 효율 순위 | 역할 |
|------|-----------|------|
| start_gold_flat | 10위 | 시작 골드+ 보너스 |
| start_gold_multi | 11위 | 시작 골드* 보너스 |
| start_combo_flex | 12위 | 시작 콤보유연성 |
| start_combo_damage | 13위 | 시작 콤보데미지 |
| start_mouse | 14위 | 시작 마우스 레벨 |
| crit_damage | 15위 | (base_stats이지만 여기 언급) |
| start_level | 16위 | 시작 레벨 보너스 |
| start_gold | 17위 | 시작 골드 보너스 |
| start_keyboard | 19위 | 시작 키보드 레벨 |

**문제의 본질:**
- 8개 스탯 모두가 하위권
- "빠른 시작" 전략이 존재하지 않음
- 초반을 강화하는 것보다 영구 전투력이 훨씬 효율적

**원인:**
```
시작 보너스의 한계:
  - 초반 1~10레벨만 빠름
  - 중후반(20~50레벨)에서는 효과 미미
  - 영구 공격력이 모든 구간에서 유효

결론: 시작 보너스는 구조적으로 불리
```

---

### 문제 5: crit_damage의 배신 🟡

| 지표 | 값 |
|------|-----|
| 효율 순위 | 15위 (D등급) |
| 같은 카테고리 내 순위 | base_stats 5개 중 5위 (최하위) |
| 시너지 스탯(crit_chance) | 3위 (90% 사용) |

**이상한 점:**
- crit_chance는 상위 빌드 90%가 사용
- 크리티컬이 잘 터진다면 crit_damage도 좋아야 하는데
- crit_damage는 아무도 안 씀 (0%)

**원인 분석:**
```
현재 crit_damage 효과: 레벨당 +0.2x 배율

예시 계산:
  crit_chance 10레벨 투자 → 5% 확률
  crit_damage 10레벨 투자 → 2x 배율

  기댓값: 5% × 2x = 0.1 (10% 증가)

  vs.

  attack_percent 10레벨 투자 → 10% 데미지

→ crit_damage가 확률에 의존하므로 불안정
→ attack_percent가 안정적이고 직관적
```

---

## 3. 긍정적 변화 (이전 분석 대비)

### 변화 1: time_extend 부활 ✅

| 지표 | 2026-01-28_01 | 2026-01-28_02 | 변화 |
|------|---------------|---------------|------|
| 상위 빌드 사용률 | 0% | 60% | +60% |
| 단독 효율 순위 | 6위 | 6위 | 유지 |

**의미:**
- 히스토리 시스템이 작동 중
- 간과된 스탯을 재발견
- 유틸리티 카테고리에 희망이 보임

### 변화 2: 지배 비율 개선 ✅

| 지표 | 2026-01-28_01 | 2026-01-28_02 | 변화 |
|------|---------------|---------------|------|
| 1위 레벨 | 1738 | 1702 | -36 |
| 지배 비율 | 1.11 | 1.03 | -0.08 |

**의미:**
- 1위와 다른 빌드들의 격차 감소
- 더 균형잡힌 상태로 수렴

### 변화 3: 다양성 향상 ⚠️

| 지표 | 2026-01-28_01 | 2026-01-28_02 | 변화 |
|------|---------------|---------------|------|
| 다양성 점수 | 0.30 | 0.42 | +0.12 |

**의미:**
- 상위 빌드들이 더 다른 스탯 조합 사용
- 여전히 보통 수준 (0.5 미만)

---

## 4. 권장 조치사항 (우선순위별)

### 🔴 우선순위 1: attack_percent 너프

**목표:** 다른 공격 스탯과의 격차 줄이기

**현재 설정:**
```json
"attack_percent": {
  "base_cost": 1,
  "growth_rate": 0.5,
  "multiplier": 1.4,
  "softcap_interval": 10,
  "effect_per_level": 1.0
}
```

**방법 A: 비용 증가 (추천)**
```json
"attack_percent": {
  "base_cost": 1 → 2,          // 2배 증가
  "growth_rate": 0.5 → 0.7,    // 고레벨 비용 증가 가속
  "multiplier": 1.4 → 1.5      // 소프트캡 더 가파름
}
```

**방법 B: 효과 감소**
```json
"attack_percent": {
  "effect_per_level": 1.0 → 0.7  // 효과 30% 감소
}
```

**예상 결과:**
- attack_percent 효율 1위 → 2~3위로 하락
- base_attack, crit_chance와 경쟁 구도

---

### 🔴 우선순위 2: gold_multi_perm 대폭 버프

**목표:** "골드 파밍" 전략을 유효하게 만들기

**현재 상태:**
- 효율 4위인데도 10%만 사용
- 잠재력은 있으나 약함

**현재 설정:**
```json
"gold_multi_perm": {
  "base_cost": 1,
  "growth_rate": 0.6,
  "multiplier": 1.5,
  "effect_per_level": 3
}
```

**방법: 효과 대폭 증가**
```json
"gold_multi_perm": {
  "effect_per_level": 3 → 8,     // 거의 3배 증가
  "base_cost": 1 → 1,            // 비용 유지
  "growth_rate": 0.6 → 0.5       // 약간 저렴하게
}
```

**예상 결과:**
- gold_multi_perm 효율 4위 → 2~3위로 상승
- "골드 → 업그레이드" 전략이 유효해짐
- 상위 빌드 사용률 10% → 40% 이상

---

### 🟡 우선순위 3: utility 카테고리 분화

**time_extend:** 성공 사례, 유지 또는 약간 버프
**upgrade_discount:** 대폭 버프 필요

**현재 upgrade_discount 설정:**
```json
"upgrade_discount": {
  "base_cost": 1,
  "growth_rate": 1.5,
  "multiplier": 2.5,
  "effect_per_level": 0.1
}
```

**방법: 효과 폭발적 증가**
```json
"upgrade_discount": {
  "effect_per_level": 0.1 → 1.0,    // 10배 증가
  "base_cost": 1 → 1,               // 비용 유지
  "growth_rate": 1.5 → 1.0,         // 비용 증가 완화
  "multiplier": 2.5 → 2.0
}
```

**예상 결과:**
- upgrade_discount 효율 18위 → 10위 이내
- "할인 → 많은 업그레이드" 전략 가능

---

### 🟢 우선순위 4: starting_bonus 리워크 (장기 과제)

**현실적 판단:** 이 카테고리는 구조적 문제

**두 가지 접근:**

#### 접근 A: 효과 극대화 (단기)
```json
"start_level": {
  "effect_per_level": 1 → 5,      // 레벨당 +5
  "base_cost": 1 → 0.5            // 비용 절반
}

"start_gold": {
  "effect_per_level": 50 → 500    // 10배 증가
}
```

#### 접근 B: 메커니즘 변경 (장기)
- 시작 보너스가 아닌 "영구 초반 가속" 개념
- 예: "레벨 1~20 구간 경험치 +X%"
- 게임 코드 변경 필요

**권장:** 접근 A를 먼저 시도, 효과 없으면 접근 B 고려

---

### 🟢 우선순위 5: crit_damage 시너지 개선

**목표:** crit_chance와 함께 투자할 가치 부여

**현재 설정:**
```json
"crit_damage": {
  "base_cost": 1,
  "growth_rate": 0.6,
  "effect_per_level": 0.2
}
```

**방법: 효과 3배 증가**
```json
"crit_damage": {
  "effect_per_level": 0.2 → 0.6,   // 3배
  "base_cost": 1 → 1,              // 비용 유지
  "growth_rate": 0.6 → 0.5         // 약간 저렴
}
```

**예상 결과:**
- crit_chance + crit_damage 조합 빌드 등장
- "크리티컬 특화" 전략 형성

---

## 5. 조정 후 목표 상태

### 이상적인 분석 결과

```
밸런스 등급: B 이상

상위 5개 빌드:
1위: 공격력 특화 (attack + base_attack + crit)      ← 현재와 비슷
2위: 골드 파밍 (gold_multi_perm + attack)            ← 새로 등장
3위: 크리티컬 폭딜 (crit_chance + crit_damage)      ← 새로 등장
4위: 시간 확보 (time_extend + attack)               ← 현재 5번 빌드
5위: 할인 특화 (upgrade_discount + gold)            ← 새로 등장

카테고리 사용률:
- base_stats: 60~80%        (현재 100%)
- currency_bonus: 40~60%    (현재 10%) ← 목표
- utility: 30~50%           (현재 10%) ← 목표
- starting_bonus: 10~20%    (현재 0%)  ← 보너스 목표
```

### 검증 방법

```bash
# 조정 전 저장 (이미 완료)
# balanceDoc/2026-01-28_02/

# config/PermanentStats.json 수정
# 위의 권장 조치사항 적용

# 조정 후 분석
dotnet run --project DeskWarrior.Simulator -- --analyze --target 50 --cps 5 --crystals 500

# 비교 체크리스트:
# ✓ 등급이 C → B 이상으로 상승했는가?
# ✓ 다양성 점수가 0.42 → 0.55 이상으로 상승했는가?
# ✓ currency_bonus 사용률이 10% → 40% 이상으로 상승했는가?
# ✓ utility 사용률이 10% → 30% 이상으로 상승했는가?
# ✓ starting_bonus 사용률이 0% → 10% 이상으로 상승했는가?
```

---

## 6. 액션 아이템 체크리스트

| 우선순위 | 항목 | 대상 | 액션 | 파일 |
|----------|------|------|------|------|
| 🔴 높음 | attack_percent 너프 | attack_percent | base_cost: 1→2, growth_rate: 0.5→0.7 | PermanentStats.json |
| 🔴 높음 | gold_multi_perm 버프 | gold_multi_perm | effect_per_level: 3→8, growth_rate: 0.6→0.5 | PermanentStats.json |
| 🟡 중간 | upgrade_discount 버프 | upgrade_discount | effect_per_level: 0.1→1.0, growth_rate: 1.5→1.0 | PermanentStats.json |
| 🟡 중간 | time_extend 유지 | time_extend | 현재 상태 유지 (잘 작동 중) | - |
| 🟢 낮음 | crit_damage 버프 | crit_damage | effect_per_level: 0.2→0.6 | PermanentStats.json |
| 🟢 낮음 | starting_bonus 버프 | start_level, start_gold | 대폭 증가 (구체적 수치는 테스트 필요) | PermanentStats.json |

---

## 7. 히스토리 시스템의 성과

### 이번 분석에서 히스토리가 한 일

```
Previous analysis: 2026-01-28 19:50
  Top stats: attack_percent, base_attack, crit_chance

Focus stats:
  attack_percent (#1/CUR_TOP)   ← 현재 1위
  base_attack (#2/CUR_TOP)      ← 현재 2위
  crit_chance (#3/CUR_TOP)      ← 현재 3위
  start_keyboard (#19/CUR_BTM)  ← 현재 최하위 (재탐색)
  crit_damage (#15/RANDOM)      ← 중하위 (탐색)
  multi_hit (#5/RANDOM)         ← 중상위 (탐색)
```

**결과:**
- time_extend가 상위 빌드에 재등장 (0% → 60%)
- 시스템이 의도대로 작동 중

**다음 분석 예상:**
- gold_multi_perm을 Focus에 포함할 가능성
- 버프 후 상승 여부 확인

---

## 8. 요약: 밸런스 매니저의 3단계 플랜

### 1단계: 즉시 조치 (이번 주)
```
✓ attack_percent 너프 (비용 2배)
✓ gold_multi_perm 버프 (효과 3배)
→ 재분석 → 등급 C→B 목표
```

### 2단계: 추가 조정 (다음 주)
```
✓ upgrade_discount 버프 (효과 10배)
✓ crit_damage 버프 (효과 3배)
→ 재분석 → 다양성 0.55 이상 목표
```

### 3단계: 구조 개선 (장기)
```
✓ starting_bonus 리워크 검토
✓ 새로운 시너지 메커니즘 고려
→ 최종 목표: 밸런스 등급 A
```

---

*이 문서는 report.md (a299442c) 분석 결과를 바탕으로 작성되었습니다.*
*이전 분석 (2026-01-28_01)과의 비교를 포함합니다.*
