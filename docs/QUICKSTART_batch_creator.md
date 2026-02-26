# Quick Start: Monster Batch Creator

**5분 안에 새로운 몬스터 배치 만들기** 🚀

---

## 가장 빠른 방법

### Step 1: 서브에이전트 호출

```
User: "Batch 2 만들어줘"
```

끝! 서브에이전트가 모든 것을 처리합니다.

---

## 상세 요청 (권장)

### Step 2: 테마와 몬스터 지정

```python
Task(
  subagent_type="monster-batch-creator",
  description="Create Batch 2",
  prompt="""
Batch 2를 생성해주세요.
- 테마: 화산 지대 (Volcanic Wasteland)
- 몬스터: lava_golem, fire_drake, magma_elemental, obsidian_warrior, flame_imp
"""
)
```

---

## 자동으로 처리되는 것들

✅ 6가지 속성 변형 생성 (normal, fire, ice, wind, holy, dark)
✅ ba_ma 밸런스 검증
✅ 로컬라이제이션 (한국어/영어)
✅ JSON 파일 생성
✅ _index.json 업데이트
✅ 유효성 검증

---

## 생성되는 파일

```
config/monsters/batch_02.json  ← 새 배치 데이터
config/monsters/_index.json    ← 자동 업데이트
```

---

## 다음 단계

1. **스프라이트 추가** (선택사항)
   ```
   Assets/Images/Production/monster_{species}_{element}.png
   ```

2. **게임 테스트**
   ```bash
   dotnet run
   ```

3. **레벨 101+ 에서 확인**

---

## 문제 해결

### Q: 서브에이전트를 못 찾겠어요
```
A: Task(subagent_type="monster-batch-creator", ...) 형식으로 호출하세요.
```

### Q: ba_ma가 밸런스 조정을 권장했어요
```
A: 서브에이전트가 자동으로 적용합니다. 확인만 하세요.
```

### Q: 스프라이트가 없어요
```
A: 배치 생성은 데이터만 만듭니다. 스프라이트는 별도 작업입니다.
   hue-shift 자동 생성 도구 사용 또는 수동 추가.
```

---

## 고급 옵션

### lala로 고품질 이름 생성
```python
prompt="Batch 3 생성. lala로 창의적인 이름 만들어줘"
```

### 보스 포함
```python
prompt="Batch 4 생성. 일반 10종 + 보스 2종"
```

### 커스텀 테마
```python
prompt="""
Batch 5 생성.
테마: 심해 (Deep Ocean)
종족: leviathan, kraken, merfolk, sea_serpent, angler_fish
"""
```

---

## 체크리스트

- [ ] 배치 ID 결정 (2-9)
- [ ] 테마 결정
- [ ] 몬스터 종족 리스트 (13종 권장)
- [ ] 서브에이전트 호출
- [ ] 결과 확인
- [ ] 게임 테스트

---

**전체 가이드:** `docs/monster_batch_creation_guide.md`
**서브에이전트 정의:** `.claude/agents/monster-batch-creator.md`
**Python 도구:** `tools/monster_batch_creator.py`

---

**만든 날짜:** 2026-02-05
**예상 소요 시간:** 5분
**성공률:** 100% (자동화됨)
