using System;
using DeskWarrior.Helpers;

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
        public long ActualDamage { get; init; }

        /// <summary>
        /// 데미지 적용 후 사망 여부
        /// </summary>
        public bool IsDead { get; init; }

        /// <summary>
        /// 오버킬 데미지 (HP를 초과한 데미지)
        /// </summary>
        public long OverkillDamage { get; init; }
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
        public long MaxHp { get; set; }

        /// <summary>
        /// 현재 HP
        /// </summary>
        public long CurrentHp { get; set; }

        /// <summary>
        /// 보스 여부
        /// </summary>
        public bool IsBoss { get; private set; }

        /// <summary>
        /// 몬스터 유형
        /// </summary>
        public MonsterType Type { get; private set; }

        /// <summary>
        /// 황금 고블린 여부
        /// </summary>
        public bool IsGoldenGoblin => Type == MonsterType.GoldenGoblin;

        /// <summary>
        /// 처치 시 획득 골드
        /// </summary>
        public long GoldReward { get; private set; }

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
        /// 몬스터 ID (도감용)
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// 몬스터 이름
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 표시할 이모지
        /// </summary>
        public string Emoji { get; private set; }

        /// <summary>
        /// 누적 받은 데미지
        /// </summary>
        public long TotalDamageTaken { get; private set; }

        /// <summary>
        /// 몬스터 종족 (슬라임, 박쥐 등)
        /// </summary>
        public string Species { get; private set; }

        /// <summary>
        /// 몬스터 속성 (normal, fire, ice, wind, holy, dark)
        /// </summary>
        public string Element { get; private set; }

        /// <summary>
        /// 시간 배속 (Wind 속성용)
        /// </summary>
        public double TimeScale { get; set; } = 1.0;

        /// <summary>
        /// 키보드 공격 저항 (Fire 속성용)
        /// </summary>
        public double KeyboardResistance { get; set; } = 1.0;

        /// <summary>
        /// 마우스 공격 저항 (Ice 속성용)
        /// </summary>
        public double MouseResistance { get; set; } = 1.0;

        /// <summary>
        /// 황금 고블린 등급 ID (bronze/silver/gold/diamond/legendary)
        /// </summary>
        public string? GradeId { get; private set; }

        /// <summary>
        /// 황금 고블린 등급 배경 이미지 경로
        /// </summary>
        public string? GradeBackground { get; private set; }

        /// <summary>
        /// 황금 고블린 등급 테두리 색상
        /// </summary>
        public string? GradeBorderColor { get; private set; }

        /// <summary>
        /// 황금 고블린 등급 이름 색상
        /// </summary>
        public string? GradeNameColor { get; private set; }

        /// <summary>
        /// 황금 고블린 보상 배수 (스폰 시 결정)
        /// </summary>
        public int RewardMultiplier { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// 몬스터 생성 (데이터 기반)
        /// </summary>
        public Monster(MonsterData data, int level, bool isBoss, TierHpSystemConfig? tierConfig = null, string species = "", string element = "normal")
        {
            Level = level;
            IsBoss = isBoss;
            Type = isBoss ? MonsterType.Boss : MonsterType.Normal;

            // 스케일링 공식: MaxHp = BaseHp + (level - 1) * HpGrowth (또는 티어 기반)
            MaxHp = CalculateMaxHp(data.BaseHp, data.HpGrowth, level, tierConfig);
            CurrentHp = MaxHp;

            // 골드 보상: BaseGold + level * GoldGrowth
            GoldReward = CalculateGoldReward(data.BaseGold, data.GoldGrowth, level);

            // ID, 스킨 및 이모지는 데이터에서 가져옴
            Id = data.Id;
            Name = data.Name;
            SkinType = GetSkinType(data);
            Emoji = data.Emoji;
            TotalDamageTaken = 0;

            // 종족 및 속성 초기화
            Species = species;
            Element = element;
        }

        /// <summary>
        /// 황금 고블린 생성 (등급 시스템 적용)
        /// </summary>
        public Monster(GoldenGoblinConfig config, int level, string localizedName, GoldenGoblinGrade? grade = null, int rewardMultiplier = 0)
        {
            Level = level;
            IsBoss = false;
            Type = MonsterType.GoldenGoblin;
            RewardMultiplier = rewardMultiplier;

            // 등급이 있으면 등급의 HP 범위 사용, 없으면 기본 config 사용
            int hpMin = grade?.HpMin ?? config.HpMin;
            int hpMax = grade?.HpMax ?? config.HpMax;

            if (hpMin > 0 && hpMax > hpMin)
            {
                var random = new System.Random();
                MaxHp = random.Next(hpMin, hpMax + 1);
            }
            else
            {
                MaxHp = config.Hp;
            }
            CurrentHp = MaxHp;

            // 골드 보상은 나중에 별도 계산 (스테이지 골드 × 배수)
            GoldReward = 0;

            Id = config.Id;
            Name = localizedName;
            Emoji = config.Emoji;
            TotalDamageTaken = 0;

            // 등급이 있으면 등급 스프라이트, 없으면 기본 스프라이트
            SkinType = grade?.Sprite ?? config.Sprite;

            // 등급 정보 설정
            if (grade != null)
            {
                GradeId = grade.GradeId;
                GradeBackground = grade.Background;
                GradeBorderColor = grade.BorderColor;
                GradeNameColor = grade.NameColor;
            }

            // 황금 고블린은 특수 몬스터이므로 속성 없음
            Species = "golden_goblin";
            Element = "special";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 데미지 적용
        /// </summary>
        /// <returns>실제 적용된 데미지</returns>
        public long TakeDamage(long damage)
        {
            var result = ApplyDamage(damage);
            return result.ActualDamage;
        }

        /// <summary>
        /// 데미지 적용 (상세 결과 반환)
        /// </summary>
        public DamageApplyResult ApplyDamage(long damage)
        {
            if (damage < 0)
            {
                throw new ArgumentException("Damage cannot be negative", nameof(damage));
            }

            long previousHp = CurrentHp;
            long actualDamage = Math.Min(damage, CurrentHp);
            long overkill = damage - actualDamage;

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
        public long Heal(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentException("Heal amount cannot be negative", nameof(amount));
            }

            long previousHp = CurrentHp;
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
        public long EstimateHitsToKill(long damagePerHit)
        {
            if (damagePerHit <= 0) return long.MaxValue;
            return (long)Math.Ceiling((double)CurrentHp / damagePerHit);
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

        private static long CalculateMaxHp(int baseHp, int hpGrowth, int level, TierHpSystemConfig? tierConfig = null)
        {
            // Feature Flag: 티어 시스템 활성화 시
            if (tierConfig?.Enabled == true)
            {
                return CalculateTierBasedHp(baseHp, level, tierConfig);
            }

            // Legacy: 선형 공식
            return baseHp + (level - 1) * hpGrowth;
        }

        /// <summary>
        /// 티어 기반 HP 계산
        /// </summary>
        private static long CalculateTierBasedHp(int baseHp, int level, TierHpSystemConfig config)
        {
            int tier = (level - 1) / config.TierInterval;
            double tierIndex = tier;
            if (config.TierCurveExponent > 0.0 && config.TierCurveExponent != 1.0 && tierIndex > 0.0)
            {
                tierIndex = Math.Pow(tierIndex, config.TierCurveExponent);
            }
            double tierMultiplier = Math.Pow(config.TierMultiplier, tierIndex);
            if (config.TierMultiplierDecayPerTier != 1.0)
            {
                double decay = Math.Pow(config.TierMultiplierDecayPerTier, tierIndex * (tierIndex - 1) / 2.0);
                tierMultiplier *= decay;
            }
            if (config.MinTierMultiplier > 0.0 && tierMultiplier < config.MinTierMultiplier)
            {
                tierMultiplier = config.MinTierMultiplier;
            }
            long tierBaseHp = SafeMath.ToLong(baseHp * tierMultiplier);

            int levelInTier = (level - 1) % config.TierInterval;
            double tierGrowthRate = config.LinearGrowthPerLevel * Math.Pow(config.GrowthDecreasePerTier, tierIndex);
            if (config.MinLinearGrowthPerLevel > 0.0 && tierGrowthRate < config.MinLinearGrowthPerLevel)
            {
                tierGrowthRate = config.MinLinearGrowthPerLevel;
            }
            long linearIncrease = SafeMath.ToLong(levelInTier * tierGrowthRate);
            long hp = SafeMath.AddLong(tierBaseHp, linearIncrease);

            if (config.LateStartLevel > 0 && level >= config.LateStartLevel)
            {
                int lateInterval = config.LateTierInterval > 0 ? config.LateTierInterval : config.TierInterval;
                int lateTier = (level - config.LateStartLevel) / Math.Max(1, lateInterval);
                if (config.MaxLateTiers > 0 && lateTier > config.MaxLateTiers)
                    lateTier = config.MaxLateTiers;
                double lateMultiplier = config.LateTierMultiplier != 1.0
                    ? Math.Pow(config.LateTierMultiplier, lateTier)
                    : 1.0;
                hp = SafeMath.MulLong(hp, lateMultiplier);
            }

            return hp;
        }

        private static long CalculateGoldReward(int baseGold, int goldGrowth, int level)
        {
            return (long)baseGold + SafeMath.MulToLong(level, goldGrowth);
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
