using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskWarrior.Core.Balance;

/// <summary>
/// 밸런스 리포트 익스포터
/// JSON 및 Markdown 형식으로 출력
/// </summary>
public class BalanceReportExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// JSON 파일로 내보내기
    /// </summary>
    public void ExportJson(BalanceReport report, string filePath)
    {
        var json = JsonSerializer.Serialize(report, JsonOptions);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }

    /// <summary>
    /// JSON 문자열로 변환
    /// </summary>
    public string ToJson(BalanceReport report)
    {
        return JsonSerializer.Serialize(report, JsonOptions);
    }

    /// <summary>
    /// Markdown 파일로 내보내기
    /// </summary>
    public void ExportMarkdown(BalanceReport report, string filePath)
    {
        var markdown = ToMarkdown(report);
        File.WriteAllText(filePath, markdown, Encoding.UTF8);
    }

    /// <summary>
    /// Markdown 문자열로 변환
    /// </summary>
    public string ToMarkdown(BalanceReport report)
    {
        var sb = new StringBuilder();

        // 헤더
        WriteHeader(sb, report);

        // 요약
        WriteSummary(sb, report);

        // 상위 패턴
        WriteTopPatterns(sb, report);

        // 카테고리 분석
        WriteCategoryAnalysis(sb, report);

        // 스탯 분석
        WriteStatAnalysis(sb, report);

        // 권장사항
        WriteRecommendations(sb, report);

        // 푸터
        WriteFooter(sb, report);

        return sb.ToString();
    }

    private void WriteHeader(StringBuilder sb, BalanceReport report)
    {
        sb.AppendLine("# DeskWarrior Balance Analysis Report");
        sb.AppendLine();
        sb.AppendLine($"**Report ID:** `{report.Metadata.ReportId}`  ");
        sb.AppendLine($"**Generated:** {report.Metadata.GeneratedAt:yyyy-MM-dd HH:mm:ss}  ");
        sb.AppendLine($"**Analysis Duration:** {report.Metadata.AnalysisDurationSeconds:F1}s  ");
        sb.AppendLine($"**Patterns Evaluated:** {report.Metadata.TotalPatternsEvaluated:N0}  ");
        sb.AppendLine($"**Total Simulations:** {report.Metadata.TotalSimulationsRun:N0}  ");
        sb.AppendLine();

        sb.AppendLine("## Analysis Configuration");
        sb.AppendLine();
        sb.AppendLine("| Parameter | Value |");
        sb.AppendLine("|-----------|-------|");
        sb.AppendLine($"| Target Level | {report.Config.TargetLevel} |");
        sb.AppendLine($"| CPS | {report.Config.Cps:F1} |");
        sb.AppendLine($"| Crystal Budget | {report.Config.CrystalBudget:N0} |");
        sb.AppendLine($"| Analysis Mode | {report.Config.AnalysisMode} |");
        sb.AppendLine($"| Simulations/Pattern | {report.Config.SimulationsPerPattern} |");
        if (report.Config.AnalysisMode == "Full")
        {
            sb.AppendLine($"| GA Generations | {report.Config.GaGenerations} |");
            sb.AppendLine($"| GA Population | {report.Config.GaPopulationSize} |");
        }
        sb.AppendLine();
    }

    private void WriteSummary(StringBuilder sb, BalanceReport report)
    {
        var summary = report.Summary;

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine();

        // 등급 배지
        var gradeEmoji = summary.Grade switch
        {
            "A" => ":green_circle:",
            "B" => ":large_blue_circle:",
            "C" => ":yellow_circle:",
            "D" => ":orange_circle:",
            "F" => ":red_circle:",
            _ => ":white_circle:"
        };

        sb.AppendLine($"### Balance Grade: **{summary.Grade}** {gradeEmoji}");
        sb.AppendLine();
        sb.AppendLine($"> {summary.GradeDescription}");
        sb.AppendLine();

        // 핵심 지표
        sb.AppendLine("### Key Metrics");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value | Status |");
        sb.AppendLine("|--------|-------|--------|");

        var dominanceStatus = summary.HasDominantRoute ? "FAIL" : "PASS";
        var dominanceIcon = summary.HasDominantRoute ? ":x:" : ":white_check_mark:";
        sb.AppendLine($"| Dominance Ratio | {summary.DominanceRatio:F2} | {dominanceIcon} {dominanceStatus} |");

        var diversityStatus = summary.DiversityScore >= 0.5 ? "GOOD" : (summary.DiversityScore >= 0.3 ? "MODERATE" : "POOR");
        var diversityIcon = summary.DiversityScore >= 0.5 ? ":white_check_mark:" : (summary.DiversityScore >= 0.3 ? ":warning:" : ":x:");
        sb.AppendLine($"| Diversity Score | {summary.DiversityScore:F2} | {diversityIcon} {diversityStatus} |");

        sb.AppendLine($"| Pattern Similarity | {summary.TopPatternSimilarity:F2} | - |");
        sb.AppendLine($"| Active Categories | {summary.ActiveCategories}/{summary.TotalCategories} | - |");
        sb.AppendLine();

        // 레벨 통계
        sb.AppendLine("### Level Statistics");
        sb.AppendLine();
        sb.AppendLine($"- **Best Pattern:** Level {summary.BestPatternLevel:F1}");
        sb.AppendLine($"- **Worst Pattern:** Level {summary.WorstPatternLevel:F1}");
        sb.AppendLine($"- **Average:** Level {summary.AveragePatternLevel:F1}");
        sb.AppendLine($"- **Spread:** {summary.LevelSpread:F1} levels");
        sb.AppendLine();

        if (summary.UnderusedCategories.Count > 0)
        {
            sb.AppendLine($":warning: **Underused Categories:** {string.Join(", ", summary.UnderusedCategories)}");
            sb.AppendLine();
        }
    }

    private void WriteTopPatterns(StringBuilder sb, BalanceReport report)
    {
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Top Patterns");
        sb.AppendLine();

        // 상위 10개
        sb.AppendLine("### Top 10 Upgrade Strategies");
        sb.AppendLine();
        sb.AppendLine("| Rank | Pattern ID | Avg Level | Success | Main Stats | Diff |");
        sb.AppendLine("|------|------------|-----------|---------|------------|------|");

        foreach (var pattern in report.TopPatterns.Take(10))
        {
            var mainStats = string.Join(", ", pattern.MainStats.Take(3));
            var diff = pattern.LevelDiffPercent >= 0 ? "-" : $"{pattern.LevelDiffPercent:F1}%";
            sb.AppendLine($"| {pattern.Rank} | `{pattern.PatternId}` | {pattern.AverageLevel:F1} | {pattern.SuccessRate:P0} | {mainStats} | {diff} |");
        }
        sb.AppendLine();

        // 상위 5개 상세
        sb.AppendLine("### Detailed Breakdown (Top 5)");
        sb.AppendLine();

        foreach (var pattern in report.TopPatterns.Take(5))
        {
            sb.AppendLine($"#### #{pattern.Rank}: `{pattern.PatternId}`");
            sb.AppendLine();
            sb.AppendLine($"- **Category:** {pattern.PrimaryCategory}");
            sb.AppendLine($"- **Average Level:** {pattern.AverageLevel:F1} (Min: {pattern.MinLevel:F0}, Max: {pattern.MaxLevel:F0})");
            sb.AppendLine($"- **Std Deviation:** {pattern.StandardDeviation:F2}");
            sb.AppendLine($"- **Success Rate:** {pattern.SuccessRate:P1}");
            sb.AppendLine();

            sb.AppendLine("**Allocation:**");
            foreach (var (statId, ratio) in pattern.Allocation.OrderByDescending(kvp => kvp.Value))
            {
                var bar = new string('█', (int)(ratio * 20));
                sb.AppendLine($"- `{statId}`: {ratio:P0} {bar}");
            }
            sb.AppendLine();
        }
    }

    private void WriteCategoryAnalysis(StringBuilder sb, BalanceReport report)
    {
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Category Analysis");
        sb.AppendLine();

        sb.AppendLine("| Category | Usage | Status | Best Pattern | Best Level |");
        sb.AppendLine("|----------|-------|--------|--------------|------------|");

        foreach (var category in report.Categories)
        {
            var statusIcon = category.UsageStatus switch
            {
                "Active" => ":white_check_mark:",
                "Moderate" => ":warning:",
                "Underused" => ":x:",
                _ => ""
            };
            sb.AppendLine($"| {category.CategoryName} | {category.UsageRate:P0} | {statusIcon} {category.UsageStatus} | `{category.BestPatternInCategory}` | {category.BestLevelInCategory:F1} |");
        }
        sb.AppendLine();

        // 카테고리별 상세
        foreach (var category in report.Categories)
        {
            sb.AppendLine($"### {category.CategoryName}");
            sb.AppendLine();
            sb.AppendLine($"**Stats:** {string.Join(", ", category.Stats.Select(s => $"`{s}`"))}");
            sb.AppendLine();
            sb.AppendLine($"**Recommendation:** {category.Recommendation}");
            sb.AppendLine();
        }
    }

    private void WriteStatAnalysis(StringBuilder sb, BalanceReport report)
    {
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Stat Analysis");
        sb.AppendLine();

        sb.AppendLine("### Single-Stat Efficiency Ranking");
        sb.AppendLine();
        sb.AppendLine("| Rank | Stat | Category | Level | Rating | Top10 Usage | Status |");
        sb.AppendLine("|------|------|----------|-------|--------|-------------|--------|");

        foreach (var stat in report.Stats.OrderBy(s => s.SingleStatRank))
        {
            var ratingStyle = stat.EfficiencyRating switch
            {
                "S" => "**S**",
                "A" => "**A**",
                "B" => "B",
                "C" => "C",
                "D" => "_D_",
                _ => stat.EfficiencyRating
            };

            var statusIcon = stat.Status switch
            {
                "Overused" => ":arrow_up:",
                "Balanced" => ":heavy_minus_sign:",
                "Underused" => ":arrow_down:",
                _ => ""
            };

            sb.AppendLine($"| {stat.SingleStatRank} | `{stat.StatId}` | {stat.Category} | {stat.SingleStatLevel:F1} | {ratingStyle} | {stat.UsageInTopPatterns:P0} | {statusIcon} {stat.Status} |");
        }
        sb.AppendLine();

        // 효율 요약
        var sStats = report.Stats.Where(s => s.EfficiencyRating == "S").ToList();
        var dStats = report.Stats.Where(s => s.EfficiencyRating == "D").ToList();

        if (sStats.Count > 0)
        {
            sb.AppendLine($":star: **S-Tier Stats:** {string.Join(", ", sStats.Select(s => $"`{s.StatId}`"))}");
            sb.AppendLine();
        }

        if (dStats.Count > 0)
        {
            sb.AppendLine($":warning: **D-Tier Stats (needs buff):** {string.Join(", ", dStats.Select(s => $"`{s.StatId}`"))}");
            sb.AppendLine();
        }
    }

    private void WriteRecommendations(StringBuilder sb, BalanceReport report)
    {
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine();

        var grouped = report.Recommendations.GroupBy(r => r.Priority);

        foreach (var group in grouped)
        {
            var priorityIcon = group.Key switch
            {
                "Critical" => ":rotating_light:",
                "High" => ":warning:",
                "Medium" => ":bulb:",
                "Low" => ":information_source:",
                _ => ""
            };

            sb.AppendLine($"### {priorityIcon} {group.Key} Priority");
            sb.AppendLine();

            foreach (var rec in group)
            {
                sb.AppendLine($"#### [{rec.Category}] {rec.Target}");
                sb.AppendLine();
                sb.AppendLine($"- **Issue:** {rec.Issue}");
                sb.AppendLine($"- **Suggestion:** {rec.Suggestion}");
                sb.AppendLine($"- **Expected Impact:** {rec.ExpectedImpact}");
                sb.AppendLine();
            }
        }
    }

    private void WriteFooter(StringBuilder sb, BalanceReport report)
    {
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Appendix");
        sb.AppendLine();
        sb.AppendLine("### Glossary");
        sb.AppendLine();
        sb.AppendLine("| Term | Definition |");
        sb.AppendLine("|------|------------|");
        sb.AppendLine("| Dominance Ratio | 1st place / 2nd place average level. >1.3 indicates dominant route |");
        sb.AppendLine("| Diversity Score | Jaccard distance between top patterns (0-1). Higher = more diverse |");
        sb.AppendLine("| Pattern Similarity | Cosine similarity between top 3 patterns (0-1). Lower = more diverse |");
        sb.AppendLine("| Usage Rate | % of top patterns using this category/stat significantly (>10%) |");
        sb.AppendLine();

        sb.AppendLine("### Grade Scale");
        sb.AppendLine();
        sb.AppendLine("| Grade | Meaning |");
        sb.AppendLine("|-------|---------|");
        sb.AppendLine("| A | Excellent - Multiple viable routes, high diversity |");
        sb.AppendLine("| B | Good - Minor dominance tendency |");
        sb.AppendLine("| C | Moderate - Some paths underutilized |");
        sb.AppendLine("| D | Poor - Significant path preference |");
        sb.AppendLine("| F | Fail - Single dominant route detected |");
        sb.AppendLine();

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"*Generated by {report.Metadata.GeneratorVersion}*");
    }
}
