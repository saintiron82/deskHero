using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 도감 보상 설정 데이터 (CollectionRewards.json에서 로드)
    /// </summary>
    public class CollectionRewards
    {
        [JsonPropertyName("species_completion")]
        public Dictionary<string, SpeciesReward> SpeciesCompletion { get; set; } = new();

        [JsonPropertyName("batch_completion")]
        public Dictionary<string, BatchReward> BatchCompletion { get; set; } = new();

        [JsonPropertyName("milestone_rewards")]
        public Dictionary<string, MilestoneReward> MilestoneRewards { get; set; } = new();

        /// <summary>
        /// JSON 파일에서 로드
        /// </summary>
        public static CollectionRewards LoadFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<CollectionRewards>(json)
                        ?? new CollectionRewards();
                }
            }
            catch (Exception ex)
            {
                DeskWarrior.Helpers.Logger.LogError($"Failed to load CollectionRewards from {filePath}", ex);
            }
            return new CollectionRewards();
        }
    }

    /// <summary>
    /// 종족 도감 완성 보상
    /// </summary>
    public class SpeciesReward
    {
        [JsonPropertyName("required_variations")]
        public int RequiredVariations { get; set; } = 6;

        [JsonPropertyName("rewards")]
        public RewardData Rewards { get; set; } = new();
    }

    /// <summary>
    /// 배치 도감 완성 보상
    /// </summary>
    public class BatchReward
    {
        [JsonPropertyName("required_total")]
        public int RequiredTotal { get; set; }

        [JsonPropertyName("rewards")]
        public RewardData Rewards { get; set; } = new();
    }

    /// <summary>
    /// 마일스톤 보상
    /// </summary>
    public class MilestoneReward
    {
        [JsonPropertyName("description")]
        public Dictionary<string, string> Description { get; set; } = new();

        [JsonPropertyName("crystals")]
        public int Crystals { get; set; }

        [JsonPropertyName("title")]
        public Dictionary<string, string>? Title { get; set; }
    }

    /// <summary>
    /// 보상 데이터
    /// </summary>
    public class RewardData
    {
        [JsonPropertyName("crystals")]
        public int Crystals { get; set; }

        [JsonPropertyName("title")]
        public Dictionary<string, string> Title { get; set; } = new();

        [JsonPropertyName("permanent_bonus")]
        public PermanentBonus? PermanentBonus { get; set; }
    }

    /// <summary>
    /// 영구 보너스
    /// </summary>
    public class PermanentBonus
    {
        /// <summary>
        /// 보너스 타입: species_gold, global_gold
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        /// <summary>
        /// 대상 (species_gold인 경우 종족 이름)
        /// </summary>
        [JsonPropertyName("target")]
        public string? Target { get; set; }

        /// <summary>
        /// 보너스 값 (퍼센트)
        /// </summary>
        [JsonPropertyName("value")]
        public double Value { get; set; }
    }
}
