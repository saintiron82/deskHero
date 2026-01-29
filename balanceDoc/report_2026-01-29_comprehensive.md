# 밸런스 분석 보고서
- 분석일: 2026-01-29
- 분석 대상: 신규 적용 규칙 (스테이지별 비용 배율, 데미지 공식, 유틸리티 보너스)

## 분석 당시 데이터 기준값

### 핵심 상수
| 상수 | 값 | 설명 |
|------|-----|------|
| BASE_CPS | 5.0 | 기본 CPS (초당 5회 입력) |
| BASE_HP | 100 | 1스테이지 몬스터 HP |
| HP_GROWTH | 1.2 | 스테이지당 HP 증가율 |
| BOSS_HP_MULTI | 5.0 | 보스 HP 배수 |
| COMBO_MAX_STACK | 3 | 최대 콤보 스택 |
| COMBO_MULTIPLIER | 8 | 콤보 3스택 시 배율 |

### 신규 적용 규칙
1. **스테이지별 업그레이드 비용 배율**: 50스테이지마다 ×2
2. **데미지 공식**: `attack_percent`는 `base_power`에만 적용, `utility_bonus` 추가
3. **유틸리티 보너스**: `1 + (time_extend + upgrade_discount) × 0.003`

---

## 분석 결과

### 1. 10시간 게임 플레이 전략별 비교 (CPS 5 기준)

| 전략 | 최고 도달 레벨 | 총 세션 수 | 크리스탈 획득 | 핵심 스탯 | 평가 |
|------|---------------|-----------|--------------|----------|------|
| **Balanced** | **2,680** | 5 | 473,961 | CritDmg(382), CritChance(100), MultiHit(77) | S |
| **DamageFirst** | 1,490 | 7 | 417,481 | BaseAttack(86), AttackPercent(63), CritChance(53) | A |
| **Greedy** | 1,180 | 9 | 268,646 | GoldFlat(77), GoldMulti(78), CritDmg(62) | B |
| **CrystalFarm** | 530 | 13 | 151,234 | GoldFlat(124), CrystalMulti(46) | C |
| **SurvivalFirst** | 340 | 939 | 5,973 | TimeExtend(60), StartLevel(61) | D |

#### 핵심 발견

**1. Balanced 전략이 압도적 우위 (2.3배 차이)**
- 최고 도달: Lv.2,680
- Greedy보다 2.3배, DamageFirst보다 1.8배 높음
- 균형잡힌 크리티컬 빌드가 가장 효율적

**2. Balanced 전략의 특징**
```
핵심 투자:
- CritDamage: Lv.382 (최우선)
- CritChance: Lv.100 (Max)
- MultiHit: Lv.77
- GoldFlat: Lv.110 (경제 지원)
- TimeExtend: Lv.60 (생존 지원)

투자 비율:
- 데미지 스탯: 70%
- 경제 스탯: 20%
- 유틸 스탯: 10%
```

**3. DamageFirst의 한계**
- BaseAttack 중심 → 후반 효율 저하
- AttackPercent는 base_power에만 적용되어 제한적
- 크리티컬 빌드 전환 지연으로 Balanced에 뒤짐

**4. Greedy의 함정**
- 초반 GoldMulti/GoldFlat 집중
- StartGold(365 레벨!)에 과도한 투자
- 경제 최적화 집착이 진행 속도 저해

**5. CrystalFarm과 Survival의 실패**
- 데미지 투자 부족으로 진행 불가
- CrystalFarm: 크리스탈 모으기만 집중 → Lv.530 정체
- Survival: TimeExtend만 투자 → 세션당 38초로 끝남 (939세션 반복)

---

### 2. 밸런스 다양성 분석 (크리스탈 1,000 예산, 목표 Lv.100)

#### 밸런스 등급: **B** (Good)

| 지표 | 값 | 목표 | 판정 |
|------|-----|------|------|
| **Dominance Ratio** | 1.11 | < 1.3 | ✅ 양호 |
| **Diversity Score** | 0.29 | > 0.5 | ❌ 낮음 |
| **Pattern Similarity** | 0.61 | < 0.5 | ⚠️ 보통 |
| **Active Categories** | 3/4 | 4/4 | ⚠️ 1개 미사용 |

#### 최적 패턴 (GA 최적화 결과)
```
최고 도달: Lv.653

스탯 배분:
- time_extend:      25% (시간 확보)
- base_attack:      20% (기본 데미지)
- gold_multi_perm:  16% (경제)
- crit_chance:      15% (크리티컬)
- multi_hit:        11% (추가 타격)
- upgrade_discount: 10% (비용 절감)
- start_level:       1% (시작 보너스)
- gold_flat_perm:    1% (경제 보조)
```

#### 스탯별 효율 순위

| 순위 | 스탯 | 단독 레벨 | 등급 | Top10 사용률 | 상태 |
|------|------|----------|------|-------------|------|
| 1 | base_attack | 457.5 | S | 100% | 과도사용 |
| 2 | time_extend | 449.0 | S | 100% | 과도사용 |
| 3 | gold_multi_perm | 430.5 | S | 100% | 과도사용 |
| 4 | multi_hit | 425.5 | A | 100% | 과도사용 |
| 5 | crit_chance | 407.0 | A | 10% | 저사용 |
| 6 | gold_flat_perm | 403.5 | A | 0% | 저사용 |
| 7 | attack_percent | 401.5 | B | 0% | 저사용 |
| ... | ... | ... | ... | ... | ... |
| 18 | crystal_multi | 329.0 | D | 0% | 저사용 |
| 19 | start_level | 36.0 | D | 0% | 저사용 |

#### 카테고리별 사용률

| 카테고리 | 사용률 | 상태 |
|----------|--------|------|
| base_stats | 100% | ✅ 활성 |
| utility | 100% | ✅ 활성 |
| currency_bonus | 60% | ✅ 활성 |
| **starting_bonus** | **0%** | ❌ **미사용** |

---

## 밸런스 판정

### 긍정적 요소 ✅

1. **지배적 루트 없음** (Dominance 1.11)
   - 1위와 2위 격차 11%로 적절
   - 여러 빌드가 경쟁력 유지

2. **핵심 카테고리 활성화**
   - base_stats, utility, currency_bonus 모두 사용
   - 데미지/경제/생존 균형 필요성 확인

3. **전략 선택지 존재**
   - Balanced, DamageFirst 모두 유효
   - 플레이 스타일별 차별화 가능

### 문제점 ❌

1. **낮은 패턴 다양성** (Diversity 0.29)
   - 대부분 패턴이 유사한 구성
   - `time_extend` + `base_attack` + `gold_multi_perm` 조합 편중
   - 창의적 빌드 부족

2. **Starting Bonus 카테고리 완전 무시** (0% 사용)
   - 8개 스탯 모두 비효율적
   - `start_level`, `start_gold`, `start_keyboard` 등 무의미

3. **Greedy 전략의 비효율**
   - StartGold에 365레벨 투자 → 실제 효과 미미
   - 경제 최적화가 오히려 진행 방해

4. **4대 스탯 과도 집중**
   - base_attack, time_extend, gold_multi_perm, multi_hit
   - Top10 패턴 모두 이 4개 스탯 포함
   - 14개 스탯은 저평가

---

## 권장 조치

### 우선순위 1: 빌드 다양성 확보 ⚠️

**문제:** 상위 패턴 61% 유사도 → 획일적 메타

**조치:**
1. **과도사용 스탯 상향 조정 (비용 증가)**
   ```
   base_attack:     base_cost 10 → 15  (+50%)
   time_extend:     multiplier 1.25 → 1.35  (+8%)
   gold_multi_perm: growth_rate 0.5 → 0.6  (+20%)
   ```

2. **저사용 스탯 하향 조정 (효과 증가 or 비용 감소)**
   ```
   attack_percent:  effect 1.0 → 1.5  (+50% 효율)
   crit_damage:     base_cost 1 → 0.5  (비용 절반)
   crit_chance:     base_cost 1 → 0.7  (비용 -30%)
   ```

3. **시너지 보너스 추가**
   ```
   AttackPercent + CritDamage 조합:
   - 두 스탯 모두 Lv.20+ 시 데미지 +10%
   - Lv.50+ 시 데미지 +20%

   목적: 비주류 빌드 유도
   ```

### 우선순위 2: Starting Bonus 카테고리 재설계 🔧

**문제:** 8개 스탯 완전 무의미 (사용률 0%)

**현재 문제점:**
- `start_level`: 레벨 2씩 증가 → 초반 1~2분 단축 효과만
- `start_gold`: 골드 50씩 증가 → Lv20 이상 의미 없음
- `start_keyboard/mouse`: 공격력 0.1씩 → 미미한 효과

**조치:**
```
A안: 효과 대폭 상향 (10배)
  start_level:    effect 2 → 20  (20레벨씩 점프)
  start_gold:     effect 50 → 500  (의미있는 골드)
  start_keyboard: effect 0.1 → 1.0  (실질적 파워)

B안: 카테고리 통폐합
  - start_* 스탯 삭제
  - 대신 "Meta Progress" 추가
    * session_count_bonus: 세션 횟수에 비례한 영구 버프
    * death_resilience: 사망 시 부분 진행도 보존

C안: 전략적 선택지로 재설계
  - "Speed Run" 빌드: start_level 대폭 강화
  - "Economic" 빌드: start_gold로 초반 압도

권장: A + C 혼합 (효과 10배 + 전략 명확화)
```

### 우선순위 3: Greedy 전략 로직 수정 📊

**문제:** StartGold에 365레벨 투자 → 비효율의 극치

**원인:** 효율 계산이 "즉시 효과"만 고려, 장기 효과 미반영

**조치:**
1. Greedy 효율 계산에 "감가상각" 개념 도입
   ```csharp
   // 현재
   efficiency = effect / cost

   // 수정안
   remaining_sessions = estimated_total_sessions - current_session;
   long_term_value = effect * remaining_sessions;
   efficiency = long_term_value / cost;

   // StartGold는 초반에만 유효하므로
   if (stat == "start_gold" && current_session > 5)
       efficiency *= 0.1;  // 90% 페널티
   ```

2. 카테고리별 가중치 도입
   ```
   base_stats:      1.0 (기본)
   currency_bonus:  0.8 (경제는 데미지보다 낮게)
   starting_bonus:  0.5 / (session + 1)  (세션 진행시 급감)
   ```

---

## 수치 요약

### 전략별 최고 도달 레벨 (10시간 플레이)

```
Balanced:       Lv.2,680  ██████████████████████████
DamageFirst:    Lv.1,490  ██████████████
Greedy:         Lv.1,180  ███████████
CrystalFarm:    Lv.530    █████
SurvivalFirst:  Lv.340    ███
```

### 스탯 효율 분포

```
S등급 (450+):   3개  ████████████
A등급 (400+):   3개  ████████████
B등급 (350+):   4개  ████████████████
C등급 (330+):   4개  ████████████████
D등급 (<330):   5개  ██████████████████

문제: S등급 과집중, D등급 무의미
```

### 카테고리 사용률

```
base_stats:       100%  ██████████
utility:          100%  ██████████
currency_bonus:    60%  ██████
starting_bonus:     0%  (사용 안 함)
```

---

## 결론

### 현재 밸런스 상태: **B등급 (Good with Issues)**

**장점:**
- 지배적 루트 없음 (1위/2위 격차 11%)
- Balanced vs DamageFirst 경쟁 구도
- 핵심 메커니즘 작동 (크리티컬 빌드 효과적)

**단점:**
- 패턴 다양성 낮음 (29점/100점)
- Starting Bonus 카테고리 무용지물
- 4대 스탯 과도 집중 (14개 스탯 저평가)
- Greedy 전략 비효율 (StartGold 365레벨 투자)

### 핵심 개선 방향

1. **빌드 다양성 확보** - 과도사용 스탯 너프, 저사용 스탯 버프
2. **Starting Bonus 재설계** - 효과 10배 증가 or 카테고리 재구성
3. **Greedy 로직 수정** - 장기 가치 반영, 초반 스탯 페널티
4. **시너지 보너스 추가** - 비주류 조합 유도

### 기대 효과

조치 적용 시 예상:
- Diversity Score: 0.29 → 0.45+ (55% 증가)
- Starting Bonus 사용률: 0% → 40%+
- 유효 빌드 수: 5개 → 10개+
- Greedy 효율: Lv.1,180 → Lv.1,800+ (50% 개선)

---

**보고서 작성:** 2026-01-29, Game Balance Analyst Agent
**분석 도구:** DeskWarrior.Simulator v1.0
**시뮬레이션 횟수:** 61,360회 (3,068 패턴)
