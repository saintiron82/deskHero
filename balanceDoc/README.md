# DeskWarrior 밸런스 문서 (balanceDoc/)

이 디렉토리는 DeskWarrior 게임의 모든 밸런스 관련 문서와 테스트 결과를 포함합니다.

---

## 📋 핵심 문서

### 1. balance_reference.md (★ 필수 읽기)
**목적**: 현재 구현된 모든 시스템과 수치의 완전한 레퍼런스
**용도**: 밸런스 작업 전 "현재 상태" 파악

**⚠️ MANDATORY**: 모든 밸런스 수정 전 이 문서를 읽고, 수정 후 반드시 업데이트하세요.

**포함 내용**:
- 전투 시스템 (데미지 8단계 공식)
- 몬스터 시스템 (HP, 골드, 속성, Tier HP)
- 확률 시스템 (등장 확률, 크리티컬)
- 영구 스탯 시스템 (36개 스탯)
- 크리스탈 보상 시스템
- 최종 밸런스 검증 결과 (50시간 테스트)

---

## 📊 분석 문서

### element_system_balance.md
**작성**: ba_ma 에이전트 (2026-02-03)
**내용**: 속성 시스템 밸런스 분석
- 속성별 등장 확률 계산
- 가중치 조정 권장사항
- Holy/Dark 레어도 분석

---

## 📁 날짜별 테스트 결과

### 2026-02-05/ (★ 최신)
**Tier HP 시스템 적용 및 50시간 검증**

**핵심 문서**:
- `CHANGES.md` - 모든 변경사항 상세 설명
- `balance_test_summary.md` - 테스트 결과 요약

**테스트 결과**:
- `test_10h_tier300.txt` - 10시간 (레벨 4,523)
- `test_20h_tier300_v2.txt` - 20시간 (레벨 5,811)
- `test_50h_tier300.txt` - 50시간 (레벨 6,332)

**주요 변경**:
- Tier HP 시스템 활성화 (tier_interval: 300, multiplier: 1.5)
- 모든 몬스터 HP 레벨 기준 통일 (base_hp: 40, hp_growth: 10)
- Timeout 기반 time_extend 자동 투자

### 2026-02-04/
**이전 버전 테스트 결과** (Tier HP 적용 전)
- `test_10h_balanced.csv`
- `test_20h_balanced.csv`
- `test_50h_adjusted.csv`

---

## 🔄 밸런스 작업 워크플로우

### 1. 작업 전 (필수)
```bash
# 1. 현재 상태 확인
Read: balanceDoc/balance_reference.md

# 2. ba_ma 에이전트로 분석 (필수!)
Task(subagent_type="ba_ma", prompt="...")
```

### 2. 변경 수행
```bash
# JSON 파일만 수정 (코드 수정 금지)
Edit: config/GameData.json
Edit: config/monsters/batch_01.json
```

### 3. 작업 후 (필수)
```bash
# 1. 영향 분석 (ba_ma)
Task(subagent_type="ba_ma", prompt="변경 영향 분석...")

# 2. balance_reference.md 업데이트
Edit: balanceDoc/balance_reference.md

# 3. 변경 이력 기록
git commit -m "..."
```

---

## 🤖 ba_ma 에이전트 사용 규칙

**⚠️ 모든 밸런스 작업은 반드시 ba_ma 서브에이전트를 사용합니다.**

**필수 사용 상황**:
- 공식/수치 변경 (HP, 데미지, 확률 등)
- 밸런스 분석 및 권장사항 도출
- 영향 예측 및 시뮬레이션
- 밸런스 문서 작성/갱신

**ba_ma 호출 시 필수 사항**:
```python
Task(
    subagent_type="ba_ma",
    prompt="""
    ⚠️ 먼저 balanceDoc/balance_reference.md를 읽고
    현재 구현된 정확한 공식과 수치를 파악하세요.

    [분석 요청 내용]
    """
)
```

**⛔ 절대 금지**:
- Claude가 직접 밸런스 분석/권장
- ba_ma 없이 밸런스 수치 조정
- 문서 확인 없이 밸런스 변경

---

## 📚 문서 구조

```
balanceDoc/
├── README.md                          ← 이 문서
├── balance_reference.md               ← ★ 핵심 레퍼런스
├── element_system_balance.md          ← 속성 시스템 분석 (ba_ma)
│
├── 2026-02-05/                        ← ★ 최신 (Tier HP)
│   ├── CHANGES.md                     ← 변경사항 상세
│   ├── balance_test_summary.md        ← 테스트 요약
│   ├── test_10h_tier300.txt
│   ├── test_20h_tier300_v2.txt
│   └── test_50h_tier300.txt
│
└── 2026-02-04/                        ← 이전 버전
    ├── test_10h_balanced.csv
    ├── test_20h_balanced.csv
    └── ...
```

---

## 🎯 현재 밸런스 상태 (2026-02-05)

### 활성화된 시스템
- ✅ Tier HP 시스템 (tier_interval: 300, multiplier: 1.5)
- ✅ 레벨 기준 HP (모든 몬스터 base_hp: 40, hp_growth: 10)
- ✅ Timeout 기반 time_extend 자동 투자
- ✅ 속성별 가중치 (normal: 50, fire/ice/wind: 50, holy: 2, dark: 6)

### 검증 상태
- ✅ 50시간 플레이 테스트 완료
- ✅ 레벨 6,332 도달 확인
- ✅ 평균 세션 89.9초 (안정적)
- ✅ 초반 쉽고 후반 완만한 난이도 곡선 달성

### 프로덕션 상태
- ✅ 배포 준비 완료
- ⏳ 실제 게임 수동 테스트 대기
- ⏳ 플레이어 피드백 수집 예정

---

**최종 업데이트**: 2026-02-05
**버전**: 2.0.0
**상태**: Production Ready
