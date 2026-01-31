using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 영구 능력치 (메타 진행도) - 19종 스탯
    /// </summary>
    public class PermanentStats
    {
        #region A. 기본 능력 (5종)

        /// <summary>
        /// 기본 공격력 (레벨)
        /// </summary>
        [JsonPropertyName("base_attack")]
        public int BaseAttackLevel { get; set; } = 0;

        /// <summary>
        /// 공격력 배수 (레벨)
        /// </summary>
        [JsonPropertyName("attack_percent")]
        public int AttackPercentLevel { get; set; } = 0;

        /// <summary>
        /// 크리티컬 확률 (레벨)
        /// </summary>
        [JsonPropertyName("crit_chance")]
        public int CritChanceLevel { get; set; } = 0;

        /// <summary>
        /// 크리티컬 배율 (레벨)
        /// </summary>
        [JsonPropertyName("crit_damage")]
        public int CritDamageLevel { get; set; } = 0;

        /// <summary>
        /// 멀티히트 확률 (레벨)
        /// </summary>
        [JsonPropertyName("multi_hit")]
        public int MultiHitLevel { get; set; } = 0;

        #endregion

        #region B. 재화 보너스 (4종)

        /// <summary>
        /// 영구 골드+ (레벨)
        /// </summary>
        [JsonPropertyName("gold_flat_perm")]
        public int GoldFlatPermLevel { get; set; } = 0;

        /// <summary>
        /// 영구 골드* (레벨)
        /// </summary>
        [JsonPropertyName("gold_multi_perm")]
        public int GoldMultiPermLevel { get; set; } = 0;

        /// <summary>
        /// 크리스탈+ (레벨)
        /// </summary>
        [JsonPropertyName("crystal_flat")]
        public int CrystalFlatLevel { get; set; } = 0;

        /// <summary>
        /// 크리스탈* (레벨)
        /// </summary>
        [JsonPropertyName("crystal_multi")]
        public int CrystalMultiLevel { get; set; } = 0;

        #endregion

        #region C. 유틸리티 (2종)

        /// <summary>
        /// 기본 시간 연장 (레벨)
        /// </summary>
        [JsonPropertyName("time_extend")]
        public int TimeExtendLevel { get; set; } = 0;

        /// <summary>
        /// 업그레이드 할인 (레벨)
        /// </summary>
        [JsonPropertyName("upgrade_discount")]
        public int UpgradeDiscountLevel { get; set; } = 0;

        #endregion

        #region D. 시작 보너스 (8종)

        /// <summary>
        /// 시작 레벨 (레벨)
        /// </summary>
        [JsonPropertyName("start_level")]
        public int StartLevelLevel { get; set; } = 0;

        /// <summary>
        /// 시작 골드 (레벨)
        /// </summary>
        [JsonPropertyName("start_gold")]
        public int StartGoldLevel { get; set; } = 0;

        /// <summary>
        /// 시작 키보드 (레벨)
        /// </summary>
        [JsonPropertyName("start_keyboard")]
        public int StartKeyboardLevel { get; set; } = 0;

        /// <summary>
        /// 시작 마우스 (레벨)
        /// </summary>
        [JsonPropertyName("start_mouse")]
        public int StartMouseLevel { get; set; } = 0;

        /// <summary>
        /// 시작 골드+ (레벨)
        /// </summary>
        [JsonPropertyName("start_gold_flat")]
        public int StartGoldFlatLevel { get; set; } = 0;

        /// <summary>
        /// 시작 골드* (레벨)
        /// </summary>
        [JsonPropertyName("start_gold_multi")]
        public int StartGoldMultiLevel { get; set; } = 0;

        /// <summary>
        /// 시작 콤보유연성 (레벨)
        /// </summary>
        [JsonPropertyName("start_combo_flex")]
        public int StartComboFlexLevel { get; set; } = 0;

        /// <summary>
        /// 시작 콤보데미지 (레벨)
        /// </summary>
        [JsonPropertyName("start_combo_damage")]
        public int StartComboDamageLevel { get; set; } = 0;

        #endregion

        #region Legacy Properties (Backward Compatibility)

        // 시뮬레이터와 동기화된 효과 계산 (PermanentStats.json 기준)
        [JsonIgnore]
        public int BaseAttack => BaseAttackLevel * 3; // 3 per level

        [JsonIgnore]
        public double AttackPercentBonus => AttackPercentLevel * 0.02; // 2% per level (Config 동기화)

        [JsonIgnore]
        public double GoldPercentBonus => GoldMultiPermLevel * 0.15; // 15% per level (Config 동기화)

        [JsonIgnore]
        public double CriticalChanceBonus => CritChanceLevel * 0.005; // 0.5% per level (Config 동기화)

        [JsonIgnore]
        public double CriticalDamageBonus => CritDamageLevel * 0.2; // 0.2x per level (Config 동기화)

        [JsonIgnore]
        public double MultiHitChance => MultiHitLevel * 0.01; // 1% per level

        [JsonIgnore]
        public int StartingLevelBonus => StartLevelLevel * 5; // 5 per level (Config 동기화)

        [JsonIgnore]
        public int StartingGoldBonus => StartGoldLevel * 150; // 150 per level (Config 동기화)

        [JsonIgnore]
        public int StartingKeyboardPower => StartKeyboardLevel * 2; // 2 per level (Config 동기화)

        [JsonIgnore]
        public int StartingMousePower => StartMouseLevel * 2; // 2 per level (Config 동기화)

        [JsonIgnore]
        public double GameOverTimeExtension => TimeExtendLevel * 0.4; // 0.4초 per level (Config 동기화)

        [JsonIgnore]
        public double UpgradeCostReduction => UpgradeDiscountLevel * 0.03; // 3% per level (Config 동기화)

        [JsonIgnore]
        public int CrystalFlatBonus => CrystalFlatLevel * 10; // 10 per level (Config 동기화)

        [JsonIgnore]
        public double CrystalDropChanceBonus => CrystalMultiLevel * 0.02; // 2% per level (Config 동기화)

        #endregion
    }
}
