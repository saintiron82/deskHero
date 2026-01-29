namespace DeskWarrior.Core.Models;

/// <summary>
/// 다중 세션 시뮬레이션 결과
/// </summary>
public class ProgressionResult
{
    /// <summary>목표 레벨 도달 성공 여부</summary>
    public bool Success { get; set; }

    /// <summary>목표 레벨 도달까지 필요한 시도 횟수</summary>
    public int AttemptsNeeded { get; set; }

    /// <summary>최종 영구 스탯 상태</summary>
    public SimPermanentStats FinalStats { get; set; } = new();

    /// <summary>총 획득 크리스털</summary>
    public int TotalCrystalsEarned { get; set; }

    /// <summary>총 소비 크리스털</summary>
    public int TotalCrystalsSpent { get; set; }

    /// <summary>세션별 기록</summary>
    public List<SessionProgressRecord> SessionHistory { get; set; } = new();

    /// <summary>업그레이드 기록</summary>
    public List<UpgradeRecord> UpgradeHistory { get; set; } = new();

    /// <summary>최종 도달 레벨 (실패 시 마지막 최고 레벨)</summary>
    public int FinalMaxLevel { get; set; }

    /// <summary>총 게임 플레이 시간 (초)</summary>
    public double TotalGameTimeSeconds { get; set; }

    /// <summary>역대 최고 도달 레벨</summary>
    public int BestLevelEver { get; set; }
}

/// <summary>
/// 세션별 진행 기록
/// </summary>
public class SessionProgressRecord
{
    public int SessionNumber { get; set; }
    public int MaxLevel { get; set; }
    public int CrystalsEarned { get; set; }
    public int CrystalsBeforeSession { get; set; }
    public int CrystalsAfterSession { get; set; }
    public double SessionDurationSeconds { get; set; }
    public double CumulativeGameTimeSeconds { get; set; }
}

/// <summary>
/// 업그레이드 기록
/// </summary>
public class UpgradeRecord
{
    public int AfterSessionNumber { get; set; }
    public string StatId { get; set; } = "";
    public int FromLevel { get; set; }
    public int ToLevel { get; set; }
    public int CrystalsCost { get; set; }
}

/// <summary>
/// 업그레이드 전략
/// </summary>
public enum UpgradeStrategy
{
    /// <summary>비용 대비 효율 최대화 (그리디)</summary>
    Greedy,

    /// <summary>공격력 우선</summary>
    DamageFirst,

    /// <summary>시간 연장 우선</summary>
    SurvivalFirst,

    /// <summary>크리스털 획득 우선</summary>
    CrystalFarm,

    /// <summary>균형 잡힌 업그레이드</summary>
    Balanced,

    /// <summary>시뮬레이션 기반 최적화</summary>
    SimulationBased,

    /// <summary>업그레이드 안함 (테스트용)</summary>
    None
}
