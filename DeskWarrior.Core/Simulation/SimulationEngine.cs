using DeskWarrior.Core.Models;
using System.Linq;

namespace DeskWarrior.Core.Simulation;

/// <summary>
/// 헤드리스 게임 시뮬레이션 엔진
/// GameManager와 동일한 로직을 UI 없이 실행
/// </summary>
public class SimulationEngine
{
    private readonly GameConfig _gameConfig;
    private readonly Dictionary<string, StatGrowthConfig> _inGameStatConfigs;
    private readonly Dictionary<string, StatGrowthConfig> _permanentStatConfigs;
    private readonly MonsterConfig _monsterConfig;
    private readonly BossDropConfig _bossDropConfig;
    private readonly SimGoldenGoblinConfig _goldenGoblinConfig;
    private readonly Random _random;
    private readonly SimMonsterBatchProvider? _monsterBatchProvider;
    private readonly bool _useBatchSystem;

    // 황금 고블린 상태
    private bool _isGoldenGoblinActive = false;
    private int _killsSinceLastGoblin = 0;

    /// <summary>
    /// 디버그용 레벨별 이벤트 (DebugRunner에서 사용)
    /// </summary>
    public event Action<DebugLevelInfo>? OnLevelProcessed;

    public SimulationEngine(
        GameConfig gameConfig,
        Dictionary<string, StatGrowthConfig> inGameStats,
        Dictionary<string, StatGrowthConfig> permanentStats,
        MonsterConfig? monsterConfig = null,  // 레거시 폴백용
        BossDropConfig? bossDropConfig = null,
        SimGoldenGoblinConfig? goldenGoblinConfig = null,
        int? seed = null,
        string? monstersConfigPath = null)
    {
        _gameConfig = gameConfig;
        _inGameStatConfigs = inGameStats;
        _permanentStatConfigs = permanentStats;
        _monsterConfig = monsterConfig ?? new MonsterConfig();
        _bossDropConfig = bossDropConfig ?? new BossDropConfig();
        _goldenGoblinConfig = goldenGoblinConfig ?? new SimGoldenGoblinConfig();
        _random = seed.HasValue ? new Random(seed.Value) : new Random();

        var monstersPath = monstersConfigPath ?? ResolveMonsterConfigPath();
        if (monstersPath != null)
        {
            var provider = new SimMonsterBatchProvider(_gameConfig, _random, monstersPath);
            if (provider.TryLoad())
            {
                _monsterBatchProvider = provider;
                _useBatchSystem = true;
            }
        }
    }

    /// <summary>
    /// 영구 스탯 Config 참조 반환 (외부에서 SimPermanentStats 생성 시 사용)
    /// </summary>
    public Dictionary<string, StatGrowthConfig> PermanentStatConfigs => _permanentStatConfigs;

    /// <summary>
    /// 황금 고블린 쿨다운 리셋 (새 시뮬레이션 시작 시 호출)
    /// </summary>
    public void ResetGoldenGoblinState()
    {
        _killsSinceLastGoblin = 0;
        _isGoldenGoblinActive = false;
    }

    private static string? ResolveMonsterConfigPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidate = Path.Combine(baseDir, "config", "monsters");
        if (Directory.Exists(candidate))
        {
            return candidate;
        }

        var cwd = Directory.GetCurrentDirectory();
        var fallback = Path.Combine(cwd, "config", "monsters");
        if (Directory.Exists(fallback))
        {
            return fallback;
        }

        return null;
    }

    /// <summary>
    /// 단일 세션 시뮬레이션
    /// </summary>
    public SessionResult SimulateSession(
        SimPermanentStats permStats,
        InputProfile profile,
        int? maxStartLevel = null)
    {
        // Config 자동 주입 (효과 계산에 사용)
        permStats.SetConfig(_permanentStatConfigs);

        var result = new SessionResult();
        var inGameStats = new SimInGameStats();
        var crystalTracker = new CrystalTracker(_bossDropConfig, _random);

        // 시작 보너스 적용 (GameManager.StartGame과 동일)
        int startLevel = permStats.StartLevel;
        if (maxStartLevel.HasValue)
        {
            startLevel = Math.Min(startLevel, Math.Max(0, maxStartLevel.Value));
        }
        long currentLevel = 1 + startLevel;
        long gold = 20 + permStats.StartGold;  // ✅ 시작 보너스: 첫 업그레이드 보장
        long startGold = gold;  // ✅ 시작 골드 추적 (SpentGold 계산용)
        inGameStats.KeyboardPowerLevel = permStats.StartKeyboardPower;
        inGameStats.MousePowerLevel = permStats.StartMousePower;

        double baseTimeLimit = _gameConfig.Balance.TimeLimit + permStats.TimeExtend;
        double sessionTime = 0;

        // 콤보 상태
        int comboStack = 0;
        double lastInputInterval = 0;

        try
        {
        while (true)
        {

            // 몬스터 스폰
            bool isBoss = currentLevel > 0 && currentLevel % _gameConfig.Balance.BossInterval == 0;

            // 황금 고블린 스폰 체크 (보스가 아닐 때만)
            _isGoldenGoblinActive = false;
            if (!isBoss && CanSpawnGoldenGoblin())
            {
                _isGoldenGoblinActive = true;

                // 황금 고블린은 실제 전투 시뮬레이션 (고정 1 데미지, 10초 제한)
                var goblin = CreateGoldenGoblin(currentLevel);
                double goblinTimeLimit = _goldenGoblinConfig.TimeLimit;
                double goblinTimeElapsed = 0;

                while (goblinTimeElapsed < goblinTimeLimit && goblin.IsAlive)
                {
                    double inputInterval = GenerateInputInterval(profile);

                    double timeRemaining = goblinTimeLimit - goblinTimeElapsed;
                    if (inputInterval >= timeRemaining)
                    {
                        goblinTimeElapsed = goblinTimeLimit;
                        sessionTime += timeRemaining;
                        break;
                    }

                    goblinTimeElapsed += inputInterval;
                    sessionTime += inputInterval;
                    result.TotalInputs++;

                    // 황금 고블린은 업그레이드/크리티컬/콤보 무시, 1 데미지 고정
                    goblin.TakeDamage(1);
                    result.TotalDamage += 1;
                }

                if (!goblin.IsAlive)
                {
                    // 처치 성공 - 삼각분포로 보상 배수 계산
                    double u1 = _random.NextDouble();
                    double u2 = _random.NextDouble();
                    double triangular = (u1 + u2) / 2.0;

                    int multiplier = _goldenGoblinConfig.RewardMultiplierMin +
                        (int)(triangular * (_goldenGoblinConfig.RewardMultiplierMax - _goldenGoblinConfig.RewardMultiplierMin));

                    long expectedGold = CalculateStageExpectedGold(currentLevel);
                    long goldenGoblinReward = OverflowGuard.Mul(expectedGold, multiplier, "Gold.GoldenGoblin", currentLevel);

                    gold = OverflowGuard.Add(gold, goldenGoblinReward, "Gold.GoblinAccum", currentLevel);
                    result.TotalGold = OverflowGuard.Add(result.TotalGold, goldenGoblinReward, "Gold.TotalAccum", currentLevel);
                    result.GoldenGoblinsKilled++;
                    result.GoldenGoblinGoldEarned += goldenGoblinReward;
                    result.MonstersKilled++;

                    // 쿨다운 리셋 (처치)
                    _killsSinceLastGoblin = 0;
                }
                else
                {
                    // 도주 (게임오버 아님)
                    result.GoldenGoblinsEscaped++;
                    _killsSinceLastGoblin = Math.Max(0, _killsSinceLastGoblin) + 1;
                }

                OnLevelProcessed?.Invoke(new DebugLevelInfo
                {
                    Level = currentLevel,
                    MonsterHp = goblin.MaxHp,
                    IsBoss = false,
                    IsGoldenGoblin = true,
                    Element = "golden",
                    BaseDamage = 1,
                    Gold = gold,
                    GoldReward = goblin.IsAlive ? 0 : result.GoldenGoblinGoldEarned,
                    KeyboardLevel = inGameStats.KeyboardPowerLevel,
                    MouseLevel = inGameStats.MousePowerLevel,
                    TimeElapsed = goblinTimeElapsed,
                    TimeLimit = goblinTimeLimit,
                    Survived = !goblin.IsAlive
                });

                _isGoldenGoblinActive = false;
                currentLevel++;
                continue; // 다음 스테이지로
            }

            var monster = CreateMonster(currentLevel, isBoss, out var timeScale);

            // ✅ 수정: 매 몬스터마다 baseTimeLimit 시간 부여 (30초)
            double monsterTimeLimit = baseTimeLimit;
            double monsterTimeElapsed = 0;

            // 전투 시뮬레이션
            while (monsterTimeElapsed < monsterTimeLimit && monster.IsAlive)
            {
                // 자동 업그레이드 시도
                if (profile.AutoUpgrade)
                {
                    TryAutoUpgrade(ref inGameStats, ref gold, permStats.UpgradeCostReduction, permStats.CostFlatReduction);
                }

                // 입력 생성 (CPS 기반)
                double inputInterval = GenerateInputInterval(profile);

                // 남은 시간 확인
                double timeRemaining = monsterTimeLimit - monsterTimeElapsed;
                double scaledInterval = inputInterval * timeScale;
                if (scaledInterval >= timeRemaining)
                {
                    monsterTimeElapsed = monsterTimeLimit;
                    sessionTime += timeRemaining;
                    break;
                }

                monsterTimeElapsed += scaledInterval;
                sessionTime += scaledInterval;
                result.TotalInputs++;

                // 콤보 판정
                comboStack = ProcessCombo(profile, comboStack, inputInterval, ref lastInputInterval);

                // 데미지 계산
                bool useMouse = _random.NextDouble() < profile.MouseRatio;
                int basePower = useMouse
                    ? 1 + GetStatEffect("mouse_power", inGameStats.MousePowerLevel)
                    : 1 + GetStatEffect("keyboard_power", inGameStats.KeyboardPowerLevel);

                basePower += permStats.BaseAttack;

                long damage = CalculateDamage(basePower, permStats, comboStack, monster, useMouse, out bool isCrit);

                if (isCrit) result.CriticalHits++;

                monster.TakeDamage(damage);
                result.TotalDamage += damage;
            }

            // 타임아웃 처리 = 게임오버
            if (monster.IsAlive)
            {
                OnLevelProcessed?.Invoke(new DebugLevelInfo
                {
                    Level = currentLevel,
                    MonsterHp = monster.MaxHp,
                    IsBoss = isBoss,
                    Element = monster.Element,
                    BaseDamage = 1 + GetStatEffect("keyboard_power", inGameStats.KeyboardPowerLevel) + permStats.BaseAttack,
                    Gold = gold,
                    GoldReward = 0,
                    KeyboardLevel = inGameStats.KeyboardPowerLevel,
                    MouseLevel = inGameStats.MousePowerLevel,
                    TimeElapsed = monsterTimeElapsed,
                    TimeLimit = monsterTimeLimit,
                    Survived = false
                });

                result.MaxLevel = currentLevel;
                result.EndReason = "timeout";
                result.SessionDuration = sessionTime;

                // 게임오버 시 크리스털 보너스
                result.CrystalsFromStages = crystalTracker.GetStageCompletionCrystals();
                result.CrystalsFromGoldConvert = crystalTracker.ConvertGoldToCrystals(gold);

                // ✅ 인게임 업그레이드 최종 상태
                result.SpentGold = startGold - gold;
                result.FinalKeyboardPowerLevel = inGameStats.KeyboardPowerLevel;
                result.FinalMousePowerLevel = inGameStats.MousePowerLevel;

                return result;
            }

            // 몬스터 처치
            result.MonstersKilled++;

            // 쿨다운 카운터 증가 (황금 고블린 스폰용)
            _killsSinceLastGoblin++;

            // ✅ 모든 몬스터 처치 시 크리스탈 지급 (100레벨마다 +1)
            crystalTracker.ProcessStageClear(currentLevel);

            // 보스 처치 시 추가 보너스 크리스털 지급 (100% 확정, 속성별 배율 적용)
            if (isBoss)
            {
                result.BossesKilled++;

                // ✅ 속성별 크리스털 배율 추출
                var crystalMultipliers = _gameConfig.ElementProperties.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.CrystalMultiplier
                );

                // ✅ 보스 속성 전달
                var crystalDrop = crystalTracker.ProcessBossKill(
                    currentLevel,
                    monster.Element,
                    permStats.CrystalFlat,
                    crystalMultipliers
                );

                // ✅ 100% 지급이므로 조건 불필요
                result.CrystalsFromBosses += crystalDrop.Amount;
            }

            // 골드 획득 (GameManager.OnMonsterDefeated와 동일)
            double baseGold = monster.GoldReward;
            double goldFlatPerm = permStats.GoldFlatPerm;
            double goldFlat = baseGold + goldFlatPerm;
            double goldMultiPerm = permStats.GoldMultiPerm;
            long goldReward = OverflowGuard.ToLong(goldFlat * (1.0 + goldMultiPerm), "Gold.Reward", currentLevel);

            gold = OverflowGuard.Add(gold, goldReward, "Gold.MonsterAccum", currentLevel);
            result.TotalGold = OverflowGuard.Add(result.TotalGold, goldReward, "Gold.TotalAccum", currentLevel);

            OnLevelProcessed?.Invoke(new DebugLevelInfo
            {
                Level = currentLevel,
                MonsterHp = monster.MaxHp,
                IsBoss = isBoss,
                Element = monster.Element,
                BaseDamage = 1 + GetStatEffect("keyboard_power", inGameStats.KeyboardPowerLevel) + permStats.BaseAttack,
                Gold = gold,
                GoldReward = goldReward,
                KeyboardLevel = inGameStats.KeyboardPowerLevel,
                MouseLevel = inGameStats.MousePowerLevel,
                TimeElapsed = monsterTimeElapsed,
                TimeLimit = monsterTimeLimit,
                Survived = true
            });

            // 인게임 업그레이드 (골드 사용)
            PerformInGameUpgrades(ref gold, inGameStats, currentLevel, permStats.UpgradeCostReduction, permStats.CostFlatReduction);

            currentLevel++;
        }
        } // try
        catch (SimulationOverflowException ex)
        {
            result.MaxLevel = currentLevel;
            result.EndReason = $"overflow:{ex.Location}";
            result.SessionDuration = sessionTime;
            result.CrystalsFromStages = crystalTracker.GetStageCompletionCrystals();
            result.OverflowDetected = true;
            result.OverflowLocation = ex.Location;
            result.OverflowLevel = ex.GameLevel;
            result.OverflowValue = ex.ComputedValue;
            result.FinalKeyboardPowerLevel = inGameStats.KeyboardPowerLevel;
            result.FinalMousePowerLevel = inGameStats.MousePowerLevel;
            return result;
        }
    }

    private SimMonster CreateMonster(long level, bool isBoss, out double timeScale)
    {
        // 기본값 (레거시 폴백: CharacterData 첫 몬스터)
        int baseHp = _monsterConfig.BaseHp;
        int hpGrowth = _monsterConfig.HpGrowth;
        int baseGold = _monsterConfig.BaseGold;
        int goldGrowth = _monsterConfig.GoldGrowth;
        string element = "normal";

        // 배치 시스템 사용 시 실제 몬스터 데이터 적용
        if (_useBatchSystem && _monsterBatchProvider != null)
        {
            var data = _monsterBatchProvider.GetRandomMonsterData(level, isBoss);
            if (data != null)
            {
                baseHp = data.BaseHp;
                hpGrowth = data.HpGrowth;
                baseGold = data.BaseGold;
                goldGrowth = data.GoldGrowth;
                element = data.Element;
            }
        }

        // 속성별 보정값
        double keyboardResistance = 1.0;
        double mouseResistance = 1.0;
        double hpModifier = 1.0;
        timeScale = 1.0;
        if (_gameConfig.ElementProperties != null && _gameConfig.ElementProperties.TryGetValue(element, out var elementProps))
        {
            keyboardResistance = elementProps.KeyboardResistance;
            mouseResistance = elementProps.MouseResistance;
            hpModifier = elementProps.HpModifier;
            timeScale = elementProps.TimeScale;
        }

        var monster = new SimMonster(
            level,
            isBoss,
            baseHp,
            hpGrowth,
            baseGold,  // ✅ 수정: 실제 값 사용
            goldGrowth,
            _gameConfig.Balance.TierHpSystem,
            element,  // ✅ 추가
            keyboardResistance,  // ✅ 추가: 저항 시스템
            mouseResistance  // ✅ 추가: 저항 시스템
        );

        // HP 보정 적용 (게임 로직과 동일하게 최종 HP에 적용)
        if (hpModifier != 1.0)
        {
            monster.ApplyHpModifier(hpModifier);
        }

        return monster;
    }

    /// <summary>
    /// 황금 고블린 스폰 가능 여부 판정
    /// </summary>
    private bool CanSpawnGoldenGoblin()
    {
        // 쿨다운 체크
        if (_killsSinceLastGoblin < _goldenGoblinConfig.CooldownKills)
        {
            return false;
        }

        // 확률 체크
        return _random.NextDouble() < _goldenGoblinConfig.SpawnChance;
    }

    /// <summary>
    /// 황금 고블린 생성
    /// </summary>
    private SimMonster CreateGoldenGoblin(long level)
    {
        // 게임 로직과 동일: HpMin/HpMax 있으면 랜덤, 없으면 고정
        int hp;
        if (_goldenGoblinConfig.HpMin > 0 && _goldenGoblinConfig.HpMax > _goldenGoblinConfig.HpMin)
        {
            hp = _random.Next(_goldenGoblinConfig.HpMin, _goldenGoblinConfig.HpMax + 1);
        }
        else
        {
            hp = _goldenGoblinConfig.Hp;
        }

        return new SimMonster(
            level,
            isBoss: false,
            baseHp: hp,  // 100~200 랜덤
            hpGrowth: 0,  // 레벨과 무관하게 고정 HP
            baseGold: 0,
            goldGrowth: 0,
            tierConfig: null  // 황금 고블린은 티어 시스템 사용 안 함
        );
    }

    /// <summary>
    /// 스테이지 예상 골드 계산 (게임 로직과 동일)
    /// </summary>
    private long CalculateStageExpectedGold(long level)
    {
        // 게임의 CalculateStageExpectedGold 로직 복제
        // 배치 시스템 사용 시: 모든 몬스터 평균 골드
        if (_useBatchSystem && _monsterBatchProvider != null)
        {
            long avg = _monsterBatchProvider.CalculateStageExpectedGold(level);
            if (avg > 0)
                return avg;
        }

        // 레거시 폴백: 기본 몬스터 값
        return (long)_monsterConfig.BaseGold + level * _monsterConfig.GoldGrowth;
    }

    /// <summary>
    /// 현재 플레이어 DPS 추정
    /// </summary>
    private double EstimatePlayerDps(
        SimInGameStats inGameStats,
        SimPermanentStats permStats,
        InputProfile profile)
    {
        // 1. 평균 기본 파워 계산 (keyboard/mouse)
        double keyboardPower = 1 + GetStatEffect("keyboard_power", inGameStats.KeyboardPowerLevel);
        double mousePower = 1 + GetStatEffect("mouse_power", inGameStats.MousePowerLevel);

        int avgBasePower = (int)(
            profile.MouseRatio * mousePower +
            (1 - profile.MouseRatio) * keyboardPower
        );

        avgBasePower += permStats.BaseAttack;

        // 2. 공격력 배수 적용 (pure base에만 적용)
        int baseAttackBonus = permStats.BaseAttack;
        double pureBase = avgBasePower - baseAttackBonus;
        double attackMultiplier = 1.0 + permStats.AttackPercentBonus;
        double baseAfter = pureBase * attackMultiplier + baseAttackBonus;

        // 3. 크리티컬 평균 적용
        double critChance = permStats.CriticalChanceBonus;
        double critMultiplier = 1.5 + permStats.CriticalDamageBonus;
        double avgCritBonus = 1.0 + critChance * (critMultiplier - 1.0);

        // 4. 평균 데미지 계산
        double avgDamage = baseAfter * avgCritBonus;

        // 5. DPS = 데미지 × CPS
        return avgDamage * profile.AverageCps;
    }

    private int GetStatEffect(string statId, int level)
    {
        if (_inGameStatConfigs.TryGetValue(statId, out var config))
        {
            return (int)config.CalculateEffect(level);
        }
        return 0;
    }

    private long GetUpgradeCost(string statId, int level, double discountPercent, long flatReduction = 0, int currentStage = 1)
    {
        if (_inGameStatConfigs.TryGetValue(statId, out var config))
        {
            return config.CalculateCost(level + 1, discountPercent, flatReduction);
        }
        return long.MaxValue;
    }

    private void TryAutoUpgrade(ref SimInGameStats stats, ref long gold, double discountPercent, long flatReduction = 0)
    {
        // 키보드 파워 우선 업그레이드
        long kbCost = GetUpgradeCost("keyboard_power", stats.KeyboardPowerLevel, discountPercent, flatReduction);
        if (gold >= kbCost)
        {
            gold -= kbCost;
            stats.KeyboardPowerLevel++;
            return;
        }

        // 마우스 파워 업그레이드
        long msCost = GetUpgradeCost("mouse_power", stats.MousePowerLevel, discountPercent, flatReduction);
        if (gold >= msCost)
        {
            gold -= msCost;
            stats.MousePowerLevel++;
        }
    }

    private double GenerateInputInterval(InputProfile profile)
    {
        // CPS 기반 입력 간격 생성
        double baseCps = profile.AverageCps;
        double variance = profile.CpsVariance;

        // 정규분포 근사 (Box-Muller)
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        double randNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

        double actualCps = baseCps * (1.0 + randNormal * variance);
        actualCps = Math.Max(0.1, actualCps); // 최소 CPS

        return 1.0 / actualCps;
    }

    private int ProcessCombo(InputProfile profile, int currentStack, double interval, ref double lastInterval)
    {
        if (profile.ComboSkill == ComboSkillLevel.None)
        {
            lastInterval = interval;
            return 0;
        }

        // 첫 입력
        if (lastInterval == 0)
        {
            lastInterval = interval;
            return 0;
        }

        // 콤보 만료 (3초)
        if (interval > 3.0)
        {
            lastInterval = interval;
            return 0;
        }

        // 리듬 판정
        double successRate = ComboSkillHelper.GetSuccessRate(profile.ComboSkill);
        int maxStack = ComboSkillHelper.GetMaxStack(profile.ComboSkill);

        if (_random.NextDouble() < successRate)
        {
            // 리듬 성공
            currentStack = Math.Min(currentStack + 1, maxStack);
        }
        else
        {
            // 리듬 실패
            currentStack = 0;
        }

        lastInterval = interval;
        return currentStack;
    }

    private long CalculateDamage(int basePower, SimPermanentStats permStats, int comboStack, SimMonster monster, bool useMouse, out bool isCritical)
    {
        // ① basePower에는 BaseAttack이 포함됨 (GameManager 공식과 동일하게 분리)
        int baseAttackBonus = permStats.BaseAttack;
        int pureBasePower = basePower - baseAttackBonus;
        double effectivePower = pureBasePower;

        // ② 공격력 배수는 pureBasePower에만 적용
        double attackMultiplier = permStats.AttackPercentBonus;
        effectivePower *= (1.0 + attackMultiplier);

        // ③ base_attack는 가산
        effectivePower += baseAttackBonus;

        // ④ 크리티컬
        double critChance = _gameConfig.Balance.CriticalChance + permStats.CriticalChanceBonus;
        double critMultiplier = _gameConfig.Balance.CriticalMultiplier + permStats.CriticalDamageBonus;

        isCritical = _random.NextDouble() < critChance;
        if (isCritical)
        {
            effectivePower *= critMultiplier;
        }

        // ⑤ 멀티히트
        if (_random.NextDouble() < permStats.MultiHitChance)
        {
            effectivePower *= 2;
        }

        // ⑥ 콤보
        if (comboStack > 0)
        {
            double stackMultiplier = Math.Pow(2, comboStack);
            effectivePower *= stackMultiplier;
        }

        // ⑦ 유틸리티 보너스 (Config의 damage_bonus_per_level 사용)
        double totalDamageBonus = 0.0;
        if (_permanentStatConfigs.TryGetValue("time_extend", out var timeExtendCfg) &&
            timeExtendCfg.DamageBonusPerLevel.HasValue)
        {
            totalDamageBonus += permStats.TimeExtendLevel * timeExtendCfg.DamageBonusPerLevel.Value;
        }
        if (_permanentStatConfigs.TryGetValue("upgrade_discount", out var upgradeDiscountCfg) &&
            upgradeDiscountCfg.DamageBonusPerLevel.HasValue)
        {
            totalDamageBonus += permStats.UpgradeDiscountLevel * upgradeDiscountCfg.DamageBonusPerLevel.Value;
        }

        double utilityBonus = 1.0 + totalDamageBonus / 100.0;
        effectivePower *= utilityBonus;

        // ⑧ 저항 시스템 (속성별 키보드/마우스 저항)
        double resistanceModifier = useMouse
            ? monster.MouseResistance
            : monster.KeyboardResistance;
        effectivePower *= resistanceModifier;

        // Note: Step ⑨ (연속 키 페널티)는 시뮬레이터에서 의도적으로 생략
        // CPS 기반 랜덤 간격 입력이므로 키 반복 매크로 방지용 페널티가 발동하지 않음

        return Math.Max(1L, OverflowGuard.ToLong(effectivePower, "Damage.Calculate", monster.Level));
    }

    /// <summary>
    /// 인게임 업그레이드 수행 (골드 사용)
    /// 키보드/마우스 파워를 교대로 업그레이드
    /// </summary>
    private void PerformInGameUpgrades(ref long gold, SimInGameStats inGameStats, long currentLevel, double discountPercent, long flatReduction = 0)
    {
        // 게임 로직과 유사한 비용 계산 적용
        while (true)
        {
            long kbCost = GetUpgradeCost("keyboard_power", inGameStats.KeyboardPowerLevel, discountPercent, flatReduction);
            long msCost = GetUpgradeCost("mouse_power", inGameStats.MousePowerLevel, discountPercent, flatReduction);

            if (kbCost == long.MaxValue && msCost == long.MaxValue)
                break;

            if (kbCost <= msCost)
            {
                if (gold < kbCost) break;
                gold -= kbCost;
                inGameStats.KeyboardPowerLevel++;
            }
            else
            {
                if (gold < msCost) break;
                gold -= msCost;
                inGameStats.MousePowerLevel++;
            }
        }
    }

}
