# 🐛 버그 솔루션 기록

> 테스트 중 발견된 주요 버그와 해결 방법 기록

---

## CP-003: 코어 게임 로직

### BUG-001: 네임스페이스 충돌
**증상**: `InputEventArgs`, `InputManager` 등이 `System.Windows.Input`과 충돌

**원인**: WPF 기본 네임스페이스와 커스텀 클래스명이 동일

**해결**: 
- `InputEventArgs` → `GameInputEventArgs`
- `InputManager` → `GlobalInputManager`
- `InputType` → `GameInputType`
- `MouseButton` → `GameMouseButton`

**교훈**: WPF 프로젝트에서는 `System.Windows.Input` 네임스페이스의 타입명을 피할 것

---

### BUG-002: 트레이 아이콘 미표시
**증상**: NotifyIcon이 시스템 트레이에 표시되지 않음

**원인**: `SystemIcons.Application`이 제대로 표시되지 않음

**해결**: `SystemIcons.Shield`로 변경

**교훈**: 트레이 아이콘은 명확하게 보이는 시스템 아이콘 사용

---

### BUG-003: 이미지 알파 채널 미적용
**증상**: AI 생성 이미지의 체커보드 배경이 그대로 표시

**원인**: AI 이미지 생성기가 실제 알파 채널이 아닌 체커보드 패턴 생성

**해결**: 
1. 초록색(#00FF00) 배경으로 이미지 생성
2. `ImageHelper.cs`에서 크로마 키 처리 (초록색 → 투명)

**코드**:
```csharp
if (g > 150 && g > r + 50 && g > b + 50)
{
    pixels[i + 3] = 0; // 알파를 0으로
}
```

**교훈**: 투명 배경이 필요하면 크로마 키 방식 사용

---

## 버그 기록 템플릿

```markdown
### BUG-XXX: [제목]
**증상**: 

**원인**: 

**해결**: 

**교훈**: 
```
