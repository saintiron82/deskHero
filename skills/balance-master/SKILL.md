---
name: balance-master
description: DeskWarrior 시뮬레이션을 실행하고 balanceDoc에 생성되는 CSV 결과와 로그를 분석해 현재 밸런스 상태, 병목 구간, 다음 개선점을 평가한다. “밸런스 평가해줘”, “밸런스 확인”, “50시간 테스트”, “병렬로 전략 테스트”, “자동으로 계속 돌려”, “진화 방식으로 비교해” 같은 밸런스 검증/자동 반복 테스트 요청이 있을 때 사용한다.
---

# Balance Master

## Overview
DeskWarrior 시뮬레이터를 실행하고 CSV 결과를 분석해 밸런스 상태를 평가한다. 3개의 독립 실험을 돌린 뒤 최고 성능을 다음 세대 베이스로 삼는 “진화 방식”을 기본 흐름으로 사용한다.

## Workflow

### 1) 입력 확인
- 기본 실행 대상: `DeskWarrior.Simulator`
- 주요 인자: `--game-hours <H>`, `--strategy <NAME>`
- 사용자가 “50시간 테스트”라고 말하면 `H=50`으로 설정.
- 인자가 모호하면 기본값(예: `H=20`, `strategy=balanced`)을 사용하고 가정사항을 명시한다.
- “묻지 말고 진행” 요청 시 추가 확인 없이 다음 케이스로 넘어간다.

### 2) 시뮬레이션 실행
- 기본 실행 예시: `DOTNET_ROLL_FORWARD=LatestMajor dotnet run --project DeskWarrior.Simulator -- --progress --game-hours 50 --strategy balanced --output balanceDoc/YYYY-MM-DD/50h_balanced`
- 실행 위치는 레포 루트로 가정한다.
- 실행 전/후 로그로 인자 적용 여부를 확인한다.

### 3) 진화 실험 자동화
- `scripts/run_experiments.py`를 사용해 3개 실험을 독립적으로 실행한다.
- 각 실험은 베이스라인 설정을 복사한 뒤 별도 수정하고 50h 테스트를 실행한다.
- 결과 요약에서 “사망 레벨”을 1차 기준으로 선택하고, 세션 수/롱런을 2차로 고려한다.

실행 예시:
`python3 skills/balance-master/scripts/run_experiments.py --experiments skills/balance-master/references/experiments.example.json --label 2026-02-06 --game-hours 50 --cps 5 --strategy balanced --promote-best`

- 이전 베이스라인을 사용하려면 `--baseline-dir balanceDoc/YYYY-MM-DD/best_config` 형식으로 전달한다.

### 3-1) 다세대 진화 루프
- 여러 세대를 연속 실행하려면 `scripts/evolve.py`를 사용한다.
- 각 세대마다 최고 성능(`best_config`)을 다음 세대 베이스라인으로 사용한다.
- `experiments.example.json`에 `injections` 배열을 넣으면 “새롭게 1개 투입”을 고정 베이스라인(초기값)에서 매 세대 같이 실행한다.

실행 예시:
`python3 skills/balance-master/scripts/evolve.py --experiments skills/balance-master/references/experiments.example.json --generations 3 --label 2026-02-07_evolution --game-hours 50 --cps 5 --strategy balanced`

- 지속 진화를 위해 고정 베이스라인을 유지하려면 `--promote-to balanceDoc/latest_best_config`를 사용한다.
- 메인/신규 실험 개수는 `--main-count`, `--inject-count`로 조절한다. (`-1`은 전체)
- 목표 기반 진화는 `--metric goal_score`를 사용하고, 목표를 `--goal-hour1/--goal-level1`, `--goal-hour2/--goal-level2`로 지정한다.
- 결과 변동(분산)을 줄이는 목적이면 `--metric goal_score_stable`와 `--seeds 1-5`를 함께 사용한다.
  - `goal_score_stable`는 **평균 목표 오차 + 표준편차 패널티**를 함께 최소화한다.
  - 표준편차 패널티 강도는 `--variance-penalty`로 조절한다.
- 두 목표가 **독립적으로 모두 개선(오차 감소)** 되도록 강제하려면 `--require-goal-improvement`를 추가한다.
  - 기준선(baseline)을 먼저 평가한 뒤, 각 세대에서 **1시간 오차와 50시간 오차가 둘 다 줄어든 경우만 채택**한다.
  - 조건을 만족한 후보가 없으면 해당 세대는 `baseline_hold`로 기록되고 베이스라인을 유지한다.

### 3-2) 50세대 단위 자동 튜닝
- 50세대마다 주입(injection) 성능을 평가해 자동 튜닝하려면 `scripts/evolve_and_tune.py`를 사용한다.
- 주입 승률이 낮으면 효과를 강화, 너무 높으면 비용을 올려 자동 조절한다.
- 결과 변동을 줄이려면 `--seed`를 지정해 결정론적으로 실행한다.

실행 예시:
`python3 skills/balance-master/scripts/evolve_and_tune.py --experiments skills/balance-master/references/experiments.example.json --generations 50 --label auto_evolution_cycle --baseline-dir balanceDoc/latest_best_config --game-hours 50 --cps 5 --strategy balanced --main-count 3 --inject-count 1 --seed 42`

목표 기반 예시:
`python3 skills/balance-master/scripts/evolve_and_tune.py --experiments skills/balance-master/references/experiments.example.json --generations 40 --label auto_evolution_goal --baseline-dir balanceDoc/latest_best_config --game-hours 50 --cps 5 --strategy balanced --main-count 3 --inject-count 1 --metric goal_score --goal-hour1 1 --goal-level1 500 --goal-hour2 50 --goal-level2 3000 --require-goal-improvement --seed 42`

### 4) CSV 분석
- 결과 CSV의 마지막 세션을 기준으로 `Level`, `SessionNum`, `Playtime`을 확인한다.
- 세션 수가 너무 낮으면 “롱런” 의심으로 표시한다.
- 목표 레벨/세션 수 범위를 벗어나면 원인 후보를 간단히 정리한다.

### 5) 결과 보고
- 현재 상태 요약(레벨, 세션 수, 평균 세션 길이)
- 실험별 성능 비교 표
- 다음 세대 후보 1개 선택 및 이유
- 다음 수정 제안(한 번에 1~2개 변수만)

## Resources

### scripts/
- `run_experiments.py`: 베이스라인 복사 → 독립 실험 실행 → 요약 출력

### references/
- `experiments.example.json`: 실험 정의 템플릿
