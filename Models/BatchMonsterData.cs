using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 다국어 텍스트 지원
    /// </summary>
    public class LocalizedText
    {
        [JsonPropertyName("ko-KR")]
        public string KoKR { get; set; } = string.Empty;

        [JsonPropertyName("en-US")]
        public string EnUS { get; set; } = string.Empty;

        /// <summary>
        /// 현재 언어에 맞는 텍스트 반환 (fallback: en-US)
        /// </summary>
        public string GetText(string languageCode = "ko-KR")
        {
            return languageCode switch
            {
                "ko-KR" => !string.IsNullOrEmpty(KoKR) ? KoKR : EnUS,
                "en-US" => !string.IsNullOrEmpty(EnUS) ? EnUS : KoKR,
                _ => !string.IsNullOrEmpty(EnUS) ? EnUS : KoKR
            };
        }
    }

    /// <summary>
    /// 다국어 이름 + 설명
    /// </summary>
    public class LocalizedMonsterInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 몬스터 속성 변형 데이터
    /// </summary>
    public class MonsterVariation
    {
        [JsonPropertyName("localization")]
        public Dictionary<string, LocalizedMonsterInfo> Localization { get; set; } = new();

        [JsonPropertyName("sprite")]
        public string Sprite { get; set; } = string.Empty;

        [JsonPropertyName("emoji")]
        public string Emoji { get; set; } = string.Empty;

        [JsonPropertyName("hp_modifier")]
        public double HpModifier { get; set; } = 1.0;

        [JsonPropertyName("gold_modifier")]
        public double GoldModifier { get; set; } = 1.0;

        /// <summary>
        /// 언어별 이름 가져오기
        /// </summary>
        public string GetName(string languageCode = "ko-KR")
        {
            if (Localization.TryGetValue(languageCode, out var info))
                return info.Name;
            if (Localization.TryGetValue("en-US", out var fallback))
                return fallback.Name;
            return Localization.Values.FirstOrDefault()?.Name ?? "???";
        }

        /// <summary>
        /// 언어별 설명 가져오기
        /// </summary>
        public string GetDescription(string languageCode = "ko-KR")
        {
            if (Localization.TryGetValue(languageCode, out var info))
                return info.Description;
            if (Localization.TryGetValue("en-US", out var fallback))
                return fallback.Description;
            return Localization.Values.FirstOrDefault()?.Description ?? "";
        }
    }

    /// <summary>
    /// 몬스터 기본 스탯 (배치 파일용)
    /// </summary>
    public class MonsterBaseStats
    {
        [JsonPropertyName("base_hp")]
        public int BaseHp { get; set; }

        [JsonPropertyName("hp_growth")]
        public int HpGrowth { get; set; }

        [JsonPropertyName("base_gold")]
        public int BaseGold { get; set; }

        [JsonPropertyName("gold_growth")]
        public int GoldGrowth { get; set; }
    }

    /// <summary>
    /// 배치 파일 내 몬스터 정의
    /// </summary>
    public class BatchMonsterEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("species")]
        public string Species { get; set; } = string.Empty;

        [JsonPropertyName("is_boss")]
        public bool IsBoss { get; set; }

        [JsonPropertyName("base_stats")]
        public MonsterBaseStats BaseStats { get; set; } = new();

        [JsonPropertyName("spawn_weight")]
        public int SpawnWeight { get; set; } = 100;

        [JsonPropertyName("variations")]
        public Dictionary<string, MonsterVariation> Variations { get; set; } = new();
    }

    /// <summary>
    /// 배치 데이터 (batch_XX.json 루트)
    /// </summary>
    public class BatchData
    {
        [JsonPropertyName("batch_id")]
        public int BatchId { get; set; }

        [JsonPropertyName("name")]
        public LocalizedText Name { get; set; } = new();

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = string.Empty;

        [JsonPropertyName("unlock_condition")]
        public BatchUnlockCondition? UnlockCondition { get; set; }

        [JsonPropertyName("monsters")]
        public List<BatchMonsterEntry> Monsters { get; set; } = new();

        [JsonPropertyName("bosses")]
        public List<BatchMonsterEntry> Bosses { get; set; } = new();
    }

    /// <summary>
    /// 배치 해금 조건
    /// </summary>
    public class BatchUnlockCondition
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;  // "level", "achievement", "purchase" 등

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("achievement_id")]
        public string? AchievementId { get; set; }
    }

    /// <summary>
    /// 배치 인덱스 항목 (_index.json 내 항목)
    /// </summary>
    public class BatchIndexEntry
    {
        [JsonPropertyName("batch_id")]
        public int BatchId { get; set; }

        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public LocalizedText Name { get; set; } = new();

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("min_level")]
        public int MinLevel { get; set; } = 1;

        [JsonPropertyName("max_level")]
        public int? MaxLevel { get; set; } = null;

        [JsonPropertyName("activation_weight")]
        public double ActivationWeight { get; set; } = 1.0;
    }

    /// <summary>
    /// 배치 인덱스 (_index.json 루트)
    /// </summary>
    public class BatchIndex
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("total_batches")]
        public int TotalBatches { get; set; }

        [JsonPropertyName("batches")]
        public List<BatchIndexEntry> Batches { get; set; } = new();
    }

    /// <summary>
    /// 평탄화된 몬스터 데이터 (런타임 사용, MonsterData와 호환)
    /// 배치 데이터에서 변환하여 기존 코드와 호환성 유지
    /// </summary>
    public class FlattenedMonsterData
    {
        public string Id { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public string Element { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Sprite { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int BaseHp { get; set; }
        public int HpGrowth { get; set; }
        public int BaseGold { get; set; }
        public int GoldGrowth { get; set; }
        public bool IsBoss { get; set; }
        public int BatchId { get; set; }
        public int SpawnWeight { get; set; } = 100;
        public int FinalWeight { get; set; } = 100;

        /// <summary>
        /// MonsterData로 변환 (하위 호환용)
        /// </summary>
        public MonsterData ToMonsterData()
        {
            return new MonsterData
            {
                Id = Id,
                Name = Name,
                Sprite = Sprite,
                BaseHp = BaseHp,
                HpGrowth = HpGrowth,
                BaseGold = BaseGold,
                GoldGrowth = GoldGrowth,
                Emoji = Emoji
            };
        }
    }
}
