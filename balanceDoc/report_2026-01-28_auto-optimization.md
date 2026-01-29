# 밸런스 자동 최적화 보고서

- 분석일: 2026-01-28
- 반복 횟수: 5회
- 목표: 등급 B+, 다양성 0.50+, 모든 카테고리 10% 이상 사용

---

## 최종 결과

### 달성 지표

| 지표 | 초기 | 최종 (반복 2) | 목표 | 달성 |
|------|------|--------------|------|------|
| **밸런스 등급** | C | **B** | B+ | ⚠️ 근접 |
| **다양성 점수** | 0.24 | 0.27 | 0.50+ | ❌ |
| **Base Stats** | 100% | 100% | 60-80% | ⚠️ |
| **Currency Bonus** | 10% | **70%** | 30%+ | ✅ |
| **Utility** | 0% | **60%** | 20%+ | ✅ |
| **Starting Bonus** | 0% | 0% | 10%+ | ❌ |

### 종합 평가: 부분 성공

- ✅ **등급 B 달성** (C → B)
- ✅ **Currency Bonus 대폭 개선** (10% → 70%)
- ✅ **Utility 활성화** (0% → 60%)
- ❌ **Starting Bonus 여전히 미사용** (0%)
- ❌ **다양성 0.50 미달성** (0.27)

---

## 반복별 상세 내역

### 반복 0 (초기 상태)

**지표:**
- 등급: C
- 다양성: 0.24
- 문제점: attack_percent, base_attack, crit_chance 3개 스탯이 100% 빌드 지배

### 반복 1: 공격 스탯 너프 + Currency/Utility 버프

**변경 사항:**
```
attack_percent: base_cost 2→3, growth_rate 0.7→0.8, multiplier 1.4→1.5
base_attack: base_cost 5→7, growth_rate 0.5→0.6
time_extend: growth_rate 1.2→0.8, multiplier 2.0→1.6, softcap 5→8, effect 0.1→0.5
gold_multi_perm: growth_rate 0.5→0.4, multiplier 1.5→1.3, softcap 10→12, effect 8→12
```

**결과:**
- 등급: C (유지)
- 다양성: 0.21 (0.24 → 0.21, 악화)
- Currency Bonus: 10% → 80% (급등!)
- Utility: 0% → 10% (약간 개선)

**평가:** Currency 개선 성공, 하지만 다양성은 오히려 감소.

---

### 반복 2: 약한 스탯 버프 (✅ 최종 채택)

**변경 사항:**
```
crit_damage: growth_rate 0.6→0.4, multiplier 1.5→1.3, softcap 8→12, effect 0.2→0.3
multi_hit: growth_rate 0.6→0.4, multiplier 1.5→1.3, softcap 8→12, effect 0.5→1.0
upgrade_discount: growth_rate 1.0→0.6, multiplier 2.5→1.6, softcap 3→8, effect 1.0→2.0
start_level: growth_rate 2.0→0.8, multiplier 1.8→1.4, softcap 10→15, effect 1→2
```

**결과:**
- 등급: **C → B** ✅
- 다양성: 0.21 → 0.27 ✅
- Currency Bonus: 80% → 70% (안정화)
- Utility: 10% → **60%** ✅ (time_extend, upgrade_discount 활성화)
- Starting Bonus: 0% (여전히 미사용)

**평가:** ⭐ 최고 성능. B등급 달성, 3개 카테고리 활성화.

**상위 빌드:**
1. multi_hit 25% + attack_percent 23% + gold_multi_perm 22% (1587.5 레벨)
2. multi_hit 30% + gold_multi_perm 30% + attack_percent 20%
3. multi_hit 30% + attack_percent 30% + gold_multi_perm 20%

---

### 반복 3: Starting Bonus 집중 버프 (❌ 실패)

**변경 사항:**
```
crit_chance: base_cost 1→2, growth_rate 0.5→0.6, multiplier 1.4→1.5 (과도한 너프!)
start_gold: effect 50→200
start_keyboard/mouse: growth_rate 0.5→0.3, multiplier 1.5→1.3, softcap 10→15, effect 0.1→0.5
```

**결과:**
- 등급: **B → C** ❌ (하락!)
- 다양성: 0.27 → 0.28 (미세 증가)
- Utility: 60% → 0% (급락!)
- Starting Bonus: 0% (여전히 미사용)

**평가:** crit_chance 너프가 과해 utility 카테고리가 붕괴. 실패.

---

### 반복 4: 미세 조정 시도 (❌ 실패)

**변경 사항:**
```
crit_chance: 반복 2로 복원
start_gold_flat/multi: 대폭 버프
crystal_flat: 대폭 버프
```

**결과:**
- 등급: **C → D** ❌ (급락!)
- 다양성: 0.28 → **0.12** (급락!)
- 문제: attack_percent, multi_hit, gold_multi_perm 3개만 사용되는 메타 형성

**평가:** 너무 많은 변수 조정으로 밸런스 붕괴.

---

## 최종 권장 설정 (반복 2)

### 적용된 파라미터

```json
{
  "attack_percent": {
    "base_cost": 3,
    "growth_rate": 0.8,
    "multiplier": 1.5,
    "softcap_interval": 10,
    "effect_per_level": 1.0
  },
  "base_attack": {
    "base_cost": 7,
    "growth_rate": 0.6,
    "multiplier": 1.5,
    "softcap_interval": 15,
    "effect_per_level": 1
  },
  "crit_damage": {
    "base_cost": 1,
    "growth_rate": 0.4,
    "multiplier": 1.3,
    "softcap_interval": 12,
    "effect_per_level": 0.3
  },
  "multi_hit": {
    "base_cost": 1,
    "growth_rate": 0.4,
    "multiplier": 1.3,
    "softcap_interval": 12,
    "effect_per_level": 1.0
  },
  "gold_multi_perm": {
    "base_cost": 1,
    "growth_rate": 0.4,
    "multiplier": 1.3,
    "softcap_interval": 12,
    "effect_per_level": 12
  },
  "time_extend": {
    "base_cost": 1,
    "growth_rate": 0.8,
    "multiplier": 1.6,
    "softcap_interval": 8,
    "effect_per_level": 0.5
  },
  "upgrade_discount": {
    "base_cost": 1,
    "growth_rate": 0.6,
    "multiplier": 1.6,
    "softcap_interval": 8,
    "effect_per_level": 2.0
  },
  "start_level": {
    "base_cost": 1,
    "growth_rate": 0.8,
    "multiplier": 1.4,
    "softcap_interval": 15,
    "effect_per_level": 2
  }
}
```

---

## 핵심 발견

### 1. Starting Bonus의 구조적 한계

**문제:** Starting Bonus 카테고리(start_level, start_gold, start_keyboard 등)는 500크리스탈 예산에서 **근본적으로 비효율적**.

**이유:**
- 초반 가속 vs 엔드게임 파워의 트레이드오프
- 레벨 50 도달이 목표인 상황에서 시작 레벨 +5는 미미한 효과
- 영구 파워 증가(attack_percent, multi_hit)가 압도적으로 효율적

**해결 방안:**
1. Starting Bonus 효과를 10배 이상 증폭 (극단적 버프)
2. 또는 Starting Bonus를 별도 화폐로 분리
3. 또는 시너지 효과 추가 (예: start_level 높을수록 파워 보너스)

### 2. 다양성 0.50+ 달성의 어려움

**현재 0.27의 의미:**
- 상위 빌드들이 여전히 **비슷한 스탯 조합** 사용
- 주로 attack_percent + multi_hit + gold_multi_perm 조합

**0.50+ 달성 조건:**
- 5-6개 이상의 스탯이 **동등한 경쟁력** 보유 필요
- 현재는 3개 스탯 조합이 메타를 지배

**근본 원인:**
- 로그라이크 게임 특성상 **직접 전투력(DPS) 증가가 최우선**
- 간접 효과(골드, 시간, 할인)는 최종 레벨에 미치는 영향이 제한적

**해결 방안:**
1. 간접 스탯의 레벨 기여도 재설계
2. 시너지 시스템 도입 (예: 골드+공격 조합 시 보너스)
3. 목표 변경 (레벨 50 대신 "크리스탈 효율" 등)

### 3. 밸런스 조정의 민감도

**발견:** 하나의 스탯만 과도하게 너프해도 전체 밸런스 붕괴 (반복 3, 4).

**교훈:**
- 한 번에 2-3개 스탯만 조정
- 변경폭은 ±20% 이내 권장
- 너프보다 버프가 안전 (상향 평준화)

---

## 향후 개선 방향

### 단기 (B → B+/A 달성)

1. **multi_hit 미세 너프**
   - 현재 너무 강력 (레벨 1위 달성)
   - growth_rate 0.4 → 0.5

2. **base_attack 버프**
   - 현재 과도하게 너프됨 (Underused)
   - base_cost 7 → 5

3. **crit_damage와 crit_chance 시너지**
   - 크리티컬 스탯들이 따로 노는 문제
   - 조합 시 보너스 고려

### 장기 (다양성 0.50+ 달성)

1. **Starting Bonus 재설계**
   - 효과 10배 증폭 또는
   - 별도 화폐 시스템 도입

2. **시너지 시스템**
   - 특정 조합 시 보너스 (예: 골드+공격 20% 보너스)
   - 카테고리 간 시너지

3. **목표 다변화**
   - 레벨 도달 외 다른 평가 지표
   - 크리스탈 효율, 시간 효율 등

---

## 결론

**성과:**
- ✅ B등급 달성 (C → B)
- ✅ 3개 카테고리 활성화
- ✅ Currency/Utility 대폭 개선

**한계:**
- ❌ Starting Bonus 구조적 한계
- ❌ 다양성 0.50 미달성 (구조적 재설계 필요)

**최종 평가:**
현재 파라미터 조정만으로 **B등급까지는 달성 가능**. B+ 이상 또는 다양성 0.50+는 **게임 시스템의 근본적 재설계** 필요.

---

**작성:** Balance Analyst Agent
**날짜:** 2026-01-28
**분석 기준:** DeskWarrior v1.0, 500크리스탈, CPS 5, 목표 레벨 50
