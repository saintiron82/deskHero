using DeskWarrior.Core.Models;

namespace DeskWarrior.Simulator;

/// <summary>
/// 세션 결과를 CSV 파일로 출력
/// </summary>
public static class CsvExporter
{
    /// <summary>
    /// 세션 결과 목록을 CSV 파일로 저장
    /// </summary>
    public static void ExportSessions(List<SessionResult> sessions, string outputPath)
    {
        using var writer = new StreamWriter(outputPath);

        // 헤더
        writer.WriteLine(
            "SessionNum,Playtime,Duration,Level,Gold,SpentGold,TotalDamage," +
            "Monsters,Bosses,Inputs,Crits," +
            "KbPower,MsPower," +
            "CrystalsStage,CrystalsBoss,CrystalsGold,CrystalsTotal," +
            "CrystalsSpent,CrystalsRemain," +
            "GGKilled,GGEscaped,GGGold," +
            "BaseAttackLv,AttackPctLv,CritChanceLv,CritDamageLv,MultiHitLv," +
            "TimeExtendLv,UpgradeDiscountLv,CostFlatReductionLv,CrystalDiscountLv,CrystalFlatReductionLv," +
            "GoldFlatPermLv,GoldMultiPermLv,CrystalFlatLv"
        );

        // 데이터
        foreach (var s in sessions)
        {
            writer.WriteLine(
                $"{s.SessionNumber}," +
                $"{s.TotalPlaytime:F1}," +
                $"{s.SessionDuration:F1}," +
                $"{s.MaxLevel}," +
                $"{s.TotalGold}," +
                $"{s.SpentGold}," +
                $"{s.TotalDamage}," +
                $"{s.MonstersKilled}," +
                $"{s.BossesKilled}," +
                $"{s.TotalInputs}," +
                $"{s.CriticalHits}," +
                $"{s.FinalKeyboardPowerLevel}," +
                $"{s.FinalMousePowerLevel}," +
                $"{s.CrystalsFromStages}," +
                $"{s.CrystalsFromBosses}," +
                $"{s.CrystalsFromGoldConvert}," +
                $"{s.TotalCrystals}," +
                $"{s.SpentCrystals}," +
                $"{s.RemainingCrystals}," +
                $"{s.GoldenGoblinsKilled}," +
                $"{s.GoldenGoblinsEscaped}," +
                $"{s.GoldenGoblinGoldEarned}," +
                $"{s.PermanentStatLevels.GetValueOrDefault("base_attack", 0)}," +
                $"{s.PermanentStatLevels.GetValueOrDefault("attack_percent", 0)}," +
                $"{s.PermanentStatLevels.GetValueOrDefault("crit_chance", 0)}," +
                $"{s.PermanentStatLevels.GetValueOrDefault("crit_damage", 0)}," +
                $"{s.PermanentStatLevels.GetValueOrDefault("multi_hit", 0)}," +
                $"{s.PermanentStatLevels.GetValueOrDefault("time_extend", 0)}," +
                $"{s.PermanentStatLevels.GetValueOrDefault("upgrade_discount", 0)}," +
                $"{s.PermanentStatLevels.GetValueOrDefault("cost_flat_reduction", 0)}," +
                $"{s.PermanentStatLevels.GetValueOrDefault("crystal_discount", 0)}," +
                $"{s.PermanentStatLevels.GetValueOrDefault("crystal_flat_reduction", 0)}," +
                $"{s.PermanentStatLevels.GetValueOrDefault("gold_flat_perm", 0)}," +
                $"{s.PermanentStatLevels.GetValueOrDefault("gold_multi_perm", 0)}," +
                $"{s.PermanentStatLevels.GetValueOrDefault("crystal_flat", 0)}"
            );
        }

        Console.WriteLine($"✅ Exported {sessions.Count} sessions to {outputPath}");
    }
}
