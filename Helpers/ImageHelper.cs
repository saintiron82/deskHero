using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DeskWarrior.Helpers
{
    /// <summary>
    /// 이미지 처리 헬퍼 (크로마 키 처리 등)
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// 이미지 로드 (투명도가 적용된 PNG를 그대로 로드)
        /// 이전의 'LoadWithChromaKey' 메서드를 대체합니다.
        /// </summary>
        public static BitmapSource LoadWithChromaKey(string packUri)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(packUri, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // 메모리에 로드
                bitmap.EndInit();
                return bitmap;
            }
            catch
            {
                return null!;
            }
        }
    }
}
