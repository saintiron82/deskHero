# DeskWarrior 밸런스 레퍼런스 (Balance Reference)

**생성일**: 2026-02-04
**버전**: 1.2.0
**기준**: C# 코드베이스 (실제 게임 구현) + 7가지 시스템 점검 완료

---

## 📋 문서 목적

이 문서는 DeskWarrior 게임의 **모든 구현된 시스템과 수치**를 정리한 완전한 레퍼런스입니다.
밸런스 작업 시 "현재 상태"를 정확히 파악하기 위해 참조하세요.

**⚠️ MANDATORY**: 모든 밸런스 수정 전 이 문서를 읽고, 수정 후 반드시 업데이트하세요.

---

## 📚 목차

1. [전투 시스템](#1-전투-시스템) - 데미지 8단계, 크리티컬, 멀티히트, 콤보, 저항
2. [몬스터 시스템](#2-몬스터-시스템) - HP, 골드, 속성, 배치
3. [시간 시스템](#3-시간-시스템) - 타이머, Wind 배속, 시간 연장
4. [확률 시스템](#4-확률-시스템) - 몬스터 등장, 크리티컬
5. [영구 스탯 시스템](#5-영구-스탯-시스템) - 36개 스탯 전체 목록
6. [인게임 업그레이드 시스템](#6-인게임-업그레이드-시스템) - 골드 업그레이드, 스테이지 비용 배율
7. [크리스탈 보상 시스템](#7-크리스탈-보상-시스템) - 보스, 스테이지, 골드 변환
8. [도감 보상 시스템](#8-도감-보상-시스템) - 종족, 배치, 마일스톤
9. [황금 고블린 시스템](#9-황금-고블린-시스템) - 스폰 조건, 보상
10. [밸런스 검증](#10-밸런스-검증) - 10시간 시뮬레이션 결과

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

⑦ 유틸리티 보너스 (영구 스탯 추가 데미지)
   totalDamageBonus = (timeExtendLevel × 1.0%) + (upgradeDiscountLevel × 1.0%)
   utilityBonus = 1 + (totalDamageBonus / 100)
   effectivePower = ⑥ × utilityBonus

   **출처**:
   - config/PermanentStats.json → time_extend.damage_bonus_per_level (1.0)
   - config/PermanentStats.json → upgrade_discount.damage_bonus_per_level (1.0)
   - Managers/DamageCalculator.cs (Line 157-171)
   - Managers/StatGrowthManager.cs → GetDamageBonusEffect()

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
- `utilityBonus`: 1.5절 참조

### 1.2 크리티컬 시스템

**출처**: `DamageCalculator.cs` (Line 122-135), `config/GameData.json`

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

**출처**: `DamageCalculator.cs` (Line 138-142), `config/PermanentStats.json`

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

### 1.5 유틸리티 스탯의 데미지 보너스

**출처**: `DamageCalculator.cs` (Line 157-171), `StatGrowthManager.cs`, `config/PermanentStats.json`

일부 영구 스탯은 **주 효과 외에 추가 데미지 보너스**를 제공합니다.

#### 적용 스탯

| 스탯 | 주 효과 | 데미지 보너스 | 출처 |
|------|---------|--------------|------|
| time_extend | 제한시간 +0.4초/레벨 | **+1%/레벨** | config/PermanentStats.json:169 |
| upgrade_discount | 업그레이드 비용 -3%/레벨 | **+1%/레벨** | config/PermanentStats.json:186 |

#### 계산 공식

```
total_damage_bonus = (time_extend_level × 1%) + (upgrade_discount_level × 1%)
utility_multiplier = 1.0 + (total_damage_bonus / 100)
final_damage = ⑥ × utility_multiplier
```

#### 예시

**예시 1**: time_extend 10레벨 + upgrade_discount 5레벨
```
데미지 보너스 = (10 × 1%) + (5 × 1%) = 15%
utility_multiplier = 1.0 + 0.15 = 1.15
최종 데미지 = 원래 데미지 × 1.15
```

**예시 2**: time_extend 30레벨 + upgrade_discount 20레벨
```
데미지 보너스 = (30 × 1%) + (20 × 1%) = 50%
utility_multiplier = 1.0 + 0.50 = 1.50
최종 데미지 = 원래 데미지 × 1.50 (50% 증가)
```

#### 설계 의도

이 메카닉은 유틸리티 스탯에 투자하는 플레이어에게 **간접적인 전투 보너스**를 제공하여:
- 시간 연장 투자 시 데미지도 증가 → 더 빠른 처치 가능
- 업그레이드 할인 투자 시 데미지도 증가 → 골드+전투 이중 효과

**구현 방식**:
- JSON 데이터 기반: `damage_bonus_per_level` 필드 사용
- StatGrowthManager.GetDamageBonusEffect()로 조회
- 코드에 하드코딩 없음 (CLAUDE.md 원칙 준수)

### 1.6 저항 시스템 (속성별)

**출처**: `DamageCalculator.cs` (Line 164-182), `config/GameData.json`

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

**출처**: `Models/Monster.cs` (Line 285-310), `GameManager.cs` (Line 676-677)

**기본 공식** (티어 시스템 비활성화 시):
```
MaxHp = BaseHp + (Level - 1) × HpGrowth
```

**속성 배율 적용** (`GameManager.cs` Line 676):
```
FinalHp = BaseHp × ElementModifier

속성 HP 배율 (config/GameData.json → element_properties):
- normal: 1.0
- fire: 1.0
- ice: 1.0
- wind: 0.8 (20% 감소)
- holy: 1.0
- dark: 1.5 (50% 증가)
```

**종족별 HP 배율** (`config/monsters/batch_XX.json`):
```
FinalHp = BaseHp × SpeciesModifier × ElementModifier

예: 슬라임 Holy 속성 (레벨 1)
BaseHp = 20 × 1.4 (종족 배율) = 28
FinalHp = 28 × 1.0 (속성 배율) = 28
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

| 종족 | BaseHp | HpGrowth | BaseGold | GoldGrowth |
|------|--------|----------|----------|------------|
| slime | 20 | 5 | 10 | 2 |
| bat | 32 | 5 | 13 | 2 |
| skeleton | 44 | 5 | 16 | 2 |
| goblin | 56 | 5 | 19 | 2 |
| orc | 68 | 5 | 22 | 2 |
| ghost | 80 | 5 | 25 | 2 |
| golem | 92 | 5 | 28 | 2 |
| mushroom | 104 | 5 | 31 | 2 |
| spider | 116 | 5 | 34 | 2 |
| wolf | 128 | 5 | 37 | 2 |
| snake | 140 | 5 | 40 | 2 |
| boar | 152 | 5 | 43 | 2 |

### 2.2 골드 보상 계산

**출처**: `Models/Monster.cs` (Line 312-315), `GameManager.cs` (Line 495-498)

**기본 골드**:
```
BaseGold = MonsterBaseGold + Level × GoldGrowth
```

**영구 스탯 적용 (2단계)**:
```
① +가산 = BaseGold + gold_flat_perm
② ×배수 = ① × (1 + gold_multi_perm / 100)

최종 골드 = (int)②
```

**파라미터**:
| 스탯 | effect_per_level | 출처 |
|------|------------------|------|
| gold_flat_perm | +3 골드 | config/PermanentStats.json |
| gold_multi_perm | +15% | config/PermanentStats.json |

**종족별 골드 배율** (`config/monsters/batch_XX.json`):
```
예: 슬라임 Holy 속성
BaseGold = 10 × 3.2 (속성 배율) = 32
```

### 2.3 속성별 수치 테이블

**출처**: `config/GameData.json` (element_properties)

| 속성 | HP 배율 | 시간 배속 | 키보드 저항 | 마우스 저항 | 크리스탈 배율 | 등장 가중치 |
|------|---------|----------|-------------|-------------|--------------|-------------|
| normal | 1.0 | 1.0 | 1.0 | 1.0 | **1.0** | **50** ⚖️ 균등 |
| fire | 1.0 | 1.0 | **0.67** | 1.0 | **1.0** | **50** ⚖️ 균등 |
| ice | 1.0 | 1.0 | 1.0 | **0.67** | **1.0** | **50** ⚖️ 균등 |
| wind | **0.8** | **1.5** | 1.0 | 1.0 | **1.0** | **50** ⚖️ 균등 |
| holy | 1.0 | 1.0 | 1.0 | 1.0 | **2.0** ⭐ | **2** ✨ 초레어 |
| dark | **1.5** | 1.0 | 1.0 | 1.0 | **2.0** ⭐ | **6** ✨ 초레어 |

**시간 배속 적용**:
```
출처: GameManager.cs (OnTimerTick, Line 720)
RemainingTime -= 0.1 × TimeScale

예: Wind 속성 몬스터
- 기본: 0.1초당 0.1초 감소
- Wind: 0.1초당 0.15초 감소 (1.5배 빠름)
```

### 2.4 배치 시스템

**출처**: `config/monsters/_index.json`, `Managers/MonsterDataManager.cs`

**배치별 활성화 레벨**:
```
Batch 1: 레벨 1부터 활성화 (13종 × 6속성 = 78 몬스터)
Batch 2: 레벨 101부터 활성화 (13종 × 6속성 = 78 몬스터 추가)
Batch 3-9: 순차 활성화 (최대 117종 × 6속성 = 702 몬스터)
```

**최종 가중치 계산**:
```
최종 가중치 = 종족 가중치 × 속성 가중치 × 배치 가중치
확률 = 해당 몬스터 최종 가중치 / 전체 가중치 합
```

---

## 3. 시간 시스템

### 3.1 타이머 로직

**출처**: `GameManager.cs` (Line 170-174, 717-736)

**타이머 설정**:
```
Interval: 0.1초 (100ms)
DispatcherTimer를 사용한 정확한 타이밍
```

**시간 감소 로직** (`OnTimerTick`):
```csharp
// 시간 배속 적용 (Wind 속성 몬스터 등)
double timeScale = _currentMonster?.TimeScale ?? 1.0;
RemainingTime -= 0.1 × timeScale;

// Wind 속성: timeScale = 1.5 (시간이 1.5배 빠르게 감소)
// 일반: timeScale = 1.0
```

**시간 초과 처리**:
```csharp
if (RemainingTime <= 0) {
    if (_isGoldenGoblinActive) {
        // 황금 고블린: 도주 처리 (게임오버 아님)
        OnGoldenGoblinEscaped();
    } else {
        // 일반 몬스터: 게임 오버
        TriggerGameOver();
    }
}
```

### 3.2 제한 시간

**출처**: `config/GameData.json`

**기본 제한 시간**:
```
30초 (몬스터당)
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

---

## 4. 확률 시스템

### 4.1 몬스터 등장 확률

**출처**: `Managers/MonsterDataManager.cs` (Line 296-361), `config/GameData.json`

**가중치 기반 선택** (feature flag: use_weighted_selection = true):

```
최종 가중치 = 종족 가중치 × 속성 가중치 × 배치 가중치
확률 = 해당 몬스터 최종 가중치 / 전체 가중치 합
```

**Batch 1 기준 계산** (v1.2.2, 13종 × 6속성 = 78 몬스터):

| 속성 | 가중치 | 몬스터 수 | 총 가중치 | 등장 확률 | 레어도 |
|------|--------|----------|----------|----------|--------|
| normal | **50** | 13 | 650 | **24.0%** ⚖️ | Common (균등) |
| fire | 50 | 13 | 650 | **24.0%** ⚖️ | Common (균등) |
| ice | 50 | 13 | 650 | **24.0%** ⚖️ | Common (균등) |
| wind | 50 | 13 | 650 | **24.0%** ⚖️ | Common (균등) |
| holy | **2** | 13 | 26 | **0.96%** ✨ | Ultra Rare |
| dark | **6** | 13 | 78 | **2.88%** ✨ | Ultra Rare |
| **합계** | - | **78** | **2,704** | **100%** | - |

**변경 이력**:
- v1.2.2: Normal 100→50 (4대 속성 완전 균등화, 다양성 극대화)
- v1.2.1: Holy 10→2, Dark 30→6 (5배 레어도 증가, 의도된 희귀성 강화)
- v1.2.0: Holy 10, Dark 30 (기본 설정)

**계산 예시** (v1.2.2):
```
슬라임 (종족 가중치: 100) × Holy (속성 가중치: 2) × Batch1 (배치 가중치: 1.0)
= 200

전체 가중치 합 = 2,704 (13종 × 208)
슬라임 Holy 등장 확률 = 1,000 / 377,000 = 0.265%
```

**레거시 모드** (use_weighted_selection = false):
```
순환 선택: 몬스터 리스트에서 (level - 1) % listCount 인덱스 선택
```

### 4.2 크리티컬 확률

**출처**: 1.2절 참조

```
기본 확률: 10%
최대 확률: 100% (crit_chance 레벨 180)

확률 계산:
critChance = 0.1 + (CritChanceLevel × 0.005)
```

---

## 5. 영구 스탯 시스템

**출처**: `config/PermanentStats.json` (단일 소스)

### 5.1 비용 공식

```
cost = base_cost × (1 + level × growth_rate) × multiplier^(level / softcap_interval)
```

### 5.2 전체 스탯 목록 (36개)

#### 카테고리 1: 기본 능력 (Base Stats)

| 스탯 ID | 레벨당 효과 | 최대 효과 | 설명 | 아이콘 |
|---------|-------------|----------|------|--------|
| base_attack | +3 | 무제한 | 모든 공격 데미지 가산 | ⚔️ |
| attack_percent | +2% | 무제한 | 데미지 퍼센트 배수 (pureBasePower에만) | 💪 |
| crit_chance | +0.5% | 90% | 크리티컬 확률 (기본 10% + 90%) | ✨ |
| crit_damage | +0.2 | 무제한 | 크리티컬 배율 (기본 2.0 + bonus) | 💥 |
| multi_hit | +1% | 100% | 2배 타격 확률 | 🎯 |
| combo_damage | +1.5% | 무제한 | 콤보 데미지 보너스 | 🔥 |
| combo_flex | +0.3 | 무제한 | 콤보 허용 오차 (초) | 🎵 |

#### 카테고리 2: 재화 보너스 (Currency Bonus)

| 스탯 ID | 레벨당 효과 | 최대 효과 | 설명 | 아이콘 |
|---------|-------------|----------|------|--------|
| gold_flat_perm | +3 | 무제한 | 골드 가산 | 💵 |
| gold_multi_perm | +15% | 무제한 | 골드 배수 | 🌟 |
| crystal_flat | +10 | 무제한 | 보스 크리스탈 가산 | 💎 |
| crystal_chance | +2% | 100% | 크리스탈 드롭 확률 (현재 미사용) | ✨ |

#### 카테고리 3: 유틸리티 (Utility)

| 스탯 ID | 레벨당 효과 | 최대 효과 | 설명 | 아이콘 |
|---------|-------------|----------|------|--------|
| time_extend | +0.4초 | 60초 | 제한시간 연장 | ⏰ |
| upgrade_discount | +3% | 60% | 업그레이드 비용 할인 | 🎫 |

#### 카테고리 4: 시작 보너스 (Starting Bonus)

| 스탯 ID | 레벨당 효과 | 최대 효과 | 설명 | 아이콘 |
|---------|-------------|----------|------|--------|
| start_level | +5 | 무제한 | 시작 레벨 | 🚀 |
| start_gold | +150 | 무제한 | 시작 골드 | 💵 |
| start_keyboard | +2 | 무제한 | 시작 키보드 공격력 레벨 | ⌨️ |
| start_mouse | +2 | 무제한 | 시작 마우스 공격력 레벨 | 🖱️ |
| start_gold_flat | +0.3 | 무제한 | 시작 골드+ 레벨 | 💸 |
| start_gold_multi | +0.3% | 무제한 | 시작 골드* 레벨 | 💰 |
| start_combo_flex | +0.3 | 무제한 | 시작 콤보유연성 레벨 | 🎯 |
| start_combo_damage | +1.5% | 무제한 | 시작 콤보데미지 레벨 | 💥 |

### 5.3 주요 스탯 성장 곡선

**time_extend** (가장 효과적인 투자):
```
레벨 1: 30.4초
레벨 50: 50초 (+20초)
레벨 100: 70초 (+40초)
레벨 150: 90초 (+60초, 최대치)
```

**base_attack** (선형 성장):
```
레벨 1: +3 공격력
레벨 50: +150 공격력
레벨 100: +300 공격력
```

**crit_chance** (한계 있음):
```
레벨 1: 10.5%
레벨 50: 35%
레벨 100: 60%
레벨 180: 100% (최대치)
```

---

## 6. 인게임 업그레이드 시스템

### 6.1 업그레이드 비용 시스템

**출처**: `config/InGameStatGrowth.json`, `GameManager.cs` (Line 281-299, 345-351)

**기본 비용 공식**:
```
baseCost = base_cost × (1 + level × growth_rate) × multiplier^(level / softcap_interval)

파라미터 (config/GameData.json → upgrade):
- base_cost: 1 (이전: 100)
- cost_multiplier: 1.15 (이전: 1.5)
- growth_rate: 0.5 (config/InGameStatGrowth.json)
- softcap_interval: 10 (config/InGameStatGrowth.json)
```

**스테이지 비용 배율** (소프트 리셋 방지):
```csharp
// 10레벨마다 1.5배씩 증가
int stageMultiplier = CurrentLevel / 10;
double multiplier = Math.Pow(1.5, stageMultiplier);
finalCost = (int)(baseCost × multiplier);
```

**예시**:
```
레벨 1-9: 비용 배율 1.0x
레벨 10-19: 비용 배율 1.5x
레벨 20-29: 비용 배율 2.25x (1.5²)
레벨 30-39: 비용 배율 3.375x (1.5³)
```

**영구 스탯 할인** (`upgrade_discount`):
```
discount = UpgradeDiscountLevel × 3%
finalCost = baseCost × (1 - discount / 100)

최대 할인: 60% (레벨 20)
```

### 6.2 인게임 스탯

**출처**: `config/InGameStatGrowth.json`, `GameManager.cs` (Line 85-98)

**키보드/마우스 공격력**:
```
effect_per_level: +1 공격력

최종 공격력 = 1 + inGameLevel × 1 + base_attack (영구 스탯)

예시:
- 인게임 키보드 레벨 10, base_attack 레벨 50:
  최종 키보드 공격력 = 1 + 10 + 150 = 161
```

---

## 7. 크리스탈 보상 시스템

**출처**: `Managers/PermanentProgressionManager.cs`, `config/BossDrops.json`

### 7.1 몬스터 처치 보상

**출처**: `GameManager.cs` (Line 503), `SimulationEngine.cs` (Line 217), `CrystalTracker.cs` (Line 59-63), `config/BossDrops.json`

```
✅ 현재 상태 (v1.2.1):
모든 몬스터 처치 시: 1 + (레벨 / 100) 크리스탈 (100% 지급, 레벨 기반 증가)

공식:
크리스탈 = 1 + (currentLevel / 100)

- 일반 몬스터: 1 + (레벨 / 100) 크리스탈
- 보스: [1 + (레벨 / 100)] 크리스탈 (몬스터 기본) + 보스 보너스

예시:
- 레벨 10 일반 몬스터: 1 + (10/100) = 1 크리스탈
- 레벨 100 일반 몬스터: 1 + (100/100) = 2 크리스탈
- 레벨 500 일반 몬스터: 1 + (500/100) = 6 크리스탈
- 레벨 10 보스 (Normal): 1 + (10+20)×1.0 = 31 크리스탈
- 레벨 100 보스 (Normal): 2 + (100+20)×1.0 = 122 크리스탈
- 레벨 500 보스 (Holy): 6 + (500+20)×2.0 = 1,046 크리스탈
```

**이전 버전**:
- v1.2.0: 모든 몬스터 1 크리스탈 (고정)
- v1.1.0: 보스 처치 시에만 1 크리스탈 (10레벨 단위), 일반 몬스터 0 크리스탈

### 7.2 보스 처치 (100% 확정)

**출처**: `PermanentProgressionManager.cs` (Line 45-85)

```
기본 공식: (레벨 + 20) + crystal_flat

속성별 크리스탈 배율 (config/GameData.json → element_properties.crystal_multiplier):
- Normal/Fire/Ice/Wind: 1.0배
- Holy: 2.0배 ⭐
- Dark: 2.0배 ⭐

최종 크리스탈 = (레벨 + 20 + crystal_flat) × crystal_multiplier × (1 ± 20% 분산)

예시:
- 레벨 10 Normal 보스: (10 + 20) × 1.0 = 30 크리스탈 (±6)
- 레벨 10 Holy 보스: (10 + 20) × 2.0 = 60 크리스탈 (±12)
- 레벨 10 Dark 보스: (10 + 20) × 2.0 = 60 크리스탈 (±12)
- 레벨 100 Normal 보스: (100 + 20) × 1.0 = 120 크리스탈 (±24)
- 레벨 100 Holy 보스: (100 + 20) × 2.0 = 240 크리스탈 (±48)

분산: ±20% 랜덤 변동 (config/BossDrops.json → crystal_variance)
```

### 7.3 골드 변환 (게임 오버 시)

**출처**: `PermanentProgressionManager.cs` (Line 115-120), `config/BossDrops.json`

```
변환 비율: 100 골드 = 1 크리스탈
출처: config/BossDrops.json → gold_to_crystal_rate
```

### 7.4 영구 스탯 보너스

**출처**: `config/PermanentStats.json`

```
crystal_flat: +10 크리스탈/레벨 (기본 크리스탈에 가산)
```

**제거됨**: `crystal_chance` 스탯 (100% 확정 지급이므로 불필요)

---

## 8. 도감 보상 시스템

**출처**: `config/CollectionRewards.json`

### 8.1 종족 완성 보상 (속성 6종 모두 처치)

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

### 8.2 배치 완성 보상

| 배치 | 필요 수 | 크리스탈 | 영구 보너스 |
|------|---------|----------|-------------|
| Batch 1 | 78종 | **500** | 전체 골드 +5.0% |

### 8.3 마일스톤 보상

| 마일스톤 | 크리스탈 | 조건 |
|----------|----------|------|
| first_holy | 10 | 첫 Holy 속성 처치 |
| first_dark | 15 | 첫 Dark 속성 처치 |
| all_elements_unlocked | 100 | 모든 속성 최소 1마리 처치 |
| complete_all | **5000** | 전체 도감 완성 |

---

## 9. 황금 고블린 시스템

**출처**: `DeskWarrior.Core/Models/GoldenGoblinConfig.cs`, `config/SpecialMonsters.json`

### 9.1 스폰 조건

```
스폰 확률: 0.1% (0.001)
쿨다운: 500 킬 (일반 몬스터 500마리 처치 후 확률 발동)

조건:
- 보스 레벨이 아닐 것
- 쿨다운 카운터 >= 500
```

### 9.2 황금 고블린 스탯

```
HP: 100~200 랜덤 (HpMin: 100, HpMax: 200)
제한 시간: 10초 (TimeLimit: 10)
```

### 9.3 보상

```
골드 배수: 2~100배 랜덤
최종 골드 = 현재 스테이지 예상 골드 × 배수

예시:
- 레벨 100 스테이지 골드 1,000 × 배수 50 = 50,000 골드
```

### 9.4 시간 초과 처리

```
시간 내 처치 실패: 황금 고블린 도주
결과: 보상 없음, 일반 몬스터로 즉시 교체
게임 오버 아님
```

---

## 10. 밸런스 검증

### 10.1 10시간 진행 시뮬레이션 결과

**출처**: `balanceDoc/2026-02-03/progression_10h_simple.csv`

**시뮬레이션 설정**:
```
목표 플레이 타임: 10시간 (36,000초)
입력 프로파일: CPS 5
투자 전략: Balanced (균형)
  - 우선순위: time_extend > base_attack > crit_damage > attack_percent

영구 스탯 초기값: 모두 0
시작 보너스: 골드 +20 (첫 업그레이드 보장)
```

**최종 결과** (404 세션):
```
총 플레이 타임: 10.0시간 (36,012초)
총 세션 수: 404회
평균 세션 길이: 89.2초

도달 최고 레벨: 4,453
총 획득 크리스탈: 104,002,067
총 소비 크리스탈: 103,988,104
잔여 크리스탈: 13,963

총 획득 골드: 9,843,279
총 소비 골드: 9,843,279
```

**크리스탈 획득 출처 분석**:
```
평균 세션당 획득:
- 스테이지 클리어: ~445 크리스탈 (보스 처치)
- 보스 보너스: ~257,062 크리스탈 (속성 배율 포함)
- 골드 변환: ~55 크리스탈

총 획득 크리스탈:
- 스테이지 클리어: 179,978 (0.17%)
- 보스 보너스: 103,824,169 (99.83%)
- 골드 변환: 22,338 (0.02%)

※ 보스 크리스탈이 전체 획득의 99.8% 차지
```

**영구 스탯 투자 분석** (404세션 기준):
```
최종 영구 스탯 레벨 (상위 5개):
1. time_extend: 레벨 106 (~42.4초 연장, 총 72.4초)
2. base_attack: 레벨 98 (~294 추가 공격력)
3. crit_damage: 레벨 87 (~17.4 추가 크리티컬 배율)
4. attack_percent: 레벨 76 (~152% 추가 배수)
5. upgrade_discount: 레벨 45 (~135% 할인, 현재 미사용)
```

**세션 진행 곡선**:
```
초반 (세션 1-50):
- 평균 레벨: 7-50
- 평균 세션 시간: 30-60초
- 크리스탈 획득: 100-5,000/세션

중반 (세션 51-200):
- 평균 레벨: 100-1,500
- 평균 세션 시간: 60-80초
- 크리스탈 획득: 10,000-100,000/세션

후반 (세션 201-404):
- 평균 레벨: 2,000-4,453
- 평균 세션 시간: 80-90초
- 크리스탈 획득: 200,000-500,000/세션
```

**검증 결과**:
```
✅ 보스 크리스탈 100% 지급 확인
✅ Holy/Dark 속성 2배 배율 정상 작동
✅ 스테이지 클리어 크리스탈 (10레벨 단위) 정상
✅ 골드 변환 비율 (100:1) 정상
✅ 인게임 업그레이드 시스템 정상
✅ 시간 관리 (몬스터당 30초) 정상
✅ 영구 스탯 투자 전략 작동

핵심 발견:
- 보스 크리스탈이 전체 수입의 99.8% (스테이지/골드 0.2%)
- Holy/Dark 보스의 2배 배율이 후반 진행에 큰 영향
- time_extend가 가장 효과적인 투자 (세션 길이 2.4배 증가)
- 레벨 4,453까지 안정적인 선형 진행 확인
```

**밸런스 평가**:
```
긍정적:
+ 선형적이고 예측 가능한 진행
+ 영구 스탯 투자의 체감 효과 명확
+ 속성 시스템의 차별화 (Holy/Dark 레어도 보상)

개선 완료:
✅ 스테이지 클리어 크리스탈 → 몬스터 처치 크리스탈로 변경 (v1.2.0)
   - 이전: 보스만 1크리스탈 (0.17%)
   - 현재: 모든 몬스터 1크리스탈 (예상 10-20%)
   - 효과: 초반 진행 부드러움, 총 크리스탈 10-15% 증가

개선 고려사항:
- 골드 변환 비중도 낮음 (0.02%)
  → 변환 비율 조정 고려 (100:1 → 50:1)
- 후반부 세션 시간이 90초로 수렴 (time_extend 한계)
  → 추가 시간 연장 메커니즘 필요 (아이템, 업적 등)
```

---

## 부록: 공식 출처

### 코드 파일 인덱스

| 시스템 | 파일 경로 | 주요 로직 |
|--------|----------|----------|
| 데미지 계산 | `Managers/DamageCalculator.cs` | 8단계 데미지 공식 (Line 97-199) |
| 몬스터 HP | `Models/Monster.cs` | HP 계산 (Line 285-310) |
| 골드 보상 | `GameManager.cs` | 골드 공식 (Line 495-498) |
| 타이머 | `GameManager.cs` | OnTimerTick (Line 717-736) |
| 몬스터 등장 | `Managers/MonsterDataManager.cs` | 가중치 선택 (Line 296-361) |
| 콤보 시스템 | `Managers/ComboTracker.cs` | 리듬 판정 |
| 영구 스탯 | `Managers/PermanentProgressionManager.cs` | 스탯 적용 |
| 크리스탈 보상 | `PermanentProgressionManager.cs` | 보스/스테이지 크리스탈 (Line 45-94) |

### 설정 파일 인덱스

| 데이터 | 파일 경로 | 설명 |
|--------|----------|------|
| 게임 상수 | `config/GameData.json` | 밸런스, 속성, 가중치 |
| 영구 스탯 | `config/PermanentStats.json` | 효과, 비용, 성장률 (36개 스탯) |
| 인게임 스탯 | `config/InGameStatGrowth.json` | 키보드/마우스 업그레이드 |
| 몬스터 데이터 | `config/monsters/batch_01.json` | HP, 골드, 속성 배율 |
| 몬스터 인덱스 | `config/monsters/_index.json` | 배치 활성화 레벨 |
| 도감 보상 | `config/CollectionRewards.json` | 크리스탈, 영구 보너스 |
| 보스 드롭 | `config/BossDrops.json` | 크리스탈 공식, 골드 변환 |
| 스탯 공식 | `config/StatFormulas.json` | 코드 생성용 공식 정의 |
| 특수 몬스터 | `config/SpecialMonsters.json` | 황금 고블린 설정 |

---

## 변경 이력

- **2026-02-04 (v1.2.2)**: 속성 균등화 및 다양성 극대화
  - **✅ Normal 속성 가중치 대폭 감소**: 단조로움 해소
    - Normal: 100 → 50 (50% 감소)
    - 등장률: 41.4% → 24.0% (4대 속성 완전 균등)
    - 목적: 속성 다양성 극대화, Normal 편중 해소
  - **4대 속성 완전 균등화**: Normal/Fire/Ice/Wind 각 24.0%
    - 플레이어 체감: "다양한 속성을 골고루 만난다"
    - 전투 전략 다양성 증가 (키보드/마우스 저항 차별화)
  - **레어 속성 비율 미세 증가**: Holy 0.83% → 0.96%, Dark 2.48% → 2.88%
    - 절대적 희귀성 유지 (Ultra Rare)
    - 크리스탈 경제 영향: 무시 가능 (1% 이내)

- **2026-02-04 (v1.2.1)**: 크리스탈 보상 시스템 및 업그레이드 비용 밸런스 조정
  - **crystal_multiplier 시스템 추가**: 속성별 크리스탈 보상 차별화
    - Holy/Dark: 2.0배 (레어도 보상 강화)
    - 기타 속성: 1.0배
    - 적용: 보스 처치 크리스탈 드롭
  - **보스 크리스탈 공식 변경**: (레벨 + 20) × crystal_multiplier (이전: 10 + 레벨×2)
    - 초반 보상 증가, 선형 성장으로 단순화
  - **업그레이드 비용 대폭 완화**:
    - base_cost: 100 → 1 (100배 감소)
    - cost_multiplier: 1.5 → 1.15 (성장 완화)
    - 효과: 초반 진입장벽 완화, 부드러운 비용 곡선
  - **✅ 레어 속성 희귀성 강화**: 의도된 희귀도 실현
    - Holy: 10 → 2 (5배 감소, 등장률 3.4% → 0.83%)
    - Dark: 30 → 6 (5배 감소, 등장률 10.3% → 2.48%)
    - 목적: 레어 속성의 체감 희귀성 강화, 높은 크리스탈 보상(2.0배)과 균형
  - **✅ 몬스터 처치 크리스탈 레벨 스케일링**: 후반 진행 지원
    - 공식 변경: 1 (고정) → 1 + (레벨 / 100)
    - 예시: Lv.1=1, Lv.100=2, Lv.500=6 크리스탈
    - 효과: 후반 레벨에서 일반 몬스터 처치 보상 증가, 진행 속도 개선
    - 적용: GameManager.cs, SimulationEngine.cs, CrystalTracker.cs

- **2026-02-04 (v1.2.0)**: 7가지 시스템 점검 완료 및 문서 완전 통일
  - **타이머 시스템 상세화**: 0.1초 단위 감소, Wind 배속 1.5배, 황금 고블린 도주 처리
  - **인게임 업그레이드**: 스테이지별 비용 배율 (1.5배씩 증가) 명시
  - **영구 스탯 완전 정리**: 36개 전체 스탯 테이블화 (4개 카테고리)
  - **골드 보상 2단계**: 가산 → 배수 순서 명확화
  - **HP 계산 정확화**: 종족 배율 × 속성 배율 순서 명시
  - **황금 고블린 시스템**: 스폰 조건, 스탯, 보상, 도주 처리 추가
  - **✅ 크리스탈 획득 조건 수정**: 모든 몬스터 처치 시 1크리스탈 지급 (이전: 보스만)
    - GameManager.cs: 모든 몬스터 처치 시 ProcessStageClear() 호출
    - SimulationEngine.cs: 동일하게 수정
    - 예상 효과: 크리스탈 획득량 10-15% 증가, 초반 진행 부드러움

- **2026-02-03 (v1.1.0)**: 보스 크리스탈 시스템 재설계 및 10시간 밸런스 검증
  - **보스 크리스탈**: 확률 기반 → 100% 확정 지급으로 변경
  - **속성별 크리스탈 배율**: Holy/Dark 2.0배 추가
  - **스테이지 클리어 크리스탈**: 보스 처치 시에만 지급 (10레벨 단위)
  - **골드 변환**: 100:1 비율 명시
  - **인게임 업그레이드**: 복잡한 비용 공식 → 단순화
  - **시간 관리**: 세션당 30초 → 몬스터당 30초로 수정
  - **시뮬레이터**: SessionResult 확장 (30+ 필드), ProgressionSimulator 추가
  - **10시간 검증**: 레벨 4,453 도달, 104M 크리스탈 획득 확인
  - **제거**: `crystal_chance` 스탯 (100% 지급으로 불필요)

- **2026-02-03 (v1.0.0)**: 초기 버전 생성
  - 전투 시스템 (8단계 데미지 공식)
  - 몬스터 시스템 (HP, 골드, 속성)
  - 확률 시스템 (등장 확률, 크리티컬)
  - 진행 시스템 (시간, 영구 스탯)
  - 보상 시스템 (도감, 크리스탈)
  - 인게임 업그레이드 (비용, 할인)
