using DeskWarrior.Core.Models;

namespace DeskWarrior.Core.Balance;

/// <summary>
/// 유전 알고리즘 최적화기
/// Grid Search 결과를 초기 개체군으로 사용하여 정밀 탐색
/// </summary>
public class GeneticOptimizer
{
    private readonly Random _random;
    private readonly string[] _statIds;

    public int PopulationSize { get; set; } = 50;
    public int Generations { get; set; } = 100;
    public double MutationRate { get; set; } = 0.1;
    public double CrossoverRate { get; set; } = 0.7;
    public int EliteCount { get; set; } = 5;
    public int TournamentSize { get; set; } = 3;

    public GeneticOptimizer(IEnumerable<string> statIds, Random? random = null)
    {
        _statIds = statIds.ToArray();
        _random = random ?? new Random();
    }

    /// <summary>
    /// 시드 패턴에서 진화 시작
    /// </summary>
    /// <param name="seedPatterns">초기 개체군 (Grid 상위 결과)</param>
    /// <param name="fitnessFunc">적합도 함수 (패턴 → 점수)</param>
    /// <param name="progress">진행률 콜백 (generation, totalGenerations)</param>
    public AllocationPattern Evolve(
        IEnumerable<AllocationPattern> seedPatterns,
        Func<AllocationPattern, double> fitnessFunc,
        Action<int, int>? progress = null)
    {
        // 1. 초기 개체군 구성
        var population = InitializePopulation(seedPatterns.ToList());

        AllocationPattern? best = null;
        double bestFitness = double.MinValue;

        // 2. 세대 반복
        for (int gen = 0; gen < Generations; gen++)
        {
            progress?.Invoke(gen + 1, Generations);

            // 3. 적합도 평가
            var evaluated = population
                .Select(p => (Pattern: p, Fitness: fitnessFunc(p)))
                .OrderByDescending(x => x.Fitness)
                .ToList();

            // 최고 기록 갱신
            if (evaluated[0].Fitness > bestFitness)
            {
                bestFitness = evaluated[0].Fitness;
                best = evaluated[0].Pattern.Clone();
            }

            // 4. 다음 세대 생성
            var nextGen = new List<AllocationPattern>();

            // 4a. 엘리트 보존
            nextGen.AddRange(evaluated.Take(EliteCount).Select(x => x.Pattern.Clone()));

            // 4b. 교차 및 돌연변이
            while (nextGen.Count < PopulationSize)
            {
                // 토너먼트 선택
                var parent1 = TournamentSelect(evaluated);
                var parent2 = TournamentSelect(evaluated);

                // 교차
                AllocationPattern child;
                if (_random.NextDouble() < CrossoverRate)
                {
                    child = Crossover(parent1, parent2);
                }
                else
                {
                    child = parent1.Clone();
                }

                // 돌연변이
                if (_random.NextDouble() < MutationRate)
                {
                    Mutate(child);
                }

                nextGen.Add(child);
            }

            population = nextGen;
        }

        return best ?? population[0];
    }

    /// <summary>
    /// 초기 개체군 구성
    /// </summary>
    private List<AllocationPattern> InitializePopulation(List<AllocationPattern> seeds)
    {
        var population = new List<AllocationPattern>();

        // 시드 패턴 추가
        foreach (var seed in seeds.Take(PopulationSize / 2))
        {
            population.Add(seed.Clone());
        }

        // 나머지는 랜덤 생성
        while (population.Count < PopulationSize)
        {
            population.Add(GenerateRandomPattern());
        }

        return population;
    }

    /// <summary>
    /// 랜덤 패턴 생성
    /// </summary>
    private AllocationPattern GenerateRandomPattern()
    {
        var pattern = new AllocationPattern();

        // 랜덤 배분
        double remaining = 1.0;
        var shuffled = _statIds.OrderBy(_ => _random.Next()).ToList();

        for (int i = 0; i < shuffled.Count - 1; i++)
        {
            double ratio = _random.NextDouble() * remaining;
            pattern.Allocation[shuffled[i]] = ratio;
            remaining -= ratio;
        }
        pattern.Allocation[shuffled[^1]] = remaining;

        return pattern;
    }

    /// <summary>
    /// 토너먼트 선택
    /// </summary>
    private AllocationPattern TournamentSelect(List<(AllocationPattern Pattern, double Fitness)> evaluated)
    {
        var candidates = new List<(AllocationPattern Pattern, double Fitness)>();
        for (int i = 0; i < TournamentSize; i++)
        {
            int idx = _random.Next(evaluated.Count);
            candidates.Add(evaluated[idx]);
        }
        return candidates.OrderByDescending(c => c.Fitness).First().Pattern;
    }

    /// <summary>
    /// 교차 (Uniform Crossover)
    /// </summary>
    private AllocationPattern Crossover(AllocationPattern parent1, AllocationPattern parent2)
    {
        var child = new AllocationPattern();

        foreach (var statId in _statIds)
        {
            double val1 = parent1.Allocation.GetValueOrDefault(statId, 0);
            double val2 = parent2.Allocation.GetValueOrDefault(statId, 0);

            // 부모 중 하나를 랜덤 선택하거나 평균
            if (_random.NextDouble() < 0.5)
            {
                child.Allocation[statId] = _random.NextDouble() < 0.5 ? val1 : val2;
            }
            else
            {
                child.Allocation[statId] = (val1 + val2) / 2;
            }
        }

        child.Normalize();
        return child;
    }

    /// <summary>
    /// 돌연변이
    /// </summary>
    private void Mutate(AllocationPattern pattern)
    {
        // 두 스탯 간 비율 교환
        var statIds = pattern.Allocation.Keys.ToList();
        if (statIds.Count < 2) return;

        string stat1 = statIds[_random.Next(statIds.Count)];
        string stat2 = statIds[_random.Next(statIds.Count)];
        if (stat1 == stat2) return;

        double transfer = _random.NextDouble() * 0.15;  // 최대 15% 이동
        double val1 = pattern.Allocation.GetValueOrDefault(stat1, 0);

        if (val1 >= transfer)
        {
            pattern.Allocation[stat1] = val1 - transfer;
            pattern.Allocation[stat2] = pattern.Allocation.GetValueOrDefault(stat2, 0) + transfer;
        }

        // 새 스탯 추가 돌연변이 (10% 확률)
        if (_random.NextDouble() < 0.1)
        {
            var unusedStats = _statIds.Except(pattern.Allocation.Keys.Where(k => pattern.Allocation[k] > 0.01)).ToList();
            if (unusedStats.Count > 0)
            {
                var newStat = unusedStats[_random.Next(unusedStats.Count)];
                double newRatio = _random.NextDouble() * 0.1;  // 최대 10%

                // 기존에서 빼서 새 스탯에 할당
                foreach (var key in pattern.Allocation.Keys.ToList())
                {
                    if (pattern.Allocation[key] > newRatio / pattern.Allocation.Count)
                    {
                        pattern.Allocation[key] -= newRatio / pattern.Allocation.Count;
                    }
                }
                pattern.Allocation[newStat] = newRatio;
            }
        }

        pattern.Normalize();
    }
}
