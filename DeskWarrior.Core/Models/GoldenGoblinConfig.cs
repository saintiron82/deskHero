using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskWarrior.Core.Models;

/// <summary>
/// 황금 고블린 설정 (시뮬레이터용)
/// </summary>
public class SimGoldenGoblinConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "special_golden_goblin";

    [JsonPropertyName("spawn_chance")]
    public double SpawnChance { get; set; } = 0.001; // 0.1%

    [JsonPropertyName("cooldown_kills")]
    public int CooldownKills { get; set; } = 500;

    [JsonPropertyName("hp")]
    public int Hp { get; set; } = 150;

    [JsonPropertyName("time_limit")]
    public int TimeLimit { get; set; } = 10;

    [JsonPropertyName("reward_min")]
    public int RewardMultiplierMin { get; set; } = 2;

    [JsonPropertyName("reward_max")]
    public int RewardMultiplierMax { get; set; } = 100;

    [JsonPropertyName("emoji")]
    public string Emoji { get; set; } = "💰";
}

/// <summary>
/// 특수 몬스터 설정 파일 루트
/// </summary>
public class SpecialMonstersConfig
{
    [JsonPropertyName("golden_goblin")]
    public SimGoldenGoblinConfig GoldenGoblin { get; set; } = new();

    public static SpecialMonstersConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new SpecialMonstersConfig();
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<SpecialMonstersConfig>(json) ?? new SpecialMonstersConfig();
    }
}
