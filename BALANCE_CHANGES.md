# 밸런스 변경 로그

**일시:** 2026-01-29
**기준:** 50시간 플레이 테스트 결과 분석
**변경 사유:** Balanced 전략 압도적 우세 문제 해결 (Dominance Ratio 1.96 → 목표 1.3 이하)

---

## 문제 진단

### 50시간 테스트 결과 (변경 전)

| 전략 | 평균 레벨 | 격차 | 문제점 |
|------|----------|------|--------|
| **Balanced** | 302 | - | 압도적 1위 |
| DamageFirst | 154 | -49.0% | 공격 특화인데도 절반 수준 |
| SurvivalFirst | 65 | -78.5% | 시간 관리 전략 거의 무용지물 |
| Greedy | 37 | -87.6% | 정체 |
| CrystalFarm | 35 | -88.4% | 정체 |

**Dominance Ratio:** 1.96 (Grade D - Poor)

---

## 변경 내역

### 1. 핵심 스탯 밸런싱

#### time_extend (시간 연장)
```diff
- "effect_per_level": 0.4  // 레벨당 0.4초
+ "effect_per_level": 0.8  // 레벨당 0.8초 (2배 증가)

- "max_effect": 30  // 최대 +30초
+ "max_effect": 60  // 최대 +60초
```

**이유:**
- SurvivalFirst 전략이 -78.5% 격차로 극도로 비효율적
- 시간 관리만으로는 레벨 돌파 불가능
- 시간 연장 효과를 2배로 증가하여 생존 전략의 효율성 개선

**예상 효과:**
- SurvivalFirst 전략 성장률 50-100% 개선
- 시간 관리의 전략적 가치 상승

#### attack_percent (공격력 배수)
```diff
- "effect_per_level": 2.5  // 레벨당 2.5%
+ "effect_per_level": 2.0  // 레벨당 2.0% (20% 감소)
```

**이유:**
- 공식에서 1.5배 가중치가 적용되어 실제 효과는 3.75%/레벨
- DamageFirst가 Balanced보다 효율적이어야 하나 실제로는 절반 수준
- 과도한 효과로 Balanced 전략에 너무 큰 이득 제공

**예상 효과:**
- Balanced 전략의 전반적인 성장률 10-15% 감소
- 다른 전략과의 격차 완화

#### crit_damage (크리티컬 배율)
```diff
- "effect_per_level": 0.15
+ "effect_per_level": 0.2  // 33% 증가
```

**이유:**
- 크리티컬 빌드의 다양성 부족
- 크리티컬 특화 전략이 범용 전략보다 매력적이어야 함

**예상 효과:**
- 크리티컬 특화 빌드의 경쟁력 향상
- 빌드 다양성 증가

#### upgrade_discount (업그레이드 할인)
```diff
- "effect_per_level": 2.0  // 레벨당 2%
+ "effect_per_level": 3.0  // 레벨당 3% (50% 증가)

- "max_effect": 50  // 최대 50% 할인
+ "max_effect": 60  // 최대 60% 할인
```

**이유:**
- 경제 관리 전략의 가치 부족
- 할인 효과가 직접 전투력 증가보다 덜 매력적

**예상 효과:**
- 장기 플레이에서 경제 관리 전략의 효율성 증가
- 유틸리티 스탯 투자 매력도 상승

---

### 2. Starting Bonus 스탯 전면 개선

**문제:** 모든 Starting Bonus 스탯이 일관되게 Bottom 5에 위치
- 비용 대비 효과가 너무 낮음
- 세션 시작에만 적용되는 일회성 효과

#### start_level (시작 레벨)
```diff
- "growth_rate": 0.8
+ "growth_rate": 0.4  // 비용 성장률 50% 감소

- "multiplier": 1.4
+ "multiplier": 1.3  // 비용 증가폭 감소
```

#### start_gold (시작 골드)
```diff
- "growth_rate": 0.3
+ "growth_rate": 0.2  // 비용 성장률 감소

- "multiplier": 1.2
+ "multiplier": 1.15  // 비용 증가폭 감소

- "effect_per_level": 50
+ "effect_per_level": 100  // 효과 2배
```

#### start_keyboard / start_mouse (시작 무기 공격력)
```diff
- "growth_rate": 0.5
+ "growth_rate": 0.3  // 비용 성장률 40% 감소

- "multiplier": 1.5
+ "multiplier": 1.3

- "effect_per_level": 1.0
+ "effect_per_level": 2.0  // 효과 2배
```

#### start_gold_flat / start_gold_multi (시작 골드 보너스)
```diff
start_gold_flat:
- "growth_rate": 0.6
+ "growth_rate": 0.3  // 비용 성장률 50% 감소

- "multiplier": 1.5
+ "multiplier": 1.3

- "effect_per_level": 0.1
+ "effect_per_level": 0.3  // 효과 3배

start_gold_multi:
- "growth_rate": 0.8
+ "growth_rate": 0.4  // 비용 성장률 50% 감소

- "multiplier": 1.6
+ "multiplier": 1.4

- "effect_per_level": 0.1
+ "effect_per_level": 0.3  // 효과 3배
```

#### start_combo_flex / start_combo_damage (시작 콤보 보너스)
```diff
start_combo_flex:
- "growth_rate": 1.0
+ "growth_rate": 0.5  // 비용 성장률 50% 감소

- "multiplier": 1.8
+ "multiplier": 1.5

- "effect_per_level": 0.1
+ "effect_per_level": 0.3  // 효과 3배

start_combo_damage:
- "growth_rate": 1.0
+ "growth_rate": 0.5  // 비용 성장률 50% 감소

- "multiplier": 1.8
+ "multiplier": 1.5

- "effect_per_level": 0.5
+ "effect_per_level": 1.5  // 효과 3배
```

---

## 예상 결과

### 목표 지표

| 지표 | 변경 전 | 목표 |
|------|---------|------|
| Dominance Ratio | 1.96 | ≤ 1.3 |
| Balance Grade | D | B 이상 |
| 전략 다양성 | 단일 전략 압도 | 3개 이상 전략 경쟁 가능 |

### 전략별 예상 성과 변화

| 전략 | 변경 전 | 예상 변화 | 개선 근거 |
|------|---------|-----------|-----------|
| **Balanced** | 302 | 250-280 (-10~-15%) | attack_percent 감소 |
| **DamageFirst** | 154 | 200-230 (+30~+50%) | crit_damage 증가 |
| **SurvivalFirst** | 65 | 120-150 (+85~+130%) | time_extend 2배 |
| **Greedy** | 37 | 80-120 (+115~+220%) | 경제 관리 개선 |
| **CrystalFarm** | 35 | 90-130 (+155~+270%) | Starting Bonus 활용 |

### 검증 필요 사항

1. **재시뮬레이션 필수**
   - 50시간 플레이 테스트 재실행
   - Dominance Ratio가 1.3 이하로 개선되었는지 확인
   - 전략별 격차가 30% 이내인지 확인

2. **추가 조정 가능성**
   - SurvivalFirst가 여전히 약하면 time_extend를 1.0초/레벨까지 증가 고려
   - Balanced가 여전히 강하면 attack_percent를 1.8% 또는 1.5%로 추가 감소

3. **부작용 모니터링**
   - Starting Bonus 스탯이 과도하게 강해지지 않았는지
   - 초반 게임 진행이 너무 쉬워지지 않았는지
   - 후반 밸런스 붕괴 가능성

---

## 다음 단계

1. **즉시 실행:** C# 시뮬레이터로 50시간 테스트 재실행
   ```bash
   cd DeskWarrior.Simulator
   dotnet run -- --analyze --crystals 0 --game-hours 50 --cps 5
   ```

2. **결과 검증:** Dominance Ratio, 전략별 레벨, 시간별 추이 확인

3. **추가 조정:** 필요시 2차 밸런스 패치 적용

4. **플레이 테스트:** 실제 게임에서 체감 난이도 확인

---

**변경 파일:**
- `/Users/saintiron/Public/deskHero/config/PermanentStats.json`

**백업 권장:**
- 변경 전 config 파일 백업 필수
- 롤백 가능하도록 git commit 권장
