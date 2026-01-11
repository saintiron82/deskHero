# ⚔️ DeskWarrior (Work Warrior)

> **"일(Typing)을 열심히 했더니, 어느새 용사가 세상을 구했다."**

Bongo Cat 스타일의 데스크탑 액세서리 게임. 키보드/마우스 입력을 게임 공격으로 변환하여 업무 중 시각적 즐거움을 제공합니다.

## 📋 Quick Links

- [Game Design Document](docs/GDD.md)
- [Game Mechanics](docs/MECHANICS.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Balance Config](config/GameData.json)

## 🛠️ Tech Stack

- **Platform**: Windows Desktop (WPF)
- **Framework**: .NET 6/8
- **Language**: C#

## 🚀 Getting Started

```powershell
# 프로젝트 빌드
dotnet build

# 실행
dotnet run
```

## 👥 Agents

| Agent | Role | Doc |
|-------|------|-----|
| **jina** | 기획 | [.agent/jina.md](.agent/jina.md) |
| **lily** | 코드 | [.agent/lily.md](.agent/lily.md) |

## 📁 Project Structure

```
DeskWarrior/
├── .agent/           # 에이전트 제어
├── docs/             # 게임 문서
├── config/           # 설정 파일
└── src/              # 소스 코드 (TBD)
```

## 📜 License

Private Project
