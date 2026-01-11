---
description: WPF 프로젝트 빌드 방법
---

# Build Workflow

## Prerequisites
- .NET 6 SDK 또는 .NET 8 SDK 설치
- Visual Studio 2022 (권장) 또는 VS Code + C# Extension

## Build Steps

// turbo-all

1. 프로젝트 루트로 이동
```powershell
cd c:\Users\saint\Game\DeskWarrior
```

2. NuGet 패키지 복원
```powershell
dotnet restore
```

3. Debug 빌드
```powershell
dotnet build --configuration Debug
```

4. Release 빌드
```powershell
dotnet build --configuration Release
```

5. 실행 파일 위치
- Debug: `bin\Debug\net6.0-windows\DeskWarrior.exe`
- Release: `bin\Release\net6.0-windows\DeskWarrior.exe`
