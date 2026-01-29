using System.Collections.Concurrent;
using DeskWarrior.Core.Models;

namespace DeskWarrior.Core.Simulation;

/// <summary>
/// 병렬 배치 시뮬레이션 실행기
/// </summary>
public class BatchSimulator
{
    private readonly GameConfig _gameConfig;
    private readonly Dictionary<string, StatGrowthConfig> _inGameStats;
    private readonly Dictionary<string, StatGrowthConfig> _permanentStats;
    private readonly MonsterConfig _monsterConfig;
    private readonly BossDropConfig _bossDropConfig;

    public BatchSimulator(
        GameConfig gameConfig,
        Dictionary<string, StatGrowthConfig> inGameStats,
        Dictionary<string, StatGrowthConfig> permanentStats,
        MonsterConfig? monsterConfig = null,
        BossDropConfig? bossDropConfig = null)
    {
        _gameConfig = gameConfig;
        _inGameStats = inGameStats;
        _permanentStats = permanentStats;
        _monsterConfig = monsterConfig ?? new MonsterConfig();
        _bossDropConfig = bossDropConfig ?? new BossDropConfig();
    }

    /// <summary>
    /// 배치 시뮬레이션 실행
    /// </summary>
    /// <param name="permStats">영구 스탯</param>
    /// <param name="profile">입력 프로파일</param>
    /// <param name="numSimulations">시뮬레이션 횟수</param>
    /// <param name="targetLevel">목표 레벨 (0 = 무시)</param>
    /// <param name="parallelism">병렬 처리 수 (-1 = 모든 코어)</param>
    /// <param name="progress">진행률 콜백</param>
    /// <returns>배치 결과</returns>
    public BatchResult RunSimulations(
        SimPermanentStats permStats,
        InputProfile profile,
        int numSimulations,
        int targetLevel = 0,
        int parallelism = -1,
        Action<int, int>? progress = null)
    {
        var results = new ConcurrentBag<SessionResult>();
        int completed = 0;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = parallelism == -1 ? Environment.ProcessorCount : parallelism
        };

        Parallel.For(0, numSimulations, options, i =>
        {
            var engine = new SimulationEngine(
                _gameConfig,
                _inGameStats,
                _permanentStats,
                _monsterConfig,
                _bossDropConfig,
                seed: i  // 재현 가능한 시드
            );

            var result = engine.SimulateSession(permStats, profile);
            results.Add(result);

            int current = Interlocked.Increment(ref completed);
            if (progress != null && current % 100 == 0)
            {
                progress(current, numSimulations);
            }
        });

        return AnalyzeResults(results.ToList(), targetLevel, numSimulations);
    }

    /// <summary>
    /// 순차 시뮬레이션 실행 (디버깅용)
    /// </summary>
    public BatchResult RunSimulationsSequential(
        SimPermanentStats permStats,
        InputProfile profile,
        int numSimulations,
        int targetLevel = 0,
        Action<int, int>? progress = null)
    {
        var results = new List<SessionResult>();

        for (int i = 0; i < numSimulations; i++)
        {
            var engine = new SimulationEngine(
                _gameConfig,
                _inGameStats,
                _permanentStats,
                _monsterConfig,
                _bossDropConfig,
                seed: i
            );

            var result = engine.SimulateSession(permStats, profile);
            results.Add(result);

            if (progress != null && (i + 1) % 100 == 0)
            {
                progress(i + 1, numSimulations);
            }
        }

        return AnalyzeResults(results, targetLevel, numSimulations);
    }

    private BatchResult AnalyzeResults(List<SessionResult> results, int targetLevel, int numSimulations)
    {
        var levels = results.Select(r => (double)r.MaxLevel).ToArray();
        Array.Sort(levels);

        var batch = new BatchResult
        {
            NumSimulations = numSimulations,
            AverageLevel = levels.Average(),
            MedianLevel = GetMedian(levels),
            MinLevel = levels.Min(),
            MaxLevel = levels.Max(),
            StandardDeviation = CalculateStdDev(levels),
            AverageDuration = results.Average(r => r.SessionDuration),
            AllResults = results,

            // 크리스털 통계
            AverageCrystals = results.Average(r => r.TotalCrystals),
            AverageCrystalsFromBosses = results.Average(r => r.CrystalsFromBosses),
            AverageCrystalsFromStages = results.Average(r => r.CrystalsFromStages),
            AverageCrystalsFromGoldConvert = results.Average(r => r.CrystalsFromGoldConvert)
        };

        // 레벨 분포 (1~max)
        int maxLevel = (int)batch.MaxLevel;
        batch.LevelDistribution = new double[maxLevel];
        foreach (var result in results)
        {
            if (result.MaxLevel > 0 && result.MaxLevel <= maxLevel)
            {
                batch.LevelDistribution[result.MaxLevel - 1]++;
            }
        }
        for (int i = 0; i < batch.LevelDistribution.Length; i++)
        {
            batch.LevelDistribution[i] /= numSimulations;
        }

        // 목표 레벨 분석
        if (targetLevel > 0)
        {
            batch.TargetLevel = targetLevel;
            int successes = results.Count(r => r.MaxLevel >= targetLevel);
            batch.SuccessRate = (double)successes / numSimulations;

            if (batch.SuccessRate > 0)
            {
                // 기하분포: 중앙값 = -1 / log2(1 - p)
                batch.MedianAttemptsToTarget = Math.Ceiling(-1.0 / Math.Log(1.0 - batch.SuccessRate, 2));
            }
            else
            {
                batch.MedianAttemptsToTarget = double.PositiveInfinity;
            }
        }

        return batch;
    }

    private static double GetMedian(double[] sorted)
    {
        int n = sorted.Length;
        if (n == 0) return 0;
        if (n % 2 == 0)
        {
            return (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
        }
        return sorted[n / 2];
    }

    private static double CalculateStdDev(double[] values)
    {
        if (values.Length == 0) return 0;
        double mean = values.Average();
        double sumSquares = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumSquares / values.Length);
    }
}
