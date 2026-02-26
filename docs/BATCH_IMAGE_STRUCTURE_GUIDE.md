# Batch Image Directory Structure Guide

**Purpose:** Standard directory structure and naming conventions for monster batch images

---

## Pipeline Authority

```
monster_planning.md → monster_progress.md → Org 저장 → Production 변환
```

**규칙:** `planning.md`에 정의된 몬스터만 Org에 저장. 파일 추가/이동 시 `progress.md` 업데이트 필수.

---

## Directory Structure

### 2-Tier Resource Pipeline

```
Resources/Org/                    <-- 1단계: 원본 (AI 생성, 미처리)
  ├── Batch_01/
  │   ├── Monster/                  monster_slimeA.png, monster_bat_fire.png ...
  │   └── Boss/                     boss_dragonA.png ...
  ├── Batch_02/
  │   ├── Monster/
  │   └── Boss/
  ├── Batch_03/
  │   ├── Monster/
  │   └── Boss/
  ├── Batch_04/
  │   └── Monster/
  ├── Hero/                         hero_warrior.png ...
  ├── Special/                      monster_goldengoblin.png ...
  └── Uncategorized/                테스트/미분류 파일

Assets/Images/Production/         <-- 2단계: 프로덕션 (처리 완료, 게임 로드)
  ├── Batch1/
  │   ├── Monster/                  monster_slime.png (256x256, 방향 보정)
  │   └── Boss/                     boss_dragon.png
  ├── Batch2/
  │   ├── Monster/
  │   └── Boss/
  ├── Special/                      monster_goldengoblin.png
  └── (future: UI/, Background/, Hero/)
```

### Processing Flow

```
Resources/Org/{Batch}/          원본 이미지
        |
        v  (sprite-processor)
        |  - 배경 제거
        |  - 여백 조절 (fill ratio ~85%)
        |  - 256x256 리사이즈
        |  - 방향 보정 (왼쪽/플레이어 방향)
        |  - 파일명 정규화 (A 접미사 제거)
        v
Assets/Images/Production/{Batch}/   게임 로드용 최종 이미지
```

---

## Naming Conventions

### Original Resources (Resources/Org/)

**Base Image:** `monster_{species}A.png` (AI 생성 원본)
**Element Variation:** `monster_{species}_{element}.png` (속성 변형)
**Boss:** `boss_{species}A.png`

### Production Resources (Assets/Images/Production/)

**Monster:** `monster_{species}.png` (A 접미사 없음)
**Boss:** `boss_{species}.png`

**Rules:**
- Lowercase only
- Underscores for separation
- Species name from JSON `"species"` field

---

## File Requirements

### Image Format

- **Format:** PNG
- **Transparency:** Alpha channel required (RGBA 32-bit)
- **Size:** 256x256 pixels
- **Direction:** 몬스터가 왼쪽(플레이어 방향)을 향하도록 보정 완료

### Hue-Shift Support

모든 몬스터 이미지는 런타임 hue-shift로 6속성 변형을 지원합니다.
단일 `monster_slime.png`로 6가지 색상 슬라임을 렌더링합니다.

---

## Game Load Path

### How sprites are loaded

```
batch_01.json sprite 값
    "Production/Batch1/Monster/monster_slime.png"
        |
        v
pack://application:,,,/Assets/Images/ + sprite
        |
        v
Assets/Images/Production/Batch1/Monster/monster_slime.png
```

### ResourcePaths.json

```json
{
  "batches": {
    "batch_01": {
      "sprite_base": "Production/Batch1",
      "monster_folder": "Production/Batch1/Monster",
      "boss_folder": "Production/Batch1/Boss"
    }
  }
}
```

---

## Adding New Batch Images

### Step 1: Generate original images
Place in `Resources/Org/Batch_0N/Monster/` and `Boss/`

### Step 2: Process with sprite-processor
```
sprite-processor agent:
- Source: Resources/Org/Batch_0N/
- Output: Assets/Images/Production/BatchN/
```

### Step 3: Update ResourcePaths.json
Batch entry should point to `Production/BatchN/`

### Step 4: Create batch_0N.json
Use `monster-batch-creator` sub-agent

### Step 5: Verify
- `dotnet run` - check sprite loading in-game
- `python tools/sprite_viewer.py` - visual overview

---

## Deprecated Directories

### Do NOT Use

- `Assets/Images/Batches/` - **DELETED** (replaced by Production/)
- `Assets/Images/_deprecated/` - archived
- `Assets/Images/UseImage/` - archived
- `Assets/Images/Raw_Green/` - archived (merged into `Resources/Org/`)
- `Resources/Org/Raw_Green/` - **DELETED** (merged into `Resources/Org/Batch_0N/`)

---

## Batch 1 Reference (25 species)

### Monsters (20)
slime, bat, skeleton, goblin, orc, ghost, golem, mushroom, spider,
wolf, snake, boar, bee, crab, turtle, plant, mimic, eyeball, elemental, rat

### Bosses (5)
dragon, knight, lich, demon, reaper

---

## Checklist for New Batch

- [ ] Generate images to `Resources/Org/Batch_0N/Monster/` and `Boss/`
- [ ] Run sprite-processor: Org -> Production
- [ ] Update `config/ResourcePaths.json` with Production paths
- [ ] Create `config/monsters/batch_0N.json` (use monster-batch-creator)
- [ ] Verify sprite paths in JSON point to `Production/BatchN/...`
- [ ] Test in-game: `dotnet run`
- [ ] Update `config/monsters/_index.json` with batch activation settings

---

**Version:** 2.1
**Last Updated:** 2026-02-25
**Changes:** Raw_Green merged into Org/, Batch_04 added, deprecated list updated
