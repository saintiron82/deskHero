using DeskWarrior.Core.Models;

namespace DeskWarrior.Core.Simulation;

/// <summary>
/// 크리스털 획득 추적기
/// 보스 드롭, 스테이지 클리어, 골드 변환을 시뮬레이션
/// </summary>
public class CrystalTracker
{
    private readonly BossDropConfig _config;
    private readonly Random _random;
    private int _stageCompletionCrystals;  // 매 몬스터 처치 시 누적

    public CrystalTracker(BossDropConfig config, Random random)
    {
        _config = config;
        _random = random;
        _stageCompletionCrystals = 0;
    }

    /// <summary>
    /// 보스 처치 시 크리스털 지급 (100% 확정, 속성별 배율 적용)
    /// </summary>
    /// <param name="bossLevel">보스 레벨</param>
    /// <param name="bossElement">보스 속성 (normal, fire, holy 등)</param>
    /// <param name="crystalFlat">영구 스탯: 크리스털 추가량</param>
    /// <param name="crystalMultipliers">속성별 크리스털 배율 (config에서 로드)</param>
    public CrystalDropResult ProcessBossKill(int bossLevel, string bossElement, int crystalFlat, Dictionary<string, double> crystalMultipliers)
    {
        // ✅ 기본 크리스탈 계산 (100% 지급)
        int baseCrystals = _config.BaseCrystalAmount + bossLevel * _config.CrystalPerLevel;
        baseCrystals += crystalFlat;  // 영구 스탯 보너스

        // ✅ 속성별 배율 적용
        double elementMultiplier = crystalMultipliers.GetValueOrDefault(bossElement, 1.0);
        int crystalsBeforeVariance = (int)(baseCrystals * elementMultiplier);

        // ✅ 분산 적용 (±20%)
        double variance = 1.0 + (_random.NextDouble() * 2 - 1) * _config.CrystalVariance;
        int finalCrystals = Math.Max(1, (int)(crystalsBeforeVariance * variance));

        // ✅ 속성 보너스 계산
        int elementBonus = (int)((elementMultiplier - 1.0) * baseCrystals);

        return new CrystalDropResult
        {
            Dropped = true,  // 항상 true
            Amount = finalCrystals,
            ElementBonus = elementBonus,
            WasGuaranteed = false  // 더 이상 의미 없음
        };
    }

    /// <summary>
    /// 몬스터(스테이지) 클리어 시 크리스털 처리
    /// 게임과 동일: 매 몬스터 처치 시 StageCompletionCrystal 지급
    /// </summary>
    public void ProcessStageClear()
    {
        _stageCompletionCrystals += _config.StageCompletionCrystal;
    }

    /// <summary>
    /// 누적된 스테이지 클리어 크리스털 반환
    /// </summary>
    public int GetStageCompletionCrystals()
    {
        return _stageCompletionCrystals;
    }

    /// <summary>
    /// 골드를 크리스털로 변환 (세션 종료 시)
    /// </summary>
    public int ConvertGoldToCrystals(int remainingGold)
    {
        return remainingGold / _config.GoldToCrystalRate;
    }

    /// <summary>
    /// 트래커 상태 리셋 (새 세션 시작)
    /// </summary>
    public void Reset()
    {
        _stageCompletionCrystals = 0;
    }
}
