using DeskWarrior.Core.Models;
using DeskWarrior.Core.Simulation;

namespace DeskWarrior.Simulator;

/// <summary>
/// 디버그용 단일 시뮬레이션 러너
/// SimulationEngine을 사용하여 실제 게임과 동일한 공식으로 시뮬레이션
/// </summary>
public static class DebugRunner
{
    public static void Run(string configPath)
    {
        Console.WriteLine("=== Debug Single Session ===\n");

        var engine = SimulatorFactory.CreateEngine(configPath, seed: 42);
        var permStats = new SimPermanentStats();
        permStats.SetConfig(engine.PermanentStatConfigs);

        var profile = new InputProfile
        {
            AverageCps = 5.0,
            CpsVariance = 0.0,
            ComboSkill = ComboSkillLevel.None,
            AutoUpgrade = true
        };

        Console.WriteLine($"Starting simulation: CPS={profile.AverageCps}\n");
        Console.WriteLine($"{"Lv.",4} | {"HP",8} | {"Dmg",5} | {"Element",-8} | {"Time",6} | {"Gold",8} | {"KbLv",4} | {"MsLv",4} | {"Result",-8}");
        Console.WriteLine(new string('-', 80));

        // 디버그 이벤트 구독
        engine.OnLevelProcessed += info =>
        {
            string typeTag = info.IsGoldenGoblin ? "GOBLIN"
                : info.IsBoss ? "BOSS"
                : "";

            string resultTag = info.IsGoldenGoblin
                ? (info.Survived ? "KILLED!" : "ESCAPED")
                : (info.Survived ? $"+{info.GoldReward}g" : "DEAD");

            Console.WriteLine(
                $"{info.Level,4} | {info.MonsterHp,8} | {info.BaseDamage,5} | {info.Element,-8} | {info.TimeElapsed,5:F1}s | {info.Gold,8} | {info.KeyboardLevel,4} | {info.MouseLevel,4} | {resultTag,-8} {typeTag}");
        };

        var result = engine.SimulateSession(permStats, profile);

        Console.WriteLine(new string('-', 80));

        if (result.OverflowDetected)
        {
            Console.WriteLine($"\n>>> OVERFLOW DETECTED at Level {result.OverflowLevel} <<<");
            Console.WriteLine($"    Location: {result.OverflowLocation}");
            Console.WriteLine($"    Computed Value: {result.OverflowValue:E4}");
            Console.WriteLine($"    long.MaxValue:  {long.MaxValue:E4}");
        }
        else
        {
            Console.WriteLine($"\n>>> GAME OVER at Level {result.MaxLevel} <<<");
        }

        Console.WriteLine($"    Reason: {result.EndReason}");
        Console.WriteLine($"    Monsters Killed: {result.MonstersKilled}");
        Console.WriteLine($"    Total Gold: {result.TotalGold:N0}");
        Console.WriteLine($"    Total Damage: {result.TotalDamage:N0}");
        Console.WriteLine($"    Critical Hits: {result.CriticalHits}");
        Console.WriteLine($"    Session Duration: {result.SessionDuration:F1}s");
        Console.WriteLine($"    Golden Goblins: Killed={result.GoldenGoblinsKilled}, Escaped={result.GoldenGoblinsEscaped}");
        Console.WriteLine($"    Final Keyboard Lv: {result.FinalKeyboardPowerLevel}");
        Console.WriteLine($"    Final Mouse Lv: {result.FinalMousePowerLevel}");
    }
}
