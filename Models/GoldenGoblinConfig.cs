using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 황금 고블린 설정
    /// </summary>
    public class GoldenGoblinConfig
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("spawn_chance")]
        public double SpawnChance { get; set; }

        [JsonPropertyName("cooldown_kills")]
        public int CooldownKills { get; set; }

        [JsonPropertyName("hp")]
        public int Hp { get; set; }

        [JsonPropertyName("hp_min")]
        public int HpMin { get; set; }

        [JsonPropertyName("hp_max")]
        public int HpMax { get; set; }

        [JsonPropertyName("time_limit")]
        public int TimeLimit { get; set; }

        [JsonPropertyName("reward_min")]
        public int RewardMultiplierMin { get; set; }

        [JsonPropertyName("reward_max")]
        public int RewardMultiplierMax { get; set; }

        [JsonPropertyName("sprite")]
        public string Sprite { get; set; } = "";

        [JsonPropertyName("emoji")]
        public string Emoji { get; set; } = "";

        [JsonPropertyName("name")]
        public Dictionary<string, string> Name { get; set; } = new();

        [JsonPropertyName("grades")]
        public List<GoldenGoblinGrade> Grades { get; set; } = new();
    }

    /// <summary>
    /// 황금 고블린 등급 설정 (config/SpecialMonsters.json에서 로드)
    /// </summary>
    public class GoldenGoblinGrade
    {
        [JsonPropertyName("grade_id")]
        public string GradeId { get; set; } = "";

        [JsonPropertyName("reward_min")]
        public int RewardMin { get; set; }

        [JsonPropertyName("reward_max")]
        public int RewardMax { get; set; }

        [JsonPropertyName("hp_min")]
        public int HpMin { get; set; }

        [JsonPropertyName("hp_max")]
        public int HpMax { get; set; }

        [JsonPropertyName("time_limit")]
        public int TimeLimit { get; set; }

        [JsonPropertyName("sprite")]
        public string Sprite { get; set; } = "";

        [JsonPropertyName("sprite_hue_shift")]
        public int SpriteHueShift { get; set; }

        [JsonPropertyName("background")]
        public string Background { get; set; } = "";

        [JsonPropertyName("border_color")]
        public string BorderColor { get; set; } = "";

        [JsonPropertyName("name_color")]
        public string NameColor { get; set; } = "";

        [JsonPropertyName("effect")]
        public string Effect { get; set; } = "";

        [JsonPropertyName("name")]
        public Dictionary<string, string> Name { get; set; } = new();
    }

    /// <summary>
    /// 특수 몬스터 설정 파일 루트
    /// </summary>
    public class SpecialMonstersConfig
    {
        [JsonPropertyName("golden_goblin")]
        public GoldenGoblinConfig GoldenGoblin { get; set; } = new();
    }
}
