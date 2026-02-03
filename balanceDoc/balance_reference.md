# DeskWarrior 밸런스 레퍼런스 (Balance Reference)

생성일: 2026-02-03
버전: 1.0.0
기준: C# 코드베이스 (실제 게임 구현)

## 문서 목적

이 문서는 DeskWarrior 게임의 현재 구현된 모든 수치와 공식을 정리한 레퍼런스입니다.
밸런스 작업 시 "현재 상태"를 정확히 파악하기 위해 참조하세요.

---

## 1. 전투 시스템

### 1.1 데미지 계산 공식 (8단계)

**출처**: `Managers/DamageCalculator.cs` (Line 97-199)

데미지는 다음 8단계를 거쳐 계산됩니다:

```
① 기본 파워 분리
   pureBasePower = basePower - baseAttackBonus
   effectivePower = pureBasePower

② 공격력 퍼센트 배수 적용 (pureBasePower에만 적용)
   effectivePower = pureBasePower × (1 + attack_percent / 100)

③ 기본 공격력 가산
   effectivePower = ② + baseAttackBonus

④ 크리티컬 배수 (확률 적용)
   if (random < critChance):
       effectivePower = ③ × critMultiplier

⑤ 멀티히트 배수 (확률 적용)
   if (random < multiHitChance):
       effectivePower = ④ × 2

⑥ 콤보 배수 (리듬 발동 시)
   if (comboStack > 0):
       effectivePower = ⑤ × (1 + comboDamageBonus) × 2^comboStack

   콤보 스택별 배율:
   - Stack 1: ×2
   - Stack 2: ×4
   - Stack 3: ×8

⑦ 유틸리티 보너스
   utilityBonus = 1 + (timeExtendLevel + upgradeDiscountLevel) × 0.01
   effectivePower = ⑥ × utilityBonus

⑧ 저항 배율 적용 (속성별)
   resistanceModifier = 키보드 공격 시 KeyboardResistance, 마우스 공격 시 MouseResistance
   effectivePower = ⑦ × resistanceModifier

   최종 데미지 = (int)⑧
```

**파라미터 출처**:
- `basePower`: 키보드/마우스 공격력 (`GameManager.cs` Line 82-99)
- `attack_percent`: `config/PermanentStats.json` → attack_percent (effect_per_level: 2.0%)
- `baseAttackBonus`: `config/PermanentStats.json` → base_attack (effect_per_level: 3)
- `critChance`, `critMultiplier`: 1.2절 참조
- `multiHitChance`: 1.3절 참조
- `comboDamageBonus`, `comboStack`: 1.4절 참조

### 1.2 크리티컬 시스템

**출처**: `DamageCalculator.cs` (Line 122-135), `config/GameData.json` (Line 10-11)

**기본 크리티컬 확률**:
```
기본값: 10% (0.1)
출처: config/GameData.json → balance.critical_chance
```

**영구 스탯 보너스**:
```
추가 확률 = crit_chance 레벨 × 0.5%
출처: config/PermanentStats.json → crit_chance.effect_per_level
최대 효과: 90% (기본 10% + 90% = 100%)
```

**크리티컬 배율**:
```
기본 배율: 2.0배 (2x)
출처: config/GameData.json → balance.critical_multiplier

추가 배율 = crit_damage 레벨 × 0.2
출처: config/PermanentStats.json → crit_damage.effect_per_level
최대 효과: 무제한
```

**최종 크리티컬 계산**:
```
critChance = 0.1 + (CritChanceLevel × 0.005)
critMultiplier = 2.0 + (CritDamageLevel × 0.2)

if (Random.NextDouble() < critChance):
    데미지 ×= critMultiplier
```

### 1.3 멀티히트 시스템

**출처**: `DamageCalculator.cs` (Line 138-142), `config/PermanentStats.json` (Line 73-90)

**확률 계산**:
```
multiHitChance = MultiHitLevel × 1.0%
최대 확률: 100% (레벨 100)
```

**효과**:
```
if (Random.NextDouble() < multiHitChance / 100):
    데미지 ×= 2
```

**파라미터**:
| 항목 | 값 |
|------|-----|
| effect_per_level | 1.0% |
| max_effect | 100% |
| base_cost | 1 크리스탈 |
| growth_rate | 0.4 |
| multiplier | 1.3 |

### 1.4 콤보 시스템

**출처**: `Managers/ComboTracker.cs`, `DamageCalculator.cs` (Line 145-154)

**리듬 판정**:
```
기본 허용 오차: ±0.01초 (BASE_TOLERANCE)
콤보 유지 시간: 3초 (COMBO_EXPIRE_TIME)
최대 스택: 3

판정 공식:
tolerance = BASE_TOLERANCE + comboFlexBonus
intervalDiff = |현재 입력 간격 - 이전 입력 간격|

if (intervalDiff <= tolerance):
    comboStack = min(comboStack + 1, 3)
else:
    comboStack = 0 (리셋)
```

**데미지 배율**:
```
콤보 데미지 보너스: comboDamageBonus (영구 스탯)
스택별 배율: 2^comboStack

최종 콤보 배수 = (1 + comboDamageBonus) × 2^comboStack

Stack 1: (1 + bonus) × 2
Stack 2: (1 + bonus) × 4
Stack 3: (1 + bonus) × 8
```

### 1.5 저항 시스템 (속성별)

**출처**: `DamageCalculator.cs` (Line 164-182), `config/GameData.json` (Line 51-88)

**저항 배율** (데미지 감소):

| 속성 | 키보드 저항 | 마우스 저항 | 설명 |
|------|-------------|-------------|------|
| normal | 1.0 | 1.0 | 저항 없음 |
| fire | **0.67** | 1.0 | 키보드 공격 33% 감소 |
| ice | 1.0 | **0.67** | 마우스 공격 33% 감소 |
| wind | 1.0 | 1.0 | 저항 없음 |
| holy | 1.0 | 1.0 | 저항 없음 |
| dark | 1.0 | 1.0 | 저항 없음 |

**적용 방식**:
```
if (resistanceModifier < 1.0):
    isResisted = true
    effectivePower ×= resistanceModifier
```

---

## 2. 몬스터 시스템

### 2.1 HP 계산

**출처**: `Models/Monster.cs` (Line 285-310), `config/monsters/batch_01.json`

**기본 공식** (티어 시스템 비활성화 시):
```
MaxHp = BaseHp + (Level - 1) × HpGrowth
```

**속성 배율 적용**:
```
FinalHp = BaseHp × ElementModifier

속성 HP 배율:
- normal: 1.0
- fire: 1.0
- ice: 1.0
- wind: 0.8 (20% 감소)
- holy: 1.0
- dark: 1.5 (50% 증가)
```

**종족별 HP 배율** (추가):
```
FinalHp = BaseHp × SpeciesModifier × ElementModifier

예: 슬라임 Holy 속성
BaseHp = 20, HpModifier = 1.4, ElementModifier = 1.0
FinalHp = 20 × 1.4 × 1.0 = 28
```

**티어 시스템** (선택적, 현재 비활성화):
```
출처: config/GameData.json → balance.tier_hp_system
enabled: false

활성화 시:
tier = (level - 1) / tierInterval
tierMultiplier = TierMultiplier^tier
tierBaseHp = BaseHp × tierMultiplier
levelInTier = (level - 1) % tierInterval
linearIncrease = levelInTier × LinearGrowthPerLevel
FinalHp = tierBaseHp + linearIncrease

파라미터:
- tierInterval: 100
- tierMultiplier: 5.0
- linearGrowthPerLevel: 5
```

**Batch 1 몬스터 기본 스탯** (레벨 1 기준):

| 종족 | BaseHp | HpGrowth |
|------|--------|----------|
| slime | 20 | 5 |
| bat | 32 | 5 |
| skeleton | 44 | 5 |
| goblin | 56 | 5 |
| orc | 68 | 5 |
| ghost | 80 | 5 |
| golem | 92 | 5 |
| mushroom | 104 | 5 |
| spider | 116 | 5 |
| wolf | 128 | 5 |
| snake | 140 | 5 |
| boar | 152 | 5 |

### 2.2 골드 보상 계산

**출처**: `Models/Monster.cs` (Line 312-315), `GameManager.cs` (Line 485-496)

**기본 골드**:
```
BaseGold = MonsterBaseGold + Level × GoldGrowth
```

**영구 스탯 적용**:
```
① +가산 = BaseGold + gold_flat_perm
② ×배수 = ①× (1 + gold_multi_perm / 100)

최종 골드 = (int)②
```

**파라미터**:
| 스탯 | effect_per_level | 출처 |
|------|------------------|------|
| gold_flat_perm | +3 골드 | config/PermanentStats.json |
| gold_multi_perm | +15% | config/PermanentStats.json |

**Batch 1 몬스터 기본 골드** (레벨 1 기준):

| 종족 | BaseGold | GoldGrowth |
|------|----------|------------|
| slime | 10 | 2 |
| bat | 13 | 2 |
| skeleton | 16 | 2 |
| goblin | 19 | 2 |
| orc | 22 | 2 |
| ghost | 25 | 2 |
| golem | 28 | 2 |
| mushroom | 31 | 2 |
| spider | 34 | 2 |
| wolf | 37 | 2 |
| snake | 40 | 2 |
| boar | 43 | 2 |

**종족별 골드 배율** (속성별 추가):

예: 슬라임 Holy 속성
```
BaseGold = 10
GoldModifier = 3.2
FinalBaseGold = 10 × 3.2 = 32
```

### 2.3 속성별 수치 테이블

**출처**: `config/GameData.json` (Line 51-88)

| 속성 | HP 배율 | 시간 배속 | 키보드 저항 | 마우스 저항 | 등장 가중치 |
|------|---------|----------|-------------|-------------|-------------|
| normal | 1.0 | 1.0 | 1.0 | 1.0 | **100** |
| fire | 1.0 | 1.0 | **0.67** | 1.0 | 50 |
| ice | 1.0 | 1.0 | 1.0 | **0.67** | 50 |
| wind | **0.8** | **1.5** | 1.0 | 1.0 | 50 |
| holy | 1.0 | 1.0 | 1.0 | 1.0 | **10** |
| dark | **1.5** | 1.0 | 1.0 | 1.0 | 30 |

**시간 배속 적용**:
```
출처: GameManager.cs (OnTimerTick)
RemainingTime -= 0.1 × TimeScale

예: Wind 속성 몬스터
- 기본: 0.1초당 0.1초 감소
- Wind: 0.1초당 0.15초 감소 (1.5배 빠름)
```

---

## 3. 확률 시스템

### 3.1 몬스터 등장 확률

**출처**: `Managers/MonsterDataManager.cs` (Line 296-361), `config/GameData.json` (Line 40-47)

**가중치 기반 선택** (feature flag: use_weighted_selection = true):

```
최종 가중치 = 종족 가중치 × 속성 가중치 × 배치 가중치

확률 = 해당 몬스터 최종 가중치 / 전체 가중치 합
```

**Batch 1 기준 계산** (13종 × 6속성 = 78 몬스터):

| 속성 | 가중치 | 몬스터 수 | 총 가중치 | 등장 확률 |
|------|--------|----------|----------|----------|
| normal | 100 | 13 | 130,000 | **34.5%** |
| fire | 50 | 13 | 65,000 | 17.2% |
| ice | 50 | 13 | 65,000 | 17.2% |
| wind | 50 | 13 | 65,000 | 17.2% |
| holy | **10** | 13 | 13,000 | **3.4%** |
| dark | 30 | 13 | 39,000 | 10.3% |
| **합계** | - | **78** | **377,000** | **100%** |

**계산 예시**:
```
슬라임 (종족 가중치: 100) × Holy (속성 가중치: 10) × Batch1 (배치 가중치: 1.0)
= 1,000

전체 가중치 합 = 377,000
슬라임 Holy 등장 확률 = 1,000 / 377,000 = 0.265%
```

**레거시 모드** (use_weighted_selection = false):
```
순환 선택: 몬스터 리스트에서 (level - 1) % listCount 인덱스 선택
```

### 3.2 크리티컬 확률

**출처**: 1.2절 참조

```
기본 확률: 10%
최대 확률: 100% (crit_chance 레벨 180)

확률 계산:
critChance = 0.1 + (CritChanceLevel × 0.005)
```

---

## 4. 진행 시스템

### 4.1 시간 제한

**출처**: `config/GameData.json` (Line 9), `GameManager.cs`

**기본 제한 시간**:
```
30초
출처: config/GameData.json → balance.time_limit
```

**영구 스탯 시간 연장**:
```
추가 시간 = time_extend 레벨 × 0.4초
최대 효과: +60초 (기본 30초 + 60초 = 90초)
출처: config/PermanentStats.json → time_extend

레벨 150 투자 시 90초 제한
```

**속성별 시간 배속**:
```
실제 감소 = 0.1초 × TimeScale

normal, fire, ice, holy, dark: 1.0 (0.1초당 0.1초 감소)
wind: 1.5 (0.1초당 0.15초 감소, 1.5배 빠름)
```

### 4.2 영구 스탯 효과

**출처**: `config/PermanentStats.json`

**비용 공식**:
```
cost = base_cost × (1 + level × growth_rate) × multiplier^(level / softcap_interval)
```

**스탯 효과 테이블**:

| 스탯 ID | 레벨당 효과 | 최대 효과 | 설명 |
|---------|-------------|----------|------|
| base_attack | +3 | 무제한 | 모든 공격 데미지 가산 |
| attack_percent | +2% | 무제한 | 데미지 퍼센트 배수 (pureBasePower에만) |
| crit_chance | +0.5% | 90% | 크리티컬 확률 (기본 10% + 90%) |
| crit_damage | +0.2 | 무제한 | 크리티컬 배율 (기본 2.0 + bonus) |
| multi_hit | +1% | 100% | 2배 타격 확률 |
| gold_flat_perm | +3 | 무제한 | 골드 가산 |
| gold_multi_perm | +15% | 무제한 | 골드 배수 |
| crystal_flat | +10 | 무제한 | 보스 크리스탈 가산 |
| crystal_chance | +2% | 100% | 크리스탈 드롭 확률 |
| time_extend | +0.4초 | 60초 | 제한시간 연장 |
| upgrade_discount | +3% | 60% | 업그레이드 비용 할인 |
| start_level | +5 | 무제한 | 시작 레벨 |
| start_gold | +150 | 무제한 | 시작 골드 |
| start_keyboard | +2 | 무제한 | 시작 키보드 공격력 |
| start_mouse | +2 | 무제한 | 시작 마우스 공격력 |
| start_gold_flat | +0.3 | 무제한 | 시작 골드+ |
| start_gold_multi | +0.3% | 무제한 | 시작 골드* |
| start_combo_flex | +0.3 | 무제한 | 시작 콤보 유연성 |
| start_combo_damage | +1.5% | 무제한 | 시작 콤보 데미지 |

---

## 5. 보상 시스템

### 5.1 도감 보상

**출처**: `config/CollectionRewards.json`

**종족 완성 보상** (속성 6종 모두 처치):

| 종족 | 크리스탈 | 영구 보너스 |
|------|----------|-------------|
| slime | 50 | 해당 종족 골드 +1.0 |
| bat | 50 | 해당 종족 골드 +1.0 |
| skeleton | 50 | 해당 종족 골드 +1.0 |
| goblin | 50 | 해당 종족 골드 +1.0 |
| orc | 50 | 해당 종족 골드 +1.0 |
| ghost | 50 | 해당 종족 골드 +1.0 |
| golem | 50 | 해당 종족 골드 +1.0 |
| mushroom | 50 | 해당 종족 골드 +1.0 |
| spider | 50 | 해당 종족 골드 +1.0 |
| wolf | 50 | 해당 종족 골드 +1.0 |
| snake | 50 | 해당 종족 골드 +1.0 |
| boar | 50 | 해당 종족 골드 +1.0 |

**배치 완성 보상**:

| 배치 | 필요 수 | 크리스탈 | 영구 보너스 |
|------|---------|----------|-------------|
| Batch 1 | 78종 | **500** | 전체 골드 +5.0% |

**마일스톤 보상**:

| 마일스톤 | 크리스탈 | 조건 |
|----------|----------|------|
| first_holy | 10 | 첫 Holy 속성 처치 |
| first_dark | 15 | 첫 Dark 속성 처치 |
| all_elements_unlocked | 100 | 모든 속성 최소 1마리 처치 |
| complete_all | **5000** | 전체 도감 완성 |

### 5.2 크리스탈 획득

**보스 드롭**:
```
기본 드롭 확률: (현재 코드에 명시되지 않음, 추가 확인 필요)
드롭량: 10 크리스탈 (추정)

영구 스탭 보너스:
- crystal_flat: +10 크리스탈/레벨
- crystal_chance: +2% 확률/레벨 (최대 100%)
```

**업적 보상**:
```
도감 마일스톤: 위 5.1절 참조
```

---

## 6. 인게임 업그레이드

### 6.1 업그레이드 비용 시스템

**출처**: `GameManager.cs` (Line 323-351)

**기본 비용**:
```
출처: config/GameData.json → upgrade
base_cost: 100
cost_multiplier: 1.5

레거시 공식:
upgradeCost = base_cost × cost_multiplier^(currentLevel - 1)
```

**스테이지 구간별 배율**:
```
interval = 50 (업그레이드 비용 구간)
tier = (CurrentLevel - 1) / interval
multiplier = 2^tier

최종 비용 = baseCost × multiplier

예:
- 레벨 1-50: 배율 1배
- 레벨 51-100: 배율 2배
- 레벨 101-150: 배율 4배
- 레벨 151-200: 배율 8배
```

**영구 스탯 할인**:
```
discount = upgrade_discount 레벨 × 3%
최대 할인: 60%

할인 적용 비용 = baseCost × (1 - discount / 100)
```

### 6.2 인게임 스탯

**출처**: `Managers/StatGrowthManager.cs`, `config/InGameStatGrowth.json`

**키보드/마우스 공격력**:
```
effect_per_level: (config 파일에서 로드, 추가 확인 필요)

최종 공격력 = 1 + inGameLevel × effect + base_attack (영구 스탯)
```

---

## 부록: 공식 출처

### 코드 파일 인덱스

| 시스템 | 파일 경로 | 주요 로직 |
|--------|----------|----------|
| 데미지 계산 | `Managers/DamageCalculator.cs` | 8단계 데미지 공식 (Line 97-199) |
| 몬스터 HP | `Models/Monster.cs` | HP 계산 (Line 285-310) |
| 골드 보상 | `GameManager.cs` | 골드 공식 (Line 485-496) |
| 몬스터 등장 | `Managers/MonsterDataManager.cs` | 가중치 선택 (Line 296-361) |
| 콤보 시스템 | `Managers/ComboTracker.cs` | 리듬 판정 (Line 55-104) |
| 영구 스탯 | `Managers/PermanentProgressionManager.cs` | 스탯 적용 |

### 설정 파일 인덱스

| 데이터 | 파일 경로 | 설명 |
|--------|----------|------|
| 게임 상수 | `config/GameData.json` | 밸런스, 속성, 가중치 |
| 영구 스탯 | `config/PermanentStats.json` | 효과, 비용, 성장률 |
| 몬스터 데이터 | `config/monsters/batch_01.json` | HP, 골드, 속성 배율 |
| 도감 보상 | `config/CollectionRewards.json` | 크리스탈, 영구 보너스 |
| 인게임 성장 | `config/InGameStatGrowth.json` | 업그레이드 효과 |

---

## 변경 이력

- 2026-02-03: 초기 버전 생성 (v1.0.0)
  - 전투 시스템 (8단계 데미지 공식)
  - 몬스터 시스템 (HP, 골드, 속성)
  - 확률 시스템 (등장 확률, 크리티컬)
  - 진행 시스템 (시간, 영구 스탯)
  - 보상 시스템 (도감, 크리스탈)
  - 인게임 업그레이드 (비용, 할인)
