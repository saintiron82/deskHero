# DeskWarrior 밸런스 리포트 해설서

## 목차
1. [리포트의 목적](#1-리포트의-목적)
2. [핵심 지표 이해하기](#2-핵심-지표-이해하기)
3. [등급 시스템](#3-등급-시스템)
4. [리포트 섹션별 해설](#4-리포트-섹션별-해설)
5. [실제 리포트 해석 예시](#5-실제-리포트-해석-예시)
6. [밸런스 조정 가이드](#6-밸런스-조정-가이드)

---

## 1. 리포트의 목적

### 핵심 질문
> **"플레이어가 레벨 50에 도달하려면 어떤 영구 스탯을 올려야 하는가?"**

이 질문에 대한 답이 **"attack_percent만 올리면 됨"** 이라면 밸런스 실패입니다.

좋은 밸런스란:
- 다양한 업그레이드 경로(루트)가 존재
- 각 경로가 비슷한 효율을 보임
- 플레이어에게 의미 있는 선택지 제공

### 리포트가 답하는 질문들

| 질문 | 리포트 섹션 |
|------|-------------|
| 압도적으로 좋은 전략이 있나? | Executive Summary > Dominance Ratio |
| 다양한 빌드가 가능한가? | Executive Summary > Diversity Score |
| 어떤 스탯이 너무 강한가? | Stat Analysis > Overused |
| 어떤 스탯이 쓸모없는가? | Stat Analysis > Underused |
| 구체적으로 뭘 고쳐야 하나? | Recommendations |

---

## 2. 핵심 지표 이해하기

### 2.1 Dominance Ratio (지배 비율)

```
지배 비율 = 1등 전략 평균 레벨 / 2등 전략 평균 레벨
```

**해석:**
| 값 | 의미 | 상태 |
|----|------|------|
| 1.00 ~ 1.10 | 1등과 2등이 거의 동등 | ✅ 우수 |
| 1.10 ~ 1.20 | 약간의 차이 존재 | ✅ 양호 |
| 1.20 ~ 1.30 | 눈에 띄는 차이 | ⚠️ 주의 |
| 1.30 이상 | 1등이 압도적 | ❌ 실패 |

**예시:**
- 1등: 레벨 1738, 2등: 레벨 1560
- 지배 비율 = 1738 / 1560 = **1.11** ✅

### 2.2 Diversity Score (다양성 점수)

상위 전략들이 얼마나 **다른 스탯 조합**을 사용하는지 측정합니다.

**계산 방식:** Jaccard 거리 (두 집합이 얼마나 다른지)

**해석:**
| 값 | 의미 | 상태 |
|----|------|------|
| 0.7 ~ 1.0 | 상위 전략들이 완전히 다른 스탯 사용 | ✅ 우수 |
| 0.5 ~ 0.7 | 적당히 다양함 | ✅ 양호 |
| 0.3 ~ 0.5 | 일부 스탯이 공통으로 사용됨 | ⚠️ 보통 |
| 0.0 ~ 0.3 | 대부분 같은 스탯 사용 | ❌ 미흡 |

**예시:**
- 다양성 0.30 = 상위 전략들이 대부분 비슷한 스탯 조합 사용 ⚠️

### 2.3 Pattern Similarity (패턴 유사도)

상위 3개 전략의 **스탯 배분 비율**이 얼마나 비슷한지 측정합니다.

**계산 방식:** 코사인 유사도 (벡터가 얼마나 같은 방향인지)

**해석:**
| 값 | 의미 |
|----|------|
| 0.9 ~ 1.0 | 거의 동일한 배분 (나쁨) |
| 0.7 ~ 0.9 | 비슷한 배분 |
| 0.5 ~ 0.7 | 적당히 다름 |
| 0.0 ~ 0.5 | 완전히 다른 배분 (좋음) |

### 2.4 Category Usage (카테고리 사용률)

각 스탯 카테고리가 상위 전략에서 얼마나 사용되는지 측정합니다.

**4개 카테고리:**
| 카테고리 | 포함 스탯 | 역할 |
|----------|-----------|------|
| base_stats | attack, crit, multi_hit | 직접 전투력 |
| currency_bonus | gold, crystal 획득 | 경제 |
| utility | time_extend, discount | 유틸리티 |
| starting_bonus | 시작 보너스들 | 초반 가속 |

**해석:**
| 사용률 | 상태 |
|--------|------|
| 50% 이상 | ✅ Active (활성) |
| 30~50% | ⚠️ Moderate (보통) |
| 30% 미만 | ❌ Underused (미사용) |

---

## 3. 등급 시스템

### 밸런스 등급

| 등급 | 의미 | 판정 조건 |
|------|------|-----------|
| **A** | 우수 | 다양성 ≥ 0.5 + 모든 카테고리 활성 |
| **B** | 양호 | 다양성 ≥ 0.5 |
| **C** | 보통 | 2개 이상 카테고리 미사용 |
| **D** | 미흡 | 다양성 < 0.2 |
| **F** | 실패 | 지배 비율 > 1.3 |

### 스탯 효율 등급

| 등급 | 순위 | 의미 |
|------|------|------|
| **S** | 1~3위 | 최상위 효율 |
| **A** | 4~6위 | 상위 효율 |
| **B** | 7~10위 | 중상위 효율 |
| **C** | 11~14위 | 중하위 효율 |
| **D** | 15위~ | 하위 효율 (버프 필요) |

---

## 4. 리포트 섹션별 해설

### 4.1 Executive Summary

**가장 먼저 봐야 할 섹션입니다.**

```markdown
### Balance Grade: **C** :yellow_circle:
> Moderate balance issues - some paths underutilized.
```

이것만 보면 현재 밸런스 상태를 즉시 파악할 수 있습니다.

### 4.2 Top Patterns

**"어떤 전략이 가장 좋은가?"**

```
| Rank | Pattern ID | Avg Level | Main Stats |
| 1 | ga_optimized | 1738.0 | attack_percent, base_attack, crit_chance |
```

- **Pattern ID**: 전략 이름
- **Avg Level**: 이 전략으로 평균 도달 레벨
- **Main Stats**: 주로 투자하는 스탯 (10% 이상)

### 4.3 Category Analysis

**"어떤 종류의 스탯이 인기 있는가?"**

```
| Category | Usage | Status |
| Base Stats | 100% | ✅ Active |
| Utility | 0% | ❌ Underused |
```

Underused 카테고리는 버프가 필요합니다.

### 4.4 Stat Analysis

**"개별 스탯의 효율은 어떤가?"**

```
| Rank | Stat | Level | Rating | Top10 Usage | Status |
| 1 | attack_percent | 1492.5 | S | 100% | Overused |
```

- **Level**: 이 스탯만 100% 투자 시 평균 도달 레벨
- **Rating**: 효율 등급 (S~D)
- **Top10 Usage**: 상위 10개 전략 중 몇 %가 이 스탯 사용
- **Status**: Overused / Balanced / Underused

### 4.5 Recommendations

**"구체적으로 뭘 고쳐야 하는가?"**

우선순위별로 정렬된 액션 아이템입니다.

```
### :bulb: Medium Priority
#### [Nerf] Attack Percent
- Issue: 100% usage in top builds
- Suggestion: Slightly increase cost
```

---

## 5. 실제 리포트 해석 예시

### 2026-01-28 분석 결과 해석

#### 현황
```
밸런스 등급: C (보통)
지배 비율: 1.11 (양호)
다양성: 0.30 (보통)
```

#### 문제점 1: attack_percent 독주

**증거:**
- 단일 스탯 효율 1위 (레벨 1492)
- 상위 빌드 100% 사용
- 2위(base_attack)와 270 레벨 차이

**원인 추정:**
- 비용 대비 효과가 너무 좋음
- 다른 공격 스탯보다 확실한 이점

**해결책:**
- attack_percent 비용 증가 (base_cost 또는 growth_rate)
- 또는 다른 공격 스탯(base_attack, crit) 버프

#### 문제점 2: utility/starting_bonus 카테고리 전멸

**증거:**
- utility 카테고리 사용률 0%
- starting_bonus 카테고리 사용률 0%
- time_extend가 6위인데도 상위 빌드에서 미사용

**원인 추정:**
- 효과가 간접적이라 체감이 안 됨
- 비용 대비 레벨 도달 효과가 낮음

**해결책:**
- time_extend 효과 증가 (초당 더 많은 시간)
- starting_bonus 효과를 더 극적으로

#### 문제점 3: crit_damage 최하위

**증거:**
- 19개 스탯 중 15위
- D등급 (버프 필요)

**원인 추정:**
- crit_chance가 낮으면 crit_damage 효과 미미
- 크리티컬 확률이 높아져야 의미 있음

**해결책:**
- crit_damage 자체 효과 증가
- 또는 crit_chance와 시너지 구조 설계

---

## 6. 밸런스 조정 가이드

### 6.1 너프 vs 버프 선택

| 상황 | 추천 액션 |
|------|-----------|
| 1개 스탯만 과다 사용 | 해당 스탯 너프 |
| 다수 스탯이 미사용 | 미사용 스탯들 버프 |
| 전체적으로 너무 쉬움 | 강한 스탯들 너프 |
| 전체적으로 너무 어려움 | 약한 스탯들 버프 |

### 6.2 구체적 조정 방법

**비용 조정:**
```json
// PermanentStats.json
"attack_percent": {
  "base_cost": 10,      // 현재 값
  "growth_rate": 0.5,   // 레벨당 비용 증가율
  "multiplier": 1.5,    // 소프트캡 배수
  "softcap_interval": 15
}
```

- **base_cost ↑**: 초반부터 비싸짐
- **growth_rate ↑**: 고레벨에서 급격히 비싸짐
- **multiplier ↑**: 소프트캡 이후 폭발적 증가
- **softcap_interval ↓**: 더 빨리 비싸짐

**효과 조정:**
```json
"time_extend": {
  "effect_per_level": 1.0  // 레벨당 추가 초
}
```

- **effect_per_level ↑**: 같은 비용으로 더 큰 효과

### 6.3 조정 후 검증

1. 밸런스 분석 재실행
2. 등급 변화 확인
3. 새로운 문제점 발생 여부 확인
4. 반복

```bash
# 조정 전
simulate --analyze --target 50 --cps 5 --crystals 500 --output balanceDoc/before

# config 수정 후
simulate --analyze --target 50 --cps 5 --crystals 500 --output balanceDoc/after

# 비교
```

---

## 7. 분석 히스토리 시스템

### 7.1 히스토리란?

시스템은 매 분석 결과를 자동으로 저장합니다. 다음 분석 시 과거 데이터를 참조하여:
- **과거에 강했던 스탯**이 여전히 강한지 확인
- **과거에 약했던 스탯**이 버프 후 개선되었는지 확인
- **탐색 다양성** 확보 (매번 같은 스탯만 테스트하지 않음)

### 7.2 Focus 스탯 선택 로직

분석 시 집중 탐색할 스탯 6개를 다음 순서로 선택합니다:

| 순번 | 소스 | 설명 |
|------|------|------|
| 1-3 | CUR_TOP | 현재 분석 상위 3개 (검증된 강자) |
| 4 | HIST_TOP1 | 과거 분석 1위 (중복 시 스킵) |
| 5 | HIST_TOP2 | 과거 분석 2위 (중복 시 스킵) |
| 6 | CUR_BTM | 현재 분석 최하위 (숨겨진 시너지 탐색) |
| 보충 | RANDOM | 부족하면 중간에서 랜덤 선택 |

**예시 출력:**
```
Focus stats: attack_percent(#1/CUR_TOP), base_attack(#2/CUR_TOP),
             crit_chance(#3/CUR_TOP), start_keyboard(#19/CUR_BTM),
             time_extend(#6/RANDOM), start_mouse(#14/RANDOM)
```

### 7.3 밸런스 매니저 워크플로우

#### 일반적인 밸런스 조정 사이클

```
┌─────────────────────────────────────────────────────────┐
│  1. 현재 상태 분석                                        │
│     simulate --analyze --cps 5 --crystals 500            │
│                                                         │
│  2. 리포트 검토                                          │
│     - 등급 확인 (목표: B 이상)                            │
│     - 지배적 스탯 확인 (Overused)                         │
│     - 미사용 스탯 확인 (Underused)                        │
│                                                         │
│  3. config/PermanentStats.json 수정                      │
│     - Overused → 비용 증가 또는 효과 감소                  │
│     - Underused → 비용 감소 또는 효과 증가                 │
│                                                         │
│  4. 재분석 및 비교                                        │
│     simulate --analyze --cps 5 --crystals 500            │
│     (시스템이 과거 결과와 자동 비교)                        │
│                                                         │
│  5. 개선 확인                                            │
│     - 등급 상승? ✓                                       │
│     - 과거 약점 스탯 순위 상승? ✓                         │
│     - 새로운 문제 발생? → 3번으로                          │
└─────────────────────────────────────────────────────────┘
```

#### 히스토리 활용 시나리오

**시나리오 1: 버프 효과 검증**
```bash
# 1. 버프 전 분석
simulate --analyze --target 50 --cps 5 --crystals 500

# 결과: time_extend가 18위 (D등급)
# 히스토리에 기록됨

# 2. config 수정: time_extend 효과 2배

# 3. 버프 후 분석
simulate --analyze --target 50 --cps 5 --crystals 500

# 결과: "Using historical data: btm=time_extend"
# → time_extend가 Focus 스탯에 포함되어 집중 테스트됨
# → 순위 변화 확인 (18위 → ?위)
```

**시나리오 2: 장기 밸런스 추적**
```bash
# 분석 결과가 누적됨 (최근 10회 보관)

# 첫 주: 등급 C, attack_percent 독주
# 둘째 주: 조정 후 등급 C → B
# 셋째 주: 추가 조정 후 등급 B → A

# 히스토리를 통해:
# - 과거 강했던 스탯이 계속 강한지 추적
# - 버프한 스탯이 실제로 순위 상승했는지 검증
```

### 7.4 히스토리 파일 구조

```
balanceDoc/
├── analysis_history.json     ← 자동 생성/업데이트
├── 2026-01-28_01/
│   ├── report.json
│   ├── report.md
│   └── analysis.md
└── 2026-01-28_02/           ← 같은 날 2회차
    └── ...
```

**analysis_history.json 예시:**
```json
{
  "Records": [
    {
      "RecordId": "a5ce1e8d",
      "AnalyzedAt": "2026-01-28T19:43:42",
      "BalanceGrade": "C",
      "TopStats": ["attack_percent", "base_attack", "crit_chance"],
      "BottomStats": ["start_keyboard", "upgrade_discount"],
      "BestPatternId": "ga_optimized",
      "BestPatternLevel": 1710.5
    }
  ]
}
```

### 7.5 보고서 저장 규칙 (필수)

**⚠️ 모든 분석은 반드시 보고서를 남겨야 합니다.**

```bash
# 올바른 방법 - 항상 --output 사용
simulate --analyze --cps 5 --crystals 500 --output balanceDoc/2026-01-28_01/report

# 잘못된 방법 - 보고서 없이 실행
simulate --analyze --cps 5 --crystals 500  # ❌ 기록 안 남음
```

**반복 최적화 시 규칙:**
```
반복 1: --output balanceDoc/2026-01-28_04/report
반복 2: --output balanceDoc/2026-01-28_05/report
반복 3: --output balanceDoc/2026-01-28_06/report
...
```

각 반복마다:
1. 변경 내용 기록 (어떤 스탯을 어떻게 수정했는지)
2. 분석 실행 + 보고서 저장
3. 결과 평가

이렇게 하면 나중에 어느 시점의 설정이 가장 좋았는지 추적할 수 있습니다.

### 7.6 히스토리 리셋

밸런스 구조가 크게 변경되어 과거 데이터가 의미없을 때:

```bash
# 히스토리 파일 삭제
del balanceDoc\analysis_history.json

# 새로운 분석 시작
simulate --analyze --cps 5 --crystals 500 --output balanceDoc/2026-01-28_01/report
# → "No previous analysis history found."
```

---

## 부록: 용어 정리

| 용어 | 영문 | 설명 |
|------|------|------|
| 패턴 | Pattern | 스탯 투자 비율의 조합 (예: attack 70% + gold 30%) |
| 루트 | Route | 업그레이드 경로 (패턴과 동의어) |
| 지배 | Dominance | 특정 전략이 압도적으로 좋은 상태 |
| 다양성 | Diversity | 다양한 전략이 공존하는 상태 |
| 카테고리 | Category | 스탯들의 그룹 (base_stats, utility 등) |
| 시뮬레이션 | Simulation | 가상 게임 플레이 |
| Grid Search | - | 모든 조합을 체계적으로 테스트 |
| Genetic Algorithm | GA | 진화 알고리즘으로 최적 조합 탐색 |
| 히스토리 | History | 과거 분석 결과 저장소 |
| Focus 스탯 | Focus Stats | 집중 탐색 대상 스탯 (6개) |

---

*최종 수정: 2026-01-28*
*작성: DeskWarrior Balance Analysis System*
*히스토리 시스템 추가: 2026-01-28*
