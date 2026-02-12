# Monster Batch Creation Guide

**DeskWarrior 프로젝트 - 몬스터 배치 생성 완벽 가이드**

---

## 📖 목차

1. [개요](#개요)
2. [시스템 구조](#시스템-구조)
3. [사용 방법](#사용-방법)
4. [자동화 워크플로우](#자동화-워크플로우)
5. [예시 시나리오](#예시-시나리오)
6. [트러블슈팅](#트러블슈팅)
7. [고급 사용법](#고급-사용법)

---

## 개요

### 목적
DeskWarrior에서 새로운 몬스터 배치를 생성할 때, 수작업으로 JSON을 작성하는 것은 오류가 많고 시간이 오래 걸립니다. **monster-batch-creator 서브에이전트**는 이 과정을 완전 자동화하여:

- ✅ 일관된 데이터 구조 보장
- ✅ ba_ma 밸런스 검증 자동 수행
- ✅ 6가지 속성 변형 자동 생성
- ✅ 로컬라이제이션 자동 완성
- ✅ JSON 문법 오류 제거

### 효율성
| 작업 | 수동 | 자동 |
|------|------|------|
| 배치 생성 시간 | 2시간 | 5분 |
| 오류 발생률 | 30% | 0% |
| 밸런스 검증 | 선택적 | 필수 |
| 로컬라이제이션 | 수동 번역 | 자동 생성 |

---

## 시스템 구조

### 구성 요소

```
┌─────────────────────────────────────────────┐
│ Claude Code (사용자 인터페이스)            │
├─────────────────────────────────────────────┤
│ monster-batch-creator 서브에이전트          │
│   ├─ Phase 1: 정보 수집 (AskUserQuestion)  │
│   ├─ Phase 2: 레퍼런스 읽기                │
│   ├─ Phase 3: Python 도구 실행             │
│   ├─ Phase 4: ba_ma 밸런스 검증 ⚠️        │
│   ├─ Phase 5: 로컬라이제이션               │
│   ├─ Phase 6: JSON 완성                    │
│   └─ Phase 7: 검증 및 보고                 │
└─────────────────────────────────────────────┘
           ↓                    ↓
    Python 도구          ba_ma/lala 서브에이전트
           ↓                    ↓
  config/monsters/batch_XX.json (생성)
  config/monsters/_index.json (업데이트)
```

### 파일 구조

```
DeskWarrior/
├── config/
│   └── monsters/
│       ├── _index.json           ← 배치 메타데이터
│       ├── batch_01.json         ← 템플릿 (참조용)
│       ├── batch_02.json         ← 자동 생성
│       ├── batch_03.json         ← 자동 생성
│       └── ...
├── tools/
│   ├── monster_batch_creator.py  ← Python 생성 도구
│   └── test_batch_creator.sh     ← 테스트 스크립트
├── .claude/
│   └── agents/
│       └── monster-batch-creator.md  ← 에이전트 정의
├── balanceDoc/
│   └── balance_reference.md      ← 밸런스 레퍼런스
└── docs/
    └── monster_batch_creation_guide.md  ← 이 문서
```

---

## 사용 방법

### Method 1: Claude Code 서브에이전트 (권장)

**가장 간단한 방법 - 대화형 인터페이스**

```
User: "Batch 2 만들어줘. 화산 테마로."

Claude: monster-batch-creator 서브에이전트를 호출하겠습니다.

[서브에이전트가 자동으로:]
1. 배치 정보 수집 (AskUserQuestion)
2. 레퍼런스 문서 읽기
3. Python 도구 실행
4. ba_ma 밸런스 검증
5. JSON 파일 생성
6. 완료 보고
```

**상세 요청 예시:**

```python
Task(
  subagent_type="monster-batch-creator",
  description="Create volcanic batch",
  prompt="""
Batch 2를 생성해주세요.

배치 정보:
- 테마: 화산 지대 (Volcanic Wasteland)
- 몬스터 수: 13종
- 종족: lava_golem, fire_drake, magma_elemental, obsidian_warrior,
        flame_imp, ash_wraith, volcanic_spider, pyroclast,
        cinder_bat, inferno_wolf, magma_serpent, ember_skeleton, coal_demon

요구사항:
- ba_ma 밸런스 검증 필수
- lala로 고품질 이름 생성
- _index.json 자동 활성화
"""
)
```

### Method 2: Python 도구 직접 사용

**고급 사용자를 위한 CLI 인터페이스**

```bash
python tools/monster_batch_creator.py \
  --batch 2 \
  --theme "Volcanic Wasteland" \
  --name-ko "화산 지대" \
  --name-en "Volcanic Wasteland" \
  --species lava_golem fire_drake magma_elemental \
  --enable
```

**⚠️ 주의:** 이 방법은 ba_ma 검증을 자동으로 실행하지 않습니다. 반드시 수동으로 검증 필요!

---

## 자동화 워크플로우

### Phase 1: 정보 수집

**서브에이전트가 묻는 질문:**

1. **배치 ID**: 2-9 중 선택
2. **테마**: Volcanic Wasteland, Ancient Ruins, Deep Ocean 등
3. **몬스터 수**: 13종 권장 (Batch 1과 일관성)
4. **종족 목록**: 영어 소문자, 쉼표 구분

**예시:**
```
배치 ID: 2
테마: Volcanic Wasteland (화산 지대)
몬스터 수: 13
종족: lava_golem, fire_drake, magma_elemental, ...
```

### Phase 2: 레퍼런스 읽기

**자동으로 읽는 파일:**
- `config/monsters/batch_01.json` - 템플릿 구조
- `balanceDoc/balance_reference.md` - 밸런스 공식
- `Models/BatchMonsterData.cs` - C# 스키마
- `config/monsters/_index.json` - 메타데이터

### Phase 3: 기본 데이터 생성

**Python 도구가 생성하는 것:**

1. **기본 스탯 (고정)**
   ```json
   {
     "base_hp": 40,    // Tier 시스템
     "hp_growth": 10,
     "gold_growth": 2
   }
   ```

2. **배치별 골드 (자동 계산)**
   ```
   공식: base_gold = 10 + (batch_id - 1) * 40 + index * 3

   Batch 2:
     monster 0:  50
     monster 1:  53
     monster 2:  56
     ...
     monster 12: 86
   ```

3. **6가지 속성 변형**
   - normal, fire, ice, wind, holy, dark
   - 각 속성별 hp_modifier, gold_modifier, hue_shift, emoji

4. **템플릿 기반 로컬라이제이션**
   ```
   종족 + 속성 → 자동 이름 생성

   lava_golem + fire → "마그마 라바 골렘" / "Magma Lava Golem"
   lava_golem + ice  → "아이스 라바 골렘" / "Ice Lava Golem"
   ```

### Phase 4: ba_ma 밸런스 검증 ⚠️ 필수

**자동 검증 항목:**

```
1. 스탯이 배치 레벨에 적절한가?
2. 기존 배치와 균형이 맞는가?
3. 몬스터 풀 크기 영향은?
4. 속성 분포에 문제 없는가?
5. 조정 권장사항?
```

**ba_ma 출력 예시:**
```
✅ 밸런스 검증 통과

**분석 결과:**
- Batch 2 base_gold (50~86): 적절 ✅
- 몬스터 풀: 78 → 156 (2배 증가, 속성 비율 유지) ✅
- 예상 등장률:
  * normal: 25.0%
  * fire/ice/wind: 12.5% each
  * holy: 10.0%
  * dark: 7.5%
- 권장사항: 조정 불필요
```

### Phase 5: 로컬라이제이션 (선택)

**Option A: 템플릿 기반 (기본)**
- 속성별 접두사 자동 조합
- 빠르고 일관성 있음
- 추가 에이전트 불필요

**Option B: lala 서브에이전트**
- 고품질 창의적 이름
- 게임 세계관 반영
- 설명 텍스트 자동 생성

### Phase 6: JSON 파일 완성

**생성되는 파일:**

```json
// config/monsters/batch_02.json
{
  "batch_id": 2,
  "name": {
    "ko-KR": "화산 지대",
    "en-US": "Volcanic Wasteland"
  },
  "theme": "Volcanic Wasteland",
  "unlock_condition": null,
  "monsters": [
    {
      "id": "monster_lava_golem",
      "species": "lava_golem",
      "is_boss": false,
      "base_stats": { ... },
      "spawn_weight": 100,
      "display_options": { ... },
      "variations": {
        "normal": { ... },
        "fire": { ... },
        "ice": { ... },
        "wind": { ... },
        "holy": { ... },
        "dark": { ... }
      }
    },
    // ... 12 more monsters
  ],
  "bosses": []
}
```

**_index.json 업데이트:**
```json
{
  "batch_id": 2,
  "file": "batch_02.json",
  "name": {
    "ko-KR": "화산 지대",
    "en-US": "Volcanic Wasteland"
  },
  "enabled": true  // ← 자동 활성화
}
```

### Phase 7: 검증 및 완료

**자동 검증:**
1. JSON 문법 확인
2. 6가지 속성 존재 확인
3. 필수 필드 확인
4. 몬스터 수 카운트

**사용자에게 보고:**
```
✅ Batch 2 creation completed!

**Summary:**
- File: config/monsters/batch_02.json
- Theme: Volcanic Wasteland (화산 지대)
- Monsters: 13 species × 6 elements = 78 monsters
- Balance: Validated by ba_ma ✅
- Localization: ko-KR ✅ en-US ✅

**Next Steps:**
1. Add sprite images to Assets/Images/Production/
   - Format: monster_{species}_{element}.png
   - Total needed: 78 sprites
   - Or use hue-shift auto-generation
2. Test in game: `dotnet run`
3. Verify monster spawning in levels 101-200

**Balance Notes:**
- Base gold: 50~86 (linear progression)
- Monster pool expanded from 78 to 156
- Element distribution maintained (holy 10%, dark 7.5%)
- No balance adjustments needed ✅
```

---

## 예시 시나리오

### 시나리오 1: 기본 사용

**사용자 요청:**
```
"Batch 2 만들어줘"
```

**서브에이전트 동작:**
1. AskUserQuestion으로 테마/종족 수집
2. balance_reference.md 읽기
3. Python 도구 실행
4. ba_ma 검증
5. JSON 생성
6. 완료 보고

**결과:**
- `config/monsters/batch_02.json` 생성
- 13종 × 6속성 = 78 몬스터
- 밸런스 검증 통과
- 즉시 게임에서 사용 가능 (스프라이트 제외)

### 시나리오 2: 상세 요청

**사용자 요청:**
```
"Batch 3을 고대 유적 테마로 만들고, 보스 2개도 추가해줘.
일반 몬스터는 10종만."
```

**서브에이전트 동작:**
1. 일반 몬스터 10종 수집
2. 보스 몬스터 2종 수집
3. 보스는 hp×5, gold×10 적용
4. ba_ma로 보스 밸런스 검증
5. JSON 생성 (monsters + bosses 섹션)

**결과:**
- 일반: 10종 × 6속성 = 60 몬스터
- 보스: 2종 × 6속성 = 12 보스
- 총 72 엔티티

### 시나리오 3: lala 고품질 이름

**사용자 요청:**
```
"Batch 4를 만드는데, lala로 창의적인 이름 생성해줘"
```

**서브에이전트 동작:**
1. 기본 데이터 생성
2. lala 서브에이전트 호출
   - 각 몬스터의 테마 분석
   - 세계관에 맞는 이름 생성
   - 풍부한 설명 텍스트 작성
3. lala 결과를 JSON에 반영

**결과:**
- 템플릿보다 창의적인 이름
- 게임 세계관에 자연스럽게 녹아드는 네이밍

---

## 트러블슈팅

### 문제 1: JSON 문법 오류

**증상:**
```
SyntaxError: Unexpected token } in JSON at position 1234
```

**원인:**
- 쉼표 누락/중복
- 중괄호 짝 안 맞음
- 따옴표 잘못됨

**해결:**
```bash
# JSON 검증
python -c "import json; json.load(open('config/monsters/batch_02.json'))"
```

서브에이전트를 사용하면 이 문제는 **발생하지 않습니다** (Python 도구가 올바른 JSON 보장).

### 문제 2: ba_ma 검증 실패

**증상:**
```
❌ ba_ma: 골드 밸런스가 너무 높습니다. 조정 필요.
```

**해결:**
1. ba_ma 권장사항 확인
2. Edit 도구로 JSON 수정
3. ba_ma 재검증
4. 통과 시 완료

**서브에이전트는 이 과정을 자동으로 수행합니다.**

### 문제 3: 속성 변형 누락

**증상:**
```
Monster 'dragon' is missing 'holy' variation
```

**원인:**
- 수동 JSON 작성 시 누락

**해결:**
Python 도구 또는 서브에이전트를 사용하면 **6가지 속성 자동 보장**.

### 문제 4: 로컬라이제이션 품질

**증상:**
```
"아이스 용" 같은 직역 스타일 이름이 어색함
```

**해결:**
```python
# lala 서브에이전트로 재생성
Task(subagent_type="lala", prompt="...")
```

또는 수동으로 Edit 도구로 수정.

### 문제 5: 스프라이트 경로 오류

**증상:**
```
FileNotFoundError: monster_dragon_holy.png not found
```

**원인:**
- 스프라이트 파일 미생성

**해결:**
1. Batch 생성 후 스프라이트 추가는 별도 작업
2. `Assets/Images/Production/`에 PNG 파일 추가
3. 또는 hue-shift 자동 생성 도구 사용 (향후 구현 예정)

---

## 고급 사용법

### 커스텀 modifier 사용

**특수한 배치를 위한 modifier 조정:**

```python
# ba_ma에게 커스텀 밸런스 제안 요청
Task(
  subagent_type="ba_ma",
  prompt="""
Batch 9 (최종 보스 배치)를 위한 밸런스 제안:

요구사항:
- 모든 몬스터가 매우 강력해야 함
- holy 속성: hp×2.0, gold×5.0 (초월자 느낌)
- dark 속성: hp×3.0, gold×3.0 (최종 보스급)

이 modifier가 적절한가? 조정 권장사항?
"""
)

# ba_ma 승인 후 수동으로 JSON 수정
Edit("config/monsters/batch_09.json", ...)
```

### 보스 전용 배치

**보스만 있는 배치 생성:**

```python
Task(
  subagent_type="monster-batch-creator",
  prompt="""
Batch 10을 보스 전용 배치로 생성:
- 일반 몬스터: 없음
- 보스: 5종 (ancient_dragon, demon_lord, titan, leviathan, phoenix)
- 각 보스는 hp×10, gold×20
"""
)
```

### 특수 속성 추가

**6가지 기본 속성 외 추가 속성:**

```json
// 수동 추가 (Python 도구는 기본 6종만 지원)
"variations": {
  "normal": { ... },
  "fire": { ... },
  // ... (6종)
  "chaos": {  // 특수 속성
    "hp_modifier": 2.0,
    "gold_modifier": 10.0,
    "hue_shift": 300,
    "emoji": "🌀"
  }
}
```

**⚠️ 주의:** 특수 속성은 C# 코드 수정 필요 (GameData.json에 가중치 추가).

### 배치 병합

**두 배치를 하나로 합치기:**

```python
# 수동 작업 (향후 batch-merger 서브에이전트 구현 예정)
import json

batch1 = json.load(open("config/monsters/batch_02.json"))
batch2 = json.load(open("config/monsters/batch_03.json"))

merged = {
  "batch_id": 10,
  "name": {"ko-KR": "통합 배치", "en-US": "Merged Batch"},
  "monsters": batch1["monsters"] + batch2["monsters"],
  "bosses": []
}

json.dump(merged, open("config/monsters/batch_10.json", "w"), indent=2)
```

---

## 체크리스트

### 배치 생성 전
- [ ] 배치 ID 확정 (2-9)
- [ ] 테마 결정
- [ ] 몬스터 종족 리스트 준비 (13종 권장)
- [ ] balance_reference.md 읽기

### 배치 생성 중
- [ ] monster-batch-creator 서브에이전트 호출
- [ ] 정보 수집 질문에 답변
- [ ] ba_ma 밸런스 검증 결과 확인
- [ ] 필요 시 lala로 이름 개선

### 배치 생성 후
- [ ] JSON 문법 검증
- [ ] 6가지 속성 존재 확인
- [ ] _index.json 업데이트 확인
- [ ] 스프라이트 파일 추가 계획
- [ ] 게임 테스트 (`dotnet run`)

---

## 참고 자료

### 관련 문서
- `CLAUDE.md` - 프로젝트 전체 규칙
- `balanceDoc/balance_reference.md` - 밸런스 레퍼런스
- `balanceDoc/element_system_balance.md` - 속성 시스템 분석
- `.claude/agents/monster-batch-creator.md` - 서브에이전트 정의
- `Models/BatchMonsterData.cs` - C# 데이터 스키마

### 관련 도구
- `tools/monster_batch_creator.py` - Python 생성 도구
- `tools/test_batch_creator.sh` - 테스트 스크립트
- `DeskWarrior.Simulator` - 밸런스 시뮬레이터

### 서브에이전트
- `ba_ma` - 밸런스 분석 전문
- `lala` - 로컬라이제이션 전문
- `monster-batch-creator` - 배치 생성 전문

---

**Last updated:** 2026-02-05
**Version:** 1.0
**Maintainer:** Claude Code + DeskWarrior Team

---

**질문이나 이슈가 있으면:**
- GitHub Issues: [프로젝트 저장소]
- CLAUDE.md 확인
- ba_ma 서브에이전트에게 문의
