# balanceDoc - Balance Master 참조 문서

이 폴더는 `balance_master` 에이전트가 참조하는 문서들을 보관합니다.

## 파일 목록

| 파일 | 용도 |
|------|------|
| `balance-knowledge.md` | 핵심 공식, 상수, 밸런스 기준, 분석 리포트 |

## 사용법

`balance_master` 에이전트는 분석 전 이 폴더의 문서들을 먼저 읽습니다.

```
Task tool → balance_master 에이전트
                    ↓
           balanceDoc/*.md 읽기
                    ↓
              분석 수행
```

## 문서 업데이트

- 분석 결과는 `balance-knowledge.md`에 추가됩니다
- 공식 변경 시 문서도 함께 업데이트해야 합니다
