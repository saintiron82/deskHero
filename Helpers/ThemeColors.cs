using System.Windows.Media;

namespace DeskWarrior.Helpers
{
    /// <summary>
    /// 앱 전체에서 사용되는 테마 색상 상수
    /// </summary>
    public static class ThemeColors
    {
        // Primary Colors
        public static readonly Color Gold = Color.FromRgb(255, 215, 0);
        public static readonly Color Orange = Color.FromRgb(255, 170, 0);
        public static readonly Color Cyan = Color.FromRgb(0, 206, 209);

        // UI Background Colors
        public static readonly Color DarkGray = Color.FromRgb(51, 51, 51);
        public static readonly Color LightGray = Color.FromRgb(136, 136, 136);
        public static readonly Color MediumGray = Color.FromRgb(170, 170, 170);
        public static readonly Color MainBackground = Color.FromRgb(0x1a, 0x1a, 0x2e);

        // Status Colors
        public static readonly Color Success = Color.FromRgb(136, 255, 136);
        public static readonly Color Warning = Color.FromRgb(255, 255, 0);
        public static readonly Color Danger = Color.FromRgb(255, 100, 100);

        // HP Bar Colors
        public static readonly Color HpFull = Color.FromRgb(0, 255, 0);
        public static readonly Color HpMedium = Color.FromRgb(255, 255, 0);
        public static readonly Color HpLow = Color.FromRgb(255, 0, 0);

        // Timer Colors
        public static readonly Color TimerNormal = Color.FromRgb(135, 206, 235);
        public static readonly Color TimerWarning = Color.FromRgb(255, 255, 0);
        public static readonly Color TimerDanger = Color.FromRgb(255, 0, 0);

        // Brush Helpers
        public static SolidColorBrush ToBrush(this Color color) => new(color);
        public static SolidColorBrush ToBrush(this Color color, double opacity) => new(color) { Opacity = opacity };
    }
}
