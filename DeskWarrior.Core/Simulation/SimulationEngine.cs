using DeskWarrior.Core.Models;
using DeskWarrior.Core.Formulas;

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
    private readonly BossDropConfig _bossDropConfig;
    private readonly SimGoldenGoblinConfig _goldenGoblinConfig;
    private readonly Random _random;

    // 황금 고블린 상태
    private bool _isGoldenGoblinActive = false;
    private int _killsSinceLastGoblin = 0;

    public SimulationEngine(
        GameConfig gameConfig,
        Dictionary<string, StatGrowthConfig> inGameStats,
        Dictionary<string, StatGrowthConfig> permanentStats,
        MonsterConfig? monsterConfig = null,  // 하위 호환성 유지 (무시됨)
        BossDropConfig? bossDropConfig = null,
        SimGoldenGoblinConfig? goldenGoblinConfig = null,
        int? seed = null)
    {
        _gameConfig = gameConfig;
        _inGameStatConfigs = inGameStats;
        _permanentStatConfigs = permanentStats;
        _bossDropConfig = bossDropConfig ?? new BossDropConfig();
        _goldenGoblinConfig = goldenGoblinConfig ?? new SimGoldenGoblinConfig();
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
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

    /// <summary>
    /// 단일 세션 시뮬레이션
    /// </summary>
    public SessionResult SimulateSession(SimPermanentStats permStats, InputProfile profile)
    {
        // Config 자동 주입 (효과 계산에 사용)
        permStats.SetConfig(_permanentStatConfigs);

        var result = new SessionResult();
        var inGameStats = new SimInGameStats();
        var crystalTracker = new CrystalTracker(_bossDropConfig, _random);

        // 시작 보너스 적용 (GameManager.StartGame과 동일)
        int currentLevel = 1 + permStats.StartLevel;
        int gold = 20 + permStats.StartGold;  // ✅ 시작 보너스: 첫 업그레이드 보장
        int startGold = gold;  // ✅ 시작 골드 추적 (SpentGold 계산용)
        inGameStats.KeyboardPowerLevel = permStats.StartKeyboardPower;
        inGameStats.MousePowerLevel = permStats.StartMousePower;

        double baseTimeLimit = _gameConfig.Balance.TimeLimit + permStats.TimeExtend;
        double sessionTime = 0;

        // 콤보 상태
        int comboStack = 0;
        double lastInputInterval = 0;

        while (true)
        {
            // 세션 시간 초과 체크
            if (sessionTime >= baseTimeLimit)
            {
                result.MaxLevel = currentLevel;
                result.EndReason = "session_timeout";
                result.SessionDuration = sessionTime;
                result.CrystalsFromStages = crystalTracker.GetStageCompletionCrystals();
                result.CrystalsFromGoldConvert = crystalTracker.ConvertGoldToCrystals(gold);

                // ✅ 인게임 업그레이드 최종 상태
                result.SpentGold = startGold - gold;
                result.FinalKeyboardPowerLevel = inGameStats.KeyboardPowerLevel;
                result.FinalMousePowerLevel = inGameStats.MousePowerLevel;

                return result;
            }

            // 몬스터 스폰
            bool isBoss = currentLevel > 0 && currentLevel % _gameConfig.Balance.BossInterval == 0;

            // 황금 고블린 스폰 체크 (보스가 아닐 때만)
            _isGoldenGoblinActive = false;
            if (!isBoss && CanSpawnGoldenGoblin())
            {
                // 황금 고블린: DPS 기반 처치 확률 계산
                double avgDps = EstimatePlayerDps(inGameStats, permStats, profile);
                double requiredDps = _goldenGoblinConfig.Hp / (double)_goldenGoblinConfig.TimeLimit;
                double killChance = Math.Min(0.95, Math.Max(0.05, avgDps / requiredDps));

                // 10초 시간 소모
                double goblinTime = Math.Min(_goldenGoblinConfig.TimeLimit, baseTimeLimit - sessionTime);
                sessionTime += goblinTime;

                if (_random.NextDouble() < killChance)
                {
                    // 처치 성공 - 삼각분포로 보상 배수 계산
                    // 중앙(50배)이 가장 높은 확률
                    double u1 = _random.NextDouble();
                    double u2 = _random.NextDouble();
                    double triangular = (u1 + u2) / 2.0;  // 0~1, 중앙에 집중

                    int multiplier = _goldenGoblinConfig.RewardMultiplierMin +
                        (int)(triangular * (_goldenGoblinConfig.RewardMultiplierMax - _goldenGoblinConfig.RewardMultiplierMin));

                    int expectedGold = CalculateStageExpectedGold(currentLevel);
                    int goldenGoblinReward = expectedGold * multiplier;

                    gold += goldenGoblinReward;
                    result.TotalGold += goldenGoblinReward;
                    result.GoldenGoblinsKilled++;
                    result.GoldenGoblinGoldEarned += goldenGoblinReward;
                    result.MonstersKilled++;
                }
                else
                {
                    // 도주
                    result.GoldenGoblinsEscaped++;
                }

                _killsSinceLastGoblin = 0; // 쿨다운 리셋
                currentLevel++;
                continue; // 다음 스테이지로
            }

            var monster = CreateMonster(currentLevel, isBoss);

            // ✅ 수정: 매 몬스터마다 baseTimeLimit 시간 부여 (30초)
            double monsterTimeLimit = baseTimeLimit;
            double monsterTimeElapsed = 0;

            // 전투 시뮬레이션
            while (monsterTimeElapsed < monsterTimeLimit && monster.IsAlive)
            {
                // 자동 업그레이드 시도
                if (profile.AutoUpgrade)
                {
                    TryAutoUpgrade(ref inGameStats, ref gold, permStats.UpgradeCostReduction, currentLevel);
                }

                // 입력 생성 (CPS 기반)
                double inputInterval = GenerateInputInterval(profile);

                // 남은 시간 확인
                double timeRemaining = monsterTimeLimit - monsterTimeElapsed;
                if (inputInterval >= timeRemaining)
                {
                    monsterTimeElapsed = monsterTimeLimit;
                    sessionTime += timeRemaining;
                    break;
                }

                monsterTimeElapsed += inputInterval;
                sessionTime += inputInterval;
                result.TotalInputs++;

                // 콤보 판정
                comboStack = ProcessCombo(profile, comboStack, inputInterval, ref lastInputInterval);

                // 데미지 계산
                bool useMouse = _random.NextDouble() < profile.MouseRatio;
                int basePower = useMouse
                    ? 1 + GetStatEffect("mouse_power", inGameStats.MousePowerLevel)
                    : 1 + GetStatEffect("keyboard_power", inGameStats.KeyboardPowerLevel);

                basePower += permStats.BaseAttack;

                int damage = CalculateDamage(basePower, permStats, comboStack, monster, useMouse, out bool isCrit);

                if (isCrit) result.CriticalHits++;

                monster.TakeDamage(damage);
                result.TotalDamage += damage;
            }

            // 타임아웃 처리 = 게임오버
            if (monster.IsAlive)
            {
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
            double goldMultiPerm = permStats.GoldMultiPerm / 100.0;
            int goldReward = (int)(goldFlat * (1.0 + goldMultiPerm));

            gold += goldReward;
            result.TotalGold += goldReward;

            // 인게임 업그레이드 (골드 사용)
            PerformInGameUpgrades(ref gold, inGameStats, currentLevel, permStats.UpgradeCostReduction);

            currentLevel++;
        }
    }

    private SimMonster CreateMonster(int level, bool isBoss)
    {
        // 게임 공식 (Monster.cs 동일):
        // HP = baseHp + (level - 1) * hpGrowth (선형 성장)
        // 보스: HP × BOSS_HP_MULTIPLIER

        int baseHp = _gameConfig.Balance.BaseHp;
        int hpGrowth = (int)_gameConfig.Balance.HpGrowth;

        // 보스는 배율 적용
        if (isBoss)
        {
            baseHp = (int)(baseHp * _gameConfig.Balance.BossHpMultiplier);
        }

        // 골드: baseGold + level * goldGrowth (실제 게임과 동일)
        // 몬스터 데이터의 실제 값 사용 (batch_01.json: base_gold=10, gold_growth=2)
        int baseGold = 10;
        int goldGrowth = 2;

        // ✅ TODO: 속성 가중치 기반 랜덤 선택 구현 필요
        // 현재는 기본 속성 "normal" 사용
        string element = "normal";

        // 저항값 가져오기 (element_properties에서)
        double keyboardResistance = 1.0;
        double mouseResistance = 1.0;
        if (_gameConfig.ElementProperties != null && _gameConfig.ElementProperties.TryGetValue(element, out var elementProps))
        {
            keyboardResistance = elementProps.KeyboardResistance;
            mouseResistance = elementProps.MouseResistance;
        }

        return new SimMonster(
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
    private SimMonster CreateGoldenGoblin(int level)
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
    private int CalculateStageExpectedGold(int level)
    {
        // 게임의 CalculateStageExpectedGold 로직 복제
        // 배치 시스템 사용 시: 모든 몬스터 평균 골드
        // 미사용 시: 10 + level * 2 (Legacy)

        // 현재 시뮬레이터는 배치 시스템 미사용이므로 Legacy 공식 사용
        // TODO: 향후 MonsterBatchData 추가 시 확장
        return 10 + level * 2;
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

        // 2. 공격력 배수 적용
        double attackMultiplier = 1.0 + permStats.AttackPercentBonus / 100.0;

        // 3. 크리티컬 평균 적용
        double critChance = permStats.CriticalChanceBonus / 100.0;
        double critMultiplier = 1.5 + permStats.CriticalDamageBonus;
        double avgCritBonus = 1.0 + critChance * (critMultiplier - 1.0);

        // 4. 평균 데미지 계산
        double avgDamage = avgBasePower * attackMultiplier * avgCritBonus;

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

    private int GetUpgradeCost(string statId, int level, double discountPercent, int currentStage = 1)
    {
        if (_inGameStatConfigs.TryGetValue(statId, out var config))
        {
            int baseCost = config.CalculateCost(level + 1, discountPercent);

            // 스테이지 구간별 비용 배율 적용
            double stageMultiplier = CalculateStageCostMultiplier(currentStage);
            return (int)(baseCost * stageMultiplier);
        }
        return int.MaxValue;
    }

    /// <summary>
    /// 스테이지 구간별 업그레이드 비용 배율
    /// 50스테이지마다 비용 2배 증가
    /// </summary>
    private double CalculateStageCostMultiplier(int stage)
    {
        int interval = _gameConfig.Balance.UpgradeCostInterval;
        if (interval <= 0) interval = 50;  // 기본값 50스테이지

        int tier = (stage - 1) / interval;
        return Math.Pow(2, tier);  // 2^tier 배율
    }

    private void TryAutoUpgrade(ref SimInGameStats stats, ref int gold, double discountPercent, int currentStage)
    {
        // 키보드 파워 우선 업그레이드
        int kbCost = GetUpgradeCost("keyboard_power", stats.KeyboardPowerLevel, discountPercent, currentStage);
        if (gold >= kbCost)
        {
            gold -= kbCost;
            stats.KeyboardPowerLevel++;
            return;
        }

        // 마우스 파워 업그레이드
        int msCost = GetUpgradeCost("mouse_power", stats.MousePowerLevel, discountPercent, currentStage);
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

    private int CalculateDamage(int basePower, SimPermanentStats permStats, int comboStack, SimMonster monster, bool useMouse, out bool isCritical)
    {
        // ① basePower에는 이미 BaseAttack이 포함됨
        double effectivePower = basePower;

        // ② 공격력 배수 - 기본 공식 원복
        effectivePower *= (1.0 + permStats.AttackPercentBonus);

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

        // ⑦ 유틸리티 보너스 (time_extend + upgrade_discount 투자에 따른 데미지 보너스)
        double utilityBonus = 1.0 + (permStats.TimeExtendLevel + permStats.UpgradeDiscountLevel) * 0.01;
        effectivePower *= utilityBonus;

        // ⑧ 저항 시스템 (속성별 키보드/마우스 저항)
        double resistanceModifier = useMouse
            ? monster.MouseResistance
            : monster.KeyboardResistance;
        effectivePower *= resistanceModifier;

        return (int)effectivePower;
    }

    /// <summary>
    /// 인게임 업그레이드 수행 (골드 사용)
    /// 키보드/마우스 파워를 교대로 업그레이드
    /// </summary>
    private void PerformInGameUpgrades(ref int gold, SimInGameStats inGameStats, int currentLevel, double discountPercent)
    {
        // ✅ 간단화: 골드가 1 이상이면 무조건 업그레이드 (교대로)
        while (gold >= 1)
        {
            // Keyboard와 Mouse 중 레벨이 낮은 쪽 업그레이드
            if (inGameStats.KeyboardPowerLevel <= inGameStats.MousePowerLevel)
            {
                gold -= 1;
                inGameStats.KeyboardPowerLevel++;
            }
            else
            {
                gold -= 1;
                inGameStats.MousePowerLevel++;
            }
        }
    }

    /// <summary>
    /// 인게임 업그레이드 기본 비용 계산 (GameManager와 동일)
    /// </summary>
    private int CalculateInGameUpgradeCost(int currentLevel, double discountPercent)
    {
        // GameData.json의 upgrade 설정 사용
        double baseCost = _gameConfig.Upgrade.BaseCost;
        double costMultiplier = _gameConfig.Upgrade.CostMultiplier;

        // 비용 = baseCost * (costMultiplier ^ currentLevel)
        int cost = (int)(baseCost * Math.Pow(costMultiplier, currentLevel));

        // 할인 적용
        if (discountPercent > 0)
        {
            cost = (int)(cost * (1.0 - discountPercent / 100.0));
        }

        return Math.Max(1, cost);
    }

    /// <summary>
    /// 스테이지 구간별 업그레이드 비용 배율 (GameManager와 동일)
    /// 50스테이지마다 비용 2배
    /// </summary>
    private int ApplyStageCostMultiplier(int baseCost, int currentLevel)
    {
        int interval = _gameConfig.Balance.UpgradeCostInterval;
        if (interval <= 0) interval = 50;

        int tier = (currentLevel - 1) / interval;
        double multiplier = Math.Pow(2, tier);

        return (int)(baseCost * multiplier);
    }
}
