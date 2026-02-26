namespace DeskWarrior.Core.Simulation;

/// <summary>
/// 시뮬레이션 중 long 오버플로우 감지 시 발생하는 예외.
/// 즉시 시뮬레이션을 중단하고 오버플로우 위치/값을 보고합니다.
/// </summary>
public class SimulationOverflowException : Exception
{
    public string Location { get; }
    public long GameLevel { get; }
    public double ComputedValue { get; }

    public SimulationOverflowException(string location, long gameLevel, double computedValue)
        : base($"[OVERFLOW] {location} at level {gameLevel}: value={computedValue:E4} (long.Max={long.MaxValue:E4})")
    {
        Location = location;
        GameLevel = gameLevel;
        ComputedValue = computedValue;
    }
}

/// <summary>
/// long 오버플로우 방지 유틸리티.
/// double -> long 변환 및 long 산술 연산 시 오버플로우를 감지하여
/// SimulationOverflowException을 발생시킵니다.
/// </summary>
internal static class OverflowGuard
{
    private const double SafeLongMax = 9.2E+18;

    /// <summary>
    /// double -> long 안전 변환. 범위 초과 시 예외 발생.
    /// </summary>
    internal static long ToLong(double value, string location, long level)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) ||
            value > SafeLongMax || value < -SafeLongMax)
        {
            throw new SimulationOverflowException(location, level, value);
        }
        return (long)value;
    }

    /// <summary>
    /// long + long 안전 덧셈. 오버플로우 시 예외 발생.
    /// </summary>
    internal static long Add(long a, long b, string location, long level)
    {
        if (b > 0 && a > long.MaxValue - b)
            throw new SimulationOverflowException(location, level, (double)a + b);
        if (b < 0 && a < long.MinValue - b)
            throw new SimulationOverflowException(location, level, (double)a + b);
        return a + b;
    }

    /// <summary>
    /// long * int 안전 곱셈. 오버플로우 시 예외 발생.
    /// </summary>
    internal static long Mul(long a, long b, string location, long level)
    {
        double product = (double)a * b;
        if (product > SafeLongMax || product < -SafeLongMax)
            throw new SimulationOverflowException(location, level, product);
        return a * b;
    }

    /// <summary>
    /// double 값이 long 범위를 초과하면 long.MaxValue 반환 (비용 계산용 안전 캐스트).
    /// 비용은 오버플로우 시 "구매 불가" 센티넬이므로 예외 대신 센티넬 반환.
    /// </summary>
    internal static long CostToLong(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value > SafeLongMax)
            return long.MaxValue;
        if (value < 0)
            return 0;
        return (long)Math.Ceiling(value);
    }
}
