using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 사운드 테마 데이터 (config/SoundThemes.json)
    /// sounds 맵: SoundType 이름 → 볼륨(0.0~1.0). 없는 항목 = 꺼짐.
    /// </summary>
    public class SoundThemeData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name_key")]
        public string NameKey { get; set; } = "";

        [JsonPropertyName("is_builtin")]
        public bool IsBuiltin { get; set; }

        [JsonPropertyName("sounds")]
        public Dictionary<string, double> Sounds { get; set; } = new();

        /// <summary>해당 사운드가 활성화되어 있는지 (sounds 맵에 존재하고 volume > 0)</summary>
        public bool IsEnabled(string soundTypeKey) =>
            Sounds.TryGetValue(soundTypeKey, out var vol) && vol > 0;

        /// <summary>해당 사운드의 볼륨 반환 (없으면 0)</summary>
        public double GetVolume(string soundTypeKey) =>
            Sounds.TryGetValue(soundTypeKey, out var vol) ? vol : 0.0;
    }

    /// <summary>
    /// SoundThemes.json 루트
    /// </summary>
    public class SoundThemesFile
    {
        [JsonPropertyName("themes")]
        public List<SoundThemeData> Themes { get; set; } = new();

        public static SoundThemesFile? LoadFromFile(string path)
        {
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<SoundThemesFile>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load SoundThemes: {ex.Message}");
                return null;
            }
        }

        public bool SaveToFile(string path)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save SoundThemes: {ex.Message}");
                return false;
            }
        }
    }
}
