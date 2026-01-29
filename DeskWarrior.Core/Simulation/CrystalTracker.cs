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
    private int _bossKillCounter;

    public CrystalTracker(BossDropConfig config, Random random)
    {
        _config = config;
        _random = random;
        _bossKillCounter = 0;
    }

    /// <summary>
    /// 보스 처치 시 크리스털 드롭 처리
    /// </summary>
    /// <param name="bossLevel">보스 레벨</param>
    /// <param name="crystalFlat">영구 스탯: 크리스털 추가량</param>
    /// <param name="crystalDropChanceBonus">영구 스탯: 드롭 확률 보너스</param>
    public CrystalDropResult ProcessBossKill(int bossLevel, int crystalFlat, double crystalDropChanceBonus)
    {
        _bossKillCounter++;

        // Pity 시스템: N보스마다 확정 드롭
        bool isGuaranteed = _bossKillCounter >= _config.GuaranteedDropEveryNBosses;

        // 드롭 확률 계산
        double baseChance = _config.BaseDropChance + bossLevel * _config.DropChancePerLevel;
        double totalChance = Math.Min(baseChance + crystalDropChanceBonus, _config.MaxDropChance);

        bool dropped = isGuaranteed || _random.NextDouble() < totalChance;

        if (dropped)
        {
            _bossKillCounter = 0;  // Pity 카운터 리셋

            // 드롭량 계산 (±20% 랜덤)
            int baseCrystals = _config.BaseCrystalAmount + bossLevel * _config.CrystalPerLevel;
            baseCrystals += crystalFlat;  // 영구 스탯 보너스

            double variance = 1.0 + (_random.NextDouble() * 2 - 1) * _config.CrystalVariance;
            int crystals = Math.Max(1, (int)(baseCrystals * variance));

            return new CrystalDropResult
            {
                Dropped = true,
                Amount = crystals,
                WasGuaranteed = isGuaranteed
            };
        }

        return new CrystalDropResult { Dropped = false, Amount = 0, WasGuaranteed = false };
    }

    /// <summary>
    /// 스테이지 클리어 시 크리스털 보너스 (게임오버 시 1회)
    /// </summary>
    public int GetStageCompletionCrystals()
    {
        return _config.StageCompletionCrystal;
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
        _bossKillCounter = 0;
    }
}
