using DeskWarrior.Core.Models;
using DeskWarrior.Core.Simulation;

namespace DeskWarrior.Simulator;

/// <summary>
/// 디버그용 단일 시뮬레이션 러너
/// </summary>
public static class DebugRunner
{
    public static void Run(string configPath)
    {
        Console.WriteLine("=== Debug Single Session ===\n");

        var engine = SimulatorFactory.CreateEngine(configPath, seed: 42);
        var permStats = new SimPermanentStats();
        var profile = new InputProfile
        {
            AverageCps = 5.0,
            CpsVariance = 0.0,  // 변동 없음
            ComboSkill = ComboSkillLevel.None,
            AutoUpgrade = true
        };

        // 수동 시뮬레이션으로 첫 10 레벨 추적
        var inGameStats = new SimInGameStats();
        int currentLevel = 1;
        int gold = 0;
        double timeLimit = 30.0;

        Console.WriteLine($"Starting simulation: CPS={profile.AverageCps}, TimeLimit={timeLimit}s\n");

        for (int lvl = 1; lvl <= 200; lvl++)
        {
            bool isBoss = lvl % 10 == 0;
            int monsterHp = 20 + (lvl - 1) * 5;
            if (isBoss) monsterHp = (int)(monsterHp * 5.0);

            int damage = 1 + inGameStats.KeyboardPowerLevel;  // 기본 데미지
            int hitsNeeded = (int)Math.Ceiling((double)monsterHp / damage);
            double timeNeeded = hitsNeeded / profile.AverageCps;

            int monsterGold = 10 + lvl * 2;
            int kbCost = CalculateCost(10, 0.5, 1.5, 10, inGameStats.KeyboardPowerLevel + 1);

            Console.WriteLine($"Lv.{lvl,3} | HP={monsterHp,5} | Dmg={damage,3} | Hits={hitsNeeded,4} | Time={timeNeeded,5:F1}s | Gold={gold,6} -> +{monsterGold} | KbLv={inGameStats.KeyboardPowerLevel} (cost={kbCost})");

            if (timeNeeded > timeLimit)
            {
                Console.WriteLine($"\n>>> GAME OVER at Level {lvl} <<<");
                break;
            }

            gold += monsterGold;

            // 자동 업그레이드
            while (gold >= CalculateCost(10, 0.5, 1.5, 10, inGameStats.KeyboardPowerLevel + 1))
            {
                int cost = CalculateCost(10, 0.5, 1.5, 10, inGameStats.KeyboardPowerLevel + 1);
                gold -= cost;
                inGameStats.KeyboardPowerLevel++;
                Console.WriteLine($"        >>> Upgraded Keyboard to Lv.{inGameStats.KeyboardPowerLevel} (spent {cost} gold, remaining {gold})");
            }
        }
    }

    private static int CalculateCost(double baseCost, double growthRate, double multiplier, int softcapInterval, int level)
    {
        if (level <= 0) return 0;
        double linearFactor = 1.0 + level * growthRate;
        double exponentialFactor = Math.Pow(multiplier, (double)level / softcapInterval);
        return (int)Math.Ceiling(baseCost * linearFactor * exponentialFactor);
    }
}
