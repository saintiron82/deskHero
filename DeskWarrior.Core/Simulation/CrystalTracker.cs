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
    private long _stageCompletionCrystals;  // 매 몬스터 처치 시 누적

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
    public CrystalDropResult ProcessBossKill(long bossLevel, string bossElement, int crystalFlat, Dictionary<string, double> crystalMultipliers)
    {
        // ✅ 기본 크리스탈 계산 (100% 지급)
        double growth;
        if (_config.CrystalGrowthBreakpoint > 0 && bossLevel > _config.CrystalGrowthBreakpoint)
        {
            double tail = Math.Pow(bossLevel - _config.CrystalGrowthBreakpoint, _config.CrystalGrowthExponent);
            growth = _config.CrystalGrowthBreakpoint + tail;
        }
        else
        {
            growth = bossLevel;
        }
        long baseCrystals = OverflowGuard.ToLong(
            Math.Round(_config.BaseCrystalAmount + _config.CrystalPerLevel * growth),
            "Crystal.BossBase", bossLevel);
        baseCrystals = OverflowGuard.Add(baseCrystals, crystalFlat, "Crystal.BossFlat", bossLevel);

        // ✅ 속성별 배율 적용
        if (!crystalMultipliers.TryGetValue(bossElement, out double elementMultiplier))
        {
            // 시뮬레이터에서는 누락 시 경고 (게임에선 Logger 사용)
            System.Diagnostics.Debug.WriteLine($"[Warning] Crystal multiplier not found for element '{bossElement}', using 1.0");
            elementMultiplier = 1.0;
        }
        long finalCrystals = Math.Max(1, OverflowGuard.ToLong(
            baseCrystals * elementMultiplier, "Crystal.BossElement", bossLevel));

        // ✅ 분산 제거 - 모든 보상은 고정값 (황금 고블린 제외)
        // 이전: variance ±20% 적용 (제거됨)

        // ✅ 속성 보너스 계산
        long elementBonus = OverflowGuard.ToLong(
            (elementMultiplier - 1.0) * baseCrystals, "Crystal.ElementBonus", bossLevel);

        return new CrystalDropResult
        {
            Dropped = true,  // 항상 true
            Amount = finalCrystals,
            ElementBonus = elementBonus,
            WasGuaranteed = false  // 더 이상 의미 없음
        };
    }

    /// <summary>
    /// 몬스터 처치 크리스탈 (100레벨마다 +1)
    /// 게임과 동일: 매 몬스터 처치 시 레벨 기반 크리스탈 지급
    /// </summary>
    public void ProcessStageClear(long currentLevel)
    {
        long crystalAmount = _config.StageCompletionCrystal + (currentLevel / 100);
        _stageCompletionCrystals = OverflowGuard.Add(
            _stageCompletionCrystals, crystalAmount, "Crystal.StageAccum", currentLevel);
    }

    /// <summary>
    /// 누적된 스테이지 클리어 크리스털 반환
    /// </summary>
    public long GetStageCompletionCrystals()
    {
        return _stageCompletionCrystals;
    }

    /// <summary>
    /// 골드를 크리스털로 변환 (세션 종료 시)
    /// </summary>
    public long ConvertGoldToCrystals(long remainingGold)
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
