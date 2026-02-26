using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DeskWarrior.Helpers
{
    /// <summary>
    /// Hue Shift 유틸리티 - 런타임 색상 변환 및 캐싱
    /// </summary>
    public static class HueShiftHelper
    {
        // 캐시 키 구조체
        private struct CacheKey
        {
            public string ImagePath { get; init; }
            public float Hue { get; init; }

            public override bool Equals(object? obj)
            {
                if (obj is not CacheKey other) return false;
                return ImagePath == other.ImagePath && Math.Abs(Hue - other.Hue) < 0.01f;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ImagePath, (int)(Hue * 100));
            }
        }

        private static readonly Dictionary<CacheKey, BitmapSource> _cache = new();
        private static readonly Queue<CacheKey> _lruQueue = new();
        private const int MAX_CACHE_SIZE = 200;
        private static readonly object _cacheLock = new();

        /// <summary>
        /// Hue shift 적용 (캐싱 포함)
        /// </summary>
        /// <param name="source">원본 이미지</param>
        /// <param name="imagePath">이미지 경로 (캐시 키)</param>
        /// <param name="hueDegrees">Hue 각도 (0-360)</param>
        /// <returns>색상 변환된 이미지</returns>
        public static BitmapSource ApplyHueShift(BitmapSource source, string imagePath, float hueDegrees)
        {
            // 정규화
            hueDegrees = hueDegrees % 360f;
            if (hueDegrees < 0) hueDegrees += 360f;

            // Hue = 0이면 스킵
            if (Math.Abs(hueDegrees) < 0.01f)
                return source;

            // 캐시 확인
            var cacheKey = new CacheKey { ImagePath = imagePath, Hue = hueDegrees };
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(cacheKey, out var cached))
                    return cached;
            }

            try
            {
                // WriteableBitmap으로 변환
                var writableBitmap = new WriteableBitmap(source);
                writableBitmap.Lock();

                try
                {
                    unsafe
                    {
                        int width = writableBitmap.PixelWidth;
                        int height = writableBitmap.PixelHeight;
                        int stride = writableBitmap.BackBufferStride;
                        byte* pixels = (byte*)writableBitmap.BackBuffer.ToPointer();

                        // 픽셀별 처리
                        for (int y = 0; y < height; y++)
                        {
                            byte* row = pixels + (y * stride);
                            for (int x = 0; x < width; x++)
                            {
                                int offset = x * 4; // BGRA
                                byte b = row[offset + 0];
                                byte g = row[offset + 1];
                                byte r = row[offset + 2];
                                byte a = row[offset + 3];

                                if (a == 0) continue; // 투명 픽셀 스킵

                                // RGB → HSV → Hue Shift → RGB
                                RgbToHsv(r, g, b, out float h, out float s, out float v);
                                h = (h + hueDegrees) % 360f;
                                if (h < 0) h += 360f;
                                HsvToRgb(h, s, v, out r, out g, out b);

                                row[offset + 0] = b;
                                row[offset + 1] = g;
                                row[offset + 2] = r;
                            }
                        }

                        writableBitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
                    }
                }
                finally
                {
                    writableBitmap.Unlock();
                }

                writableBitmap.Freeze(); // 성능 최적화

                // 캐시에 추가 (LRU)
                lock (_cacheLock)
                {
                    if (_cache.Count >= MAX_CACHE_SIZE)
                    {
                        var oldestKey = _lruQueue.Dequeue();
                        _cache.Remove(oldestKey);
                    }
                    _cache[cacheKey] = writableBitmap;
                    _lruQueue.Enqueue(cacheKey);
                }

                return writableBitmap;
            }
            catch (Exception ex)
            {
                Logger.Log($"[HueShift] Failed to apply hue shift ({hueDegrees}°) to {imagePath}: {ex.Message}");
                return source; // 폴백
            }
        }

        /// <summary>
        /// RGB를 HSV로 변환
        /// </summary>
        private static void RgbToHsv(byte r, byte g, byte b, out float h, out float s, out float v)
        {
            float rf = r / 255f;
            float gf = g / 255f;
            float bf = b / 255f;

            float max = Math.Max(rf, Math.Max(gf, bf));
            float min = Math.Min(rf, Math.Min(gf, bf));
            float delta = max - min;

            // Hue
            if (delta == 0)
                h = 0;
            else if (max == rf)
                h = 60f * (((gf - bf) / delta) % 6);
            else if (max == gf)
                h = 60f * (((bf - rf) / delta) + 2);
            else
                h = 60f * (((rf - gf) / delta) + 4);

            if (h < 0) h += 360f;

            // Saturation
            s = max == 0 ? 0 : delta / max;

            // Value
            v = max;
        }

        /// <summary>
        /// HSV를 RGB로 변환
        /// </summary>
        private static void HsvToRgb(float h, float s, float v, out byte r, out byte g, out byte b)
        {
            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = v - c;

            float rf, gf, bf;
            if (h < 60)
                { rf = c; gf = x; bf = 0; }
            else if (h < 120)
                { rf = x; gf = c; bf = 0; }
            else if (h < 180)
                { rf = 0; gf = c; bf = x; }
            else if (h < 240)
                { rf = 0; gf = x; bf = c; }
            else if (h < 300)
                { rf = x; gf = 0; bf = c; }
            else
                { rf = c; gf = 0; bf = x; }

            r = (byte)((rf + m) * 255);
            g = (byte)((gf + m) * 255);
            b = (byte)((bf + m) * 255);
        }

        /// <summary>
        /// 캐시 초기화
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _cache.Clear();
                _lruQueue.Clear();
            }
        }

        /// <summary>
        /// 캐시 통계 조회
        /// </summary>
        public static (int Count, int MaxSize) GetCacheStats()
        {
            lock (_cacheLock)
            {
                return (_cache.Count, MAX_CACHE_SIZE);
            }
        }
    }
}
