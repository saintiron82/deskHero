using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 영구 화폐 (크리스탈)
    /// </summary>
    public class PermanentCurrency
    {
        [JsonPropertyName("crystals")]
        public long Crystals { get; set; } = 0;

        [JsonPropertyName("lifetime_crystals_earned")]
        public long LifetimeCrystalsEarned { get; set; } = 0;

        [JsonPropertyName("lifetime_crystals_spent")]
        public long LifetimeCrystalsSpent { get; set; } = 0;

        [JsonIgnore]
        public long NetCrystals => Crystals;
    }
}
