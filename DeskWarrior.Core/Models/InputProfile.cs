namespace DeskWarrior.Core.Models;

/// <summary>
/// 입력 프로파일 - CPS 기반 입력 시뮬레이션 설정
/// </summary>
public class InputProfile
{
    /// <summary>
    /// 초당 클릭 수 (평균)
    /// </summary>
    public double AverageCps { get; set; } = 5.0;

    /// <summary>
    /// CPS 변동성 (0.0 ~ 1.0, 0.2 = ±20%)
    /// </summary>
    public double CpsVariance { get; set; } = 0.2;

    /// <summary>
    /// 콤보 스킬 레벨
    /// </summary>
    public ComboSkillLevel ComboSkill { get; set; } = ComboSkillLevel.None;

    /// <summary>
    /// 키보드/마우스 비율 (0.0 = 키보드만, 1.0 = 마우스만)
    /// </summary>
    public double MouseRatio { get; set; } = 0.0;

    /// <summary>
    /// 자동 업그레이드 활성화
    /// </summary>
    public bool AutoUpgrade { get; set; } = true;

    /// <summary>
    /// 업그레이드 우선순위 (keyboard_power, mouse_power)
    /// </summary>
    public string[] UpgradePriority { get; set; } = ["keyboard_power", "mouse_power"];
}

/// <summary>
/// 콤보 스킬 수준
/// </summary>
public enum ComboSkillLevel
{
    /// <summary>
    /// 콤보 없음 - 리듬 유지 시도 안함
    /// </summary>
    None = 0,

    /// <summary>
    /// 초보자 - 50% 성공률, 최대 1스택
    /// </summary>
    Beginner = 1,

    /// <summary>
    /// 중급자 - 70% 성공률, 최대 2스택
    /// </summary>
    Intermediate = 2,

    /// <summary>
    /// 고급자 - 90% 성공률, 최대 3스택
    /// </summary>
    Expert = 3,

    /// <summary>
    /// 완벽 - 100% 성공률, 항상 3스택
    /// </summary>
    Perfect = 4
}

/// <summary>
/// 콤보 스킬 헬퍼
/// </summary>
public static class ComboSkillHelper
{
    public static double GetSuccessRate(ComboSkillLevel level) => level switch
    {
        ComboSkillLevel.None => 0.0,
        ComboSkillLevel.Beginner => 0.5,
        ComboSkillLevel.Intermediate => 0.7,
        ComboSkillLevel.Expert => 0.9,
        ComboSkillLevel.Perfect => 1.0,
        _ => 0.0
    };

    public static int GetMaxStack(ComboSkillLevel level) => level switch
    {
        ComboSkillLevel.None => 0,
        ComboSkillLevel.Beginner => 1,
        ComboSkillLevel.Intermediate => 2,
        ComboSkillLevel.Expert => 3,
        ComboSkillLevel.Perfect => 3,
        _ => 0
    };
}
