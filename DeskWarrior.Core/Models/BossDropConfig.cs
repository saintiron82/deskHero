namespace DeskWarrior.Core.Models;

/// <summary>
/// 보스 드롭 설정 (config/BossDrops.json에서 로드)
/// </summary>
public class BossDropConfig
{
    /// <summary>기본 드롭 확률 (0.5 = 50%)</summary>
    public double BaseDropChance { get; set; } = 0.5;

    /// <summary>레벨당 드롭 확률 증가 (0.005 = 0.5%)</summary>
    public double DropChancePerLevel { get; set; } = 0.005;

    /// <summary>최대 드롭 확률 (0.95 = 95%)</summary>
    public double MaxDropChance { get; set; } = 0.95;

    /// <summary>기본 크리스털 드롭량</summary>
    public int BaseCrystalAmount { get; set; } = 5;

    /// <summary>레벨당 크리스털 증가량</summary>
    public int CrystalPerLevel { get; set; } = 1;

    /// <summary>크리스털 변동폭 (0.2 = ±20%)</summary>
    public double CrystalVariance { get; set; } = 0.2;

    /// <summary>확정 드롭 보장 간격 (Pity 시스템)</summary>
    public int GuaranteedDropEveryNBosses { get; set; } = 10;

    /// <summary>스테이지 클리어 보너스 크리스털</summary>
    public int StageCompletionCrystal { get; set; } = 1;

    /// <summary>골드 → 크리스털 변환 비율 (1000 골드 = 1 크리스털)</summary>
    public int GoldToCrystalRate { get; set; } = 1000;
}

/// <summary>
/// 크리스털 드롭 결과
/// </summary>
public class CrystalDropResult
{
    public bool Dropped { get; set; }
    public int Amount { get; set; }
    public bool WasGuaranteed { get; set; }
}
