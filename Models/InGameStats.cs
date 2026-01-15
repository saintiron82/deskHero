using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 인게임 스탯 (골드로 업그레이드, 세션 종료 시 리셋)
    /// </summary>
    public class InGameStats
    {
        /// <summary>
        /// 키보드 공격력 레벨
        /// </summary>
        [JsonPropertyName("keyboard_power")]
        public int KeyboardPowerLevel { get; set; } = 0;

        /// <summary>
        /// 마우스 공격력 레벨
        /// </summary>
        [JsonPropertyName("mouse_power")]
        public int MousePowerLevel { get; set; } = 0;

        /// <summary>
        /// 골드+ (가산) 레벨
        /// </summary>
        [JsonPropertyName("gold_flat")]
        public int GoldFlatLevel { get; set; } = 0;

        /// <summary>
        /// 골드* (배율) 레벨
        /// </summary>
        [JsonPropertyName("gold_multi")]
        public int GoldMultiLevel { get; set; } = 0;

        /// <summary>
        /// 시간 도둑 레벨
        /// </summary>
        [JsonPropertyName("time_thief")]
        public int TimeThiefLevel { get; set; } = 0;

        /// <summary>
        /// 콤보 유연성 레벨
        /// </summary>
        [JsonPropertyName("combo_flex")]
        public int ComboFlexLevel { get; set; } = 0;

        /// <summary>
        /// 콤보 데미지 레벨
        /// </summary>
        [JsonPropertyName("combo_damage")]
        public int ComboDamageLevel { get; set; } = 0;

        /// <summary>
        /// 모든 스탯 리셋
        /// </summary>
        public void Reset()
        {
            KeyboardPowerLevel = 0;
            MousePowerLevel = 0;
            GoldFlatLevel = 0;
            GoldMultiLevel = 0;
            TimeThiefLevel = 0;
            ComboFlexLevel = 0;
            ComboDamageLevel = 0;
        }

        /// <summary>
        /// 복사본 생성
        /// </summary>
        public InGameStats Clone()
        {
            return new InGameStats
            {
                KeyboardPowerLevel = this.KeyboardPowerLevel,
                MousePowerLevel = this.MousePowerLevel,
                GoldFlatLevel = this.GoldFlatLevel,
                GoldMultiLevel = this.GoldMultiLevel,
                TimeThiefLevel = this.TimeThiefLevel,
                ComboFlexLevel = this.ComboFlexLevel,
                ComboDamageLevel = this.ComboDamageLevel
            };
        }
    }
}
