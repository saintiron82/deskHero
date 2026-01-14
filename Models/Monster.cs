using System;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 데미지 적용 결과
    /// </summary>
    public struct DamageApplyResult
    {
        /// <summary>
        /// 실제 적용된 데미지
        /// </summary>
        public int ActualDamage { get; init; }

        /// <summary>
        /// 데미지 적용 후 사망 여부
        /// </summary>
        public bool IsDead { get; init; }

        /// <summary>
        /// 오버킬 데미지 (HP를 초과한 데미지)
        /// </summary>
        public int OverkillDamage { get; init; }
    }

    /// <summary>
    /// 몬스터 데이터 모델 (도메인 모델)
    /// </summary>
    public class Monster
    {
        #region Properties

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
        /// HP 퍼센트 (0 ~ 100)
        /// </summary>
        public int HpPercent => (int)(HpRatio * 100);

        /// <summary>
        /// 몬스터 스킨 타입 (파일명, 예: monster_slimeA)
        /// </summary>
        public string SkinType { get; private set; }

        /// <summary>
        /// 표시할 이모지
        /// </summary>
        public string Emoji { get; private set; }

        /// <summary>
        /// 누적 받은 데미지
        /// </summary>
        public long TotalDamageTaken { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// 몬스터 생성 (데이터 기반)
        /// </summary>
        public Monster(MonsterData data, int level, bool isBoss)
        {
            Level = level;
            IsBoss = isBoss;

            // 스케일링 공식: MaxHp = BaseHp + (level - 1) * HpGrowth
            MaxHp = CalculateMaxHp(data.BaseHp, data.HpGrowth, level);
            CurrentHp = MaxHp;

            // 골드 보상: BaseGold + level * GoldGrowth
            GoldReward = CalculateGoldReward(data.BaseGold, data.GoldGrowth, level);

            // 스킨 및 이모지는 데이터에서 가져옴
            SkinType = GetSkinType(data);
            Emoji = data.Emoji;
            TotalDamageTaken = 0;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 데미지 적용
        /// </summary>
        /// <returns>실제 적용된 데미지</returns>
        public int TakeDamage(int damage)
        {
            var result = ApplyDamage(damage);
            return result.ActualDamage;
        }

        /// <summary>
        /// 데미지 적용 (상세 결과 반환)
        /// </summary>
        public DamageApplyResult ApplyDamage(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentException("Damage cannot be negative", nameof(damage));
            }

            int previousHp = CurrentHp;
            int actualDamage = Math.Min(damage, CurrentHp);
            int overkill = damage - actualDamage;

            CurrentHp -= actualDamage;
            TotalDamageTaken += actualDamage;

            return new DamageApplyResult
            {
                ActualDamage = actualDamage,
                IsDead = !IsAlive,
                OverkillDamage = overkill
            };
        }

        /// <summary>
        /// HP 회복 (필요시 사용)
        /// </summary>
        public int Heal(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentException("Heal amount cannot be negative", nameof(amount));
            }

            int previousHp = CurrentHp;
            CurrentHp = Math.Min(CurrentHp + amount, MaxHp);
            return CurrentHp - previousHp;
        }

        /// <summary>
        /// HP를 최대치로 회복
        /// </summary>
        public void FullHeal()
        {
            CurrentHp = MaxHp;
        }

        /// <summary>
        /// 남은 HP로 예상되는 처치 필요 타수 계산
        /// </summary>
        public int EstimateHitsToKill(int damagePerHit)
        {
            if (damagePerHit <= 0) return int.MaxValue;
            return (int)Math.Ceiling((double)CurrentHp / damagePerHit);
        }

        /// <summary>
        /// 몬스터 상태 요약 문자열
        /// </summary>
        public override string ToString()
        {
            string bossTag = IsBoss ? " [BOSS]" : "";
            return $"Lv.{Level}{bossTag} - HP: {CurrentHp}/{MaxHp} ({HpPercent}%)";
        }

        #endregion

        #region Private Static Methods

        private static int CalculateMaxHp(int baseHp, int hpGrowth, int level)
        {
            return baseHp + (level - 1) * hpGrowth;
        }

        private static int CalculateGoldReward(int baseGold, int goldGrowth, int level)
        {
            return baseGold + level * goldGrowth;
        }

        private static string GetSkinType(MonsterData data)
        {
            // Sprite가 있으면 사용, 없으면 Id.png 사용 (보스용)
            if (!string.IsNullOrEmpty(data.Sprite))
            {
                return data.Sprite;
            }
            // 보스는 Id.png 형식 (예: boss_dragonA.png)
            return $"{data.Id}.png";
        }

        #endregion
    }
}
