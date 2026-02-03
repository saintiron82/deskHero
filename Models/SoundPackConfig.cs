using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeskWarrior.Interfaces;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 사운드팩 설정 (pack.json)
    /// </summary>
    public class SoundPackConfig
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("name_ko")]
        public string NameKo { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("description_ko")]
        public string DescriptionKo { get; set; } = "";

        [JsonPropertyName("author")]
        public string Author { get; set; } = "DeskHero";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("is_builtin")]
        public bool IsBuiltin { get; set; }

        [JsonPropertyName("sounds")]
        public Dictionary<string, SoundEntry> Sounds { get; set; } = new();

        [JsonPropertyName("fallback")]
        public string Fallback { get; set; } = "default";

        // 런타임 전용 (JSON에서 로드되지 않음)
        [JsonIgnore]
        public string FolderPath { get; set; } = "";

        [JsonIgnore]
        public bool IsCustom { get; set; }

        /// <summary>
        /// SoundType에 해당하는 WAV 파일 경로 반환
        /// </summary>
        public string? GetSoundPath(SoundType type)
        {
            string key = type.ToString();

            // 레거시 Hit -> KeyboardHit 매핑
            if (key == "Hit")
                key = "KeyboardHit";

            if (Sounds.TryGetValue(key, out var entry) && !string.IsNullOrEmpty(entry.File))
            {
                return Path.Combine(FolderPath, entry.File);
            }
            return null;
        }

        /// <summary>
        /// SoundType에 해당하는 볼륨 배율 반환
        /// </summary>
        public double GetVolumeMultiplier(SoundType type)
        {
            string key = type.ToString();
            if (key == "Hit")
                key = "KeyboardHit";

            if (Sounds.TryGetValue(key, out var entry))
            {
                return entry.VolumeMultiplier;
            }
            return 1.0;
        }

        /// <summary>
        /// SoundPackInfo로 변환
        /// </summary>
        public SoundPackInfo ToInfo()
        {
            return new SoundPackInfo
            {
                Id = Id,
                Name = Name,
                NameLocalized = NameKo,
                Description = Description,
                Author = Author,
                IsBuiltin = IsBuiltin,
                IsCustom = IsCustom,
                FolderPath = FolderPath
            };
        }

        /// <summary>
        /// 기본 사운드 매핑 설정
        /// </summary>
        public void SetupDefaultMappings()
        {
            foreach (SoundType type in Enum.GetValues<SoundType>())
            {
                string key = type.ToString();
                if (key == "Hit") continue; // 레거시 스킵

                Sounds[key] = new SoundEntry
                {
                    File = $"{key}.wav",
                    VolumeMultiplier = 1.0
                };
            }
        }

        /// <summary>
        /// JSON 파일에서 로드
        /// </summary>
        public static SoundPackConfig? LoadFromFile(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                return null;

            try
            {
                string json = File.ReadAllText(jsonPath);
                return JsonSerializer.Deserialize<SoundPackConfig>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// JSON 파일로 저장
        /// </summary>
        public void SaveToFile(string jsonPath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(jsonPath, json);
        }
    }

    /// <summary>
    /// 개별 사운드 항목
    /// </summary>
    public class SoundEntry
    {
        [JsonPropertyName("file")]
        public string File { get; set; } = "";

        [JsonPropertyName("volume_multiplier")]
        public double VolumeMultiplier { get; set; } = 1.0;

        [JsonPropertyName("pitch_variation")]
        public double PitchVariation { get; set; } = 0.0;
    }
}
