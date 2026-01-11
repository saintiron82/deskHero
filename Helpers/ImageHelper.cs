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
        /// 초록색 배경을 투명으로 변환 (크로마 키)
        /// </summary>
        public static BitmapSource RemoveGreenBackground(BitmapSource source)
        {
            if (source == null) return null!;

            // BGRA 형식으로 변환
            var formatted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            
            int width = formatted.PixelWidth;
            int height = formatted.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            
            formatted.CopyPixels(pixels, stride, 0);
            
            // 크로마 키 처리 (초록색 → 투명)
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                
                // 초록색 판정 (녹색이 지배적이고 밝은 경우)
                if (g > 150 && g > r + 50 && g > b + 50)
                {
                    pixels[i + 3] = 0; // 알파를 0으로 (투명)
                }
            }
            
            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        }
        
        /// <summary>
        /// pack URI에서 이미지 로드하고 크로마 키 처리
        /// </summary>
        public static BitmapSource LoadWithChromaKey(string packUri)
        {
            try
            {
                var bitmap = new BitmapImage(new Uri(packUri, UriKind.Absolute));
                return RemoveGreenBackground(bitmap);
            }
            catch
            {
                return null!;
            }
        }
    }
}
