using System.Text.Json;

namespace DeskWarrior.Core.Balance;

/// <summary>
/// 분석 히스토리 관리
/// 과거 분석 결과를 저장하고 다음 분석에 활용
/// </summary>
public class AnalysisHistory
{
    private const string HistoryFileName = "analysis_history.json";

    /// <summary>
    /// 히스토리 데이터
    /// </summary>
    public List<AnalysisRecord> Records { get; set; } = new();

    /// <summary>
    /// 히스토리 파일 경로
    /// </summary>
    public static string GetHistoryPath(string balanceDocPath)
    {
        return Path.Combine(balanceDocPath, HistoryFileName);
    }

    /// <summary>
    /// 히스토리 로드
    /// </summary>
    public static AnalysisHistory Load(string balanceDocPath)
    {
        var path = GetHistoryPath(balanceDocPath);

        if (!File.Exists(path))
        {
            return new AnalysisHistory();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AnalysisHistory>(json) ?? new AnalysisHistory();
        }
        catch
        {
            return new AnalysisHistory();
        }
    }

    /// <summary>
    /// 히스토리 저장
    /// </summary>
    public void Save(string balanceDocPath)
    {
        // 디렉토리가 없으면 생성
        if (!Directory.Exists(balanceDocPath))
        {
            Directory.CreateDirectory(balanceDocPath);
        }

        var path = GetHistoryPath(balanceDocPath);
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 새 분석 결과 추가
    /// </summary>
    public void AddRecord(AnalysisRecord record)
    {
        Records.Add(record);

        // 최근 10개만 유지
        if (Records.Count > 10)
        {
            Records = Records.TakeLast(10).ToList();
        }
    }

    /// <summary>
    /// 가장 최근 분석 결과
    /// </summary>
    public AnalysisRecord? GetLatest()
    {
        return Records.LastOrDefault();
    }

    /// <summary>
    /// 과거 분석에서 Focus 스탯 추천
    /// - 과거 최고 성능 1위
    /// - 과거 최고 성능 2위
    /// - 과거 최하위 (아직 시너지 못 찾은 스탯)
    /// </summary>
    public (string? top1, string? top2, string? bottom1) GetHistoricalRecommendations()
    {
        var latest = GetLatest();
        if (latest == null)
        {
            return (null, null, null);
        }

        return (
            latest.TopStats.ElementAtOrDefault(0),
            latest.TopStats.ElementAtOrDefault(1),
            latest.BottomStats.FirstOrDefault()
        );
    }
}

/// <summary>
/// 단일 분석 기록
/// </summary>
public class AnalysisRecord
{
    public string RecordId { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
    public string BalanceGrade { get; set; } = "";

    // 분석 조건
    public int TargetLevel { get; set; }
    public double Cps { get; set; }
    public int CrystalBudget { get; set; }

    // 스탯 순위 (상위/하위)
    public List<string> TopStats { get; set; } = new();      // 상위 5개
    public List<string> BottomStats { get; set; } = new();   // 하위 5개

    // 사용된 Focus 스탯
    public List<string> FocusStats { get; set; } = new();

    // 최고 패턴
    public string BestPatternId { get; set; } = "";
    public double BestPatternLevel { get; set; }
}
