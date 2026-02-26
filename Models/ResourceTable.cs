using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 리소스 경로 테이블 루트
    /// 모든 리소스 경로는 이 테이블에서만 정의됩니다 (CLAUDE.md 원칙 준수)
    /// </summary>
    public class ResourceTable
    {
        /// <summary>
        /// 리소스 테이블 버전
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 기본 경로 정의
        /// </summary>
        [JsonPropertyName("base_paths")]
        public Dictionary<string, string> BasePaths { get; set; } = new();

        /// <summary>
        /// 배치별 리소스 경로
        /// </summary>
        [JsonPropertyName("batches")]
        public Dictionary<string, BatchResource> Batches { get; set; } = new();

        /// <summary>
        /// 히어로 리소스 경로
        /// </summary>
        [JsonPropertyName("heroes")]
        public HeroResource Heroes { get; set; } = new();

        /// <summary>
        /// 배경 리소스 경로
        /// </summary>
        [JsonPropertyName("backgrounds")]
        public BackgroundResource Backgrounds { get; set; } = new();

        /// <summary>
        /// Placeholder 이미지 경로
        /// </summary>
        [JsonPropertyName("placeholders")]
        public Dictionary<string, string> Placeholders { get; set; } = new();

        /// <summary>
        /// 데이터 파일 경로
        /// </summary>
        [JsonPropertyName("data")]
        public Dictionary<string, string> Data { get; set; } = new();

        /// <summary>
        /// 특수 몬스터 리소스 경로
        /// </summary>
        [JsonPropertyName("special_monsters")]
        public Dictionary<string, SpecialMonsterResource> SpecialMonsters { get; set; } = new();

        /// <summary>
        /// UI 리소스 경로
        /// </summary>
        [JsonPropertyName("ui")]
        public Dictionary<string, string> UI { get; set; } = new();

        /// <summary>
        /// 사운드 리소스 경로
        /// </summary>
        [JsonPropertyName("sounds")]
        public Dictionary<string, string> Sounds { get; set; } = new();
    }

    /// <summary>
    /// 배치별 리소스 경로 정의
    /// </summary>
    public class BatchResource
    {
        /// <summary>
        /// 배치 ID
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// 배치 데이터 JSON 파일 경로
        /// </summary>
        [JsonPropertyName("data_file")]
        public string DataFile { get; set; } = "";

        /// <summary>
        /// 스프라이트 기본 경로
        /// </summary>
        [JsonPropertyName("sprite_base")]
        public string SpriteBase { get; set; } = "";

        /// <summary>
        /// 일반 몬스터 스프라이트 폴더
        /// </summary>
        [JsonPropertyName("monster_folder")]
        public string MonsterFolder { get; set; } = "";

        /// <summary>
        /// 보스 스프라이트 폴더
        /// </summary>
        [JsonPropertyName("boss_folder")]
        public string BossFolder { get; set; } = "";
    }

    /// <summary>
    /// 히어로 리소스 경로 정의
    /// </summary>
    public class HeroResource
    {
        [JsonPropertyName("sprite_folder")]
        public string SpriteFolder { get; set; } = "";
    }

    /// <summary>
    /// 배경 리소스 경로 정의
    /// </summary>
    public class BackgroundResource
    {
        [JsonPropertyName("default")]
        public string Default { get; set; } = "";

        [JsonPropertyName("available")]
        public List<string> Available { get; set; } = new();
    }

    /// <summary>
    /// 특수 몬스터 리소스 경로 정의
    /// </summary>
    public class SpecialMonsterResource
    {
        /// <summary>
        /// 스프라이트 경로 (상대 경로)
        /// </summary>
        [JsonPropertyName("sprite")]
        public string Sprite { get; set; } = "";

        /// <summary>
        /// 설정 파일 경로
        /// </summary>
        [JsonPropertyName("config")]
        public string Config { get; set; } = "";
    }
}
