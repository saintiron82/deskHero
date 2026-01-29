using DeskWarrior.Core.Models;
using DeskWarrior.Core.Simulation;

namespace DeskWarrior.Core.Balance;

/// <summary>
/// Hybrid 패턴 탐색기
/// Phase 1: Grid Search로 대략적 최적 영역 탐색
/// Phase 2: Genetic Algorithm으로 정밀 최적화
/// </summary>
public class HybridPatternExplorer
{
    private readonly GridSearchExplorer _gridSearch;
    private readonly GeneticOptimizer _genetic;
    private readonly BatchSimulator _simulator;
    private readonly StatCostCalculator _costCalculator;

    public int SimulationsPerPattern { get; set; } = 50;
    public int GridTopN { get; set; } = 20;
    public int FocusStatCount { get; set; } = 6;  // 6개로 증가 (현재 3 + 과거 2 + 탐색 1)

    public HybridPatternExplorer(
        BatchSimulator simulator,
        StatCostCalculator costCalculator,
        IEnumerable<string> statIds)
    {
        var statIdArray = statIds.ToArray();
        _simulator = simulator;
        _costCalculator = costCalculator;
        _gridSearch = new GridSearchExplorer(statIdArray);
        _genetic = new GeneticOptimizer(statIdArray);
    }

    /// <summary>
    /// 전체 탐색 실행
    /// </summary>
    /// <param name="baseStats">기본 영구 스탯</param>
    /// <param name="profile">입력 프로파일</param>
    /// <param name="crystalBudget">크리스털 예산</param>
    /// <param name="targetLevel">목표 레벨</param>
    /// <param name="history">과거 분석 히스토리 (optional)</param>
    /// <param name="progress">진행률 콜백 (phase, current, total, message)</param>
    public PatternRepository ExploreAll(
        SimPermanentStats baseStats,
        InputProfile profile,
        int crystalBudget,
        int targetLevel,
        AnalysisHistory? history = null,
        Action<int, int, int, string>? progress = null)
    {
        var repository = new PatternRepository();

        // Phase 1a: 단일 스탯 패턴 먼저 평가 (Focus 선택용)
        progress?.Invoke(1, 0, 0, "Phase 1a: Evaluating single-stat patterns...");
        var singlePatterns = _gridSearch.GenerateSingleStatPatterns().ToList();

        int singleEvaluated = 0;
        foreach (var pattern in singlePatterns)
        {
            singleEvaluated++;
            progress?.Invoke(1, singleEvaluated, singlePatterns.Count, $"Single: {singleEvaluated}/{singlePatterns.Count}");

            var result = EvaluatePattern(pattern, baseStats, profile, crystalBudget, targetLevel);
            pattern.Result = result;
            repository.Add(pattern);
        }

        // Phase 1b: 실제 성능 + 과거 데이터 기반 Focus 스탯 선택
        progress?.Invoke(1, 0, 0, "Phase 1b: Selecting focus stats with historical data...");

        // 과거 분석 데이터에서 추천 가져오기
        var (histTop1, histTop2, histBottom1) = history?.GetHistoricalRecommendations() ?? (null, null, null);

        // 과거 데이터 있으면 히스토리 기반, 없으면 현재 성능만으로 선택
        string[] focusStats;
        FocusSelectionInfo? focusSelectionInfo = null;

        if (histTop1 != null || histTop2 != null)
        {
            progress?.Invoke(1, 0, 0, $"Using historical data: top1={histTop1}, top2={histTop2}, btm={histBottom1}");
            (focusStats, focusSelectionInfo) = _gridSearch.IdentifyFocusStatsWithHistory(
                singlePatterns, histTop1, histTop2, histBottom1, FocusStatCount);
        }
        else
        {
            progress?.Invoke(1, 0, 0, "No historical data - using current performance only");
            focusStats = _gridSearch.IdentifyFocusStatsByPerformance(singlePatterns);
        }

        // Focus 선택 이유 표시
        var rankedSingle = singlePatterns
            .Where(p => p.Result != null)
            .OrderByDescending(p => p.Result!.AverageMaxLevel)
            .ToList();

        string focusInfoStr;
        if (focusSelectionInfo != null)
        {
            focusInfoStr = focusSelectionInfo.ToString();
        }
        else
        {
            var focusInfo = focusStats.Select(f =>
            {
                var rank = rankedSingle.FindIndex(p => p.Allocation.ContainsKey(f)) + 1;
                var tag = rank <= 3 ? "TOP" : (rank >= rankedSingle.Count - 1 ? "BTM" : "MID");
                return $"{f}(#{rank}/{tag})";
            });
            focusInfoStr = string.Join(", ", focusInfo);
        }

        progress?.Invoke(1, 0, 0, $"Focus stats: {focusInfoStr}");

        // Phase 1c: Grid 패턴 생성 및 평가
        progress?.Invoke(1, 0, 0, "Generating grid patterns...");
        var gridPatterns = _gridSearch.GenerateGridPatterns(focusStats).ToList();

        progress?.Invoke(1, 0, 0, "Generating two-stat combo patterns...");
        var twoStatPatterns = GenerateTwoStatPatterns(focusStats).ToList();

        var allGridPatterns = gridPatterns.Concat(twoStatPatterns).ToList();
        progress?.Invoke(1, 0, allGridPatterns.Count, $"Evaluating {allGridPatterns.Count} grid patterns...");

        int evaluated = 0;
        foreach (var pattern in allGridPatterns)
        {
            evaluated++;
            if (evaluated % 10 == 0)
            {
                progress?.Invoke(1, evaluated, allGridPatterns.Count, $"Grid: {evaluated}/{allGridPatterns.Count}");
            }

            var result = EvaluatePattern(pattern, baseStats, profile, crystalBudget, targetLevel);
            pattern.Result = result;
            repository.Add(pattern);
        }

        // Phase 2: Genetic Algorithm
        progress?.Invoke(2, 0, _genetic.Generations, "Phase 2: Genetic optimization...");

        var seedPatterns = repository.TopByLevel(GridTopN).ToList();
        if (seedPatterns.Count == 0)
        {
            return repository;
        }

        var bestPattern = _genetic.Evolve(
            seedPatterns,
            p => EvaluatePattern(p, baseStats, profile, crystalBudget, targetLevel).AverageMaxLevel,
            (gen, total) => progress?.Invoke(2, gen, total, $"GA Generation: {gen}/{total}")
        );

        // 최종 결과 평가
        bestPattern.PatternId = "ga_optimized";
        bestPattern.Result = EvaluatePattern(bestPattern, baseStats, profile, crystalBudget, targetLevel);
        repository.Add(bestPattern);

        return repository;
    }

    /// <summary>
    /// 빠른 탐색 (Grid만)
    /// </summary>
    public PatternRepository ExploreQuick(
        SimPermanentStats baseStats,
        InputProfile profile,
        int crystalBudget,
        int targetLevel,
        Action<int, int, string>? progress = null)
    {
        var repository = new PatternRepository();

        // 단일 스탯만
        var patterns = _gridSearch.GenerateSingleStatPatterns().ToList();

        int i = 0;
        foreach (var pattern in patterns)
        {
            progress?.Invoke(++i, patterns.Count, $"Evaluating {pattern.PatternId}...");
            pattern.Result = EvaluatePattern(pattern, baseStats, profile, crystalBudget, targetLevel);
            repository.Add(pattern);
        }

        return repository;
    }

    /// <summary>
    /// 패턴 평가
    /// </summary>
    private PatternResult EvaluatePattern(
        AllocationPattern pattern,
        SimPermanentStats baseStats,
        InputProfile profile,
        int crystalBudget,
        int targetLevel)
    {
        // 패턴을 실제 스탯 레벨로 변환
        var testStats = ApplyPattern(baseStats, pattern, crystalBudget);

        // 배치 시뮬레이션
        var batchResult = _simulator.RunSimulations(
            testStats,
            profile,
            SimulationsPerPattern,
            targetLevel
        );

        return new PatternResult
        {
            AverageMaxLevel = batchResult.AverageLevel,
            MedianMaxLevel = batchResult.MedianLevel,
            MinMaxLevel = batchResult.MinLevel,
            MaxMaxLevel = batchResult.MaxLevel,
            StandardDeviation = batchResult.StandardDeviation,
            SuccessRate = batchResult.SuccessRate,
            AverageCrystals = batchResult.AverageCrystals
        };
    }

    /// <summary>
    /// 패턴을 스탯 레벨로 변환
    /// </summary>
    private SimPermanentStats ApplyPattern(SimPermanentStats baseStats, AllocationPattern pattern, int crystalBudget)
    {
        var stats = baseStats.Clone();

        foreach (var (statId, ratio) in pattern.Allocation)
        {
            if (ratio <= 0) continue;

            int budget = (int)(crystalBudget * ratio);
            int currentLevel = _costCalculator.GetStatLevel(stats, statId);
            int newLevel = _costCalculator.MaxLevelForBudget(statId, budget, currentLevel);

            _costCalculator.SetStatLevel(stats, statId, newLevel);
        }

        return stats;
    }

    /// <summary>
    /// 2스탯 조합 생성 (focusStats에서만)
    /// </summary>
    private IEnumerable<AllocationPattern> GenerateTwoStatPatterns(string[] focusStats)
    {
        for (int i = 0; i < focusStats.Length; i++)
        {
            for (int j = i + 1; j < focusStats.Length; j++)
            {
                foreach (var ratio in new[] { 0.3, 0.5, 0.7 })
                {
                    var pattern = new AllocationPattern
                    {
                        PatternId = $"duo_{focusStats[i]}_{focusStats[j]}_{(int)(ratio * 100)}"
                    };
                    pattern.Allocation[focusStats[i]] = ratio;
                    pattern.Allocation[focusStats[j]] = 1.0 - ratio;
                    yield return pattern;
                }
            }
        }
    }
}
