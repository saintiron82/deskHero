# Balance - 밸런스 실험 스킬

ba_ma 에이전트를 호출하여 밸런스 실험과 측정을 수행합니다.

## 실행 방법

Task 도구로 `ba_ma` 에이전트를 호출하세요:

```
Task tool:
  subagent_type: ba_ma
  prompt: [사용자 요청에 따른 실험 지시]
```

## 인자에 따른 실험 모드

| 인자 | 실험 내용 |
|------|----------|
| (없음) | 현재 밸런스 상태 측정 (10시간 전략 비교) |
| `quick` | 빠른 측정 (1시간 전략 비교) |
| `[스탯명]` | 해당 스탯의 효과 실험 |
| `fix [문제]` | 문제 해결을 위한 반복 실험 |

## 사용 예시

```
/balance              → 현재 밸런스 측정
/balance quick        → 빠른 측정
/balance crit_damage  → crit_damage 효과 실험
/balance fix survival → 생존 전략 개선 실험
```

## ba_ma 에이전트 호출 예시

### 기본 측정
```
Task tool:
  subagent_type: ba_ma
  prompt: |
    현재 밸런스 상태를 측정하세요.
    1. C# 시뮬레이터 실행: dotnet run -- --analyze --crystals 0 --game-hours 10 --cps 5
    2. 결과 요약 (전략별 레벨, Dominance Ratio, Grade)
```

### 문제 해결 실험
```
Task tool:
  subagent_type: ba_ma
  prompt: |
    SurvivalFirst 전략이 너무 약합니다. 개선 실험을 수행하세요.
    1. 현재 상태 측정
    2. time_extend 또는 관련 스탯 효과 조정
    3. 재측정하여 개선 확인
    4. 목표: Dominance Ratio 1.3 이하
```
