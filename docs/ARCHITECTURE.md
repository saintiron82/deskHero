# 🏗️ Architecture - 기술 아키텍처

## 1. 기술 스택 (Tech Stack)

| 구분 | 기술 | 설명 |
|------|------|------|
| Platform | Windows Desktop | WPF 네이티브 |
| Framework | .NET 6/8 | LTS 버전 권장 |
| Language | C# | 최신 문법 활용 |
| Rendering | WPF Native | Image, RenderTransform |
| Input | Win32 API | Low-level Hook |
| Local Storage | System.Text.Json | 설정/저장 파일 |
| Cloud | Firebase | 랭킹 시스템 |

---

## 2. 윈도우 관리 (Window Management)

### 2.1 투명 윈도우
```xml
<Window 
    AllowsTransparency="True"
    WindowStyle="None"
    Background="Transparent"
    Topmost="True">
```

### 2.2 클릭 통과 (Click-through)
```csharp
// Win32 API 사용
[DllImport("user32.dll")]
static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

const int GWL_EXSTYLE = -20;
const int WS_EX_TRANSPARENT = 0x00000020;
const int WS_EX_LAYERED = 0x00080000;
```

---

## 3. 입력 처리 (Input Handling)

### 3.1 Global Keyboard Hook
```csharp
// Low-level keyboard hook
private const int WH_KEYBOARD_LL = 13;
private static IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);
```

### 3.2 Global Mouse Hook
```csharp
// Low-level mouse hook
private const int WH_MOUSE_LL = 14;
```

**주의**: 훅 해제 필수 (리소스 누수 방지)

---

## 4. 데이터 아키텍처 (Data Architecture)

### 4.1 설정 파일 (Read-Only)
**경로**: `config/GameData.json`

```
GameData
├── balance      # 밸런스 파라미터
├── upgrade      # 업그레이드 공식
└── visual       # 비주얼 설정
```

### 4.2 저장 파일 (Read/Write)
**경로**: `%AppData%/DeskWarrior/UserSave.json`

```
UserSave
├── profile      # 닉네임 등
├── stats        # 기록 통계
└── daily_logs   # 일별 로그
```

---

## 5. 클래스 설계 (Class Design)

```
DeskWarrior
├── App.xaml.cs           # 진입점
├── MainWindow.xaml.cs    # 메인 윈도우
├── Managers/
│   ├── GameManager.cs    # 게임 로직 총괄
│   ├── InputManager.cs   # 입력 처리
│   ├── DataManager.cs    # 저장/로드
│   └── AudioManager.cs   # 사운드 (선택)
├── Models/
│   ├── Monster.cs        # 몬스터 데이터
│   ├── GameData.cs       # 설정 데이터
│   └── UserSave.cs       # 저장 데이터
└── Views/
    ├── CharacterView.xaml
    └── MonsterView.xaml
```

---

## 6. 게임 루프 (Game Loop)

WPF의 `CompositionTarget.Rendering` 활용:

```csharp
CompositionTarget.Rendering += OnGameUpdate;

void OnGameUpdate(object sender, EventArgs e) {
    UpdateTimer();
    UpdateAnimations();
    CheckGameState();
}
```

**목표**: 항상 켜두어도 부담 없는 **경량 루프**
