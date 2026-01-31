using System;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 황금 고블린 보상 이벤트 인자
    /// </summary>
    public class GoldenGoblinRewardEventArgs : EventArgs
    {
        /// <summary>
        /// 획득한 골드
        /// </summary>
        public int GoldReward { get; }

        /// <summary>
        /// 보상 배수
        /// </summary>
        public int Multiplier { get; }

        public GoldenGoblinRewardEventArgs(int goldReward, int multiplier)
        {
            GoldReward = goldReward;
            Multiplier = multiplier;
        }
    }
}
