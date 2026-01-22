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
        /// 모든 스탯 리셋
        /// </summary>
        public void Reset()
        {
            KeyboardPowerLevel = 0;
            MousePowerLevel = 0;
        }

        /// <summary>
        /// 복사본 생성
        /// </summary>
        public InGameStats Clone()
        {
            return new InGameStats
            {
                KeyboardPowerLevel = this.KeyboardPowerLevel,
                MousePowerLevel = this.MousePowerLevel
            };
        }
    }
}
