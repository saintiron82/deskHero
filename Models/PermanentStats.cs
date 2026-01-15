using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 영구 능력치 (메타 진행도)
    /// </summary>
    public class PermanentStats
    {
        // 기본 스탯
        [JsonPropertyName("base_attack")]
        public int BaseAttack { get; set; } = 0;

        [JsonPropertyName("base_defense")]
        public int BaseDefense { get; set; } = 0;

        [JsonPropertyName("base_health")]
        public int BaseHealth { get; set; } = 0;

        // 퍼센트 보너스
        [JsonPropertyName("attack_percent_bonus")]
        public double AttackPercentBonus { get; set; } = 0.0; // 0.10 = 10%

        [JsonPropertyName("gold_percent_bonus")]
        public double GoldPercentBonus { get; set; } = 0.0;

        [JsonPropertyName("damage_percent_bonus")]
        public double DamagePercentBonus { get; set; } = 0.0;

        [JsonPropertyName("xp_percent_bonus")]
        public double XpPercentBonus { get; set; } = 0.0;

        // 특수 능력
        [JsonPropertyName("critical_chance_bonus")]
        public double CriticalChanceBonus { get; set; } = 0.0; // 기본 크리티컬 확률에 더해짐

        [JsonPropertyName("critical_damage_bonus")]
        public double CriticalDamageBonus { get; set; } = 0.0; // 크리티컬 배율에 더해짐

        [JsonPropertyName("multi_hit_chance")]
        public double MultiHitChance { get; set; } = 0.0; // 2회 타격 확률

        [JsonPropertyName("auto_combat_enabled")]
        public bool AutoCombatEnabled { get; set; } = false;

        [JsonPropertyName("auto_combat_dps")]
        public int AutoCombatDps { get; set; } = 0; // 초당 데미지

        // 시작 보너스
        [JsonPropertyName("starting_level_bonus")]
        public int StartingLevelBonus { get; set; } = 0; // 시작 레벨 보너스

        [JsonPropertyName("starting_gold_bonus")]
        public int StartingGoldBonus { get; set; } = 0;

        [JsonPropertyName("starting_keyboard_power")]
        public int StartingKeyboardPower { get; set; } = 0;

        [JsonPropertyName("starting_mouse_power")]
        public int StartingMousePower { get; set; } = 0;

        // 게임 오버 타이머 연장
        [JsonPropertyName("game_over_time_extension")]
        public int GameOverTimeExtension { get; set; } = 0; // 초 단위

        // 유틸리티
        [JsonPropertyName("upgrade_cost_reduction")]
        public double UpgradeCostReduction { get; set; } = 0.0; // 0.20 = 20% 할인
    }
}
