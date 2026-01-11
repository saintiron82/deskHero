# 🎮 Game Mechanics - 상세 메커니즘

## 1. 전투 시스템 (Combat System)

### 1.1 입력 처리
```
Global Input Event → Damage Calculation → Apply to Monster
```

| 입력 타입 | 데미지 |
|-----------|--------|
| 키보드 키 입력 | 1 × 공격력 |
| 마우스 클릭 | 1 × 공격력 |

### 1.2 데미지 공식
```
Damage = BaseAttack × AttackMultiplier
```

- `BaseAttack`: 기본 공격력 (기본값: 1)
- `AttackMultiplier`: 업그레이드로 증가

### 1.3 피격 연출 (Hit Effect)

#### Shake Effect
```csharp
// 랜덤 오프셋 적용
float offsetX = Random.Range(-shakePower, shakePower);
float offsetY = Random.Range(-shakePower, shakePower);
```

- `shakePower`: `GameData.json`에서 설정 (기본 2.5px)
- 연타 시 누적되어 격렬한 진동 효과

---

## 2. 레벨 시스템 (Level System)

### 2.1 몬스터 HP 계산
```
MonsterHP = base_hp + (level - 1) × hp_growth
```

| 레벨 | HP (base=100, growth=50) |
|------|--------------------------|
| 1 | 100 |
| 5 | 300 |
| 10 | 550 (보스: 1650) |
| 20 | 1050 (보스: 3150) |

### 2.2 보스 판정
```csharp
bool IsBoss = (level % boss_interval == 0);
```

보스 몬스터:
- HP × `boss_hp_multiplier`
- 크기 × 1.5

---

## 3. 업그레이드 시스템 (Upgrade System)

### 3.1 비용 공식
```
UpgradeCost = base_cost × (cost_multiplier ^ upgradeLevel)
```

| 레벨 | 비용 (base=100, mult=1.5) |
|------|---------------------------|
| 1 | 100 |
| 2 | 150 |
| 3 | 225 |
| 5 | 506 |
| 10 | 3844 |

### 3.2 공격력 증가
```
AttackPower = 1 + (upgradeLevel × 0.5)
```

---

## 4. 타이머 시스템 (Timer System)

### 4.1 동작 규칙
1. 몬스터 등장 → 타이머 시작 (`time_limit` 초)
2. 매 프레임 타이머 감소
3. **처치 성공**: 타이머 리셋, 다음 레벨
4. **시간 초과**: 하드 리셋 발동

### 4.2 하드 리셋 (Hard Reset)
```csharp
void HardReset() {
    currentLevel = 1;
    gold = 0;
    upgradeLevel = 0;
    SaveUserData();  // 최고 기록만 보존
}
```

---

## 5. 보상 시스템 (Reward System)

### 5.1 골드 획득
```
GoldReward = level × baseGoldMultiplier
```

보스 처치 시 추가 보너스 가능 (확장 예정)

### 5.2 기록 갱신
- `max_level_reached`: 도달한 최고 레벨
- `total_input_count`: 누적 입력 횟수
- `daily_logs`: 일별 입력량
