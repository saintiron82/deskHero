using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 게임 밸런스 설정 데이터 (GameData.json)
    /// </summary>
    public class GameData
    {
        [JsonPropertyName("balance")]
        public BalanceData Balance { get; set; } = new();

        [JsonPropertyName("upgrade")]
        public UpgradeConfig Upgrade { get; set; } = new();

        [JsonPropertyName("visual")]
        public VisualConfig Visual { get; set; } = new();

        /// <summary>
        /// JSON 파일에서 로드
        /// </summary>
        public static GameData LoadFromFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<GameData>(json) ?? new GameData();
                }
            }
            catch (Exception ex)
            {
                DeskWarrior.Helpers.Logger.LogError($"Failed to load GameData from {path}", ex);
            }
            return new GameData();
        }
    }

    public class BalanceData
    {
        [JsonPropertyName("base_hp")]
        public int BaseHp { get; set; } = 100;

        [JsonPropertyName("hp_growth")]
        public double HpGrowth { get; set; } = 1.2;

        [JsonPropertyName("boss_interval")]
        public int BossInterval { get; set; } = 10;

        [JsonPropertyName("boss_hp_multiplier")]
        public double BossHpMultiplier { get; set; } = 3.0;

        [JsonPropertyName("time_limit")]
        public int TimeLimit { get; set; } = 30;

        [JsonPropertyName("base_gold_multiplier")]
        public int BaseGoldMultiplier { get; set; } = 1;

        [JsonPropertyName("critical_chance")]
        public double CriticalChance { get; set; } = 0.1;

        [JsonPropertyName("critical_multiplier")]
        public double CriticalMultiplier { get; set; } = 2.0;

        [JsonPropertyName("upgrade_cost_interval")]
        public int UpgradeCostInterval { get; set; } = 50;  // 50스테이지마다 비용 2배

        [JsonPropertyName("game_over_messages")]
        public List<string> GameOverMessages { get; set; } = new();
    }

    public class UpgradeConfig
    {
        [JsonPropertyName("base_cost")]
        public int BaseCost { get; set; } = 100;

        [JsonPropertyName("cost_multiplier")]
        public double CostMultiplier { get; set; } = 1.5;

        [JsonPropertyName("attack_increase")]
        public double AttackIncrease { get; set; } = 0.5;
    }

    public class VisualConfig
    {
        [JsonPropertyName("shake_power")]
        public double ShakePower { get; set; } = 2.5;

        [JsonPropertyName("boss_scale")]
        public double BossScale { get; set; } = 1.5;
    }
}
