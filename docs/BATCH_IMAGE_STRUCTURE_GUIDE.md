# Batch Image Directory Structure Guide

**Purpose:** Standard directory structure and naming conventions for monster batch images

---

## Directory Structure

### Standard Layout

```
Assets/Images/Batches/
├── Batch_01/
│   ├── Monster/           ← Regular monster sprites
│   │   ├── monster_slime.png
│   │   ├── monster_bat.png
│   │   └── ...
│   └── Boss/              ← Boss monster sprites
│       ├── boss_dragon.png
│       └── ...
├── Batch_02/
│   ├── Monster/
│   └── Boss/
└── Batch_0N/              ← Future batches (up to Batch_09)
    ├── Monster/
    └── Boss/
```

---

## Naming Conventions

### Monster Sprites

**Format:** `monster_{species}.png`

**Examples:**
- `monster_slime.png`
- `monster_goblin.png`
- `monster_dragon.png`

**Rules:**
- Lowercase only
- Underscores for separation
- Species name from JSON `"species"` field

### Boss Sprites

**Format:** `boss_{species}.png`

**Examples:**
- `boss_dragon.png`
- `boss_lich.png`
- `boss_demon.png`

**Rules:**
- Lowercase only
- Underscores for separation
- Species name from JSON `"species"` field

---

## File Requirements

### Image Format

- **Format:** PNG
- **Transparency:** Alpha channel required
- **Color Mode:** RGBA (32-bit)
- **Recommended Size:** 128×128 to 256×256 pixels

### Hue-Shift Support

**Important:** All monster images support hue-shift for element variations.

**Example:**
```json
"variations": {
  "normal": { "hue_shift": 0 },
  "fire": { "hue_shift": 15 },
  "ice": { "hue_shift": 195 }
}
```

**Result:** Single `monster_slime.png` renders as 6 different colored slimes.

**Design Guideline:**
- Use **neutral/grayscale base colors** for best hue-shift results
- Avoid extreme saturation (doesn't shift well)
- Test hue-shift in-game to verify appearance

---

## Adding New Batch Images

### Step 1: Create Directory

```bash
mkdir -p Assets/Images/Batches/Batch_0N/Monster
mkdir -p Assets/Images/Batches/Batch_0N/Boss
```

### Step 2: Add Sprite Files

Place images in appropriate folders:
```
Batch_0N/
  Monster/
    monster_newspecies1.png
    monster_newspecies2.png
  Boss/
    boss_newboss1.png
```

### Step 3: Update ResourcePaths.json

Add batch configuration:
```json
{
  "batches": {
    "batch_0N": {
      "id": N,
      "data_file": "config/monsters/batch_0N.json",
      "sprite_base": "Batches/Batch_0N",
      "monster_folder": "Batches/Batch_0N/Monster",
      "boss_folder": "Batches/Batch_0N/Boss"
    }
  }
}
```

### Step 4: Create batch_0N.json

Use `monster-batch-creator` sub-agent:
```python
Task(
  subagent_type="monster-batch-creator",
  prompt="Create Batch N with theme: [Your Theme]"
)
```

Or manually reference sprite paths:
```json
{
  "monsters": [
    {
      "species": "newspecies",
      "variations": {
        "normal": {
          "sprite": "Batches/Batch_0N/Monster/monster_newspecies.png"
        }
      }
    }
  ]
}
```

---

## Sprite Path Format

### Current Standard (as of 2026-02-06)

**Full Path in JSON:**
```json
"sprite": "Batches/Batch_01/Monster/monster_slime.png"
```

**Resolution:**
```
Base: Assets/Images/
Full: Assets/Images/Batches/Batch_01/Monster/monster_slime.png
```

### Future (ResourceManager Dynamic Paths)

**Template in JSON:**
```json
"sprite_template": "monster_{species}.png"
```

**Resolution (automatic):**
```
ResourceManager.GetMonsterSpritePath(batchId=1, species="slime", element="fire")
→ "Batches/Batch_01/Monster/monster_slime.png"
```

---

## Deprecated Directories

### Do NOT Use

❌ `Assets/Images/Production/` (archived)
❌ `Assets/Images/UseImage/` (archived)

**Location:** `Assets/Images/_deprecated/`

These directories are kept for reference only. All new work uses `Batches/` structure.

---

## Tools

### update_batch_sprite_paths.py

**Purpose:** Update sprite paths in batch JSON files

**Location:** `tools/update_batch_sprite_paths.py`

**Usage:**
```bash
python tools/update_batch_sprite_paths.py
```

**When to Use:**
- Migrating old batch JSON to new structure
- Bulk path updates after directory reorganization

**Customization:**
Edit the `batch_mappings` list in `main()` to add new batches:
```python
batch_mappings = [
    {
        "file": config_dir / "batch_04.json",
        "old_prefix": "OldPath/Batch4",
        "new_prefix": "Batches/Batch_04"
    }
]
```

---

## Integration with ResourceManager

### Current Status

**MonsterDataManager:** Reads sprite paths directly from JSON

```csharp
// In FlattenMonster():
string spritePath = variation.Sprite; // Full path from JSON
```

### Future Integration (Task #1 - Pending)

**MonsterDataManager + ResourceManager:** Dynamic path resolution

```csharp
// In FlattenMonster():
string spritePath;
if (!string.IsNullOrEmpty(variation.Sprite))
{
    // Legacy: Full path from JSON
    spritePath = variation.Sprite;
}
else
{
    // New: ResourceManager dynamic resolution
    spritePath = ResourceManager.Instance.GetMonsterSpritePath(
        batchId: batch.BatchId,
        species: entry.Species,
        element: elementType
    );
}
```

**Benefits:**
- Shorter JSON files (no sprite paths)
- Centralized path management
- Easy to refactor directory structure
- Single source of truth (ResourcePaths.json)

---

## Checklist for New Batch

When creating a new monster batch:

- [ ] Create `Batches/Batch_0N/Monster` directory
- [ ] Create `Batches/Batch_0N/Boss` directory (if bosses exist)
- [ ] Add monster sprite files (PNG, named `monster_{species}.png`)
- [ ] Add boss sprite files (PNG, named `boss_{species}.png`)
- [ ] Update `config/ResourcePaths.json` with batch configuration
- [ ] Create `config/monsters/batch_0N.json` (use monster-batch-creator)
- [ ] Verify sprite paths point to `Batches/Batch_0N/...`
- [ ] Test in-game: check sprite loading, hue-shift effects
- [ ] Update `config/monsters/_index.json` with batch activation settings

---

## Troubleshooting

### Missing Sprite Error

**Error:** "Failed to load sprite: Batches/Batch_01/Monster/monster_xxx.png"

**Causes:**
1. File doesn't exist in directory
2. Filename mismatch (case-sensitive on some systems)
3. Wrong path in JSON

**Solutions:**
1. Check file exists: `ls Assets/Images/Batches/Batch_01/Monster/`
2. Verify filename matches JSON `"species"` field
3. Run `python tools/update_batch_sprite_paths.py` to fix paths

### Hue-Shift Not Working

**Problem:** All elements look the same color

**Causes:**
1. Image too dark/black (hue-shift doesn't work on grayscale)
2. Image already colored (hue-shift conflicts with base color)

**Solutions:**
1. Use neutral base colors (gray, beige)
2. Avoid extreme saturation in base image
3. Test with `hue_shift: 0, 90, 180, 270` to verify effect

### Path Not Found

**Error:** "Directory not found: Batches/Batch_0N"

**Causes:**
1. Directory not created
2. ResourcePaths.json not updated

**Solutions:**
1. Create directory: `mkdir -p Assets/Images/Batches/Batch_0N/Monster`
2. Update ResourcePaths.json with batch configuration
3. Restart game to reload ResourceManager

---

## References

- **ResourcePaths.json:** `config/ResourcePaths.json`
- **ResourceManager:** `Managers/ResourceManager.cs`
- **MonsterDataManager:** `Managers/MonsterDataManager.cs`
- **Batch Creator Guide:** `docs/monster_batch_creation_guide.md`
- **CLAUDE.md:** Project rules (Table-Driven Architecture)

---

**Version:** 1.0
**Last Updated:** 2026-02-06
**Maintainer:** lily (AI Agent)
