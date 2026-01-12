namespace DeskWarrior.Models
{
    /// <summary>
    /// 몬스터 데이터 모델
    /// </summary>
    public class Monster
    {
        /// <summary>
        /// 현재 레벨
        /// </summary>
        public int Level { get; private set; }

        /// <summary>
        /// 최대 HP
        /// </summary>
        public int MaxHp { get; private set; }

        /// <summary>
        /// 현재 HP
        /// </summary>
        public int CurrentHp { get; private set; }

        /// <summary>
        /// 보스 여부
        /// </summary>
        public bool IsBoss { get; private set; }

        /// <summary>
        /// 처치 시 획득 골드
        /// </summary>
        public int GoldReward { get; private set; }

        /// <summary>
        /// 살아있는지 여부
        /// </summary>
        public bool IsAlive => CurrentHp > 0;

        /// <summary>
        /// HP 비율 (0.0 ~ 1.0)
        /// </summary>
        public double HpRatio => MaxHp > 0 ? (double)CurrentHp / MaxHp : 0;

        /// <summary>
        /// 몬스터 스킨 타입 (파일명, 예: monster_slimeA)
        /// </summary>
        public string SkinType { get; private set; }

        /// <summary>
        /// 표시할 이모지
        /// </summary>
        public string Emoji { get; private set; }

        /// <summary>
        /// 몬스터 생성 (데이터 기반)
        /// </summary>
        public Monster(MonsterData data, int level, bool isBoss)
        {
            Level = level;
            IsBoss = isBoss;
            
            // 스케일링 공식: MaxHp = BaseHp + (level - 1) * HpGrowth
            MaxHp = data.BaseHp + (level - 1) * data.HpGrowth;
            CurrentHp = MaxHp;
            
            // 골드 보상: BaseGold + level * GoldGrowth
            GoldReward = data.BaseGold + level * data.GoldGrowth;

            // 스킨 및 이모지는 데이터에서 가져옴
            SkinType = data.Id;
            Emoji = data.Emoji;
        }

        /// <summary>
        /// 데미지 적용
        /// </summary>
        /// <returns>실제 적용된 데미지</returns>
        public int TakeDamage(int damage)
        {
            int actualDamage = System.Math.Min(damage, CurrentHp);
            CurrentHp -= actualDamage;
            return actualDamage;
        }
    }
}
