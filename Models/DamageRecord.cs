using System;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 개별 데미지 기록
    /// </summary>
    public class DamageRecord
    {
        public DateTime Timestamp { get; init; } = DateTime.Now;
        public long BasePower { get; init; }
        public long BaseAttackBonus { get; init; }
        public double AttackMultiplier { get; init; }
        public bool IsCritical { get; init; }
        public double CritMultiplier { get; init; }
        public bool IsMultiHit { get; init; }
        public bool IsCombo { get; init; }
        public int ComboStack { get; init; }
        public long FinalDamage { get; init; }
        public bool IsMouse { get; init; }

        /// <summary>
        /// 데미지 계산 과정을 문자열로 반환
        /// </summary>
        public string GetBreakdown()
        {
            var parts = new System.Collections.Generic.List<string>();

            // 기본 공격력
            long current = BasePower;
            parts.Add($"{BasePower}");

            // 기본 공격력 보너스
            if (BaseAttackBonus > 0)
            {
                current += BaseAttackBonus;
                parts.Add($"+{BaseAttackBonus}");
            }

            // 공격력 배율
            if (AttackMultiplier > 0)
            {
                parts.Add($"×{1 + AttackMultiplier:F1}");
            }

            // 크리티컬
            if (IsCritical)
            {
                parts.Add($"×{CritMultiplier:F1}(Crit)");
            }

            // 멀티히트
            if (IsMultiHit)
            {
                parts.Add("×2(Multi)");
            }

            // 콤보
            if (IsCombo)
            {
                int comboMultiplier = (int)Math.Pow(2, ComboStack);
                parts.Add($"×{comboMultiplier}(C{ComboStack})");
            }

            return string.Join(" ", parts) + $" = {FinalDamage}";
        }

        /// <summary>
        /// 짧은 요약 (아이콘 + 데미지)
        /// </summary>
        public string GetShortSummary()
        {
            string icon = IsMouse ? "🖰" : "⌨";
            string modifiers = "";
            if (IsCritical) modifiers += "!";
            if (IsMultiHit) modifiers += "×2";
            if (IsCombo) modifiers += $"C{ComboStack}";

            return $"{icon} {FinalDamage}{(modifiers.Length > 0 ? $" [{modifiers}]" : "")}";
        }
    }
}
