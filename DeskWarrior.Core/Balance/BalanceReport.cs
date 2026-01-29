using DeskWarrior.Core.Models;

namespace DeskWarrior.Core.Balance;

/// <summary>
/// 밸런스 분석 리포트 - 전체 결과를 담는 컨테이너
/// JSON/Markdown 출력용
/// </summary>
public class BalanceReport
{
    // === 메타데이터 ===
    public ReportMetadata Metadata { get; set; } = new();

    // === 분석 설정 ===
    public AnalysisConfig Config { get; set; } = new();

    // === 핵심 결과 ===
    public BalanceSummary Summary { get; set; } = new();

    // === 상위 패턴 ===
    public List<PatternDetail> TopPatterns { get; set; } = new();

    // === 카테고리 분석 ===
    public List<CategoryAnalysis> Categories { get; set; } = new();

    // === 스탯별 분석 ===
    public List<StatAnalysis> Stats { get; set; } = new();

    // === 권장사항 ===
    public List<Recommendation> Recommendations { get; set; } = new();

    // === 원시 데이터 (선택적) ===
    public RawData? RawData { get; set; }
}

/// <summary>
/// 리포트 메타데이터
/// </summary>
public class ReportMetadata
{
    public string ReportId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string Version { get; set; } = "1.0";
    public string GeneratorVersion { get; set; } = "DeskWarrior.Simulator v1.0";
    public double AnalysisDurationSeconds { get; set; }
    public int TotalPatternsEvaluated { get; set; }
    public int TotalSimulationsRun { get; set; }
}

/// <summary>
/// 분석 설정
/// </summary>
public class AnalysisConfig
{
    public int TargetLevel { get; set; }
    public double Cps { get; set; }
    public int CrystalBudget { get; set; }
    public string AnalysisMode { get; set; } = "Full";  // Quick / Full / Progression
    public int SimulationsPerPattern { get; set; }
    public int GaGenerations { get; set; }
    public int GaPopulationSize { get; set; }
    public Dictionary<string, int> InitialStats { get; set; } = new();

    // 게임 시간 기반 진행 분석 설정
    public double GameTimeHours { get; set; }  // 0 = 단일 세션 모드
    public string UpgradeStrategy { get; set; } = "Greedy";
}

/// <summary>
/// 밸런스 요약
/// </summary>
public class BalanceSummary
{
    // 핵심 등급
    public string Grade { get; set; } = "F";
    public string GradeDescription { get; set; } = "";
    public bool HasDominantRoute { get; set; }

    // 정량 지표
    public double DominanceRatio { get; set; }
    public double DiversityScore { get; set; }
    public double TopPatternSimilarity { get; set; }

    // 통계
    public double BestPatternLevel { get; set; }
    public double WorstPatternLevel { get; set; }
    public double AveragePatternLevel { get; set; }
    public double LevelSpread { get; set; }  // Best - Worst

    // 진행 시뮬레이션 통계 (GameTimeHours > 0일 때)
    public int BestLevelEver { get; set; }  // 다중 세션 중 역대 최고 레벨
    public int TotalSessionsPlayed { get; set; }
    public double TotalGameTimeHours { get; set; }

    // 카테고리 요약
    public int ActiveCategories { get; set; }
    public int TotalCategories { get; set; }
    public List<string> UnderusedCategories { get; set; } = new();
}

/// <summary>
/// 패턴 상세 정보
/// </summary>
public class PatternDetail
{
    public int Rank { get; set; }
    public string PatternId { get; set; } = "";
    public string Description { get; set; } = "";

    // 결과
    public double AverageLevel { get; set; }
    public double MedianLevel { get; set; }
    public double MinLevel { get; set; }
    public double MaxLevel { get; set; }
    public double StandardDeviation { get; set; }
    public double SuccessRate { get; set; }

    // 진행 시뮬레이션 결과 (GameTimeHours > 0일 때)
    public int BestLevelEver { get; set; }  // 역대 최고 도달 레벨
    public int TotalSessions { get; set; }  // 총 세션 수

    // 배분
    public Dictionary<string, double> Allocation { get; set; } = new();
    public List<string> MainStats { get; set; } = new();  // 10% 이상 스탯
    public string PrimaryCategory { get; set; } = "";

    // 비교
    public double LevelDiffFromBest { get; set; }
    public double LevelDiffPercent { get; set; }
}

/// <summary>
/// 카테고리 분석
/// </summary>
public class CategoryAnalysis
{
    public string CategoryId { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public List<string> Stats { get; set; } = new();

    // 사용률
    public double UsageRate { get; set; }
    public string UsageStatus { get; set; } = "";  // Active / Moderate / Underused

    // 카테고리 내 최고 패턴
    public string BestPatternInCategory { get; set; } = "";
    public double BestLevelInCategory { get; set; }

    // 권장
    public string Recommendation { get; set; } = "";
}

/// <summary>
/// 스탯별 분석
/// </summary>
public class StatAnalysis
{
    public string StatId { get; set; } = "";
    public string StatName { get; set; } = "";
    public string Category { get; set; } = "";

    // 단일 스탯 효율
    public double SingleStatLevel { get; set; }
    public int SingleStatRank { get; set; }

    // 복합 패턴에서 사용률
    public double UsageInTopPatterns { get; set; }
    public double AverageAllocationWhenUsed { get; set; }

    // 효율 지표
    public double EfficiencyScore { get; set; }
    public string EfficiencyRating { get; set; } = "";  // S/A/B/C/D/F

    // 상태
    public string Status { get; set; } = "";  // Overused / Balanced / Underused
}

/// <summary>
/// 권장사항
/// </summary>
public class Recommendation
{
    public string Priority { get; set; } = "Medium";  // Critical / High / Medium / Low
    public string Category { get; set; } = "";  // Balance / Buff / Nerf / Design
    public string Target { get; set; } = "";  // 대상 스탯/카테고리
    public string Issue { get; set; } = "";
    public string Suggestion { get; set; } = "";
    public string ExpectedImpact { get; set; } = "";
}

/// <summary>
/// 원시 데이터 (선택적)
/// </summary>
public class RawData
{
    public List<PatternRawResult> AllPatterns { get; set; } = new();
}

public class PatternRawResult
{
    public string PatternId { get; set; } = "";
    public Dictionary<string, double> Allocation { get; set; } = new();
    public double AverageLevel { get; set; }
    public double SuccessRate { get; set; }
}
