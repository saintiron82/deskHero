---
name: balance-auto-experiment
description: 자동 밸런스 실험 스킬. 10h→50h 단계 확장 테스트, 케이스별 파라미터 탐색(HP 성장/티어/시간제한/업그레이드 비용/스탯 효율), 결과 CSV 분석 및 보고서 작성이 필요할 때 사용한다. 목표 조건(1시간 레벨 범위, 50시간 성장률 유지 등)을 만족하는 조합을 재귀적으로 찾는 작업에 적합하다.
---

# Balance Auto Experiment

## Overview
10시간 기준으로 목표 조건을 만족하는 파라미터를 탐색한 뒤 50시간으로 확장해 검증하고, 결과를 문서화한다.

## Workflow

### 1) 실험 목표 입력
- 목표 예시:
  - 1h 평균 레벨: 500 ± 10%
  - 50h에서 시간당 레벨 증가 ≥ 10
- 허용 범위와 우선순위를 명시한다.

### 2) 워크트리/브랜치 구성
- 기준 브랜치에서 케이스별 worktree 생성:
  - `git worktree add -b codex/<case-name> /path/to/worktrees/<case-name> <base-branch>`

### 3) 파라미터 레버 정의
다음 축에서 조합을 구성한다.
- HP 성장 곡선 (log 감속/선형 티어 감속)
- 티어 시스템 (growth_decrease_per_tier, tier_interval)
- 시간 제한 스케일링 (time_limit_scale, time_limit_scale_factor)
- 인게임 업그레이드 비용 (upgrade.cost_multiplier)
- 스탯 효율 (PermanentStats.json)

### 4) 10h 탐색 실행
- 케이스별 10h 테스트를 실행하고 `*_sessions.csv` 생성 확인.
- 기준 커맨드:
  - `dotnet DeskWarrior.Simulator.dll --progress --game-hours 10 --strategy balanced --output <outdir>/10h_balanced`
- 1h 평균 레벨, 10h 성장률을 계산해 목표와 비교.

### 5) 재귀 탐색
- 목표보다 낮으면: 감속 약화(예: decay_scale↑), 비용 완화(cost_multiplier↓)
- 목표보다 높으면: 감속 강화(예: decay_scale↓), 비용 강화(cost_multiplier↑)
- 목표 범위에 들어올 때까지 반복.

### 6) 50h 확장
- 10h 목표 통과 케이스만 50h 실행.
- `50h_balanced_sessions.csv` 기반으로 late gain(마지막 5시간 평균)을 계산해 유지 여부 확인.

### 7) 보고서 작성
- 필수 항목:
  - 케이스별 파라미터
  - 1h 평균 레벨(목표 충족 여부)
  - 10h/50h 성장률 요약
  - 병목 시간대
  - 결론 및 추천 케이스

## Output Format
- `balanceDoc/report_YYYY-MM-DD.md`
- 표 + 요약 문단으로 구성
