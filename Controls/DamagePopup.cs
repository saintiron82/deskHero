using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DeskWarrior.Controls
{
    /// <summary>
    /// 데미지 숫자 팝업 컨트롤
    /// </summary>
    public class DamagePopup : TextBlock
    {
        public DamagePopup(int damage, bool isCritical = false)
        {
            Text = damage.ToString();
            FontSize = isCritical ? 18 : 14;
            FontWeight = FontWeights.Bold;
            Foreground = isCritical ? Brushes.Yellow : Brushes.White;
            
            // 그림자 효과
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                ShadowDepth = 1,
                BlurRadius = 2,
                Color = Colors.Black
            };
            
            RenderTransformOrigin = new Point(0.5, 0.5);
            RenderTransform = new TranslateTransform();
        }

        /// <summary>
        /// 팝업 애니메이션 시작
        /// </summary>
        public void Animate(Action onComplete)
        {
            var transform = (TranslateTransform)RenderTransform;
            
            // 위로 떠오르는 애니메이션
            var moveUp = new DoubleAnimation
            {
                From = 0,
                To = -30,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            // 페이드아웃
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(600),
                BeginTime = TimeSpan.FromMilliseconds(200)
            };
            fadeOut.Completed += (s, e) => onComplete?.Invoke();
            
            transform.BeginAnimation(TranslateTransform.YProperty, moveUp);
            BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
