using DeskWarrior.Core.Models;

namespace DeskWarrior.Core.Balance;

/// <summary>
/// 루트 다양성 분석기
/// 패턴 탐색 결과를 분석하여 밸런스 품질 판정
/// </summary>
public class RouteDiversityAnalyzer
{
    /// <summary>지배 비율 임계값 (30% 이상이면 지배적)</summary>
    public double DominanceThreshold { get; set; } = 1.3;

    /// <summary>다양성 점수 임계값</summary>
    public double DiversityThreshold { get; set; } = 0.5;

    /// <summary>카테고리 사용률 경고 임계값</summary>
    public double CategoryUsageWarning { get; set; } = 0.3;

    /// <summary>
    /// 패턴 저장소 분석
    /// </summary>
    public BalanceQualityResult Analyze(PatternRepository repository, int topN = 10)
    {
        var result = new BalanceQualityResult();

        var topPatterns = repository.TopByLevel(topN).ToList();
        if (topPatterns.Count < 2)
        {
            result.BalanceGrade = BalanceGrade.F;
            result.Recommendations.Add("Insufficient patterns to analyze");
            return result;
        }

        // 1. 지배 패턴 감지
        result.DominanceRatio = CalculateDominanceRatio(topPatterns);
        result.HasDominantRoute = result.DominanceRatio > DominanceThreshold;

        // 2. 패턴 다양성 분석
        result.DiversityScore = CalculateDiversityScore(topPatterns);
        result.TopPatternSimilarity = CalculateSimilarity(topPatterns);

        // 3. 카테고리 활용도 분석
        result.CategoryUsage = AnalyzeCategoryUsage(topPatterns);

        // 4. 과소/과다 사용 스탯 식별
        var (underused, overused) = IdentifyStatUsageIssues(topPatterns);
        result.UnderusedStats = underused;
        result.OverusedStats = overused;

        // 5. 상위 패턴 요약
        result.TopPatterns = CreatePatternSummaries(topPatterns);

        // 6. 밸런스 등급 결정
        result.BalanceGrade = DetermineGrade(result);

        // 7. 권장 사항 생성
        result.Recommendations = GenerateRecommendations(result);

        return result;
    }

    /// <summary>
    /// 지배 비율 계산 (1위/2위 평균)
    /// </summary>
    private double CalculateDominanceRatio(List<AllocationPattern> topPatterns)
    {
        if (topPatterns.Count < 2)
            return 1.0;

        double first = topPatterns[0].Result?.AverageMaxLevel ?? 0;
        double second = topPatterns[1].Result?.AverageMaxLevel ?? 0;

        if (second <= 0) return double.MaxValue;
        return first / second;
    }

    /// <summary>
    /// 다양성 점수 계산
    /// 상위 패턴들의 스탯 배분이 얼마나 다양한지 측정
    /// </summary>
    private double CalculateDiversityScore(List<AllocationPattern> patterns)
    {
        if (patterns.Count < 2) return 0;

        // 각 패턴에서 주로 사용하는 스탯 집합 추출
        var mainStats = patterns
            .Select(p => p.Allocation
                .Where(kvp => kvp.Value > 0.15)  // 15% 이상 배분된 스탯
                .Select(kvp => kvp.Key)
                .ToHashSet())
            .ToList();

        // 패턴 간 Jaccard 거리의 평균
        double totalDistance = 0;
        int comparisons = 0;

        for (int i = 0; i < mainStats.Count; i++)
        {
            for (int j = i + 1; j < mainStats.Count; j++)
            {
                var intersection = mainStats[i].Intersect(mainStats[j]).Count();
                var union = mainStats[i].Union(mainStats[j]).Count();

                if (union > 0)
                {
                    double jaccard = (double)intersection / union;
                    totalDistance += 1 - jaccard;  // 거리로 변환
                }
                comparisons++;
            }
        }

        return comparisons > 0 ? totalDistance / comparisons : 0;
    }

    /// <summary>
    /// 상위 패턴 간 유사도 계산
    /// </summary>
    private double CalculateSimilarity(List<AllocationPattern> patterns)
    {
        if (patterns.Count < 2) return 0;

        // 상위 3개 패턴 간 코사인 유사도 평균
        var vectors = patterns.Take(3)
            .Select(p => p.Allocation.Values.ToArray())
            .ToList();

        if (vectors.Count < 2) return 0;

        double totalSimilarity = 0;
        int comparisons = 0;

        for (int i = 0; i < vectors.Count; i++)
        {
            for (int j = i + 1; j < vectors.Count; j++)
            {
                totalSimilarity += CosineSimilarity(vectors[i], vectors[j]);
                comparisons++;
            }
        }

        return comparisons > 0 ? totalSimilarity / comparisons : 0;
    }

    private double CosineSimilarity(double[] a, double[] b)
    {
        int minLen = Math.Min(a.Length, b.Length);
        double dot = 0, magA = 0, magB = 0;

        for (int i = 0; i < minLen; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        double denom = Math.Sqrt(magA) * Math.Sqrt(magB);
        return denom > 0 ? dot / denom : 0;
    }

    /// <summary>
    /// 카테고리 사용률 분석
    /// </summary>
    private Dictionary<string, double> AnalyzeCategoryUsage(List<AllocationPattern> patterns)
    {
        var usage = new Dictionary<string, double>();

        foreach (var (category, stats) in StatCategories.All)
        {
            int used = 0;
            foreach (var pattern in patterns)
            {
                bool usesCategory = stats.Any(s =>
                    pattern.Allocation.TryGetValue(s, out var ratio) && ratio > 0.1);
                if (usesCategory) used++;
            }
            usage[category] = (double)used / patterns.Count;
        }

        return usage;
    }

    /// <summary>
    /// 과소/과다 사용 스탯 식별
    /// </summary>
    private (List<string> underused, List<string> overused) IdentifyStatUsageIssues(
        List<AllocationPattern> patterns)
    {
        var statUsage = new Dictionary<string, double>();

        foreach (var pattern in patterns)
        {
            foreach (var (statId, ratio) in pattern.Allocation)
            {
                if (!statUsage.ContainsKey(statId))
                    statUsage[statId] = 0;
                statUsage[statId] += ratio;
            }
        }

        // 평균 사용률
        double avgUsage = statUsage.Values.Average();

        var underused = statUsage
            .Where(kvp => kvp.Value < avgUsage * 0.2)  // 평균의 20% 미만
            .Select(kvp => kvp.Key)
            .ToList();

        var overused = statUsage
            .Where(kvp => kvp.Value > avgUsage * 3)  // 평균의 3배 초과
            .Select(kvp => kvp.Key)
            .ToList();

        return (underused, overused);
    }

    /// <summary>
    /// 상위 패턴 요약 생성
    /// </summary>
    private List<PatternSummary> CreatePatternSummaries(List<AllocationPattern> patterns)
    {
        return patterns.Select((p, i) => new PatternSummary
        {
            Rank = i + 1,
            PatternId = p.PatternId,
            Description = p.Description,
            AverageLevel = p.Result?.AverageMaxLevel ?? 0,
            SuccessRate = p.Result?.SuccessRate ?? 0,
            MainStats = p.Allocation
                .Where(kvp => kvp.Value > 0.1)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        }).ToList();
    }

    /// <summary>
    /// 밸런스 등급 결정
    /// </summary>
    private BalanceGrade DetermineGrade(BalanceQualityResult result)
    {
        // 단일 지배 루트 존재 → F
        if (result.HasDominantRoute)
            return BalanceGrade.F;

        // 다양성 매우 낮음 → D
        if (result.DiversityScore < 0.2)
            return BalanceGrade.D;

        // 카테고리 미사용 → C
        int lowUsageCategories = result.CategoryUsage.Count(kvp => kvp.Value < CategoryUsageWarning);
        if (lowUsageCategories >= 2)
            return BalanceGrade.C;

        // 다양성 양호 → B
        if (result.DiversityScore < DiversityThreshold)
            return BalanceGrade.B;

        // 모든 조건 만족 → A
        return BalanceGrade.A;
    }

    /// <summary>
    /// 권장 사항 생성
    /// </summary>
    private List<string> GenerateRecommendations(BalanceQualityResult result)
    {
        var recommendations = new List<string>();

        // 단일 지배 루트
        if (result.HasDominantRoute)
        {
            recommendations.Add($"Dominant route detected (ratio: {result.DominanceRatio:F2}). " +
                               "Consider nerfing top strategy or buffing alternatives.");
        }

        // 다양성 부족
        if (result.DiversityScore < 0.3)
        {
            recommendations.Add("Low pattern diversity. Consider balancing stat cost/effect ratios.");
        }

        // 카테고리 미사용
        foreach (var (category, usage) in result.CategoryUsage)
        {
            if (usage < CategoryUsageWarning)
            {
                recommendations.Add($"Category '{category}' underutilized ({usage:P0}). " +
                                   "Consider buffing stats in this category.");
            }
        }

        // 과소/과다 사용 스탯
        if (result.UnderusedStats.Count > 0)
        {
            recommendations.Add($"Underused stats: {string.Join(", ", result.UnderusedStats)}. " +
                               "Consider reducing costs or increasing effects.");
        }

        if (result.OverusedStats.Count > 0)
        {
            recommendations.Add($"Overused stats: {string.Join(", ", result.OverusedStats)}. " +
                               "Consider increasing costs or reducing effects.");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Balance appears healthy. Continue monitoring with different scenarios.");
        }

        return recommendations;
    }
}
