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
        /// 몬스터 생성
        /// </summary>
        public Monster(int level, int baseHp, int hpGrowth, int bossInterval, double bossHpMultiplier, int baseGoldMultiplier)
        {
            Level = level;
            IsBoss = level > 0 && level % bossInterval == 0;
            
            // HP 계산: base_hp + (level - 1) * hp_growth
            int normalHp = baseHp + (level - 1) * hpGrowth;
            MaxHp = IsBoss ? (int)(normalHp * bossHpMultiplier) : normalHp;
            CurrentHp = MaxHp;
            
            // 골드 보상: level * multiplier (보스는 3배)
            GoldReward = level * baseGoldMultiplier * (IsBoss ? 3 : 1);
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

        /// <summary>
        /// 표시할 이모지
        /// </summary>
        public string Emoji => IsBoss ? "👿" : "👹";
    }
}
