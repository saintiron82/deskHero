# Hue Shift System

## 개요
몬스터 속성별 색상 변환 시스템. 각 몬스터 변형마다 개별 Hue 값을 설정하여 **같은 속성이라도 몬스터마다 미묘하게 다른 색상**을 표현합니다.

## 데이터 구조

### GameData.json
기본 표시 설정 (크기, 회전 기본값)을 정의합니다.

```json
{
  "monster_display": {
    "default_needs_flip": false,
    "default_rotation": 0,
    "boss_size_multiplier": 1.3,
    "normal_size_multiplier": 1.0
  }
}
```

### batch_*.json
각 몬스터 종족과 변형의 Hue 값 및 표시 옵션을 정의합니다.

```json
{
  "monsters": [
    {
      "id": "monster_slime",
      "species": "slime",
      "display_options": {
        "needs_flip": true,
        "rotation": 0
      },
      "variations": {
        "fire": {
          "hue_shift": 15,
          "hp_modifier": 1.6,
          "gold_modifier": 1.0
        },
        "ice": {
          "hue_shift": 195,
          "hp_modifier": 1.2,
          "gold_modifier": 1.1
        }
      }
    }
  ]
}
```

## Hue 값 범위 가이드라인

**속성별 권장 범위** (몬스터마다 조금씩 다르게 설정):

| 속성 | Hue 범위 | 색상 테마 | 예시 |
|------|----------|-----------|------|
| `normal` | 0 | 원본 유지 | 녹색 (슬라임 기준) |
| `fire` | 10~30 | 주황~빨강 | 슬라임=15, 박쥐=20, 뱀=30 |
| `ice` | 180~210 | 시안~파랑 | 슬라임=195, 박쥐=200, 고블린=205 |
| `wind` | 80~100 | 연두~녹색 | 슬라임=90, 박쥐=85, 거미=100 |
| `holy` | 40~60 | 황금~노랑 | 슬라임=50, 박쥐=55, 뱀=60 |
| `dark` | 270~290 | 보라~자주 | 슬라임=280, 박쥐=285, 뱀=290 |

**색상 다양성 원칙**:
- 같은 속성이라도 몬스터마다 5~10도 차이를 두어 미묘한 개성 표현
- 예: Fire 슬라임(15°)은 주황에 가깝고, Fire 뱀(30°)은 빨강에 가까움

## 기술 구현

### 1. 데이터 흐름
```
config/monsters/batch_01.json (hue_shift 정의)
    ↓
Models/BatchMonsterData.cs (MonsterVariation.HueShift)
    ↓
Managers/MonsterDataManager.cs (FlattenMonster)
    ↓
Models/Monster.cs (Monster.HueShift 프로퍼티)
    ↓
MainWindow.xaml.cs (HueShiftHelper.ApplyHueShift 호출)
    ↓
화면에 색상 변환된 몬스터 표시
```

### 2. Hue Shift 알고리즘
**HueShiftHelper.cs** (RGB → HSV → Hue Shift → RGB 변환)

```csharp
public static BitmapSource ApplyHueShift(BitmapSource source, string imagePath, float hueDegrees)
{
    // 1. Hue 정규화 (0~360)
    hueDegrees = hueDegrees % 360f;
    if (Math.Abs(hueDegrees) < 0.01f) return source;

    // 2. 캐시 확인 (LRU)
    var cacheKey = new CacheKey { ImagePath = imagePath, Hue = hueDegrees };
    if (_cache.TryGetValue(cacheKey, out var cached)) return cached;

    // 3. 픽셀 단위 Hue 변환
    for (each pixel) {
        RgbToHsv(r, g, b, out h, out s, out v);
        h = (h + hueDegrees) % 360f;  // Hue 회전
        HsvToRgb(h, s, v, out r, out g, out b);
    }

    // 4. 캐싱 (LRU, 최대 200개)
    _cache[cacheKey] = result;
    return result;
}
```

### 3. 성능 최적화
- **LRU 캐싱**: 최대 200개 이미지 캐시 (동일 이미지 재사용 시 O(1))
- **Frozen Bitmap**: 메모리 복사 최소화
- **알고리즘 복잡도**: O(N) per pixel, 한 번 처리 후 캐싱

**예상 메모리 사용량**:
- 80×80 이미지: ~25KB
- 캐시 200개: ~5MB

## 수정 방법

### 1. 특정 몬스터의 Hue 값 변경
```bash
# 1. 배치 파일 열기
code config/monsters/batch_01.json

# 2. 원하는 몬스터의 variation 찾기
"fire": {
  "hue_shift": 15,  # 이 값을 변경 (0~360)
  ...
}

# 3. 게임 재시작
dotnet run
```

### 2. Hue 비활성화 (원본 색상 유지)
특정 몬스터에서 Hue Shift를 사용하지 않으려면:
```json
"variations": {
  "fire": {
    "hue_shift": 0,  # 0으로 설정
    ...
  }
}
```

### 3. 스프라이트 반전/회전
```json
{
  "display_options": {
    "needs_flip": true,   // 좌우 반전 (기본 false)
    "rotation": 0         // 회전 각도 (0~360)
  }
}
```

## 배치 확장

### 새 배치 추가 시
1. `config/monsters/batch_XX.json` 생성
2. 각 monster entry에 `display_options` 추가
3. 각 variation에 `hue_shift` 추가 (속성별 범위 가이드라인 참고)

**예시**:
```json
{
  "batch_id": 4,
  "monsters": [
    {
      "id": "monster_dragon",
      "species": "dragon",
      "display_options": {
        "needs_flip": false,
        "rotation": 0
      },
      "variations": {
        "fire": { "hue_shift": 25, ... },
        "ice": { "hue_shift": 203, ... }
      }
    }
  ]
}
```

## 색상 접근성

### 색맹 사용자 고려
Hue Shift만으로는 색맹 사용자가 속성을 구분하기 어려울 수 있습니다. 다음 보조 수단을 함께 사용합니다:

- ✅ **이모지 아이콘**: 각 속성별 고유 이모지 (🔥, ❄️, 💨, ✨, 🌑)
- ✅ **텍스트 표시**: 몬스터 이름에 속성 표시 (예: "마그마 슬라임")
- ✅ **게임플레이 특성**: 속성별 HP/저항/보상 차이로 구분 가능

## 문제 해결

### Q: Hue Shift가 적용되지 않아요
**A**: 다음을 확인하세요:
1. `hue_shift` 값이 0이 아닌지 확인
2. 배치 파일이 `config/monsters/` 폴더에 있는지 확인
3. 게임을 재시작했는지 확인
4. 빌드 로그에 오류가 없는지 확인

### Q: 색상이 이상하게 나와요
**A**: Hue 값 범위 확인:
- 0~360도 범위 내 값 사용
- 속성별 권장 범위 참고
- 예: Fire는 10~30, 100~200은 청록색 계열

### Q: 성능이 느려졌어요
**A**: 캐시 통계 확인:
```csharp
var (cacheCount, maxSize) = HueShiftHelper.GetCacheStats();
Logger.Log($"Hue Cache: {cacheCount}/{maxSize}");
```
- 캐시가 200개 이상이면 자동으로 LRU 정리
- 동일 몬스터 재사용 시 캐싱으로 성능 최적화

## 향후 확장 가능성

### 1. 고급 색상 효과
```json
"fire": {
  "hue_shift": 15,
  "saturation": 1.2,     // 채도 조정 (미구현)
  "brightness": 0.9      // 밝기 조정 (미구현)
}
```

### 2. 애니메이션 Hue Shift
특수 이벤트 시 Hue 값을 시간에 따라 변화시켜 무지개 효과 구현 가능

### 3. GUI 편집기
Hue 값을 실시간으로 미리보고 조정하는 비주얼 에디터 도구

## 관련 파일

| 파일 | 역할 | 수정 가능 |
|------|------|----------|
| `config/GameData.json` | 기본 표시 설정 | ✅ |
| `config/monsters/batch_01.json` | Hue 값 및 display_options | ✅ |
| `Helpers/HueShiftHelper.cs` | Hue 변환 로직 | ❌ |
| `Models/Monster.cs` | Monster.HueShift 프로퍼티 | ❌ |
| `MainWindow.xaml.cs` | 렌더링 로직 | ❌ |

## 작성 일자
- **최초 작성**: 2026-02-05
- **시스템 버전**: v1.0
- **작성자**: Claude Code (승인: 사용자)
