using System;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 황금 고블린 스폰 이벤트 인자 (등급 정보 포함)
    /// </summary>
    public class GoldenGoblinSpawnEventArgs : EventArgs
    {
        /// <summary>
        /// 등급 ID (bronze/silver/gold/diamond/legendary)
        /// </summary>
        public string? GradeId { get; }

        /// <summary>
        /// 배경 이미지 경로
        /// </summary>
        public string? Background { get; }

        /// <summary>
        /// 테두리 색상 (Hex)
        /// </summary>
        public string? BorderColor { get; }

        /// <summary>
        /// 이름 색상 (Hex)
        /// </summary>
        public string? NameColor { get; }

        /// <summary>
        /// 보상 배수 (스폰 시 결정됨)
        /// </summary>
        public int Multiplier { get; }

        public GoldenGoblinSpawnEventArgs(string? gradeId, string? background, string? borderColor, string? nameColor, int multiplier)
        {
            GradeId = gradeId;
            Background = background;
            BorderColor = borderColor;
            NameColor = nameColor;
            Multiplier = multiplier;
        }
    }

    /// <summary>
    /// 황금 고블린 보상 이벤트 인자
    /// </summary>
    public class GoldenGoblinRewardEventArgs : EventArgs
    {
        /// <summary>
        /// 획득한 골드
        /// </summary>
        public long GoldReward { get; }

        /// <summary>
        /// 보상 배수
        /// </summary>
        public int Multiplier { get; }

        /// <summary>
        /// 등급 ID (bronze/silver/gold/diamond/legendary)
        /// </summary>
        public string? GradeId { get; }

        public GoldenGoblinRewardEventArgs(long goldReward, int multiplier, string? gradeId = null)
        {
            GoldReward = goldReward;
            Multiplier = multiplier;
            GradeId = gradeId;
        }
    }
}
