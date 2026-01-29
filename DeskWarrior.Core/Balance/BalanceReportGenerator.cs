using DeskWarrior.Core.Models;

namespace DeskWarrior.Core.Balance;

/// <summary>
/// 밸런스 리포트 생성기
/// 분석 결과를 구조화된 리포트로 변환
/// </summary>
public class BalanceReportGenerator
{
    private readonly StatCostCalculator _costCalculator;

    public BalanceReportGenerator(StatCostCalculator costCalculator)
    {
        _costCalculator = costCalculator;
    }

    /// <summary>
    /// 전체 리포트 생성
    /// </summary>
    public BalanceReport Generate(
        PatternRepository repository,
        BalanceQualityResult analysisResult,
        AnalysisConfig config,
        double durationSeconds,
        bool includeRawData = false)
    {
        var report = new BalanceReport();

        // 메타데이터
        report.Metadata = GenerateMetadata(repository, config, durationSeconds);

        // 설정
        report.Config = config;

        // 요약
        report.Summary = GenerateSummary(repository, analysisResult);

        // 상위 패턴
        report.TopPatterns = GeneratePatternDetails(repository, 20);

        // 카테고리 분석
        report.Categories = GenerateCategoryAnalysis(repository, analysisResult);

        // 스탯별 분석
        report.Stats = GenerateStatAnalysis(repository);

        // 권장사항
        report.Recommendations = GenerateRecommendations(analysisResult, report);

        // 원시 데이터 (선택적)
        if (includeRawData)
        {
            report.RawData = GenerateRawData(repository);
        }

        return report;
    }

    private ReportMetadata GenerateMetadata(
        PatternRepository repository,
        AnalysisConfig config,
        double durationSeconds)
    {
        return new ReportMetadata
        {
            GeneratedAt = DateTime.Now,
            AnalysisDurationSeconds = durationSeconds,
            TotalPatternsEvaluated = repository.Count,
            TotalSimulationsRun = repository.Count * config.SimulationsPerPattern
        };
    }

    private BalanceSummary GenerateSummary(
        PatternRepository repository,
        BalanceQualityResult result)
    {
        var topPatterns = repository.TopByLevel(100).ToList();
        var levels = topPatterns
            .Where(p => p.Result != null)
            .Select(p => p.Result!.AverageMaxLevel)
            .ToList();

        var underusedCategories = result.CategoryUsage
            .Where(kvp => kvp.Value < 0.3)
            .Select(kvp => kvp.Key)
            .ToList();

        return new BalanceSummary
        {
            Grade = result.BalanceGrade.ToString(),
            GradeDescription = result.Summary,
            HasDominantRoute = result.HasDominantRoute,
            DominanceRatio = result.DominanceRatio,
            DiversityScore = result.DiversityScore,
            TopPatternSimilarity = result.TopPatternSimilarity,
            BestPatternLevel = levels.FirstOrDefault(),
            WorstPatternLevel = levels.LastOrDefault(),
            AveragePatternLevel = levels.Count > 0 ? levels.Average() : 0,
            LevelSpread = levels.Count > 0 ? levels.First() - levels.Last() : 0,
            ActiveCategories = result.CategoryUsage.Count(kvp => kvp.Value >= 0.3),
            TotalCategories = result.CategoryUsage.Count,
            UnderusedCategories = underusedCategories
        };
    }

    private List<PatternDetail> GeneratePatternDetails(PatternRepository repository, int count)
    {
        var topPatterns = repository.TopByLevel(count).ToList();
        var details = new List<PatternDetail>();

        double bestLevel = topPatterns.FirstOrDefault()?.Result?.AverageMaxLevel ?? 0;

        for (int i = 0; i < topPatterns.Count; i++)
        {
            var pattern = topPatterns[i];
            var result = pattern.Result;
            if (result == null) continue;

            var mainStats = pattern.Allocation
                .Where(kvp => kvp.Value >= 0.1)
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            var primaryCategory = mainStats.Count > 0
                ? StatCategories.GetCategory(mainStats[0])
                : "unknown";

            details.Add(new PatternDetail
            {
                Rank = i + 1,
                PatternId = pattern.PatternId,
                Description = pattern.Description,
                AverageLevel = result.AverageMaxLevel,
                MedianLevel = result.MedianMaxLevel,
                MinLevel = result.MinMaxLevel,
                MaxLevel = result.MaxMaxLevel,
                StandardDeviation = result.StandardDeviation,
                SuccessRate = result.SuccessRate,
                Allocation = pattern.Allocation
                    .Where(kvp => kvp.Value > 0.01)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                MainStats = mainStats,
                PrimaryCategory = primaryCategory,
                LevelDiffFromBest = result.AverageMaxLevel - bestLevel,
                LevelDiffPercent = bestLevel > 0
                    ? (result.AverageMaxLevel - bestLevel) / bestLevel * 100
                    : 0
            });
        }

        return details;
    }

    private List<CategoryAnalysis> GenerateCategoryAnalysis(
        PatternRepository repository,
        BalanceQualityResult result)
    {
        var analyses = new List<CategoryAnalysis>();
        var topPatterns = repository.TopByLevel(20).ToList();

        foreach (var (categoryId, stats) in StatCategories.All)
        {
            var usage = result.CategoryUsage.GetValueOrDefault(categoryId, 0);
            var status = usage >= 0.5 ? "Active" : (usage >= 0.3 ? "Moderate" : "Underused");

            // 해당 카테고리 스탯을 주로 사용하는 패턴 찾기
            var categoryPatterns = topPatterns
                .Where(p => p.Allocation.Any(kvp =>
                    stats.Contains(kvp.Key) && kvp.Value >= 0.3))
                .ToList();

            var bestPattern = categoryPatterns.FirstOrDefault();

            var recommendation = status switch
            {
                "Underused" => $"Consider buffing stats in this category or reducing costs",
                "Moderate" => "Monitor for potential improvements",
                "Active" => "Currently well-balanced",
                _ => ""
            };

            analyses.Add(new CategoryAnalysis
            {
                CategoryId = categoryId,
                CategoryName = FormatCategoryName(categoryId),
                Stats = stats.ToList(),
                UsageRate = usage,
                UsageStatus = status,
                BestPatternInCategory = bestPattern?.PatternId ?? "N/A",
                BestLevelInCategory = bestPattern?.Result?.AverageMaxLevel ?? 0,
                Recommendation = recommendation
            });
        }

        return analyses.OrderByDescending(c => c.UsageRate).ToList();
    }

    private List<StatAnalysis> GenerateStatAnalysis(PatternRepository repository)
    {
        var analyses = new List<StatAnalysis>();
        var allPatterns = repository.All.ToList();
        var topPatterns = repository.TopByLevel(10).ToList();

        // 단일 스탯 패턴 결과
        var singleStatResults = allPatterns
            .Where(p => p.PatternId.StartsWith("single_"))
            .OrderByDescending(p => p.Result?.AverageMaxLevel ?? 0)
            .ToList();

        int rank = 0;
        foreach (var singlePattern in singleStatResults)
        {
            rank++;
            var statId = singlePattern.PatternId.Replace("single_", "");
            var category = StatCategories.GetCategory(statId);

            // 복합 패턴에서 사용률
            var usageInTop = topPatterns
                .Count(p => p.Allocation.GetValueOrDefault(statId, 0) >= 0.1);
            var usageRate = (double)usageInTop / topPatterns.Count;

            // 사용 시 평균 배분율
            var allocationsWhenUsed = allPatterns
                .Where(p => p.Allocation.GetValueOrDefault(statId, 0) >= 0.1)
                .Select(p => p.Allocation[statId])
                .ToList();
            var avgAllocation = allocationsWhenUsed.Count > 0 ? allocationsWhenUsed.Average() : 0;

            // 효율 등급
            var efficiency = singlePattern.Result?.AverageMaxLevel ?? 0;
            var efficiencyRating = rank <= 3 ? "S" :
                                   rank <= 6 ? "A" :
                                   rank <= 10 ? "B" :
                                   rank <= 14 ? "C" : "D";

            // 상태 판정
            var status = usageRate >= 0.5 ? "Overused" :
                         usageRate >= 0.2 ? "Balanced" : "Underused";

            analyses.Add(new StatAnalysis
            {
                StatId = statId,
                StatName = FormatStatName(statId),
                Category = category,
                SingleStatLevel = singlePattern.Result?.AverageMaxLevel ?? 0,
                SingleStatRank = rank,
                UsageInTopPatterns = usageRate,
                AverageAllocationWhenUsed = avgAllocation,
                EfficiencyScore = efficiency,
                EfficiencyRating = efficiencyRating,
                Status = status
            });
        }

        return analyses;
    }

    private List<Recommendation> GenerateRecommendations(
        BalanceQualityResult result,
        BalanceReport report)
    {
        var recommendations = new List<Recommendation>();

        // 지배 루트 감지
        if (result.HasDominantRoute)
        {
            var topPattern = report.TopPatterns.FirstOrDefault();
            recommendations.Add(new Recommendation
            {
                Priority = "Critical",
                Category = "Balance",
                Target = topPattern?.PatternId ?? "Top pattern",
                Issue = $"Dominant route detected (ratio: {result.DominanceRatio:F2})",
                Suggestion = "Nerf the top strategy or significantly buff alternatives",
                ExpectedImpact = "Improved route diversity and player choice"
            });
        }

        // 다양성 부족
        if (result.DiversityScore < 0.3)
        {
            recommendations.Add(new Recommendation
            {
                Priority = "High",
                Category = "Design",
                Target = "Overall balance",
                Issue = $"Low pattern diversity (score: {result.DiversityScore:F2})",
                Suggestion = "Review stat cost/effect ratios to create more viable combinations",
                ExpectedImpact = "More strategic depth and replay value"
            });
        }

        // 카테고리별 권장
        foreach (var category in report.Categories.Where(c => c.UsageStatus == "Underused"))
        {
            recommendations.Add(new Recommendation
            {
                Priority = "Medium",
                Category = "Buff",
                Target = category.CategoryName,
                Issue = $"Category underutilized ({category.UsageRate:P0} usage)",
                Suggestion = $"Consider buffing stats: {string.Join(", ", category.Stats.Take(3))}",
                ExpectedImpact = "Increased build variety"
            });
        }

        // 과소사용 스탯
        foreach (var stat in report.Stats.Where(s => s.Status == "Underused" && s.SingleStatRank > 15))
        {
            recommendations.Add(new Recommendation
            {
                Priority = "Low",
                Category = "Buff",
                Target = stat.StatName,
                Issue = $"Stat underperforming (rank #{stat.SingleStatRank})",
                Suggestion = "Reduce cost or increase effect per level",
                ExpectedImpact = "Stat becomes viable in some builds"
            });
        }

        // 과다사용 스탯
        foreach (var stat in report.Stats.Where(s => s.Status == "Overused" && s.SingleStatRank <= 3))
        {
            recommendations.Add(new Recommendation
            {
                Priority = "Medium",
                Category = "Nerf",
                Target = stat.StatName,
                Issue = $"Stat overperforming (rank #{stat.SingleStatRank}, {stat.UsageInTopPatterns:P0} usage in top builds)",
                Suggestion = "Slightly increase cost or reduce effect",
                ExpectedImpact = "More balanced stat distribution"
            });
        }

        // 밸런스가 좋은 경우
        if (recommendations.Count == 0)
        {
            recommendations.Add(new Recommendation
            {
                Priority = "Low",
                Category = "Design",
                Target = "Overall",
                Issue = "No critical issues detected",
                Suggestion = "Continue monitoring with different scenarios (CPS, target levels)",
                ExpectedImpact = "Maintain healthy balance"
            });
        }

        return recommendations.OrderBy(r => r.Priority switch
        {
            "Critical" => 0,
            "High" => 1,
            "Medium" => 2,
            "Low" => 3,
            _ => 4
        }).ToList();
    }

    private RawData GenerateRawData(PatternRepository repository)
    {
        return new RawData
        {
            AllPatterns = repository.All
                .Where(p => p.Result != null)
                .Select(p => new PatternRawResult
                {
                    PatternId = p.PatternId,
                    Allocation = p.Allocation,
                    AverageLevel = p.Result!.AverageMaxLevel,
                    SuccessRate = p.Result.SuccessRate
                })
                .ToList()
        };
    }

    private string FormatCategoryName(string categoryId)
    {
        return categoryId switch
        {
            "base_stats" => "Base Stats (Attack/Crit)",
            "currency_bonus" => "Currency Bonus (Gold/Crystal)",
            "utility" => "Utility (Time/Discount)",
            "starting_bonus" => "Starting Bonus",
            _ => categoryId
        };
    }

    private string FormatStatName(string statId)
    {
        return statId.Replace("_", " ")
            .Split(' ')
            .Select(w => char.ToUpper(w[0]) + w[1..])
            .Aggregate((a, b) => $"{a} {b}");
    }
}
