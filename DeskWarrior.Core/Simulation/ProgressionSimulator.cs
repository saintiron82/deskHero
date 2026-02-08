using DeskWarrior.Core.Models;
using DeskWarrior.Core.Balance;

namespace DeskWarrior.Core.Simulation;

/// <summary>
/// 다중 세션 진행 시뮬레이터
/// 세션 → 크리스털 획득 → 업그레이드 → 다음 세션 반복
/// </summary>
public class ProgressionSimulator
{
    private readonly SimulationEngine _engine;
    private readonly StatCostCalculator _costCalculator;
    private readonly Random _random;

    public ProgressionSimulator(
        SimulationEngine engine,
        StatCostCalculator costCalculator,
        Random? random = null)
    {
        _engine = engine;
        _costCalculator = costCalculator;
        _random = random ?? new Random();
    }

    /// <summary>
    /// 엔진 상태 리셋 (황금 고블린 쿨다운 등)
    /// 새 시뮬레이션 시작 전에 호출
    /// </summary>
    public void ResetEngineState()
    {
        _engine.ResetGoldenGoblinState();
    }

    /// <summary>
    /// 게임 시간 기준 다중 세션 시뮬레이션
    /// 지정된 게임 시간 동안 반복 플레이하여 최고 도달 레벨 측정
    /// </summary>
    /// <param name="initialStats">초기 영구 스탯</param>
    /// <param name="profile">입력 프로파일</param>
    /// <param name="targetGameTimeHours">목표 게임 시간 (시간 단위)</param>
    /// <param name="strategy">업그레이드 전략</param>
    /// <param name="progress">진행률 콜백 (currentTimeSeconds, targetTimeSeconds)</param>
    public ProgressionResult SimulateByGameTime(
        SimPermanentStats initialStats,
        InputProfile profile,
        double targetGameTimeHours,
        UpgradeStrategy strategy,
        Action<double, double>? progress = null)
    {
        double targetTimeSeconds = targetGameTimeHours * 3600;
        var result = new ProgressionResult();
        var currentStats = initialStats.Clone();
        long crystals = 0;
        long totalCrystalsEarned = 0;
        long totalCrystalsSpent = 0;
        long bestLevelEver = 0;
        double totalGameTime = 0;
        int sessionNumber = 0;
        bool targetReachedRecorded = false;

        // 황금 고블린 통계
        int totalGoldenGoblinsKilled = 0;
        int totalGoldenGoblinsEscaped = 0;
        long totalGoldenGoblinGold = 0;

        while (totalGameTime < targetTimeSeconds)
        {
            sessionNumber++;

            // 진행률 콜백
            progress?.Invoke(totalGameTime, targetTimeSeconds);

            // 세션 시뮬레이션
            var session = _engine.SimulateSession(currentStats, profile, (int)bestLevelEver);

            // 세션 시간 누적
            totalGameTime += session.SessionDuration;

            // 목표 시간 이후 처음 도래한 사망 레벨 기록
            if (!targetReachedRecorded && totalGameTime >= targetTimeSeconds)
            {
                result.TargetReachedDeathLevel = session.MaxLevel;
                result.TargetReachedSessionNumber = sessionNumber;
                result.TargetReachedGameTimeSeconds = totalGameTime;
                targetReachedRecorded = true;
            }

            // 세션 기록
            long crystalsEarned = session.TotalCrystals;
            totalCrystalsEarned += crystalsEarned;

            // 황금 고블린 통계 누적
            totalGoldenGoblinsKilled += session.GoldenGoblinsKilled;
            totalGoldenGoblinsEscaped += session.GoldenGoblinsEscaped;
            totalGoldenGoblinGold += session.GoldenGoblinGoldEarned;

            result.SessionHistory.Add(new SessionProgressRecord
            {
                SessionNumber = sessionNumber,
                MaxLevel = session.MaxLevel,
                CrystalsEarned = crystalsEarned,
                CrystalsBeforeSession = crystals,
                CrystalsAfterSession = crystals + crystalsEarned,
                SessionDurationSeconds = session.SessionDuration,
                CumulativeGameTimeSeconds = totalGameTime,
                GoldenGoblinsKilled = session.GoldenGoblinsKilled,
                GoldenGoblinsEscaped = session.GoldenGoblinsEscaped,
                GoldenGoblinGoldEarned = session.GoldenGoblinGoldEarned
            });

            crystals += crystalsEarned;
            bestLevelEver = Math.Max(bestLevelEver, session.MaxLevel);

            // 업그레이드 전략 적용 (세션 결과 전달)
            long crystalsSpent = ApplyUpgradeStrategy(currentStats, strategy, ref crystals, sessionNumber, result.UpgradeHistory, session);
            totalCrystalsSpent += crystalsSpent;

            // ✅ 상세 세션 데이터 저장 (영구 스탯 정보 포함)
            session.SessionNumber = sessionNumber;
            session.TotalPlaytime = totalGameTime;
            session.SpentCrystals = (int)crystalsSpent;
            session.RemainingCrystals = (int)crystals;
            session.PermanentStatLevels = GetStatLevels(currentStats);
            result.DetailedSessions.Add(session);
        }

        // 결과 설정
        result.Success = true;  // 시간 기준이므로 항상 완료
        result.AttemptsNeeded = sessionNumber;
        result.FinalStats = currentStats;
        result.TotalCrystalsEarned = totalCrystalsEarned;
        result.TotalCrystalsSpent = totalCrystalsSpent;
        result.FinalMaxLevel = result.TargetReachedDeathLevel > 0
            ? result.TargetReachedDeathLevel
            : result.SessionHistory.LastOrDefault()?.MaxLevel ?? 0;
        result.BestLevelEver = bestLevelEver;
        result.TotalGameTimeSeconds = totalGameTime;

        // 황금 고블린 통계
        result.TotalGoldenGoblinsKilled = totalGoldenGoblinsKilled;
        result.TotalGoldenGoblinsEscaped = totalGoldenGoblinsEscaped;
        result.TotalGoldenGoblinGold = totalGoldenGoblinGold;

        return result;
    }

    /// <summary>
    /// 목표 레벨 도달까지 다중 세션 시뮬레이션
    /// </summary>
    /// <param name="initialStats">초기 영구 스탯</param>
    /// <param name="profile">입력 프로파일</param>
    /// <param name="targetLevel">목표 레벨</param>
    /// <param name="strategy">업그레이드 전략</param>
    /// <param name="maxAttempts">최대 시도 횟수</param>
    /// <param name="progress">진행률 콜백 (currentAttempt, maxAttempts)</param>
    public ProgressionResult SimulateProgression(
        SimPermanentStats initialStats,
        InputProfile profile,
        int targetLevel,
        UpgradeStrategy strategy,
        int maxAttempts = 1000,
        Action<int, int>? progress = null)
    {
        var result = new ProgressionResult();
        var currentStats = initialStats.Clone();
        long crystals = 0;
        long totalCrystalsEarned = 0;
        long totalCrystalsSpent = 0;
        long bestLevel = 0;

        // 황금 고블린 통계
        int totalGoldenGoblinsKilled = 0;
        int totalGoldenGoblinsEscaped = 0;
        long totalGoldenGoblinGold = 0;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            // 진행률 콜백
            progress?.Invoke(attempt, maxAttempts);

            // 세션 시뮬레이션
            var session = _engine.SimulateSession(currentStats, profile, (int)bestLevel);

            // 황금 고블린 통계 누적
            totalGoldenGoblinsKilled += session.GoldenGoblinsKilled;
            totalGoldenGoblinsEscaped += session.GoldenGoblinsEscaped;
            totalGoldenGoblinGold += session.GoldenGoblinGoldEarned;

            // 세션 기록
            long crystalsEarned = session.TotalCrystals;
            totalCrystalsEarned += crystalsEarned;

            result.SessionHistory.Add(new SessionProgressRecord
            {
                SessionNumber = attempt,
                MaxLevel = session.MaxLevel,
                CrystalsEarned = crystalsEarned,
                CrystalsBeforeSession = crystals,
                CrystalsAfterSession = crystals + crystalsEarned,
                GoldenGoblinsKilled = session.GoldenGoblinsKilled,
                GoldenGoblinsEscaped = session.GoldenGoblinsEscaped,
                GoldenGoblinGoldEarned = session.GoldenGoblinGoldEarned
            });

            crystals += crystalsEarned;
            bestLevel = Math.Max(bestLevel, session.MaxLevel);

            // 목표 달성 체크
            if (session.MaxLevel >= targetLevel)
            {
                result.Success = true;
                result.AttemptsNeeded = attempt;
                result.FinalStats = currentStats;
                result.TotalCrystalsEarned = totalCrystalsEarned;
                result.TotalCrystalsSpent = totalCrystalsSpent;
                result.FinalMaxLevel = session.MaxLevel;
                result.TotalGoldenGoblinsKilled = totalGoldenGoblinsKilled;
                result.TotalGoldenGoblinsEscaped = totalGoldenGoblinsEscaped;
                result.TotalGoldenGoblinGold = totalGoldenGoblinGold;
                return result;
            }

            // 업그레이드 전략 적용 (세션 결과 전달)
            long crystalsSpent = ApplyUpgradeStrategy(currentStats, strategy, ref crystals, attempt, result.UpgradeHistory, session);
            totalCrystalsSpent += crystalsSpent;
        }

        // 목표 미달성
        result.Success = false;
        result.AttemptsNeeded = maxAttempts;
        result.FinalStats = currentStats;
        result.TotalCrystalsEarned = totalCrystalsEarned;
        result.TotalCrystalsSpent = totalCrystalsSpent;
        result.FinalMaxLevel = bestLevel;
        result.TotalGoldenGoblinsKilled = totalGoldenGoblinsKilled;
        result.TotalGoldenGoblinsEscaped = totalGoldenGoblinsEscaped;
        result.TotalGoldenGoblinGold = totalGoldenGoblinGold;
        return result;
    }

    /// <summary>
    /// 업그레이드 전략 적용
    /// </summary>
    private long ApplyUpgradeStrategy(
        SimPermanentStats stats,
        UpgradeStrategy strategy,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> upgradeHistory,
        SessionResult? lastSession = null)
    {
        if (strategy == UpgradeStrategy.None)
            return 0;

        long totalSpent = 0;

        switch (strategy)
        {
            case UpgradeStrategy.Greedy:
                totalSpent = ApplyGreedyStrategy(stats, ref crystals, afterSession, upgradeHistory);
                break;

            case UpgradeStrategy.DamageFirst:
                totalSpent = ApplyDamageFirstStrategy(stats, ref crystals, afterSession, upgradeHistory);
                break;

            case UpgradeStrategy.SurvivalFirst:
                totalSpent = ApplySurvivalFirstStrategy(stats, ref crystals, afterSession, upgradeHistory);
                break;

            case UpgradeStrategy.CrystalFarm:
                totalSpent = ApplyCrystalFarmStrategy(stats, ref crystals, afterSession, upgradeHistory);
                break;

            case UpgradeStrategy.Balanced:
                totalSpent = ApplyBalancedStrategy(stats, ref crystals, afterSession, upgradeHistory, lastSession);
                break;

            case UpgradeStrategy.EconomyFirst:
                totalSpent = ApplyEconomyFirstStrategy(stats, ref crystals, afterSession, upgradeHistory);
                break;

            case UpgradeStrategy.DamageOnly:
                totalSpent = ApplyRestrictedStrategy(stats, ref crystals, afterSession, upgradeHistory,
                    new[] { "base_attack", "attack_percent", "crit_chance", "crit_damage", "multi_hit" });
                break;

            case UpgradeStrategy.DamageTime:
                totalSpent = ApplyRestrictedStrategy(stats, ref crystals, afterSession, upgradeHistory,
                    new[] { "base_attack", "attack_percent", "crit_chance", "crit_damage", "multi_hit", "time_extend" });
                break;

            case UpgradeStrategy.EconomyOnly:
                totalSpent = ApplyRestrictedStrategy(stats, ref crystals, afterSession, upgradeHistory,
                    new[] { "gold_flat_perm", "gold_multi_perm", "crystal_flat", "crystal_multi" });
                break;

            case UpgradeStrategy.UtilityOnly:
                totalSpent = ApplyRestrictedStrategy(stats, ref crystals, afterSession, upgradeHistory,
                    new[]
                    {
                        "time_extend", "upgrade_discount",
                        "start_level", "start_gold",
                        "start_keyboard", "start_mouse",
                        "start_gold_flat", "start_gold_multi",
                        "start_combo_flex", "start_combo_damage"
                    });
                break;

            case UpgradeStrategy.SimulationBased:
                // TODO: 시뮬레이션 기반 최적화
                totalSpent = ApplyGreedyStrategy(stats, ref crystals, afterSession, upgradeHistory);
                break;
        }

        return totalSpent;
    }

    /// <summary>
    /// 그리디 전략: 비용 대비 효율 최대화 + 공격 스탯 균형
    /// Phase 1: 효율 기반 투자 (50% 예산)
    /// Phase 2: 공격 스탯 강제 투자 (50% 예산)
    /// </summary>
    private long ApplyGreedyStrategy(
        SimPermanentStats stats,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> history)
    {
        long totalSpent = 0;
        long initialCrystals = crystals;

        // Phase 1: 효율 기반 투자 (50% 예산)
        long phase1Budget = (long)(initialCrystals * 0.5);
        long phase1Crystals = phase1Budget;

        while (phase1Crystals > 0)
        {
            var best = _costCalculator.FindBestUpgrade(stats, phase1Crystals);
            if (best == null)
                break;

            var (statId, cost, _) = best.Value;
            int fromLevel = _costCalculator.GetStatLevel(stats, statId);

            phase1Crystals -= cost;
            totalSpent += cost;
            _costCalculator.SetStatLevel(stats, statId, fromLevel + 1);

            history.Add(new UpgradeRecord
            {
                AfterSessionNumber = afterSession,
                StatId = statId,
                FromLevel = fromLevel,
                ToLevel = fromLevel + 1,
                CrystalsCost = cost
            });
        }

        crystals -= (phase1Budget - phase1Crystals);

        // Phase 2: 공격 스탯 + start_level 투자 (남은 50% 예산)
        var damageStats = new[] { "crit_damage", "base_attack", "attack_percent", "crit_chance", "start_level" };
        totalSpent += ApplyPriorityStrategy(stats, ref crystals, afterSession, history, damageStats);

        return totalSpent;
    }

    /// <summary>
    /// 공격력 우선 전략 + start_level로 450벽 우회
    /// </summary>
    private long ApplyDamageFirstStrategy(
        SimPermanentStats stats,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> history)
    {
        long totalSpent = 0;
        long initialCrystals = crystals;

        // Phase 1: 공격 스탯에 70% 예산
        var damageStats = new[] { "base_attack", "attack_percent", "crit_chance", "crit_damage", "multi_hit" };
        long phase1Budget = (long)(initialCrystals * 0.7);
        long phase1Crystals = phase1Budget;
        totalSpent += ApplyPriorityStrategy(stats, ref phase1Crystals, afterSession, history, damageStats);
        crystals -= (phase1Budget - phase1Crystals);

        // Phase 2: start_level + time_extend로 진행력 확보
        var progressStats = new[] { "start_level", "time_extend" };
        totalSpent += ApplyPriorityStrategy(stats, ref crystals, afterSession, history, progressStats);

        return totalSpent;
    }

    /// <summary>
    /// 생존 우선 전략 - 데미지 기반 + 시간/유틸리티 보조
    /// Phase 1: 데미지 스탯 (70% 예산)
    /// Phase 2: 생존/유틸리티 스탯 (30% 예산)
    /// </summary>
    private long ApplySurvivalFirstStrategy(
        SimPermanentStats stats,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> history)
    {
        long totalSpent = 0;
        long initialCrystals = crystals;

        // Phase 1: 데미지 스탯 (70% 예산) - DamageFirst와 동일
        var damageStats = new[] { "base_attack", "attack_percent", "crit_chance", "crit_damage", "multi_hit" };
        long phase1Budget = (long)(initialCrystals * 0.7);
        long phase1Crystals = phase1Budget;
        totalSpent += ApplyPriorityStrategy(stats, ref phase1Crystals, afterSession, history, damageStats);
        crystals -= (phase1Budget - phase1Crystals);

        // Phase 2: 생존/유틸리티 스탯 (남은 예산) - time_extend 우선
        var survivalStats = new[] { "time_extend", "start_level", "upgrade_discount" };
        totalSpent += ApplyPriorityStrategy(stats, ref crystals, afterSession, history, survivalStats);

        return totalSpent;
    }

    /// <summary>
    /// 크리스털 파밍 전략 - DamageFirst와 동일 (크리스탈 보조 제거)
    /// 데미지 투자가 우선, 크리스탈 수익은 진행에서 자연 획득
    /// </summary>
    private long ApplyCrystalFarmStrategy(
        SimPermanentStats stats,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> history)
    {
        // DamageFirst와 완전히 동일한 로직 사용
        return ApplyDamageFirstStrategy(stats, ref crystals, afterSession, history);
    }

    /// <summary>
    /// 균형 전략: 카테고리별 순환 + start_level로 450레벨 벽 우회
    /// ✅ 시간 초과 시 time_extend 우선 투자
    /// </summary>
    private long ApplyBalancedStrategy(
        SimPermanentStats stats,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> history,
        SessionResult? lastSession = null)
    {
        long totalSpent = 0;

        // ✅ 시간 초과로 실패했으면 time_extend 우선 투자
        if (lastSession?.EndReason == "timeout" && crystals > 0)
        {
            long timeExtendBudget = (long)(crystals * 0.3);  // 30% 예산을 time_extend에
            long timeExtendCrystals = timeExtendBudget;

            while (timeExtendCrystals > 0)
            {
                int currentLevel = _costCalculator.GetStatLevel(stats, "time_extend");
                int cost = _costCalculator.GetUpgradeCost("time_extend", currentLevel);

                if (cost > timeExtendCrystals)
                    break;

                timeExtendCrystals -= cost;
                totalSpent += cost;
                _costCalculator.SetStatLevel(stats, "time_extend", currentLevel + 1);

                history.Add(new UpgradeRecord
                {
                    AfterSessionNumber = afterSession,
                    StatId = "time_extend",
                    FromLevel = currentLevel,
                    ToLevel = currentLevel + 1,
                    CrystalsCost = cost
                });

                // 최대 5레벨까지만 (한 번에 너무 많이 투자하지 않음)
                if (_costCalculator.GetStatLevel(stats, "time_extend") - currentLevel >= 5)
                    break;
            }

            crystals -= (timeExtendBudget - timeExtendCrystals);
        }

        // 기존 균형 전략
        var categories = new[]
        {
            new[] { "base_attack", "attack_percent" },
            new[] { "crit_chance", "crit_damage", "multi_hit" },
            new[] { "gold_flat_perm", "gold_multi_perm" },
            new[] { "time_extend", "start_level" }
        };

        int startCategoryIndex = afterSession % categories.Length;

        // 모든 카테고리를 순환하면서 투자 시도
        for (int i = 0; i < categories.Length; i++)
        {
            int categoryIndex = (startCategoryIndex + i) % categories.Length;
            long spent = ApplyPriorityStrategy(stats, ref crystals, afterSession, history, categories[categoryIndex]);
            totalSpent += spent;

            // 크리스탈이 충분히 적으면 중단 (최소 업그레이드 비용 이하)
            if (crystals < 1000)
                break;
        }

        // 남은 크리스털은 그리디로
        totalSpent += ApplyGreedyStrategy(stats, ref crystals, afterSession, history);

        return totalSpent;
    }

    /// <summary>
    /// 경제력 우선 전략 - DamageFirst와 동일 (골드 보조 제거)
    /// 데미지 투자가 우선, 골드 수익은 진행에서 자연 획득
    /// </summary>
    private long ApplyEconomyFirstStrategy(
        SimPermanentStats stats,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> history)
    {
        // DamageFirst와 완전히 동일한 로직 사용
        return ApplyDamageFirstStrategy(stats, ref crystals, afterSession, history);
    }

    /// <summary>
    /// 제한된 스탯만 사용하는 그리디 전략
    /// 허용된 스탯 중 효율이 가장 높은 것을 반복 구매
    /// </summary>
    private long ApplyRestrictedStrategy(
        SimPermanentStats stats,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> history,
        string[] allowedStats)
    {
        long totalSpent = 0;

        while (true)
        {
            string? bestStat = null;
            int bestCost = 0;
            double bestEfficiency = 0;
            int bestLevel = 0;

            foreach (var statId in allowedStats)
            {
                int currentLevel = _costCalculator.GetStatLevel(stats, statId);
                if (!_costCalculator.CanUpgrade(statId, currentLevel))
                    continue;

                int cost = _costCalculator.GetUpgradeCost(statId, currentLevel);
                if (cost <= 0 || cost > crystals)
                    continue;

                double efficiency = _costCalculator.GetEfficiency(statId, currentLevel);
                if (bestStat == null || efficiency > bestEfficiency)
                {
                    bestStat = statId;
                    bestCost = cost;
                    bestEfficiency = efficiency;
                    bestLevel = currentLevel;
                }
            }

            if (bestStat == null)
                break;

            crystals -= bestCost;
            totalSpent += bestCost;
            _costCalculator.SetStatLevel(stats, bestStat, bestLevel + 1);

            history.Add(new UpgradeRecord
            {
                AfterSessionNumber = afterSession,
                StatId = bestStat,
                FromLevel = bestLevel,
                ToLevel = bestLevel + 1,
                CrystalsCost = bestCost
            });
        }

        return totalSpent;
    }

    /// <summary>
    /// 우선순위 기반 업그레이드
    /// </summary>
    private long ApplyPriorityStrategy(
        SimPermanentStats stats,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> history,
        string[] priorityStats)
    {
        long totalSpent = 0;

        // 우선순위 스탯들에 대해 업그레이드
        foreach (var statId in priorityStats)
        {
            while (crystals > 0)
            {
                int currentLevel = _costCalculator.GetStatLevel(stats, statId);
                int cost = _costCalculator.GetUpgradeCost(statId, currentLevel);

                if (cost > crystals)
                    break;

                crystals -= cost;
                totalSpent += cost;
                _costCalculator.SetStatLevel(stats, statId, currentLevel + 1);

                history.Add(new UpgradeRecord
                {
                    AfterSessionNumber = afterSession,
                    StatId = statId,
                    FromLevel = currentLevel,
                    ToLevel = currentLevel + 1,
                    CrystalsCost = cost
                });

                // 한 스탯당 최대 3레벨씩만 올리고 다음으로
                if (_costCalculator.GetStatLevel(stats, statId) - currentLevel >= 3)
                    break;
            }
        }

        return totalSpent;
    }

    /// <summary>
    /// 현재 영구 스탯 레벨들을 Dictionary로 추출
    /// </summary>
    private Dictionary<string, int> GetStatLevels(SimPermanentStats stats)
    {
        return new Dictionary<string, int>
        {
            { "base_attack", stats.BaseAttackLevel },
            { "attack_percent", stats.AttackPercentLevel },
            { "crit_chance", stats.CritChanceLevel },
            { "crit_damage", stats.CritDamageLevel },
            { "multi_hit", stats.MultiHitLevel },
            { "gold_flat_perm", stats.GoldFlatPermLevel },
            { "gold_multi_perm", stats.GoldMultiPermLevel },
            { "crystal_flat", stats.CrystalFlatLevel },
            { "crystal_multi", stats.CrystalMultiLevel },
            { "time_extend", stats.TimeExtendLevel },
            { "upgrade_discount", stats.UpgradeDiscountLevel },
            { "start_level", stats.StartLevelLevel },
            { "start_gold", stats.StartGoldLevel },
            { "start_keyboard", stats.StartKeyboardLevel },
            { "start_mouse", stats.StartMouseLevel },
            { "start_gold_flat", stats.StartGoldFlatLevel },
            { "start_gold_multi", stats.StartGoldMultiLevel },
            { "start_combo_flex", stats.StartComboFlexLevel },
            { "start_combo_damage", stats.StartComboDamageLevel }
        };
    }
}
