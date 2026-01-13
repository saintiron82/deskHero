using System;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 개별 세션(회차) 기록
    /// </summary>
    public class SessionStats
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

        [JsonPropertyName("start_time")]
        public DateTime StartTime { get; set; } = DateTime.Now;

        [JsonPropertyName("end_time")]
        public DateTime EndTime { get; set; }

        [JsonPropertyName("max_level")]
        public int MaxLevel { get; set; }

        [JsonPropertyName("total_damage")]
        public long TotalDamage { get; set; }

        [JsonPropertyName("total_gold")]
        public long TotalGold { get; set; }

        [JsonPropertyName("monsters_killed")]
        public int MonstersKilled { get; set; }

        [JsonPropertyName("bosses_killed")]
        public int BossesKilled { get; set; }

        [JsonPropertyName("keyboard_inputs")]
        public int KeyboardInputs { get; set; }

        [JsonPropertyName("mouse_inputs")]
        public int MouseInputs { get; set; }

        [JsonPropertyName("end_reason")]
        public string EndReason { get; set; } = "timeout"; // "timeout", "boss", "quit"

        /// <summary>
        /// 세션 플레이 시간 (분)
        /// </summary>
        [JsonIgnore]
        public double DurationMinutes => (EndTime - StartTime).TotalMinutes;

        /// <summary>
        /// 입력당 데미지
        /// </summary>
        [JsonIgnore]
        public double DamagePerInput
        {
            get
            {
                int totalInputs = KeyboardInputs + MouseInputs;
                return totalInputs > 0 ? (double)TotalDamage / totalInputs : 0;
            }
        }

        /// <summary>
        /// 입력당 골드
        /// </summary>
        [JsonIgnore]
        public double GoldPerInput
        {
            get
            {
                int totalInputs = KeyboardInputs + MouseInputs;
                return totalInputs > 0 ? (double)TotalGold / totalInputs : 0;
            }
        }
    }
}
