using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 속성별 특성 데이터 (JSON에서 로드)
    /// </summary>
    public class ElementProperties
    {
        [JsonPropertyName("hp_modifier")]
        public double HpModifier { get; set; } = 1.0;

        [JsonPropertyName("time_scale")]
        public double TimeScale { get; set; } = 1.0;

        [JsonPropertyName("keyboard_resistance")]
        public double KeyboardResistance { get; set; } = 1.0;

        [JsonPropertyName("mouse_resistance")]
        public double MouseResistance { get; set; } = 1.0;

        [JsonPropertyName("crystal_multiplier")]
        public double CrystalMultiplier { get; set; } = 1.0;
    }

    /// <summary>
    /// 몬스터 스폰 설정 데이터 (완전한 테이블 기반 - 모든 값은 JSON에서 로드)
    /// </summary>
    public class MonsterSpawningConfig
    {
        [JsonPropertyName("element_weights")]
        public Dictionary<string, int> ElementWeights { get; set; } = new();

        [JsonPropertyName("use_weighted_selection")]
        public bool UseWeightedSelection { get; set; } = true;

        [JsonPropertyName("use_batch_progression")]
        public bool UseBatchProgression { get; set; } = true;
    }

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

        [JsonPropertyName("monster_spawning")]
        public MonsterSpawningConfig MonsterSpawning { get; set; } = new();

        [JsonPropertyName("element_properties")]
        public Dictionary<string, ElementProperties> ElementProperties { get; set; } = new();

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

        [JsonPropertyName("tier_hp_system")]
        public TierHpSystemConfig TierHpSystem { get; set; } = new();

        [JsonPropertyName("game_over_messages")]
        public List<string> GameOverMessages { get; set; } = new();
    }

    /// <summary>
    /// 티어 기반 HP 시스템 설정
    /// </summary>
    public class TierHpSystemConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonPropertyName("tier_interval")]
        public int TierInterval { get; set; } = 100;

        [JsonPropertyName("tier_multiplier")]
        public double TierMultiplier { get; set; } = 5.0;

        [JsonPropertyName("linear_growth_per_level")]
        public int LinearGrowthPerLevel { get; set; } = 5;
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
