using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace DeskWarrior.Helpers
{
    /// <summary>
    /// 공통 애니메이션 생성 헬퍼
    /// </summary>
    public static class AnimationHelper
    {
        /// <summary>
        /// 페이드 인 애니메이션 생성
        /// </summary>
        public static DoubleAnimation CreateFadeIn(double durationMs = 150)
        {
            return new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(durationMs)
            };
        }

        /// <summary>
        /// 페이드 아웃 애니메이션 생성
        /// </summary>
        public static DoubleAnimation CreateFadeOut(double durationMs = 150)
        {
            return new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(durationMs)
            };
        }

        /// <summary>
        /// 깜빡임 애니메이션 생성
        /// </summary>
        public static DoubleAnimation CreateBlink(double from = 1.0, double to = 0.3, double durationMs = 300)
        {
            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
        }

        /// <summary>
        /// 값 변경 애니메이션 생성 (Easing 포함)
        /// </summary>
        public static DoubleAnimation CreateValueAnimation(double to, double durationMs = 300, IEasingFunction? easing = null)
        {
            return new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = easing ?? new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
        }

        /// <summary>
        /// 흔들림 애니메이션 생성
        /// </summary>
        public static DoubleAnimation CreateShake(double from, double to = 0, double durationMs = 50)
        {
            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
        }

        /// <summary>
        /// 플래시 효과 애니메이션 생성
        /// </summary>
        public static DoubleAnimation CreateFlash(double from = 1.0, double to = 0.5, double durationMs = 80)
        {
            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                AutoReverse = true
            };
        }
    }
}
