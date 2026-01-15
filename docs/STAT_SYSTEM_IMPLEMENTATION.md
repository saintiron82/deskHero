# DeskWarrior 스탯 시스템 구현 완료

## 구현 개요

26종 스탯 시스템 (인게임 7종 + 영구 19종) 구현 완료

---

## 생성된 파일

### 1. Models/
- **InGameStats.cs** - 인게임 스탯 7종 모델 (세션마다 리셋)
  - keyboard_power, mouse_power
  - gold_flat, gold_multi
  - time_thief
  - combo_flex, combo_damage

- **StatGrowthConfig.cs** - 성장 곡선 설정 모델
  - JSON 파싱용 클래스
  - 비용 계산 공식: `cost = base × (1 + level × growth_rate) × multiplier^(level / softcap_interval)`

### 2. Managers/
- **StatGrowthManager.cs** - 스탯 성장 시스템 관리자
  - InGameStatGrowth.json 로드
  - PermanentStatGrowth.json 로드
  - 비용/효과 계산 API 제공

- **ComboTracker.cs** - 콤보 시스템 (리듬 기반)
  - 연속 입력 시간 간격 감지
  - 리듬 허용 오차 ±0.01초 + combo_flex
  - 콤보 스택 1-3 (×2/×4/×8 배율)

### 3. Helpers/
- **PermanentStatsHelper.cs** - 영구 스탯 확장 메서드
  - 레벨 → 실제 효과 값 변환 헬퍼

---

## 수정된 파일

### 1. Models/PermanentStats.cs
- **변경 전**: 하드코딩된 효과 값 (int, double)
- **변경 후**: 레벨 기반 (19종, 각 int Level)
- **하위 호환성**: JsonIgnore 속성으로 기존 코드 호환 유지

#### 새 스탯 구조 (19종)

```csharp
// A. 기본 능력 (5종)
BaseAttackLevel, AttackPercentLevel, CritChanceLevel, CritDamageLevel, MultiHitLevel

// B. 재화 보너스 (4종)
GoldFlatPermLevel, GoldMultiPermLevel, CrystalFlatLevel, CrystalMultiLevel

// C. 유틸리티 (2종)
TimeExtendLevel, UpgradeDiscountLevel

// D. 시작 보너스 (8종)
StartLevelLevel, StartGoldLevel, StartKeyboardLevel, StartMouseLevel
StartGoldFlatLevel, StartGoldMultiLevel, StartComboFlexLevel, StartComboDamageLevel
```

### 2. Managers/GameManager.cs
- **StatGrowthManager** 인스턴스 추가
- **ComboTracker** 인스턴스 추가
- **InGameStats** 필드 추가
- **StartGame()** - 시작 보너스 적용 로직
- **OnKeyboardInput/OnMouseInput** - 콤보 처리 통합
- **OnMonsterDefeated()** - 골드 획득 공식 적용, 시간 도둑 처리
- **UpgradeInGameStat()** - 새 업그레이드 API

#### 새 Public API

```csharp
// 인게임 스탯 접근자
int KeyboardPower { get; }
int MousePower { get; }
double GoldFlat { get; }
double GoldMulti { get; }
double TimeThief { get; }
double ComboFlex { get; }
double ComboDamage { get; }

// 콤보 시스템
int CurrentComboStack { get; }
bool IsComboActive { get; }

// 업그레이드 API
bool UpgradeInGameStat(string statId)
int GetInGameStatUpgradeCost(string statId)
```

### 3. Managers/DamageCalculator.cs
- **DamageResult** 구조체 확장
  - IsMultiHit, IsCombo, ComboStack 필드 추가
- **Calculate()** 메서드 확장
  - comboDamageBonus, comboStack 파라미터 추가
  - 콤보 데미지 계산 로직 구현 (스택별 ×2/×4/×8)

#### 데미지 계산 공식 (STAT_SYSTEM.md 준수)

```
① 기본 = BasePower (keyboard/mouse_power)
② +가산 = 기본 + base_attack
③ ×배수 = ② × (1 + attack_percent)
④ ×크리티컬 = ③ × crit_damage (확률: crit_chance)
⑤ ×멀티히트 = ④ × 2 (확률: multi_hit)
⑥ ×콤보 = ⑤ × (1 + combo_damage) × 2^stack

최종 데미지 = (int)⑥
```

---

## 데이터 흐름

### 게임 시작
```
StartGame()
  → InGameStats.Reset()
  → 영구 스탯의 "시작 보너스" 8종 적용
  → ComboTracker.FullReset()
  → SpawnMonster()
```

### 입력 처리
```
OnKeyboardInput/OnMouseInput()
  → ComboTracker.ProcessInput() → comboStack
  → CalculateDamage(basePower, comboStack)
    → DamageCalculator.Calculate()
      → 6단계 공식 적용
  → ApplyDamage()
```

### 몬스터 처치
```
OnMonsterDefeated()
  → 골드 획득 공식 (gold_flat + gold_multi 적용)
  → 시간 도둑 (time_thief, 최대 기본시간까지)
  → 보스 크리스탈 드롭 (crystal_flat, crystal_multi)
```

### 업그레이드
```
UpgradeInGameStat(statId)
  → StatGrowthManager.GetInGameUpgradeCost()
    → 비용 공식 계산
    → 할인율 적용 (upgrade_discount)
  → Gold 차감
  → Level 증가
```

---

## Config 파일 의존성

### 필수 파일
- `config/InGameStatGrowth.json` - 인게임 스탯 7종 설정
- `config/PermanentStatGrowth.json` - 영구 스탯 19종 설정

### 로딩 실패 시
- StatGrowthManager는 빈 Dictionary로 초기화
- 모든 비용/효과 계산이 0 또는 int.MaxValue 반환
- 로그 출력 (Logger.LogError)

---

## 하위 호환성

### PermanentStats Legacy Properties
기존 코드에서 사용하던 속성은 `[JsonIgnore]`로 유지:

```csharp
BaseAttack => BaseAttackLevel
AttackPercentBonus => AttackPercentLevel × 0.05
GoldPercentBonus => GoldMultiPermLevel × 0.03
CriticalChanceBonus => CritChanceLevel × 0.01
CriticalDamageBonus => CritDamageLevel × 0.1
MultiHitChance => MultiHitLevel × 0.01
StartingLevelBonus => StartLevelLevel
StartingGoldBonus => StartGoldLevel × 50
// ... 등
```

### GameManager Legacy Methods
```csharp
bool UpgradeKeyboardPower() => UpgradeInGameStat("keyboard_power")
bool UpgradeMousePower() => UpgradeInGameStat("mouse_power")
int CalculateUpgradeCost(int currentLevel) // 기존 공식 유지
```

---

## 성능 고려사항

### 메모리
- InGameStats: ~56 bytes (7 int)
- ComboTracker: ~40 bytes (3 double + 2 DateTime)
- StatGrowthManager: ~2KB (Dictionary × 2)
- **총 증가량**: < 3KB

### CPU
- 비용 계산: O(1) - 수학 공식
- JSON 로딩: 초기화 1회 (< 10ms)
- 콤보 판정: O(1) - DateTime 비교
- **영향**: < 1ms per frame

---

## 테스트 체크리스트

### 인게임 스탯
- [ ] keyboard_power 업그레이드 → 데미지 증가
- [ ] mouse_power 업그레이드 → 데미지 증가
- [ ] gold_flat 업그레이드 → 골드 획득량 증가 (가산)
- [ ] gold_multi 업그레이드 → 골드 획득량 증가 (배율)
- [ ] time_thief 업그레이드 → 처치 시 시간 추가 (최대 기본시간까지)
- [ ] combo_flex 업그레이드 → 콤보 허용 오차 증가
- [ ] combo_damage 업그레이드 → 콤보 데미지 증가

### 영구 스탯 (19종)
- [ ] base_attack → 모든 공격 데미지 증가
- [ ] attack_percent → 데미지 배수 증가
- [ ] crit_chance → 크리티컬 확률 증가
- [ ] crit_damage → 크리티컬 배율 증가
- [ ] multi_hit → 2배 타격 확률 증가
- [ ] gold_flat_perm → 골드 가산 보너스
- [ ] gold_multi_perm → 골드 배율 보너스
- [ ] crystal_flat → 보스 크리스탈 드롭 증가
- [ ] crystal_multi → 크리스탈 드롭 확률 증가
- [ ] time_extend → 기본 시간 연장
- [ ] upgrade_discount → 업그레이드 비용 할인
- [ ] start_level → 시작 레벨
- [ ] start_gold → 시작 골드
- [ ] start_keyboard → 시작 키보드 레벨
- [ ] start_mouse → 시작 마우스 레벨
- [ ] start_gold_flat → 시작 gold_flat 레벨
- [ ] start_gold_multi → 시작 gold_multi 레벨
- [ ] start_combo_flex → 시작 combo_flex 레벨
- [ ] start_combo_damage → 시작 combo_damage 레벨

### 콤보 시스템
- [ ] 일정한 리듬 입력 → 콤보 발동 (스택 1)
- [ ] 3초 내 리듬 유지 → 스택 증가 (최대 3)
- [ ] 리듬 깨짐 → 콤보 해제
- [ ] 3초 경과 → 콤보 해제
- [ ] combo_flex 효과 → 허용 오차 증가
- [ ] combo_damage 효과 → 데미지 배율 적용

### 비용 공식
- [ ] 레벨 1 → base_cost
- [ ] 레벨 증가 → 선형 증가 (growth_rate)
- [ ] softcap_interval 도달 → 지수 증가 (multiplier)
- [ ] upgrade_discount → 비용 감소 적용

---

## 다음 단계 (UI 구현 필요)

1. **인게임 업그레이드 패널**
   - 7종 스탯 버튼 (비용 표시)
   - 현재 레벨/효과 표시
   - 골드 부족 시 비활성화

2. **영구 업그레이드 패널**
   - 19종 스탯 (카테고리별 분류)
   - 크리스탈 비용 표시
   - 최대 레벨 도달 시 "MAX" 표시

3. **콤보 표시**
   - 콤보 스택 UI (×2/×4/×8)
   - 리듬 타이밍 인디케이터
   - 콤보 만료 타이머 (3초)

4. **스탯 정보 툴팁**
   - 스탯 설명 (description)
   - 다음 레벨 효과 미리보기
   - 비용 증가 추이

---

## 버전 정보

- **구현 완료**: 2026-01-16
- **설계 문서**: docs/STAT_SYSTEM.md v1.1
- **.NET 버전**: 9.0
- **C# 버전**: 12.0

---

## 참고 파일

- `docs/STAT_SYSTEM.md` - 스탯 시스템 설계서
- `config/InGameStatGrowth.json` - 인게임 스탯 설정
- `config/PermanentStatGrowth.json` - 영구 스탯 설정
