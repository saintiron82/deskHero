namespace DeskWarrior.Core.Models;

/// <summary>
/// 보스 드롭 설정 (config/BossDrops.json에서 로드)
/// </summary>
public class BossDropConfig
{
    /// <summary>기본 드롭 확률 (config/BossDrops.json에서 로드)</summary>
    public double BaseDropChance { get; set; }

    /// <summary>레벨당 드롭 확률 증가</summary>
    public double DropChancePerLevel { get; set; }

    /// <summary>최대 드롭 확률</summary>
    public double MaxDropChance { get; set; }

    /// <summary>기본 크리스털 드롭량</summary>
    public int BaseCrystalAmount { get; set; }

    /// <summary>레벨당 크리스털 증가량</summary>
    public int CrystalPerLevel { get; set; }

    /// <summary>크리스털 성장 지수</summary>
    public double CrystalGrowthExponent { get; set; }

    /// <summary>지수 성장 적용 시작 레벨</summary>
    public int CrystalGrowthBreakpoint { get; set; }

    /// <summary>크리스털 변동폭</summary>
    public double CrystalVariance { get; set; }

    /// <summary>확정 드롭 보장 간격 (Pity 시스템)</summary>
    public int GuaranteedDropEveryNBosses { get; set; }

    /// <summary>스테이지 클리어 보너스 크리스털</summary>
    public int StageCompletionCrystal { get; set; }

    /// <summary>골드 → 크리스털 변환 비율</summary>
    public int GoldToCrystalRate { get; set; }
}

/// <summary>
/// 크리스털 드롭 결과
/// </summary>
public class CrystalDropResult
{
    public bool Dropped { get; set; }  // 항상 true (100% 지급)
    public long Amount { get; set; }
    public long ElementBonus { get; set; }  // 속성 보너스량
    public bool WasGuaranteed { get; set; }  // 더 이상 의미 없음
}
