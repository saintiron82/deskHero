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
    private readonly Random _random;

    public SimulationEngine(
        GameConfig gameConfig,
        Dictionary<string, StatGrowthConfig> inGameStats,
        Dictionary<string, StatGrowthConfig> permanentStats,
        MonsterConfig? monsterConfig = null,  // 하위 호환성 유지 (무시됨)
        BossDropConfig? bossDropConfig = null,
        int? seed = null)
    {
        _gameConfig = gameConfig;
        _inGameStatConfigs = inGameStats;
        _permanentStatConfigs = permanentStats;
        _bossDropConfig = bossDropConfig ?? new BossDropConfig();
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// 영구 스탯 Config 참조 반환 (외부에서 SimPermanentStats 생성 시 사용)
    /// </summary>
    public Dictionary<string, StatGrowthConfig> PermanentStatConfigs => _permanentStatConfigs;

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
        int gold = permStats.StartGold;
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
                return result;
            }

            // 몬스터 스폰
            bool isBoss = currentLevel > 0 && currentLevel % _gameConfig.Balance.BossInterval == 0;
            var monster = CreateMonster(currentLevel, isBoss);
            double timeRemaining = Math.Min(baseTimeLimit, baseTimeLimit - sessionTime);

            // 전투 시뮬레이션
            while (timeRemaining > 0 && monster.IsAlive)
            {
                // 자동 업그레이드 시도
                if (profile.AutoUpgrade)
                {
                    TryAutoUpgrade(ref inGameStats, ref gold, permStats.UpgradeCostReduction, currentLevel);
                }

                // 입력 생성 (CPS 기반)
                double inputInterval = GenerateInputInterval(profile);
                if (inputInterval >= timeRemaining)
                {
                    timeRemaining = 0;
                    break;
                }

                timeRemaining -= inputInterval;
                sessionTime += inputInterval;
                result.TotalInputs++;

                // 세션 시간 초과 체크
                if (sessionTime >= baseTimeLimit)
                {
                    timeRemaining = 0;
                    break;
                }

                // 콤보 판정
                comboStack = ProcessCombo(profile, comboStack, inputInterval, ref lastInputInterval);

                // 데미지 계산
                bool useMouse = _random.NextDouble() < profile.MouseRatio;
                int basePower = useMouse
                    ? 1 + GetStatEffect("mouse_power", inGameStats.MousePowerLevel)
                    : 1 + GetStatEffect("keyboard_power", inGameStats.KeyboardPowerLevel);

                basePower += permStats.BaseAttack;

                var damage = CalculateDamage(basePower, permStats, comboStack, out bool isCrit);
                if (isCrit) result.CriticalHits++;

                monster.TakeDamage(damage);
                result.TotalDamage += damage;
            }

            // 타임아웃 = 게임오버
            if (monster.IsAlive)
            {
                result.MaxLevel = currentLevel;
                result.EndReason = "timeout";
                result.SessionDuration = sessionTime;

                // 게임오버 시 크리스털 보너스
                result.CrystalsFromStages = crystalTracker.GetStageCompletionCrystals();
                result.CrystalsFromGoldConvert = crystalTracker.ConvertGoldToCrystals(gold);

                return result;
            }

            // 몬스터 처치
            result.MonstersKilled++;

            // 스테이지 클리어 크리스털 (게임과 동일: 매 몬스터 처치 시 1 크리스털)
            crystalTracker.ProcessStageClear();

            // 보스 처치 시 크리스털 드롭
            if (isBoss)
            {
                result.BossesKilled++;
                var crystalDrop = crystalTracker.ProcessBossKill(
                    currentLevel,
                    permStats.CrystalFlat,
                    permStats.CrystalDropChanceBonus
                );
                if (crystalDrop.Dropped)
                {
                    result.CrystalsFromBosses += crystalDrop.Amount;
                }
            }

            // 골드 획득 (GameManager.OnMonsterDefeated와 동일)
            double baseGold = monster.GoldReward;
            double goldFlatPerm = permStats.GoldFlatPerm;
            double goldFlat = baseGold + goldFlatPerm;
            double goldMultiPerm = permStats.GoldMultiPerm / 100.0;
            int goldReward = (int)(goldFlat * (1.0 + goldMultiPerm));

            gold += goldReward;
            result.TotalGold += goldReward;

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

        // 골드: stage * BASE_GOLD_MULTI
        double goldGrowth = _gameConfig.Balance.BaseGoldMultiplier;

        return new SimMonster(
            level,
            isBoss,
            baseHp,
            hpGrowth,
            0,  // baseGold (사용 안 함)
            goldGrowth
        );
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

    private int CalculateDamage(int basePower, SimPermanentStats permStats, int comboStack, out bool isCritical)
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

        return (int)effectivePower;
    }
}
