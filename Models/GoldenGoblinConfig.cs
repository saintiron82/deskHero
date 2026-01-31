using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 황금 고블린 설정
    /// </summary>
    public class GoldenGoblinConfig
    {
        /// <summary>
        /// 몬스터 ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "special_golden_goblin";

        /// <summary>
        /// 등장 확률 (0.00001 = 0.001%)
        /// </summary>
        [JsonPropertyName("spawn_chance")]
        public double SpawnChance { get; set; } = 0.001;

        /// <summary>
        /// 재등장까지 필요한 처치 수
        /// </summary>
        [JsonPropertyName("cooldown_kills")]
        public int CooldownKills { get; set; } = 500;

        /// <summary>
        /// 고정 HP (HpMin/HpMax가 설정되면 무시됨)
        /// </summary>
        [JsonPropertyName("hp")]
        public int Hp { get; set; } = 150;

        /// <summary>
        /// 최소 HP (랜덤 범위)
        /// </summary>
        [JsonPropertyName("hp_min")]
        public int HpMin { get; set; } = 100;

        /// <summary>
        /// 최대 HP (랜덤 범위)
        /// </summary>
        [JsonPropertyName("hp_max")]
        public int HpMax { get; set; } = 200;

        /// <summary>
        /// 제한 시간 (초)
        /// </summary>
        [JsonPropertyName("time_limit")]
        public int TimeLimit { get; set; } = 10;

        /// <summary>
        /// 최소 보상 배수
        /// </summary>
        [JsonPropertyName("reward_min")]
        public int RewardMultiplierMin { get; set; } = 2;

        /// <summary>
        /// 최대 보상 배수
        /// </summary>
        [JsonPropertyName("reward_max")]
        public int RewardMultiplierMax { get; set; } = 100;

        /// <summary>
        /// 스프라이트 경로
        /// </summary>
        [JsonPropertyName("sprite")]
        public string Sprite { get; set; } = "Production/monster_goblin.png";

        /// <summary>
        /// 표시 이모지
        /// </summary>
        [JsonPropertyName("emoji")]
        public string Emoji { get; set; } = "💰";

        /// <summary>
        /// 다국어 이름
        /// </summary>
        [JsonPropertyName("name")]
        public Dictionary<string, string> Name { get; set; } = new()
        {
            ["ko-KR"] = "황금 고블린",
            ["en-US"] = "Golden Goblin"
        };
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
