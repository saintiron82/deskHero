using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 사용자 저장 데이터 모델
    /// </summary>
    public class UserSave
    {
        [JsonPropertyName("window_position")]
        public WindowPosition Position { get; set; } = new();

        [JsonPropertyName("stats")]
        public UserStats Stats { get; set; } = new();

        [JsonPropertyName("upgrades")]
        public UpgradeData Upgrades { get; set; } = new();

        [JsonPropertyName("settings")]
        public UserSettings Settings { get; set; } = new();
    }

    public class WindowPosition
    {
        [JsonPropertyName("x")]
        public double X { get; set; } = 100;

        [JsonPropertyName("y")]
        public double Y { get; set; } = 100;
    }

    public class UserStats
    {
        [JsonPropertyName("max_level")]
        public int MaxLevel { get; set; }

        [JsonPropertyName("total_inputs")]
        public long TotalInputs { get; set; }

        [JsonPropertyName("today_inputs")]
        public int TodayInputs { get; set; }

        [JsonPropertyName("last_played")]
        public string LastPlayed { get; set; } = "";
    }

    public class UpgradeData
    {
        [JsonPropertyName("keyboard_power")]
        public int KeyboardPower { get; set; } = 1;

        [JsonPropertyName("mouse_power")]
        public int MousePower { get; set; } = 1;
    }

    public class UserSettings
    {
        [JsonPropertyName("sound_enabled")]
        public bool SoundEnabled { get; set; } = true;

        [JsonPropertyName("volume")]
        public double Volume { get; set; } = 0.5;
    }
}
