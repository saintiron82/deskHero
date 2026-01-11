using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 게임 오버 메시지 전체 데이터 루트
    /// </summary>
    public class GameOverMessageData
    {
        [JsonPropertyName("game_over_messages")]
        public GameOverMessages GameOverMessages { get; set; } = new();
    }

    /// <summary>
    /// 게임 오버 메시지 섹션 (conditions, level_based, fallback)
    /// </summary>
    public class GameOverMessages
    {
        [JsonPropertyName("conditions")]
        public List<MessageRule> Conditions { get; set; } = new();

        [JsonPropertyName("level_based")]
        public Dictionary<string, List<string>> LevelBased { get; set; } = new();

        [JsonPropertyName("fallback")]
        public List<string> Fallback { get; set; } = new();
    }

    /// <summary>
    /// 조건부 메시지 규칙
    /// </summary>
    public class MessageRule
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("condition")]
        public MessageCondition Condition { get; set; } = new();

        [JsonPropertyName("messages")]
        public List<string> Messages { get; set; } = new();
    }

    /// <summary>
    /// 메시지 선택 조건
    /// </summary>
    public class MessageCondition
    {
        [JsonPropertyName("level")]
        public int? Level { get; set; }

        [JsonPropertyName("level_min")]
        public int? LevelMin { get; set; }

        [JsonPropertyName("level_max")]
        public int? LevelMax { get; set; }

        [JsonPropertyName("death_type")]
        public string? DeathType { get; set; }
    }
}
