namespace DeskWarrior.Core.Models;

/// <summary>
/// 범용 업그레이드 패턴 - 크리스털 배분 비율 벡터
/// </summary>
public class AllocationPattern
{
    /// <summary>패턴 식별자</summary>
    public string PatternId { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// 스탯별 배분 비율 (합계 = 1.0)
    /// 예: { "base_attack": 0.4, "crit_chance": 0.3, "time_extend": 0.3 }
    /// </summary>
    public Dictionary<string, double> Allocation { get; set; } = new();

    /// <summary>시뮬레이션 결과</summary>
    public PatternResult? Result { get; set; }

    /// <summary>패턴 복사</summary>
    public AllocationPattern Clone()
    {
        return new AllocationPattern
        {
            PatternId = this.PatternId,
            Allocation = new Dictionary<string, double>(this.Allocation),
            Result = this.Result?.Clone()
        };
    }

    /// <summary>패턴 설명 문자열 (상위 배분 스탯 표시)</summary>
    public string Description
    {
        get
        {
            var top = Allocation
                .Where(kvp => kvp.Value > 0.05)
                .OrderByDescending(kvp => kvp.Value)
                .Take(3)
                .Select(kvp => $"{kvp.Key}:{kvp.Value:P0}");
            return string.Join(" + ", top);
        }
    }

    /// <summary>정규화 (합계를 1.0으로 맞춤)</summary>
    public void Normalize()
    {
        double total = Allocation.Values.Sum();
        if (total <= 0) return;

        foreach (var key in Allocation.Keys.ToList())
        {
            Allocation[key] /= total;
        }
    }
}

/// <summary>
/// 패턴 시뮬레이션 결과
/// </summary>
public class PatternResult
{
    public double AverageMaxLevel { get; set; }
    public double MedianMaxLevel { get; set; }
    public double MinMaxLevel { get; set; }
    public double MaxMaxLevel { get; set; }
    public double StandardDeviation { get; set; }
    public double SuccessRate { get; set; }
    public int AttemptsToTarget { get; set; }
    public double AverageCrystals { get; set; }

    public PatternResult Clone()
    {
        return new PatternResult
        {
            AverageMaxLevel = this.AverageMaxLevel,
            MedianMaxLevel = this.MedianMaxLevel,
            MinMaxLevel = this.MinMaxLevel,
            MaxMaxLevel = this.MaxMaxLevel,
            StandardDeviation = this.StandardDeviation,
            SuccessRate = this.SuccessRate,
            AttemptsToTarget = this.AttemptsToTarget,
            AverageCrystals = this.AverageCrystals
        };
    }
}

/// <summary>
/// 패턴 결과 저장소
/// </summary>
public class PatternRepository
{
    private readonly List<AllocationPattern> _patterns = new();

    public void Add(AllocationPattern pattern) => _patterns.Add(pattern);

    public void AddRange(IEnumerable<AllocationPattern> patterns) => _patterns.AddRange(patterns);

    public IReadOnlyList<AllocationPattern> All => _patterns;

    public int Count => _patterns.Count;

    /// <summary>평균 레벨 기준 상위 N개</summary>
    public IEnumerable<AllocationPattern> TopByLevel(int n) =>
        _patterns
            .Where(p => p.Result != null)
            .OrderByDescending(p => p.Result!.AverageMaxLevel)
            .Take(n);

    /// <summary>성공률 기준 상위 N개</summary>
    public IEnumerable<AllocationPattern> TopBySuccessRate(int n) =>
        _patterns
            .Where(p => p.Result != null)
            .OrderByDescending(p => p.Result!.SuccessRate)
            .Take(n);

    /// <summary>결과가 있는 패턴들만</summary>
    public IEnumerable<AllocationPattern> EvaluatedPatterns =>
        _patterns.Where(p => p.Result != null);

    /// <summary>통계 요약</summary>
    public PatternRepositoryStats GetStats()
    {
        var evaluated = EvaluatedPatterns.ToList();
        if (evaluated.Count == 0)
            return new PatternRepositoryStats();

        var levels = evaluated.Select(p => p.Result!.AverageMaxLevel).ToArray();
        return new PatternRepositoryStats
        {
            TotalPatterns = _patterns.Count,
            EvaluatedPatterns = evaluated.Count,
            BestLevel = levels.Max(),
            WorstLevel = levels.Min(),
            AverageLevel = levels.Average(),
            LevelStdDev = CalculateStdDev(levels)
        };
    }

    private static double CalculateStdDev(double[] values)
    {
        if (values.Length == 0) return 0;
        double mean = values.Average();
        double sumSquares = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumSquares / values.Length);
    }
}

/// <summary>
/// 패턴 저장소 통계
/// </summary>
public class PatternRepositoryStats
{
    public int TotalPatterns { get; set; }
    public int EvaluatedPatterns { get; set; }
    public double BestLevel { get; set; }
    public double WorstLevel { get; set; }
    public double AverageLevel { get; set; }
    public double LevelStdDev { get; set; }
}
