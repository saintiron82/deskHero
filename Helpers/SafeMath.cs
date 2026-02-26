using System;

namespace DeskWarrior.Helpers
{
    /// <summary>
    /// 게임 런타임용 안전 수학 연산.
    /// DeskWarrior.Core.OverflowGuard(예외 throw)와 달리 클램핑으로 처리.
    /// 게임은 절대 오버플로우로 크래시하면 안 됩니다.
    /// </summary>
    public static class SafeMath
    {
        private const double SafeLongMax = 9.2E+18;

        /// <summary>
        /// double → long 안전 변환. NaN/Infinity/범위 초과 시 long.MaxValue 클램핑.
        /// </summary>
        public static long ToLong(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value > SafeLongMax)
                return long.MaxValue;
            if (value < -SafeLongMax)
                return long.MinValue;
            return (long)value;
        }

        /// <summary>
        /// double → int 안전 변환. NaN/Infinity/범위 초과 시 int.MaxValue 클램핑.
        /// </summary>
        public static int ToInt(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value > int.MaxValue)
                return int.MaxValue;
            if (value < int.MinValue)
                return int.MinValue;
            return (int)value;
        }

        /// <summary>
        /// long + long 안전 덧셈. 오버플로우 시 long.MaxValue 클램핑.
        /// </summary>
        public static long AddLong(long a, long b)
        {
            if (b > 0 && a > long.MaxValue - b) return long.MaxValue;
            if (b < 0 && a < long.MinValue - b) return long.MinValue;
            return a + b;
        }

        /// <summary>
        /// long × double 안전 곱셈. 결과를 long으로 클램핑.
        /// </summary>
        public static long MulLong(long a, double b)
        {
            double result = (double)a * b;
            return ToLong(result);
        }

        /// <summary>
        /// int × int → long 확장. int 범위에서는 오버플로우 불가.
        /// </summary>
        public static long MulToLong(int a, int b)
        {
            return (long)a * (long)b;
        }

        /// <summary>
        /// 비용 계산용: double → int, 오버플로우 시 int.MaxValue 센티넬 ("구매 불가").
        /// 음수는 0으로 클램핑.
        /// </summary>
        public static int CostToInt(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value > int.MaxValue)
                return int.MaxValue;
            if (value < 0)
                return 0;
            return (int)Math.Ceiling(value);
        }
    }
}
