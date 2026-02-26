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
    /// 긴급 폴백 설정 (몬스터 데이터 로드 실패 시 사용)
    /// </summary>
    public class EmergencyFallbackConfig
    {
        [JsonPropertyName("base_gold")]
        public int BaseGold { get; set; }

        [JsonPropertyName("gold_per_level")]
        public int GoldPerLevel { get; set; }
    }

    /// <summary>
    /// 콤보 시스템 설정 (config/GameData.json에서 로드)
    /// </summary>
    public class ComboConfig
    {
        [JsonPropertyName("base_tolerance")]
        public double BaseTolerance { get; set; }

        [JsonPropertyName("expire_time")]
        public double ExpireTime { get; set; }

        [JsonPropertyName("max_stack")]
        public int MaxStack { get; set; }
    }

    /// <summary>
    /// 연속 동일키 입력 페널티 설정
    /// </summary>
    public class ConsecutiveKeyPenaltyConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("penalty_start_count")]
        public int PenaltyStartCount { get; set; } = 7;

        [JsonPropertyName("penalty_per_count")]
        public double PenaltyPerCount { get; set; } = 0.1;

        [JsonPropertyName("combo_exempt")]
        public bool ComboExempt { get; set; } = false;

        [JsonPropertyName("mouse_exempt")]
        public bool MouseExempt { get; set; }

        [JsonPropertyName("max_cps")]
        public int MaxCps { get; set; }
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

        [JsonPropertyName("combo")]
        public ComboConfig Combo { get; set; } = new();

        [JsonPropertyName("emergency_fallback")]
        public EmergencyFallbackConfig EmergencyFallback { get; set; } = new();

        [JsonPropertyName("consecutive_key_penalty")]
        public ConsecutiveKeyPenaltyConfig ConsecutiveKeyPenalty { get; set; } = new();

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
        public double BaseGoldMultiplier { get; set; } = 1;

        [JsonPropertyName("critical_chance")]
        public double CriticalChance { get; set; } = 0.1;

        [JsonPropertyName("critical_multiplier")]
        public double CriticalMultiplier { get; set; } = 2.0;

        [JsonPropertyName("upgrade_cost_interval")]
        public int UpgradeCostInterval { get; set; } = 50;  // 50스테이지마다 비용 2배

        [JsonPropertyName("upgrade_cost_tier_multiplier")]
        public double UpgradeCostTierMultiplier { get; set; } = 2.0;

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
        public int TierInterval { get; set; } = 1000;

        [JsonPropertyName("tier_multiplier")]
        public double TierMultiplier { get; set; } = 1.0;

        [JsonPropertyName("tier_multiplier_decay_per_tier")]
        public double TierMultiplierDecayPerTier { get; set; } = 1.0;

        [JsonPropertyName("min_tier_multiplier")]
        public double MinTierMultiplier { get; set; } = 0.0;

        [JsonPropertyName("tier_curve_exponent")]
        public double TierCurveExponent { get; set; } = 1.0;

        [JsonPropertyName("linear_growth_per_level")]
        public int LinearGrowthPerLevel { get; set; } = 5;

        [JsonPropertyName("min_linear_growth_per_level")]
        public double MinLinearGrowthPerLevel { get; set; } = 0.0;

        [JsonPropertyName("growth_decrease_per_tier")]
        public double GrowthDecreasePerTier { get; set; } = 0.85;

        [JsonPropertyName("late_start_level")]
        public int LateStartLevel { get; set; } = 0;

        [JsonPropertyName("late_tier_interval")]
        public int LateTierInterval { get; set; } = 0;

        [JsonPropertyName("late_tier_multiplier")]
        public double LateTierMultiplier { get; set; } = 1.0;

        [JsonPropertyName("max_late_tiers")]
        public int MaxLateTiers { get; set; } = 0;
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
