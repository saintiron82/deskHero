using System;

namespace DeskWarrior.Security
{
    /// <summary>
    /// 메모리 보호 값 래퍼
    /// XOR 난독화로 CheatEngine 등의 메모리 스캔 방지
    /// </summary>
    /// <typeparam name="T">int, long, float, double 등 숫자 타입</typeparam>
    public class ProtectedValue<T> where T : struct
    {
        private long _obfuscatedValue;
        private long _key;
        private readonly Random _random = new();

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public ProtectedValue()
        {
            _key = GenerateKey();
            _obfuscatedValue = _key; // 초기값 0
        }

        /// <summary>
        /// 초기값 지정 생성자
        /// </summary>
        public ProtectedValue(T initialValue)
        {
            _key = GenerateKey();
            SetInternal(initialValue);
        }

        /// <summary>
        /// 실제 값 (읽기/쓰기)
        /// </summary>
        public T Value
        {
            get => GetInternal();
            set => SetInternal(value);
        }

        /// <summary>
        /// 암시적 변환 (ProtectedValue -> T)
        /// </summary>
        public static implicit operator T(ProtectedValue<T> pv) => pv.Value;

        #region Private Methods

        private long GenerateKey()
        {
            byte[] buffer = new byte[8];
            _random.NextBytes(buffer);
            return BitConverter.ToInt64(buffer);
        }

        private void SetInternal(T value)
        {
            // 새 키 생성 (매 쓰기마다 키 변경으로 추적 어렵게)
            _key = GenerateKey();

            long rawValue = ConvertToLong(value);
            _obfuscatedValue = rawValue ^ _key;
        }

        private T GetInternal()
        {
            long rawValue = _obfuscatedValue ^ _key;
            return ConvertFromLong(rawValue);
        }

        private static long ConvertToLong(T value)
        {
            return value switch
            {
                int i => i,
                long l => l,
                float f => BitConverter.SingleToInt32Bits(f),
                double d => BitConverter.DoubleToInt64Bits(d),
                short s => s,
                byte b => b,
                uint ui => ui,
                ulong ul => (long)ul,
                _ => throw new NotSupportedException($"Type {typeof(T)} is not supported")
            };
        }

        private static T ConvertFromLong(long value)
        {
            if (typeof(T) == typeof(int))
                return (T)(object)(int)value;
            if (typeof(T) == typeof(long))
                return (T)(object)value;
            if (typeof(T) == typeof(float))
                return (T)(object)BitConverter.Int32BitsToSingle((int)value);
            if (typeof(T) == typeof(double))
                return (T)(object)BitConverter.Int64BitsToDouble(value);
            if (typeof(T) == typeof(short))
                return (T)(object)(short)value;
            if (typeof(T) == typeof(byte))
                return (T)(object)(byte)value;
            if (typeof(T) == typeof(uint))
                return (T)(object)(uint)value;
            if (typeof(T) == typeof(ulong))
                return (T)(object)(ulong)value;

            throw new NotSupportedException($"Type {typeof(T)} is not supported");
        }

        #endregion

        #region Arithmetic Operations

        /// <summary>
        /// 값 증가
        /// </summary>
        public void Add(T amount)
        {
            long current = ConvertToLong(GetInternal());
            long addValue = ConvertToLong(amount);
            SetInternal(ConvertFromLong(current + addValue));
        }

        /// <summary>
        /// 값 감소
        /// </summary>
        public void Subtract(T amount)
        {
            long current = ConvertToLong(GetInternal());
            long subValue = ConvertToLong(amount);
            SetInternal(ConvertFromLong(current - subValue));
        }

        /// <summary>
        /// 0으로 리셋
        /// </summary>
        public void Reset()
        {
            _key = GenerateKey();
            _obfuscatedValue = _key;
        }

        #endregion

        public override string ToString()
        {
            return Value.ToString() ?? "null";
        }
    }

    /// <summary>
    /// 편의를 위한 특수화된 타입 별칭
    /// </summary>
    public class ProtectedInt : ProtectedValue<int>
    {
        public ProtectedInt() : base() { }
        public ProtectedInt(int value) : base(value) { }

        public static ProtectedInt operator +(ProtectedInt a, int b)
        {
            var result = new ProtectedInt(a.Value + b);
            return result;
        }

        public static ProtectedInt operator -(ProtectedInt a, int b)
        {
            var result = new ProtectedInt(a.Value - b);
            return result;
        }
    }

    public class ProtectedLong : ProtectedValue<long>
    {
        public ProtectedLong() : base() { }
        public ProtectedLong(long value) : base(value) { }

        public static ProtectedLong operator +(ProtectedLong a, long b)
        {
            var result = new ProtectedLong(a.Value + b);
            return result;
        }

        public static ProtectedLong operator -(ProtectedLong a, long b)
        {
            var result = new ProtectedLong(a.Value - b);
            return result;
        }
    }

    public class ProtectedDouble : ProtectedValue<double>
    {
        public ProtectedDouble() : base() { }
        public ProtectedDouble(double value) : base(value) { }
    }
}
