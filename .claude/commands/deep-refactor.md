# Deep Refactor - 통합 리팩토링 스킬

recursive-planner와 code-remaker를 순차적으로 실행하여 구조 개선과 중복 제거를 한번에 수행합니다.

## 실행 워크플로우

### Phase 1: 구조 분석 및 리팩토링 (recursive-planner)
Task 도구로 `recursive-planner` 에이전트를 호출하여:
- 대상 코드의 파일 크기/복잡도 분석
- SOLID 원칙 위반 탐지
- 클래스 계층 구조 문제 파악
- 구조화된 분해 계획 수립 및 실행

### Phase 2: 중복 분석 및 통합 (code-remaker)
Phase 1 완료 후, Task 도구로 `code-remaker` 에이전트를 호출하여:
- 분리된 모듈들에서 중복 패턴 탐지
- 통합 가능한 코드 식별
- 공통 유틸리티 추출 및 통합 파이프라인 생성

## 사용법

```
/deep-refactor [대상 경로 또는 설명]
```

예시:
- `/deep-refactor src/Services/` - Services 폴더 전체 리팩토링
- `/deep-refactor UserService.cs가 너무 큼` - 특정 파일 리팩토링
- `/deep-refactor` - 대화 컨텍스트 기반으로 대상 결정

## 실행 지침

1. 사용자가 대상을 지정하지 않으면 먼저 대상을 확인할 것
2. Phase 1 완료 후 결과를 요약하고 Phase 2 진행 여부 확인
3. 각 Phase는 별도 Task 에이전트로 실행
4. 최종 결과를 정리하여 보고

## 주의사항

- 큰 변경이므로 git 상태 확인 후 진행
- 각 Phase 완료 시 중간 결과 공유
- 사용자 승인 없이 자동 커밋하지 않음
