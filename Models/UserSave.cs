using System;
using System.Collections.Generic;
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

        [JsonPropertyName("lifetime_stats")]
        public LifetimeStats LifetimeStats { get; set; } = new();

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

        [JsonPropertyName("total_damage")]
        public long TotalDamage { get; set; }

        [JsonPropertyName("max_damage")]
        public long MaxDamage { get; set; }

        [JsonPropertyName("monster_kills")]
        public int MonsterKills { get; set; }

        [JsonPropertyName("history")]
        public List<HourlyData> History { get; set; } = new();

        [JsonPropertyName("last_updated")]
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// 통산 기록 (세션 기반 누적)
    /// </summary>
    public class LifetimeStats
    {
        [JsonPropertyName("total_gold_earned")]
        public long TotalGoldEarned { get; set; }

        [JsonPropertyName("total_gold_spent")]
        public long TotalGoldSpent { get; set; }

        [JsonPropertyName("bosses_defeated")]
        public int BossesDefeated { get; set; }

        [JsonPropertyName("critical_hits")]
        public int CriticalHits { get; set; }

        [JsonPropertyName("keyboard_inputs")]
        public long KeyboardInputs { get; set; }

        [JsonPropertyName("mouse_inputs")]
        public long MouseInputs { get; set; }

        [JsonPropertyName("total_playtime_minutes")]
        public double TotalPlaytimeMinutes { get; set; }

        [JsonPropertyName("total_sessions")]
        public int TotalSessions { get; set; }

        [JsonPropertyName("best_session_level")]
        public int BestSessionLevel { get; set; }

        [JsonPropertyName("best_session_damage")]
        public long BestSessionDamage { get; set; }

        [JsonPropertyName("best_session_kills")]
        public int BestSessionKills { get; set; }

        [JsonPropertyName("best_session_gold")]
        public long BestSessionGold { get; set; }

        [JsonPropertyName("consecutive_days")]
        public int ConsecutiveDays { get; set; }

        [JsonPropertyName("last_play_date")]
        public string LastPlayDate { get; set; } = "";
    }

    public class HourlyData
    {
        [JsonPropertyName("damage")]
        public long Damage { get; set; }

        [JsonPropertyName("kills")]
        public int Kills { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime TimeStamp { get; set; }
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
        public double Volume { get; set; } = 0.2;

        [JsonPropertyName("background_opacity")]
        public double BackgroundOpacity { get; set; } = 0.4;

        [JsonPropertyName("window_opacity")]
        public double WindowOpacity { get; set; } = 1.0;

        [JsonPropertyName("auto_restart")]
        public bool AutoRestart { get; set; } = false;

        [JsonPropertyName("language")]
        public string Language { get; set; } = "";  // Empty = auto-detect
    }
}
