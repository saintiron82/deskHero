---
description: 개발 환경 설정 및 작업 흐름
---

# Development Workflow

## 환경 설정

### 필수 도구
- Windows 10/11
- .NET 6/8 SDK
- Visual Studio 2022 또는 VS Code

### 프로젝트 생성 (최초 1회)
```powershell
cd c:\Users\saint\Game\DeskWarrior
dotnet new wpf -n DeskWarrior -f net6.0-windows
```

## 개발 흐름

### 1. 기능 개발 시작
1. `docs/GDD.md`에서 구현할 기능 스펙 확인
2. `config/GameData.json`에서 관련 파라미터 확인

### 2. 코드 작성
- `src/` 디렉토리에 기능별 클래스 작성
- 네이밍 규칙은 `.agent/lily.md` 참조

### 3. 테스트
```powershell
dotnet run --project DeskWarrior
```

### 4. 커밋 메시지 규칙
```
[Type] 간단한 설명

Type:
- feat: 새 기능
- fix: 버그 수정
- docs: 문서 수정
- refactor: 리팩토링
- style: 코드 스타일
```
