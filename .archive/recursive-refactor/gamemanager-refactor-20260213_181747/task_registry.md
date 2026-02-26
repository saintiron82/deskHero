# Task Registry

**목표**: GameManager.cs 리팩토링 (900+ 줄 → 책임 분리: 몬스터 스폰, 전투, 보상, 타이머 로직 분리)
**생성**: 2026-02-13
**갱신**: 2026-02-13

---

## 현재 상태

- **노드**: `NODE-4`
- **상태**: ✅ passed

---

## 트리 구조

✅ **ROOT**: GameManager.cs 리팩토링 (900+ 줄 → 책임 분리: 몬스터... 
  ├─ ✅ **NODE-1**: Extract combat system (input handling, d... 
  ├─ ✅ **NODE-2**: Extract monster spawning logic (normal, ... [LEAF]
  ├─ ✅ **NODE-3**: Extract reward calculation (gold, crysta... [LEAF]
  ├─ ✅ **NODE-4**: Extract timer/game loop management (tick... [LEAF] ◀ CURRENT

---

## 노드 상세

### ROOT
- **목표**: GameManager.cs 리팩토링 (900+ 줄 → 책임 분리: 몬스터 스폰, 전투, 보상, 타이머 로직 분리)
- **상태**: ✅ passed
- **깊이**: 0
- **자식**: NODE-1, NODE-2, NODE-3, NODE-4

### NODE-1
- **목표**: Extract combat system (input handling, damage calculation, rate limiting) into CombatManager
- **상태**: ✅ passed
- **깊이**: 1
- **부모**: ROOT
- **테스트 항목**:
  1. ✅ Build succeeds
  2. ✅ Keyboard input triggers damage event
  3. ✅ Mouse input triggers damage event
  4. ✅ Rate limiting blocks excessive inputs
  5. ✅ Critical hits calculated correctly
  6. ✅ Combo system integrates properly

### NODE-2
- **목표**: Extract monster spawning logic (normal, boss, golden goblin selection) into MonsterSpawnManager
- **상태**: ✅ passed
- **깊이**: 1
- **리프**: Yes
- **부모**: ROOT
- **테스트 항목**:
  1. ✅ Build succeeds without errors
  2. ✅ Normal monster spawns correctly with species and element
  3. ✅ Boss monster spawns correctly at boss intervals
  4. ✅ Golden goblin spawns when cooldown is met
  5. ✅ CalculateStageExpectedGold returns valid value
  6. ✅ MonsterSpawnManager integrates with existing managers
  7. ✅ All existing tests pass

### NODE-3
- **목표**: Extract reward calculation (gold, crystals, collection milestones) into RewardManager
- **상태**: ✅ passed
- **깊이**: 1
- **리프**: Yes
- **부모**: ROOT
- **테스트 항목**:
  1. ✅ Build succeeds without errors
  2. ✅ Gold rewards calculated correctly for normal monster kills
  3. ✅ Boss crystal drops work with element multipliers
  4. ✅ Golden goblin rewards calculated properly
  5. ✅ Collection milestone rewards (first holy/dark) granted correctly
  6. ✅ Species completion rewards granted correctly
  7. ✅ All elements milestone reward works
  8. ✅ Stage clear crystals awarded properly
  9. ✅ SessionTracker records reward events correctly
  10. ✅ All existing game tests pass

### NODE-4
- **목표**: Extract timer/game loop management (tick handling, time scale, game over) into GameLoopManager
- **상태**: ✅ passed
- **깊이**: 1
- **리프**: Yes
- **부모**: ROOT
- **테스트 항목**:
  1. ⏳ Build succeeds (dotnet build)
  2. ⏳ GameLoopManager.cs created with timer logic
  3. ⏳ GameManager delegates timer operations to GameLoopManager
  4. ⏳ All timer events still triggered correctly
  5. ⏳ RemainingTime property accessible
  6. ⏳ Pause/Resume timer methods work
