# 황금 고블린 등급 시스템 기획서

**작성자**: jina (게임 디자인 AI)
**작성일**: 2026-02-19
**버전**: 1.0.0
**상태**: 기획 완료, lily 구현 대기

---

## 1. 개요

### 1.1 기획 의도

황금 고블린은 현재 500킬마다 0.1% 확률로 등장하는 희귀 특수 몬스터입니다. 처치 시 2~100배의 골드 보상을 받습니다. 이 보상 배수를 시각화하여 플레이어가 고블린을 발견하는 순간부터 "이번엔 얼마짜리야?"를 직관적으로 알 수 있는 **등급 시스템**을 도입합니다.

핵심 가치: **발견 순간의 기대감 극대화**

### 1.2 현재 시스템 분석

```
스폰 조건: 500킬 쿨다운 + 0.1% 확률
HP: 100~200 랜덤
제한 시간: 10초 고정
보상: 2~100배 (삼각분포, 51배 부근이 최고 확률)
비주얼: 단일 이미지 1종
```

**삼각분포의 수학적 특성**:
```
두 균등분포의 평균: (u1 + u2) / 2
CDF(x) = 2x^2          (x <= 0.5인 경우)
CDF(x) = 1 - 2(1-x)^2 (x > 0.5인 경우)
배수 매핑: multiplier = 2 + t * 98

중앙값: 51배 (전체의 50%)
최빈값: 51배 (정규분포의 평균/최빈값과 동일)
```

이 분포의 결과로 40~65배 구간이 전체의 약 44%를 차지하는 종 모양(bell-shaped) 분포가 형성됩니다.

---

## 2. 등급 시스템 설계

### 2.1 5단계 등급 구조

삼각분포의 자연스러운 분포를 그대로 반영합니다. 분포의 중앙(Gold 등급)이 가장 흔하고, 양 끝단(Bronze, Legendary)이 희귀합니다.

| 등급 | 한국어명 | 보상 배수 구간 | 출현 확률 | 레어도 체감 |
|------|---------|--------------|----------|-------------|
| Bronze | 청동 | 2x ~ 19x | **6.7%** | "허탕..." |
| Silver | 은 | 20x ~ 40x | **24.9%** | "그럭저럭" |
| Gold | 황금 | 41x ~ 65x | **44.3%** | "표준" (최다) |
| Diamond | 다이아 | 66x ~ 85x | **20.0%** | "럭키!" |
| Legendary | 전설 | 86x ~ 100x | **4.1%** | "대박!!!" |

**설계 근거**:
- 출현 확률이 삼각분포의 수학적 특성과 완전히 일치
- Gold가 가장 흔하지만 충분한 보상(41~65배)이므로 "표준"으로 명명
- Bronze(6.7%)와 Legendary(4.1%)는 모두 희귀 - 둘 다 "특별한 경험"
- Diamond(20.0%)는 Silver(24.9%)와 비슷한 확률이지만 훨씬 높은 보상

### 2.2 확률 검증

```
Bronze    (2~19x):   t=0.000~0.184  CDF 구간: 0.000~0.067  = 6.7%
Silver   (20~40x):  t=0.184~0.388  CDF 구간: 0.067~0.316  = 24.9%
Gold     (41~65x):  t=0.398~0.643  CDF 구간: 0.316~0.759  = 44.3%
Diamond  (66~85x):  t=0.653~0.847  CDF 구간: 0.759~0.959  = 20.0%
Legendary(86~100x): t=0.857~1.000  CDF 구간: 0.959~1.000  = 4.1%
합계:                                                        100.0%
```

**계산 코드 (검증용)**:
```python
def triangular_cdf(x):
    if x <= 0.5: return 2 * x * x
    else: return 1 - 2 * (1 - x) * (1 - x)

def prob_in_range(lo, hi):
    t_lo = (lo - 2) / 98.0
    t_hi = (hi - 2) / 98.0
    return triangular_cdf(t_hi) - triangular_cdf(t_lo)
```

---

## 3. 비주얼 차별화

### 3.1 등급별 배경 배정

기존 배경 리소스(`Resources/Images/Org/Background/`)를 활용합니다.

| 등급 | 배경 파일 | 분위기 | 선택 이유 |
|------|----------|--------|----------|
| Bronze | `bg_floor_cave_v1.png` | 어두운 동굴 | 낮은 기대감, 실망스러운 분위기 |
| Silver | `bg_floor_grassland_v2.png` | 평범한 초원 | 일반적인 느낌, 무난한 보상 |
| Gold | `bg_floor_desert_v1.png` | 황금빛 사막 | 황금 이미지, 표준 보상 |
| Diamond | `bg_floor_temple_v1.png` | 신비로운 신전 | 특별함, 웅장함 |
| Legendary | `bg_floor_lava_v1.png` | 용암 지대 | 극적인 긴장감, 대박의 열기 |

### 3.2 스프라이트 계획

**현재 보유 리소스**:
- `Resources/Org/monster_goldengoblin.png` - 기본 황금 고블린
- `Resources/Org/monster_goldengoblin_grade1.png` - 등급 1용 (이미 존재!)
- `Resources/Org/monster_goblinA/B/C/D/E/F.png` - 일반 고블린 변형 6종

**등급별 스프라이트 계획**:

| 등급 | 스프라이트 경로 | 상태 | 비주얼 방향 |
|------|--------------|------|------------|
| Bronze | `Org/monster_goldengoblin_grade1.png` | **이미 존재** | 빛바랜 금색, 낡은 느낌 |
| Silver | `Production/monster_goldengoblin_silver.png` | 신규 제작 필요 | 은빛 광택, 차가운 색조 |
| Gold | `Org/monster_goldengoblin.png` | **기존 이미지** | 표준 황금빛 |
| Diamond | `Production/monster_goldengoblin_diamond.png` | 신규 제작 필요 | 파란빛 다이아, 반짝임 강조 |
| Legendary | `Production/monster_goldengoblin_legendary.png` | 신규 제작 필요 | 무지개빛/홀로그램, 크기 약간 크게 |

**데모 버전 대안** (스프라이트 미제작 시):
```
Bronze:   기존 monster_goldengoblin_grade1.png (이미 존재)
Silver:   기존 monster_goldengoblin.png + 은색 색조 (Hue: 200)
Gold:     기존 monster_goldengoblin.png (변경 없음)
Diamond:  기존 monster_goldengoblin.png + 파란 색조 (Hue: 180)
Legendary: 기존 monster_goldengoblin.png + 무지개 색조 (Hue: -1, 순환)
```

### 3.3 UI 연출 요소

| 등급 | 테두리 색 | 이름 색 | 추가 연출 |
|------|---------|---------|----------|
| Bronze | `#CD7F32` (청동색) | 흰색 | 없음 |
| Silver | `#C0C0C0` (은색) | 흰색 | 약한 반짝임 |
| Gold | `#FFD700` (금색) | 노란색 | 중간 반짝임 |
| Diamond | `#B9F2FF` (다이아색) | 하늘색 | 강한 반짝임 + 파티클 |
| Legendary | 무지개 순환 | 무지개 순환 | 전체 화면 글로우 + 파티클 |

**등급 표시 UI**:
```
[등급 아이콘] [등급명] 황금 고블린
              ████████████ (HP 바)
              ⏱ 10초
```

---

## 4. 등급별 게임플레이 차별화

### 4.1 설계 원칙

등급은 **보상 배수를 시각화하는 것**이 핵심입니다. 게임플레이 변화는 등급 정보를 강화하는 방향으로만 최소화합니다.

### 4.2 HP 변화

등급이 높을수록 더 높은 HP를 부여합니다. "더 귀한 고블린은 더 도망치려 한다"는 컨셉.

| 등급 | HP 범위 | 평균 HP | 변화 |
|------|---------|--------|------|
| Bronze | 80~120 | 100 | 가장 낮음 (잡기 쉬움) |
| Silver | 100~150 | 125 | 현재 수준 |
| Gold | 120~180 | 150 | 현재 수준 |
| Diamond | 150~220 | 185 | 약간 높음 |
| Legendary | 180~280 | 230 | 가장 높음 (잡기 어려움) |

**설계 의도**: 높은 등급일수록 HP가 높아 더 집중해야 합니다. 하지만 10초 제한시간 내에 충분히 처치 가능한 범위로 설정합니다.

### 4.3 제한 시간 (고정 유지 권장)

제한시간은 **10초 고정**을 권장합니다.

**이유**:
- 등급별로 시간을 다르게 하면 인지 복잡성 증가
- 10초라는 긴박함이 황금 고블린의 핵심 체험
- HP 변화로 이미 충분한 난이도 차별화가 됨

데모 버전에서는 시간 변화 없이 구현하고, 플레이어 피드백 후 조정 검토.

### 4.4 등급 결정 타이밍

**스폰 시 즉시 결정 (강력 권장)**

고블린이 등장하는 순간 등급이 결정되고, 그 등급에 맞는 비주얼이 즉시 표시됩니다.

```
[고블린 스폰] → [배수 랜덤 결정] → [등급 산정] → [등급별 비주얼 표시]
```

**이유**:
- 발견 순간의 "이번엔 뭐지?" 기대감이 핵심 체험
- 처치 후 보상 공개는 긴장감이 없음
- 비주얼이 먼저 보여야 "Legendary다! 집중하자!"가 가능

---

## 5. 데이터 구조 설계

### 5.1 JSON 구조 제안

**파일**: `config/SpecialMonsters.json`

현재 구조를 하위 호환성 있게 확장합니다. 기존 필드는 유지하고 `grades` 배열을 추가합니다.

```json
{
  "golden_goblin": {
    "id": "special_golden_goblin",
    "spawn_chance": 0.001,
    "cooldown_kills": 500,
    "hp": 150,
    "hp_min": 100,
    "hp_max": 200,
    "time_limit": 10,
    "reward_min": 2,
    "reward_max": 100,
    "_reward_distribution": "삼각분포: 50배 부근이 가장 높은 확률",
    "sprite": "Production/monster_goblin.png",
    "emoji": "💰",
    "name": {
      "ko-KR": "황금 고블린",
      "en-US": "Golden Goblin",
      "ja-JP": "ゴールデンゴブリン",
      "zh-CN": "黄金哥布林",
      "zh-TW": "黃金哥布林"
    },
    "grades": [
      {
        "grade_id": "bronze",
        "reward_min": 2,
        "reward_max": 19,
        "hp_min": 80,
        "hp_max": 120,
        "time_limit": 10,
        "sprite": "Org/monster_goldengoblin_grade1.png",
        "background": "bg_floor_cave_v1",
        "border_color": "#CD7F32",
        "name_color": "#FFFFFF",
        "effect": "none",
        "name": {
          "ko-KR": "청동 황금 고블린",
          "en-US": "Bronze Golden Goblin"
        }
      },
      {
        "grade_id": "silver",
        "reward_min": 20,
        "reward_max": 40,
        "hp_min": 100,
        "hp_max": 150,
        "time_limit": 10,
        "sprite": "Production/monster_goldengoblin_silver.png",
        "sprite_fallback": "Org/monster_goldengoblin.png",
        "sprite_hue_shift": 200,
        "background": "bg_floor_grassland_v2",
        "border_color": "#C0C0C0",
        "name_color": "#FFFFFF",
        "effect": "shimmer_weak",
        "name": {
          "ko-KR": "은빛 황금 고블린",
          "en-US": "Silver Golden Goblin"
        }
      },
      {
        "grade_id": "gold",
        "reward_min": 41,
        "reward_max": 65,
        "hp_min": 120,
        "hp_max": 180,
        "time_limit": 10,
        "sprite": "Org/monster_goldengoblin.png",
        "background": "bg_floor_desert_v1",
        "border_color": "#FFD700",
        "name_color": "#FFD700",
        "effect": "shimmer_medium",
        "name": {
          "ko-KR": "황금 고블린",
          "en-US": "Golden Goblin"
        }
      },
      {
        "grade_id": "diamond",
        "reward_min": 66,
        "reward_max": 85,
        "hp_min": 150,
        "hp_max": 220,
        "time_limit": 10,
        "sprite": "Production/monster_goldengoblin_diamond.png",
        "sprite_fallback": "Org/monster_goldengoblin.png",
        "sprite_hue_shift": 180,
        "background": "bg_floor_temple_v1",
        "border_color": "#B9F2FF",
        "name_color": "#B9F2FF",
        "effect": "shimmer_strong",
        "name": {
          "ko-KR": "다이아 황금 고블린",
          "en-US": "Diamond Golden Goblin"
        }
      },
      {
        "grade_id": "legendary",
        "reward_min": 86,
        "reward_max": 100,
        "hp_min": 180,
        "hp_max": 280,
        "time_limit": 10,
        "sprite": "Production/monster_goldengoblin_legendary.png",
        "sprite_fallback": "Org/monster_goldengoblin.png",
        "sprite_hue_shift": -1,
        "background": "bg_floor_lava_v1",
        "border_color": "#FF0000",
        "name_color": "#FF00FF",
        "effect": "rainbow_glow",
        "name": {
          "ko-KR": "전설의 황금 고블린",
          "en-US": "Legendary Golden Goblin"
        }
      }
    ]
  }
}
```

### 5.2 C# 모델 변경 계획

`Models/GoldenGoblinConfig.cs`에 새 클래스 추가:

```csharp
// 추가할 클래스 (구조만 정의, 값은 JSON에서 로드)
public class GoldenGoblinGrade
{
    [JsonPropertyName("grade_id")]
    public string GradeId { get; set; }

    [JsonPropertyName("reward_min")]
    public int RewardMin { get; set; }

    [JsonPropertyName("reward_max")]
    public int RewardMax { get; set; }

    [JsonPropertyName("hp_min")]
    public int HpMin { get; set; }

    [JsonPropertyName("hp_max")]
    public int HpMax { get; set; }

    [JsonPropertyName("time_limit")]
    public int TimeLimit { get; set; }

    [JsonPropertyName("sprite")]
    public string Sprite { get; set; }

    [JsonPropertyName("sprite_fallback")]
    public string SpriteFallback { get; set; }

    [JsonPropertyName("sprite_hue_shift")]
    public int SpriteHueShift { get; set; }

    [JsonPropertyName("background")]
    public string Background { get; set; }

    [JsonPropertyName("border_color")]
    public string BorderColor { get; set; }

    [JsonPropertyName("name_color")]
    public string NameColor { get; set; }

    [JsonPropertyName("effect")]
    public string Effect { get; set; }

    [JsonPropertyName("name")]
    public Dictionary<string, string> Name { get; set; } = new();
}

// GoldenGoblinConfig에 추가할 필드
// [JsonPropertyName("grades")]
// public List<GoldenGoblinGrade> Grades { get; set; } = new();
```

---

## 6. 로직 변경 계획

### 6.1 GoldenGoblinManager.cs 변경사항

**변경 1**: `GetRewardMultiplier()` → `DetermineGrade()`

```csharp
// 현재
public int GetRewardMultiplier()
{
    double triangular = (u1 + u2) / 2.0;
    return _config.RewardMultiplierMin + (int)(triangular * range);
}

// 변경 후
public (GoldenGoblinGrade grade, int multiplier) DetermineGrade()
{
    // 1. 배수 결정 (기존 삼각분포 로직 유지)
    double triangular = (u1 + u2) / 2.0;
    int multiplier = _config.RewardMultiplierMin + (int)(triangular * range);

    // 2. 배수에 맞는 등급 찾기 (JSON 데이터 기반)
    GoldenGoblinGrade grade = _config.Grades
        .FirstOrDefault(g => multiplier >= g.RewardMin && multiplier <= g.RewardMax)
        ?? _config.Grades.First();  // 기본값

    return (grade, multiplier);
}
```

**변경 2**: `CreateGoldenGoblin()` 수정

```csharp
public (Monster monster, GoldenGoblinGrade grade, int multiplier) CreateGoldenGoblinWithGrade(int level)
{
    var (grade, multiplier) = DetermineGrade();
    // grade.HpMin/HpMax 사용하여 HP 설정
    // grade.TimeLimit 사용하여 제한시간 설정
    return (monster, grade, multiplier);
}
```

### 6.2 UI 레이어 변경사항

황금 고블린 등장 시 UI에서 처리해야 할 것들:

```
1. grade.Background 로드 → 배경 이미지 교체
2. grade.Sprite 로드 → 고블린 스프라이트 교체
3. grade.BorderColor → UI 테두리 색상 변경
4. grade.NameColor → 이름 텍스트 색상 변경
5. grade.Name[language] → 이름 표시
6. grade.Effect 파싱 → 이펙트 시스템 호출
```

---

## 7. 로컬라이제이션

`config/localization/ko-KR.json` 및 `en-US.json`에 추가 필요:

```json
{
  "ui.golden_goblin.grade.bronze": "청동",
  "ui.golden_goblin.grade.silver": "은",
  "ui.golden_goblin.grade.gold": "황금",
  "ui.golden_goblin.grade.diamond": "다이아",
  "ui.golden_goblin.grade.legendary": "전설",
  "ui.golden_goblin.escaped": "고블린이 도망쳤다!",
  "ui.golden_goblin.appear": "{grade} 황금 고블린 등장!",
  "ui.golden_goblin.reward": "x{multiplier} 골드 획득!"
}
```

---

## 8. 구현 단계별 계획

### Phase 1: 데모 버전 (MVP)

**목표**: 등급 시스템의 핵심 체험 구현 (리소스 최소화)

**구현 범위**:
- [ ] `config/SpecialMonsters.json`에 `grades` 배열 추가
- [ ] `Models/GoldenGoblinGrade.cs` 클래스 생성
- [ ] `GoldenGoblinManager`에 `DetermineGrade()` 로직 추가
- [ ] 배경 이미지 교체 (기존 11종 배경 재활용)
- [ ] 스프라이트: Bronze=grade1.png(기존), Gold=기존 이미지, 나머지 hue_shift 적용
- [ ] UI 테두리 색상 변경
- [ ] 이름 표시 (등급 포함)

**데모에서 제외**:
- 신규 스프라이트 제작 (Silver/Diamond/Legendary)
- 이펙트 시스템 (shimmer, rainbow_glow)
- HP 차별화 (모든 등급 동일 HP 사용 가능)

### Phase 2: 풀 버전

**추가 구현**:
- [ ] Silver/Diamond/Legendary 전용 스프라이트 제작
- [ ] 이펙트 시스템 구현 (shimmer_weak, shimmer_strong, rainbow_glow)
- [ ] 등급별 HP 차별화 (grade.HpMin/HpMax 적용)
- [ ] 사운드: 등급별 등장 효과음 (Legendary 시 특별 효과음)
- [ ] 도감 시스템 연동 (등급별 별도 도감 엔트리 고려)

---

## 9. 필요한 신규 리소스 목록

### 9.1 스프라이트 (이미지 작업 필요)

| 파일명 | 경로 | 우선순위 |
|--------|------|---------|
| `monster_goldengoblin_silver.png` | `Resources/Production/` | 데모 후 |
| `monster_goldengoblin_diamond.png` | `Resources/Production/` | 데모 후 |
| `monster_goldengoblin_legendary.png` | `Resources/Production/` | 풀 버전 |

**참고**: `monster_goldengoblin_grade1.png`는 이미 존재하므로 Bronze에 즉시 사용 가능

### 9.2 배경 이미지 (기존 재활용)

| 등급 | 파일 | 상태 |
|------|------|------|
| Bronze | `bg_floor_cave_v1.png` | 이미 존재 |
| Silver | `bg_floor_grassland_v2.png` | 이미 존재 |
| Gold | `bg_floor_desert_v1.png` | 이미 존재 |
| Diamond | `bg_floor_temple_v1.png` | 이미 존재 |
| Legendary | `bg_floor_lava_v1.png` | 이미 존재 |

### 9.3 사운드 (풀 버전)

현재 사운드 시스템이 프로시저럴 생성 기반이므로 등급별 사운드 매개변수 정의가 필요합니다. lily와 협의 필요.

---

## 10. lily에게 전달하는 구현 스펙

### User Story

> 플레이어로서, 황금 고블린이 등장했을 때 즉시 "이 고블린이 얼마짜리인지" 비주얼로 알 수 있어야 합니다. 고블린의 등급(Bronze/Silver/Gold/Diamond/Legendary)이 배경, 스프라이트, UI 색상으로 즉각 전달되어야 합니다.

### Acceptance Criteria

**AC-1: 등급 결정 로직**
- [ ] 황금 고블린 스폰 시 삼각분포로 보상 배수(2~100)가 결정됨
- [ ] 배수에 따라 5단계 등급 중 하나가 자동으로 선택됨
- [ ] 등급 경계값은 `config/SpecialMonsters.json`의 `grades[].reward_min/max`에서 로드됨
- [ ] C# 코드에 등급 경계값 하드코딩 없음

**AC-2: 비주얼 변경**
- [ ] 등급에 따라 배경 이미지가 교체됨
- [ ] 등급에 따라 고블린 스프라이트가 교체됨 (없으면 hue_shift 적용)
- [ ] 등급에 따라 UI 테두리 색상이 변경됨
- [ ] 등급에 따라 이름 텍스트 색상이 변경됨
- [ ] 등급명이 고블린 이름 앞에 표시됨 (예: "전설의 황금 고블린")

**AC-3: 데이터 기반 설계**
- [ ] 모든 등급 설정값은 `config/SpecialMonsters.json`에서 로드
- [ ] 새 등급 추가 또는 기존 등급 수정 시 JSON만 수정하면 됨
- [ ] `sprite_fallback` 지정 시 sprite 파일이 없어도 fallback 사용

**AC-4: 기존 기능 유지**
- [ ] 기존 황금 고블린 스폰 조건(쿨다운, 확률) 변경 없음
- [ ] `grades` 필드 없는 구버전 JSON도 기존 동작 유지 (하위 호환)
- [ ] 10초 제한시간 만료 시 도주 처리 기존과 동일

### 테스트 시나리오

| 시나리오 | 예상 결과 |
|---------|----------|
| multiplier=5 (Bronze) | 동굴 배경, grade1 스프라이트, 청동색 테두리 |
| multiplier=30 (Silver) | 초원 배경, 은빛 스프라이트, 은색 테두리 |
| multiplier=55 (Gold) | 사막 배경, 기본 스프라이트, 금색 테두리 |
| multiplier=75 (Diamond) | 신전 배경, 다이아 스프라이트, 하늘색 테두리 |
| multiplier=95 (Legendary) | 용암 배경, 전설 스프라이트, 무지개 테두리 |
| sprite 파일 없음 | sprite_fallback 사용 또는 hue_shift 적용 |
| grades 필드 없음 (구버전 JSON) | 기존 동작 유지 |

### 성능 요구사항

- 등급 결정 로직: 1ms 이내 (단순 범위 비교)
- 배경 이미지 교체: 200ms 이내 (기존 배경 교체와 동일 방식)
- 스프라이트 교체: 200ms 이내

---

## 11. 우선순위 결정 가이드

### 데모 버전 핵심 (반드시 구현)
1. JSON 구조 변경 + 등급 결정 로직
2. 배경 이미지 교체 (기존 파일 재활용, 5종 즉시 사용 가능)
3. UI 테두리/이름 색상 변경
4. Bronze 스프라이트 (grade1.png 이미 존재)

### 데모 버전 선택 구현 (시간 허용 시)
5. Gold 스프라이트 HP 차별화
6. hue_shift 기반 Silver/Diamond 스프라이트

### 풀 버전 목표
7. 전용 스프라이트 제작 (Silver, Diamond, Legendary)
8. 이펙트 시스템 (shimmer, rainbow)
9. 등급별 사운드

---

## 변경 이력

- 2026-02-19 v1.0.0: 초기 기획서 작성 (jina)
