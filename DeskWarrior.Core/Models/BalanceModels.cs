namespace DeskWarrior.Core.Models;

/// <summary>
/// 밸런스 품질 등급
/// A: 다양한 루트 존재 (우수)
/// F: 단일 지배 루트 존재 (실패)
/// </summary>
public enum BalanceGrade
{
    A,  // 우수: 다양한 루트, 높은 다양성
    B,  // 양호: 약간의 지배 경향 있음
    C,  // 보통: 일부 루트 편중
    D,  // 미흡: 상당한 편중
    F   // 실패: 단일 지배 루트 존재
}

/// <summary>
/// 밸런스 품질 분석 결과
/// </summary>
public class BalanceQualityResult
{
    // 핵심 판정
    /// <summary>단일 지배 루트 존재 여부</summary>
    public bool HasDominantRoute { get; set; }

    /// <summary>밸런스 등급</summary>
    public BalanceGrade BalanceGrade { get; set; }

    // 상세 지표
    /// <summary>지배 비율 (1위/2위 평균레벨 비율)</summary>
    public double DominanceRatio { get; set; }

    /// <summary>패턴 다양성 점수 (0.0 ~ 1.0)</summary>
    public double DiversityScore { get; set; }

    /// <summary>상위 패턴 간 유사도 (낮을수록 좋음)</summary>
    public double TopPatternSimilarity { get; set; }

    /// <summary>카테고리별 사용률</summary>
    public Dictionary<string, double> CategoryUsage { get; set; } = new();

    /// <summary>미사용/과소사용 스탯</summary>
    public List<string> UnderusedStats { get; set; } = new();

    /// <summary>과다사용 스탯</summary>
    public List<string> OverusedStats { get; set; } = new();

    // 상위 패턴
    /// <summary>상위 패턴 목록</summary>
    public List<PatternSummary> TopPatterns { get; set; } = new();

    // 권장 사항
    /// <summary>밸런스 개선 제안</summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>분석 요약 메시지</summary>
    public string Summary =>
        BalanceGrade switch
        {
            BalanceGrade.A => "Excellent! Multiple viable upgrade routes exist.",
            BalanceGrade.B => "Good balance with minor issues.",
            BalanceGrade.C => "Moderate balance issues - some paths underutilized.",
            BalanceGrade.D => "Poor balance - significant path preference.",
            BalanceGrade.F => "Balance failure - single dominant route detected.",
            _ => "Unknown"
        };
}

/// <summary>
/// 패턴 요약
/// </summary>
public class PatternSummary
{
    public int Rank { get; set; }
    public string PatternId { get; set; } = "";
    public string Description { get; set; } = "";
    public double AverageLevel { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<string, double> MainStats { get; set; } = new();
}

/// <summary>
/// 스탯 카테고리 정의
/// </summary>
public static class StatCategories
{
    public static readonly Dictionary<string, string[]> All = new()
    {
        ["base_stats"] = new[] { "base_attack", "attack_percent", "crit_chance", "crit_damage", "multi_hit" },
        ["currency_bonus"] = new[] { "gold_flat_perm", "gold_multi_perm", "crystal_flat", "crystal_multi" },
        ["utility"] = new[] { "time_extend", "upgrade_discount" },
        ["starting_bonus"] = new[] { "start_level", "start_gold", "start_keyboard", "start_mouse",
                                      "start_gold_flat", "start_gold_multi", "start_combo_flex", "start_combo_damage" }
    };

    public static string GetCategory(string statId)
    {
        foreach (var (category, stats) in All)
        {
            if (stats.Contains(statId))
                return category;
        }
        return "unknown";
    }
}
