using System;
using System.IO;
using System.Text;

namespace DeskWarrior.Helpers
{
    /// <summary>
    /// WAV 오디오 파일 생성 헬퍼
    /// </summary>
    public static class SoundGenerator
    {
        private const int SampleRate = 44100;
        private const short BitsPerSample = 16;

        public static void GenerateAllSounds(string outputDir)
        {
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            // 1. Hit: 아주 짧고 가벼운 '톡' (0.05초)
            CreateWavFile(Path.Combine(outputDir, "Hit.wav"), 0.05, (t) => 
            {
                // 800Hz 사인파 + 빠른 감소
                double frequency = 800;
                double amplitude = Math.Exp(-t * 80); // 가파른 감쇠
                return Math.Sin(2 * Math.PI * frequency * t) * amplitude * 0.3;
            });

            // 2. Upgrade: 부드러운 '띠롱' (0.3초)
            CreateWavFile(Path.Combine(outputDir, "Upgrade.wav"), 0.3, (t) =>
            {
                // 두 개의 음 (도->미)
                double freq = t < 0.15 ? 523.25 : 659.25; // C5 -> E5
                double amp = t < 0.15 
                    ? Math.Sin(t * Math.PI / 0.15) // 첫 음 포락선
                    : Math.Sin((t - 0.15) * Math.PI / 0.15); // 두 번째 음 포락선
                return Math.Sin(2 * Math.PI * freq * t) * amp * 0.4;
            });

            // 3. Defeat: 작고 낮은 '웅~' (0.2초)
            CreateWavFile(Path.Combine(outputDir, "Defeat.wav"), 0.2, (t) =>
            {
                // 저음이 부드럽게 퍼짐
                double freq = 200 - t * 400; // 200Hz -> 120Hz
                double amp = Math.Sin(t * Math.PI / 0.2); // 반원 형태 포락선
                return Math.Sin(2 * Math.PI * freq * t) * amp * 0.4;
            });
            
            // 4. GameOver: 하강음 '피유우' (0.5초)
            CreateWavFile(Path.Combine(outputDir, "GameOver.wav"), 0.5, (t) =>
            {
                double freq = 400 * (1 - t / 0.5); // 400Hz -> 0Hz
                return Math.Sin(2 * Math.PI * freq * t) * 0.3;
            });

            // 5. BossAppear: 묵직한 '둥!' (0.8초)
            CreateWavFile(Path.Combine(outputDir, "BossAppear.wav"), 0.8, (t) =>
            {
                double freq = 100;
                double amp = Math.Exp(-t * 5); // 긴 감쇠
                // 배음 추가로 풍성하게
                return (Math.Sin(2 * Math.PI * freq * t) + 
                        0.5 * Math.Sin(2 * Math.PI * freq * 0.5 * t)) * amp * 0.5;
            });
        }

        private static void CreateWavFile(string filepath, double duration, Func<double, double> signalGenerator)
        {
            int numSamples = (int)(SampleRate * duration);
            using (var stream = new FileStream(filepath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                // WAV Header
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + numSamples * 2); // File size - 8
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // Chunk size
                writer.Write((short)1); // Audio format (1 = PCM)
                writer.Write((short)1); // Channels (Mono)
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2); // Byte rate
                writer.Write((short)2); // Block align
                writer.Write(BitsPerSample);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(numSamples * 2); // Data chunk size

                // Data
                for (int i = 0; i < numSamples; i++)
                {
                    double t = (double)i / SampleRate;
                    double sample = signalGenerator(t);
                    
                    // Clipping 방지
                    sample = Math.Clamp(sample, -1.0, 1.0);
                    
                    short shortSample = (short)(sample * short.MaxValue);
                    writer.Write(shortSample);
                }
            }
        }
    }
}
