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

        while (totalGameTime < targetTimeSeconds)
        {
            sessionNumber++;

            // 진행률 콜백
            progress?.Invoke(totalGameTime, targetTimeSeconds);

            // 세션 시뮬레이션
            var session = _engine.SimulateSession(currentStats, profile);

            // 세션 시간 누적
            totalGameTime += session.SessionDuration;

            // 세션 기록
            long crystalsEarned = session.TotalCrystals;
            totalCrystalsEarned += crystalsEarned;

            result.SessionHistory.Add(new SessionProgressRecord
            {
                SessionNumber = sessionNumber,
                MaxLevel = session.MaxLevel,
                CrystalsEarned = crystalsEarned,
                CrystalsBeforeSession = crystals,
                CrystalsAfterSession = crystals + crystalsEarned,
                SessionDurationSeconds = session.SessionDuration,
                CumulativeGameTimeSeconds = totalGameTime
            });

            crystals += crystalsEarned;
            bestLevelEver = Math.Max(bestLevelEver, session.MaxLevel);

            // 업그레이드 전략 적용
            long crystalsSpent = ApplyUpgradeStrategy(currentStats, strategy, ref crystals, sessionNumber, result.UpgradeHistory);
            totalCrystalsSpent += crystalsSpent;
        }

        // 결과 설정
        result.Success = true;  // 시간 기준이므로 항상 완료
        result.AttemptsNeeded = sessionNumber;
        result.FinalStats = currentStats;
        result.TotalCrystalsEarned = totalCrystalsEarned;
        result.TotalCrystalsSpent = totalCrystalsSpent;
        result.FinalMaxLevel = result.SessionHistory.LastOrDefault()?.MaxLevel ?? 0;
        result.BestLevelEver = bestLevelEver;
        result.TotalGameTimeSeconds = totalGameTime;

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

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            // 진행률 콜백
            progress?.Invoke(attempt, maxAttempts);

            // 세션 시뮬레이션
            var session = _engine.SimulateSession(currentStats, profile);

            // 세션 기록
            long crystalsEarned = session.TotalCrystals;
            totalCrystalsEarned += crystalsEarned;

            result.SessionHistory.Add(new SessionProgressRecord
            {
                SessionNumber = attempt,
                MaxLevel = session.MaxLevel,
                CrystalsEarned = crystalsEarned,
                CrystalsBeforeSession = crystals,
                CrystalsAfterSession = crystals + crystalsEarned
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
                return result;
            }

            // 업그레이드 전략 적용
            long crystalsSpent = ApplyUpgradeStrategy(currentStats, strategy, ref crystals, attempt, result.UpgradeHistory);
            totalCrystalsSpent += crystalsSpent;
        }

        // 목표 미달성
        result.Success = false;
        result.AttemptsNeeded = maxAttempts;
        result.FinalStats = currentStats;
        result.TotalCrystalsEarned = totalCrystalsEarned;
        result.TotalCrystalsSpent = totalCrystalsSpent;
        result.FinalMaxLevel = bestLevel;
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
        List<UpgradeRecord> upgradeHistory)
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
                totalSpent = ApplyBalancedStrategy(stats, ref crystals, afterSession, upgradeHistory);
                break;

            case UpgradeStrategy.SimulationBased:
                // TODO: 시뮬레이션 기반 최적화
                totalSpent = ApplyGreedyStrategy(stats, ref crystals, afterSession, upgradeHistory);
                break;
        }

        return totalSpent;
    }

    /// <summary>
    /// 그리디 전략: 비용 대비 효율 최대화
    /// </summary>
    private long ApplyGreedyStrategy(
        SimPermanentStats stats,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> history)
    {
        long totalSpent = 0;

        while (crystals > 0)
        {
            var best = _costCalculator.FindBestUpgrade(stats, crystals);
            if (best == null)
                break;

            var (statId, cost, _) = best.Value;
            int fromLevel = _costCalculator.GetStatLevel(stats, statId);

            crystals -= cost;
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

        return totalSpent;
    }

    /// <summary>
    /// 공격력 우선 전략
    /// </summary>
    private long ApplyDamageFirstStrategy(
        SimPermanentStats stats,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> history)
    {
        var damageStats = new[] { "base_attack", "attack_percent", "crit_chance", "crit_damage", "multi_hit" };
        return ApplyPriorityStrategy(stats, ref crystals, afterSession, history, damageStats);
    }

    /// <summary>
    /// 생존 우선 전략
    /// </summary>
    private long ApplySurvivalFirstStrategy(
        SimPermanentStats stats,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> history)
    {
        var survivalStats = new[] { "time_extend", "start_level", "upgrade_discount" };
        return ApplyPriorityStrategy(stats, ref crystals, afterSession, history, survivalStats);
    }

    /// <summary>
    /// 크리스털 파밍 전략
    /// </summary>
    private long ApplyCrystalFarmStrategy(
        SimPermanentStats stats,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> history)
    {
        var crystalStats = new[] { "crystal_flat", "crystal_multi", "gold_flat_perm", "gold_multi_perm" };
        return ApplyPriorityStrategy(stats, ref crystals, afterSession, history, crystalStats);
    }

    /// <summary>
    /// 균형 전략: 카테고리별 순환
    /// </summary>
    private long ApplyBalancedStrategy(
        SimPermanentStats stats,
        ref long crystals,
        int afterSession,
        List<UpgradeRecord> history)
    {
        var categories = new[]
        {
            new[] { "base_attack", "attack_percent" },
            new[] { "crit_chance", "crit_damage", "multi_hit" },
            new[] { "gold_flat_perm", "gold_multi_perm" },
            new[] { "time_extend" }
        };

        long totalSpent = 0;
        int categoryIndex = afterSession % categories.Length;

        // 해당 카테고리에서 업그레이드
        totalSpent += ApplyPriorityStrategy(stats, ref crystals, afterSession, history, categories[categoryIndex]);

        // 남은 크리스털은 그리디로
        totalSpent += ApplyGreedyStrategy(stats, ref crystals, afterSession, history);

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
}
