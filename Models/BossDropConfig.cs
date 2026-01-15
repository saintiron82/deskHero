using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 보스 드랍 설정
    /// </summary>
    public class BossDropConfig
    {
        [JsonPropertyName("base_drop_chance")]
        public double BaseDropChance { get; set; } = 0.5; // 50% chance

        [JsonPropertyName("drop_chance_per_level")]
        public double DropChancePerLevel { get; set; } = 0.005; // +0.5% per level

        [JsonPropertyName("max_drop_chance")]
        public double MaxDropChance { get; set; } = 0.95; // Cap at 95%

        [JsonPropertyName("base_crystal_amount")]
        public int BaseCrystalAmount { get; set; } = 5;

        [JsonPropertyName("crystal_per_level")]
        public int CrystalPerLevel { get; set; } = 1;

        [JsonPropertyName("crystal_variance")]
        public double CrystalVariance { get; set; } = 0.2; // ±20% randomness

        [JsonPropertyName("guaranteed_drop_every_n_bosses")]
        public int GuaranteedDropInterval { get; set; } = 10; // Pity system
    }

    /// <summary>
    /// 보스 드랍 결과
    /// </summary>
    public class BossDropResult
    {
        public bool Dropped { get; set; }
        public int CrystalsDropped { get; set; }
        public bool WasGuaranteed { get; set; }
    }
}
