# DeskWarrior 스탯 시스템 API 레퍼런스

## GameManager API

### 인게임 스탯 조회

```csharp
// 현재 효과 값
int keyboardPower = gameManager.KeyboardPower;        // 1 + 레벨 효과
int mousePower = gameManager.MousePower;              // 1 + 레벨 효과
double goldFlat = gameManager.GoldFlat;               // 가산 골드
double goldMulti = gameManager.GoldMulti;             // 배율 (0.0 ~ 1.0)
double timeThief = gameManager.TimeThief;             // 추가 시간 (초)
double comboFlex = gameManager.ComboFlex;             // 허용 오차 (초)
double comboDamage = gameManager.ComboDamage;         // 배율 (0.0 ~ 1.0)

// 현재 레벨
InGameStats stats = gameManager.InGameStats;
int kbLevel = stats.KeyboardPowerLevel;
int msLevel = stats.MousePowerLevel;
// ... 등
```

### 인게임 스탯 업그레이드

```csharp
// 비용 조회
int cost = gameManager.GetInGameStatUpgradeCost("keyboard_power");

// 업그레이드 시도
bool success = gameManager.UpgradeInGameStat("keyboard_power");

// 레거시 메서드 (내부적으로 UpgradeInGameStat 호출)
gameManager.UpgradeKeyboardPower();
gameManager.UpgradeMousePower();
```

### 콤보 시스템

```csharp
// 콤보 상태 조회
int comboStack = gameManager.CurrentComboStack;  // 0-3
bool isActive = gameManager.IsComboActive;       // true/false

// 콤보 배율 계산
double comboMultiplier = Math.Pow(2, comboStack); // 1, 2, 4, 8
```

---

## StatGrowthManager API

### 직접 사용 (고급)

```csharp
var statGrowth = new StatGrowthManager();

// 인게임 스탯
int cost = statGrowth.GetInGameUpgradeCost("gold_flat", currentLevel, discountPercent);
double effect = statGrowth.GetInGameStatEffect("combo_damage", level);
bool canUpgrade = statGrowth.CanUpgradeInGameStat("combo_flex", currentLevel);
StatGrowthConfig? config = statGrowth.GetInGameStatConfig("time_thief");

// 영구 스탯
int cost = statGrowth.GetPermanentUpgradeCost("base_attack", currentLevel);
double effect = statGrowth.GetPermanentStatEffect("crit_chance", level);
bool canUpgrade = statGrowth.CanUpgradePermanentStat("crit_damage", currentLevel);
```

### 스탯 ID 목록

#### 인게임 (7종)
```
"keyboard_power"   - 키보드 공격력
"mouse_power"      - 마우스 공격력
"gold_flat"        - 골드+ (가산)
"gold_multi"       - 골드* (배율)
"time_thief"       - 시간 도둑
"combo_flex"       - 콤보 유연성
"combo_damage"     - 콤보 데미지
```

#### 영구 (19종)
```
// A. 기본 능력
"base_attack"      - 기본 공격력
"attack_percent"   - 공격력 배수
"crit_chance"      - 크리티컬 확률
"crit_damage"      - 크리티컬 배율
"multi_hit"        - 멀티히트 확률

// B. 재화 보너스
"gold_flat_perm"   - 영구 골드+
"gold_multi_perm"  - 영구 골드*
"crystal_flat"     - 크리스탈+
"crystal_multi"    - 크리스탈*

// C. 유틸리티
"time_extend"      - 기본 시간 연장
"upgrade_discount" - 업그레이드 할인

// D. 시작 보너스
"start_level"      - 시작 레벨
"start_gold"       - 시작 골드
"start_keyboard"   - 시작 키보드
"start_mouse"      - 시작 마우스
"start_gold_flat"  - 시작 골드+
"start_gold_multi" - 시작 골드*
"start_combo_flex" - 시작 콤보유연성
"start_combo_damage" - 시작 콤보데미지
```

---

## PermanentStats 사용법

### 레벨 기반 접근 (새 방식)

```csharp
PermanentStats permStats = saveManager.CurrentSave.PermanentStats;

// 레벨 읽기
int baseAttackLevel = permStats.BaseAttackLevel;
int critChanceLevel = permStats.CritChanceLevel;

// 레벨 증가 (업그레이드)
permStats.BaseAttackLevel++;
permStats.CritChanceLevel++;

// 실제 효과 값 계산 (Helper 사용)
int baseAttackValue = permStats.GetBaseAttackValue();
double critChanceValue = permStats.GetCritChanceValue();
```

### 레거시 접근 (하위 호환)

```csharp
// JsonIgnore 속성으로 계산됨
int baseAttack = permStats.BaseAttack;                    // BaseAttackLevel
double attackBonus = permStats.AttackPercentBonus;        // AttackPercentLevel × 0.05
double goldBonus = permStats.GoldPercentBonus;            // GoldMultiPermLevel × 0.03
// ... 등
```

---

## UI 구현 예시

### 인게임 업그레이드 버튼

```csharp
// XAML
<Button Content="{Binding KeyboardPowerUpgradeText}"
        Command="{Binding UpgradeKeyboardCommand}"
        IsEnabled="{Binding CanUpgradeKeyboard}"/>

// ViewModel
public string KeyboardPowerUpgradeText
{
    get
    {
        int level = _gameManager.InGameStats.KeyboardPowerLevel;
        int cost = _gameManager.GetInGameStatUpgradeCost("keyboard_power");
        int power = _gameManager.KeyboardPower;
        return $"키보드 공격력 Lv.{level} ({power}) - {cost}G";
    }
}

public bool CanUpgradeKeyboard
{
    get
    {
        int cost = _gameManager.GetInGameStatUpgradeCost("keyboard_power");
        return _gameManager.Gold >= cost;
    }
}

public ICommand UpgradeKeyboardCommand => new RelayCommand(() =>
{
    if (_gameManager.UpgradeInGameStat("keyboard_power"))
    {
        OnPropertyChanged(nameof(KeyboardPowerUpgradeText));
        OnPropertyChanged(nameof(CanUpgradeKeyboard));
    }
});
```

### 영구 업그레이드 패널

```csharp
// ViewModel
public class PermanentUpgradeViewModel
{
    private readonly StatGrowthManager _statGrowth;
    private readonly SaveManager _saveManager;

    public ObservableCollection<StatUpgradeItem> Stats { get; }

    public PermanentUpgradeViewModel(StatGrowthManager statGrowth, SaveManager saveManager)
    {
        _statGrowth = statGrowth;
        _saveManager = saveManager;

        Stats = new ObservableCollection<StatUpgradeItem>();
        LoadStats();
    }

    private void LoadStats()
    {
        var permStats = _saveManager.CurrentSave.PermanentStats;

        foreach (var kvp in _statGrowth.GetPermanentStatsByCategory())
        {
            string statId = kvp.Key;
            StatGrowthConfig config = kvp.Value;

            int currentLevel = GetStatLevel(permStats, statId);
            int cost = _statGrowth.GetPermanentUpgradeCost(statId, currentLevel);
            double effect = _statGrowth.GetPermanentStatEffect(statId, currentLevel);
            bool canUpgrade = _statGrowth.CanUpgradePermanentStat(statId, currentLevel);

            Stats.Add(new StatUpgradeItem
            {
                StatId = statId,
                Name = config.Name,
                Description = config.Description,
                Level = currentLevel,
                Effect = effect,
                Cost = cost,
                CanUpgrade = canUpgrade && _saveManager.CurrentSave.Currency.Crystals >= cost
            });
        }
    }

    private int GetStatLevel(PermanentStats stats, string statId)
    {
        return statId switch
        {
            "base_attack" => stats.BaseAttackLevel,
            "attack_percent" => stats.AttackPercentLevel,
            "crit_chance" => stats.CritChanceLevel,
            // ... (19종 전부)
            _ => 0
        };
    }
}

// Model
public class StatUpgradeItem
{
    public string StatId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Level { get; set; }
    public double Effect { get; set; }
    public int Cost { get; set; }
    public bool CanUpgrade { get; set; }

    public string DisplayText => $"{Name} Lv.{Level} ({Effect:F2})";
    public string CostText => CanUpgrade ? $"{Cost} 크리스탈" : "MAX";
}
```

### 콤보 표시 UI

```csharp
// XAML
<StackPanel Orientation="Horizontal"
            Visibility="{Binding IsComboActive, Converter={StaticResource BoolToVisibility}}">
    <TextBlock Text="COMBO" Foreground="Yellow" FontSize="24" FontWeight="Bold"/>
    <TextBlock Text="{Binding ComboStackText}" Foreground="Orange" FontSize="32" Margin="10,0"/>
</StackPanel>

// ViewModel
public bool IsComboActive => _gameManager.IsComboActive;

public string ComboStackText
{
    get
    {
        int stack = _gameManager.CurrentComboStack;
        double multiplier = Math.Pow(2, stack);
        return $"×{multiplier:F0}";
    }
}

// GameManager 이벤트 구독
_gameManager.DamageDealt += (s, e) =>
{
    OnPropertyChanged(nameof(IsComboActive));
    OnPropertyChanged(nameof(ComboStackText));
};
```

---

## 설정 파일 형식 예시

### config/InGameStatGrowth.json

```json
{
  "stats": {
    "keyboard_power": {
      "name": "키보드 공격력",
      "base_cost": 100,
      "growth_rate": 0.5,
      "multiplier": 1.5,
      "softcap_interval": 10,
      "effect_per_level": 1,
      "max_level": 0,
      "description": "키보드 입력 시 +{n} 데미지"
    }
  }
}
```

### config/PermanentStatGrowth.json

```json
{
  "stats": {
    "base_attack": {
      "name": "기본 공격력",
      "category": "base",
      "base_cost": 10,
      "growth_rate": 0.3,
      "multiplier": 1.3,
      "softcap_interval": 20,
      "effect_per_level": 1,
      "max_level": 0,
      "description": "모든 공격에 +{n} 데미지"
    }
  },
  "categories": {
    "base": { "name": "기본 능력", "order": 1 },
    "currency": { "name": "재화 보너스", "order": 2 }
  }
}
```

---

## 이벤트 처리

### GameManager 이벤트

```csharp
// 스탯 변경 시
_gameManager.StatsChanged += (s, e) =>
{
    // UI 갱신
    UpdateAllStatDisplays();
};

// 데미지 발생 시
_gameManager.DamageDealt += (s, e) =>
{
    var args = (DamageEventArgs)e;
    // args.Damage
    // args.IsCritical
    // args.IsMouse
};

// 몬스터 처치 시
_gameManager.MonsterDefeated += (s, e) =>
{
    // 골드 획득 표시
    // 콤보 리셋 (몬스터 변경 시)
};
```

### DamageResult 활용

```csharp
public class DamageEventArgs : EventArgs
{
    public int Damage { get; set; }
    public bool IsCritical { get; set; }
    public bool IsMouse { get; set; }
}

// DamageCalculator 결과에서 확장 정보 활용 가능
var result = damageCalculator.Calculate(basePower, permStats, comboDamage, comboStack);
// result.Damage
// result.IsCritical
// result.IsMultiHit
// result.IsCombo
// result.ComboStack
```

---

## 디버깅 팁

### 로깅

```csharp
// StatGrowthManager 로드 확인
var config = _statGrowth.GetInGameStatConfig("keyboard_power");
Console.WriteLine($"Loaded: {config?.Name ?? "null"}");

// 비용 계산 확인
for (int level = 1; level <= 10; level++)
{
    int cost = config.CalculateCost(level);
    Console.WriteLine($"Lv{level}: {cost}G");
}
```

### 테스트 치트

```csharp
// 골드 무한
_gameManager.Gold = int.MaxValue;

// 인게임 스탯 최대
for (int i = 0; i < 50; i++)
    _gameManager.UpgradeInGameStat("keyboard_power");

// 영구 스탯 최대
var permStats = _saveManager.CurrentSave.PermanentStats;
permStats.BaseAttackLevel = 100;
permStats.CritChanceLevel = 50;
```

---

## 성능 모니터링

```csharp
// 비용 계산 벤치마크
var sw = Stopwatch.StartNew();
for (int i = 0; i < 1000; i++)
{
    int cost = _statGrowth.GetInGameUpgradeCost("keyboard_power", i);
}
sw.Stop();
Console.WriteLine($"1000 calculations: {sw.ElapsedMilliseconds}ms");
// 예상: < 1ms

// 콤보 판정 벤치마크
sw.Restart();
for (int i = 0; i < 1000; i++)
{
    _comboTracker.ProcessInput();
}
sw.Stop();
Console.WriteLine($"1000 combo checks: {sw.ElapsedMilliseconds}ms");
// 예상: < 1ms
```

---

## 버전

- **API 버전**: 1.0
- **작성일**: 2026-01-16
- **호환**: .NET 9.0, C# 12.0
