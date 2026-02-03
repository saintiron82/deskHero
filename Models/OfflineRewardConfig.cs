using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 시간당 보상 설정
    /// </summary>
    public class RewardsPerHour
    {
        [JsonPropertyName("gold")]
        public int Gold { get; set; } = 100;

        [JsonPropertyName("crystals")]
        public int Crystals { get; set; } = 1;
    }

    /// <summary>
    /// 오프라인 보상 설정 (config/OfflineRewards.json)
    /// </summary>
    public class OfflineRewardConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("max_hours")]
        public int MaxHours { get; set; } = 8;

        [JsonPropertyName("rewards_per_hour")]
        public RewardsPerHour RewardsPerHour { get; set; } = new();

        [JsonPropertyName("min_offline_minutes")]
        public int MinOfflineMinutes { get; set; } = 30;

        [JsonPropertyName("popup_message")]
        public Dictionary<string, string> PopupMessage { get; set; } = new()
        {
            { "ko-KR", "돌아오셨군요! {hours}시간 동안 {gold} 골드와 {crystals} 크리스탈을 획득했습니다!" },
            { "en-US", "Welcome back! You earned {gold} gold and {crystals} crystals during {hours} hours offline!" }
        };
    }

    /// <summary>
    /// 오프라인 보상 계산 결과
    /// </summary>
    public struct OfflineRewardResult
    {
        public long Gold { get; init; }
        public int Crystals { get; init; }
        public double OfflineHours { get; init; }
        public bool HasReward => Gold > 0 || Crystals > 0;
        public static OfflineRewardResult None => default;
    }
}
