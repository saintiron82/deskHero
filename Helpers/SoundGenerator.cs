using System;
using System.IO;
using System.Text;

namespace DeskWarrior.Helpers
{
    /// <summary>
    /// 사운드 스타일 열거형
    /// </summary>
    public enum SoundStyle
    {
        Default,
        Mechanical,
        Soft,
        EightBit
    }

    /// <summary>
    /// WAV 오디오 파일 생성 헬퍼
    /// </summary>
    public static class SoundGenerator
    {
        private const int SampleRate = 44100;
        private const short BitsPerSample = 16;

        /// <summary>
        /// 사운드팩 생성 (스타일별)
        /// </summary>
        public static void GenerateSoundPack(string outputDir, SoundStyle style)
        {
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            // 키보드/마우스 입력 사운드
            GenerateKeyboardHit(Path.Combine(outputDir, "KeyboardHit.wav"), style);
            GenerateMouseClick(Path.Combine(outputDir, "MouseClick.wav"), style);

            // 게임 이벤트 사운드 (공통)
            GenerateDefeat(Path.Combine(outputDir, "Defeat.wav"), style);
            GenerateUpgrade(Path.Combine(outputDir, "Upgrade.wav"), style);
            GenerateGameOver(Path.Combine(outputDir, "GameOver.wav"), style);
            GenerateBossAppear(Path.Combine(outputDir, "BossAppear.wav"), style);
            GenerateCritical(Path.Combine(outputDir, "Critical.wav"), style);
            GenerateBossDefeat(Path.Combine(outputDir, "BossDefeat.wav"), style);
            GenerateCombo(Path.Combine(outputDir, "Combo.wav"), style);
            GenerateLevelUp(Path.Combine(outputDir, "LevelUp.wav"), style);
            GenerateOfflineReward(Path.Combine(outputDir, "OfflineReward.wav"), style);
            GenerateAchievement(Path.Combine(outputDir, "Achievement.wav"), style);
        }

        /// <summary>
        /// 레거시: 모든 사운드 생성 (기본 스타일)
        /// </summary>
        public static void GenerateAllSounds(string outputDir)
        {
            GenerateSoundPack(outputDir, SoundStyle.Default);
        }

        #region Input Sounds

        /// <summary>
        /// 키보드 히트 사운드 생성
        /// </summary>
        public static void GenerateKeyboardHit(string outputPath, SoundStyle style)
        {
            switch (style)
            {
                case SoundStyle.Mechanical:
                    // 기계식 키보드: 날카롭고 쫀득한 클릭
                    CreateWavFile(outputPath, 0.04, t =>
                    {
                        double freq = 1200 - t * 5000;
                        double amp = Math.Exp(-t * 100);
                        // 클릭감 추가 (고주파 노이즈)
                        double click = Math.Sin(2 * Math.PI * 3000 * t) * Math.Exp(-t * 200) * 0.15;
                        return (Math.Sin(2 * Math.PI * freq * t) * amp * 0.4) + click;
                    });
                    break;

                case SoundStyle.Soft:
                    // 부드러운: 멤브레인 키보드 느낌
                    CreateWavFile(outputPath, 0.06, t =>
                    {
                        double freq = 400;
                        double amp = Math.Exp(-t * 60);
                        return Math.Sin(2 * Math.PI * freq * t) * amp * 0.2;
                    });
                    break;

                case SoundStyle.EightBit:
                    // 8비트: 사각파 레트로 사운드
                    CreateSquareWave(outputPath, 0.03, 880, 0.25);
                    break;

                default:
                    // 기본: 800Hz, 0.05초
                    CreateWavFile(outputPath, 0.05, t =>
                    {
                        double freq = 800;
                        double amp = Math.Exp(-t * 80);
                        return Math.Sin(2 * Math.PI * freq * t) * amp * 0.3;
                    });
                    break;
            }
        }

        /// <summary>
        /// 마우스 클릭 사운드 생성
        /// </summary>
        public static void GenerateMouseClick(string outputPath, SoundStyle style)
        {
            switch (style)
            {
                case SoundStyle.Mechanical:
                    // 기계식: 톡 하는 마우스 버튼
                    CreateWavFile(outputPath, 0.03, t =>
                    {
                        double freq = 500;
                        double amp = Math.Exp(-t * 120);
                        return Math.Sin(2 * Math.PI * freq * t) * amp * 0.35;
                    });
                    break;

                case SoundStyle.Soft:
                    // 부드러운: 터치패드 탭 느낌
                    CreateWavFile(outputPath, 0.04, t =>
                    {
                        double freq = 300;
                        double amp = Math.Exp(-t * 80);
                        return Math.Sin(2 * Math.PI * freq * t) * amp * 0.15;
                    });
                    break;

                case SoundStyle.EightBit:
                    // 8비트: 셀렉트 사운드
                    CreateSquareWave(outputPath, 0.025, 660, 0.2);
                    break;

                default:
                    // 기본: 600Hz, 0.03초
                    CreateWavFile(outputPath, 0.03, t =>
                    {
                        double freq = 600;
                        double amp = Math.Exp(-t * 100);
                        return Math.Sin(2 * Math.PI * freq * t) * amp * 0.25;
                    });
                    break;
            }
        }

        #endregion

        #region Game Event Sounds

        private static void GenerateDefeat(string outputPath, SoundStyle style)
        {
            if (style == SoundStyle.EightBit)
            {
                // 8비트: 하강 칩튠
                CreateWavFile(outputPath, 0.2, t =>
                {
                    double freq = 300 - t * 500;
                    return SquareWave(freq, t) * 0.3 * Math.Exp(-t * 10);
                });
            }
            else
            {
                CreateWavFile(outputPath, 0.2, t =>
                {
                    double freq = 200 - t * 400;
                    double amp = Math.Sin(t * Math.PI / 0.2);
                    return Math.Sin(2 * Math.PI * freq * t) * amp * 0.4;
                });
            }
        }

        private static void GenerateUpgrade(string outputPath, SoundStyle style)
        {
            if (style == SoundStyle.EightBit)
            {
                // 8비트: 상승 아르페지오
                CreateWavFile(outputPath, 0.3, t =>
                {
                    double freq = t < 0.15 ? 440 : 554.37; // A4 -> C#5
                    double amp = t < 0.15
                        ? Math.Sin(t * Math.PI / 0.15)
                        : Math.Sin((t - 0.15) * Math.PI / 0.15);
                    return SquareWave(freq, t) * amp * 0.3;
                });
            }
            else
            {
                CreateWavFile(outputPath, 0.3, t =>
                {
                    double freq = t < 0.15 ? 523.25 : 659.25;
                    double amp = t < 0.15
                        ? Math.Sin(t * Math.PI / 0.15)
                        : Math.Sin((t - 0.15) * Math.PI / 0.15);
                    return Math.Sin(2 * Math.PI * freq * t) * amp * 0.4;
                });
            }
        }

        private static void GenerateGameOver(string outputPath, SoundStyle style)
        {
            if (style == SoundStyle.EightBit)
            {
                CreateWavFile(outputPath, 0.5, t =>
                {
                    double freq = 300 * (1 - t / 0.5);
                    return SquareWave(freq, t) * 0.25 * Math.Exp(-t * 2);
                });
            }
            else
            {
                CreateWavFile(outputPath, 0.5, t =>
                {
                    double freq = 400 * (1 - t / 0.5);
                    return Math.Sin(2 * Math.PI * freq * t) * 0.3;
                });
            }
        }

        private static void GenerateBossAppear(string outputPath, SoundStyle style)
        {
            if (style == SoundStyle.EightBit)
            {
                // 8비트: 저음 경고음
                CreateWavFile(outputPath, 0.8, t =>
                {
                    double freq = 80;
                    double amp = Math.Exp(-t * 3);
                    return (SquareWave(freq, t) + SquareWave(freq * 0.5, t) * 0.5) * amp * 0.4;
                });
            }
            else
            {
                CreateWavFile(outputPath, 0.8, t =>
                {
                    double freq = 100;
                    double amp = Math.Exp(-t * 5);
                    return (Math.Sin(2 * Math.PI * freq * t) +
                            0.5 * Math.Sin(2 * Math.PI * freq * 0.5 * t)) * amp * 0.5;
                });
            }
        }

        private static void GenerateCritical(string outputPath, SoundStyle style)
        {
            if (style == SoundStyle.EightBit)
            {
                // 8비트: 날카로운 챙
                CreateWavFile(outputPath, 0.1, t =>
                {
                    double freq = 1500 - t * 8000;
                    return SquareWave(freq, t) * Math.Exp(-t * 30) * 0.35;
                });
            }
            else
            {
                CreateWavFile(outputPath, 0.15, t =>
                {
                    double freq = 1200 - t * 2600;
                    double amp = Math.Exp(-t * 20);
                    return Math.Sin(2 * Math.PI * freq * t) * amp * 0.5;
                });
            }
        }

        private static void GenerateBossDefeat(string outputPath, SoundStyle style)
        {
            if (style == SoundStyle.EightBit)
            {
                // 8비트: 승리 팡파레
                CreateWavFile(outputPath, 0.6, t =>
                {
                    double freq;
                    if (t < 0.15) freq = 523.25;
                    else if (t < 0.3) freq = 659.25;
                    else if (t < 0.45) freq = 783.99;
                    else freq = 1046.50;

                    double localT = t % 0.15;
                    double amp = Math.Sin(localT * Math.PI / 0.15);
                    return SquareWave(freq, t) * amp * 0.3;
                });
            }
            else
            {
                CreateWavFile(outputPath, 0.6, t =>
                {
                    double freq = 150;
                    double amp = Math.Exp(-t * 4);
                    return (Math.Sin(2 * Math.PI * freq * t) * 0.6 +
                            Math.Sin(2 * Math.PI * freq * 2 * t) * 0.3 +
                            Math.Sin(2 * Math.PI * freq * 0.5 * t) * 0.4) * amp * 0.5;
                });
            }
        }

        private static void GenerateCombo(string outputPath, SoundStyle style)
        {
            if (style == SoundStyle.EightBit)
            {
                // 8비트: 빠른 상승음
                CreateWavFile(outputPath, 0.15, t =>
                {
                    double freq;
                    if (t < 0.05) freq = 440;
                    else if (t < 0.1) freq = 554.37;
                    else freq = 659.25;

                    double localT = t % 0.05;
                    double amp = Math.Sin(localT * Math.PI / 0.05);
                    return SquareWave(freq, t) * amp * 0.3;
                });
            }
            else
            {
                CreateWavFile(outputPath, 0.2, t =>
                {
                    double freq;
                    double amp;
                    if (t < 0.066)
                    {
                        freq = 400;
                        amp = Math.Sin(t * Math.PI / 0.066);
                    }
                    else if (t < 0.133)
                    {
                        freq = 600;
                        amp = Math.Sin((t - 0.066) * Math.PI / 0.066);
                    }
                    else
                    {
                        freq = 800;
                        amp = Math.Sin((t - 0.133) * Math.PI / 0.066);
                    }
                    return Math.Sin(2 * Math.PI * freq * t) * amp * 0.4;
                });
            }
        }

        private static void GenerateLevelUp(string outputPath, SoundStyle style)
        {
            if (style == SoundStyle.EightBit)
            {
                // 8비트: 클래식 레벨업
                CreateWavFile(outputPath, 0.4, t =>
                {
                    double freq;
                    if (t < 0.1) freq = 523.25;
                    else if (t < 0.2) freq = 659.25;
                    else if (t < 0.3) freq = 783.99;
                    else freq = 1046.50;

                    double localT = t % 0.1;
                    double amp = Math.Sin(localT * Math.PI / 0.1);
                    return SquareWave(freq, t) * amp * 0.3;
                });
            }
            else
            {
                CreateWavFile(outputPath, 0.4, t =>
                {
                    double freq;
                    if (t < 0.1) freq = 523.25;
                    else if (t < 0.2) freq = 659.25;
                    else if (t < 0.3) freq = 783.99;
                    else freq = 1046.50;

                    double segmentStart = (int)(t / 0.1) * 0.1;
                    double localT = t - segmentStart;
                    double amp = Math.Sin(localT * Math.PI / 0.1);
                    return Math.Sin(2 * Math.PI * freq * t) * amp * 0.4;
                });
            }
        }

        private static void GenerateOfflineReward(string outputPath, SoundStyle style)
        {
            if (style == SoundStyle.EightBit)
            {
                // 8비트: 코인 획득
                CreateWavFile(outputPath, 0.25, t =>
                {
                    double freq = t < 0.125 ? 1046.50 : 1318.51; // C6 -> E6
                    double amp = Math.Exp(-t * 10);
                    return SquareWave(freq, t) * amp * 0.25;
                });
            }
            else
            {
                CreateWavFile(outputPath, 0.3, t =>
                {
                    double freq1 = 2000;
                    double freq2 = 2500;
                    double amp = Math.Exp(-t * 12);
                    return (Math.Sin(2 * Math.PI * freq1 * t) * 0.5 +
                            Math.Sin(2 * Math.PI * freq2 * t) * 0.5) * amp * 0.3;
                });
            }
        }

        private static void GenerateAchievement(string outputPath, SoundStyle style)
        {
            if (style == SoundStyle.EightBit)
            {
                // 8비트: 업적 획득 팡파레
                CreateWavFile(outputPath, 0.5, t =>
                {
                    double freq;
                    if (t < 0.1) freq = 523.25;
                    else if (t < 0.2) freq = 659.25;
                    else if (t < 0.3) freq = 783.99;
                    else if (t < 0.4) freq = 1046.50;
                    else freq = 1318.51;

                    double localT = t % 0.1;
                    double amp = Math.Sin(localT * Math.PI / 0.1);
                    return SquareWave(freq, t) * amp * 0.3;
                });
            }
            else
            {
                // 밝고 화려한 업적 사운드
                CreateWavFile(outputPath, 0.5, t =>
                {
                    double freq;
                    if (t < 0.1) freq = 523.25;
                    else if (t < 0.2) freq = 659.25;
                    else if (t < 0.3) freq = 783.99;
                    else if (t < 0.4) freq = 1046.50;
                    else freq = 1318.51;

                    double localT = t % 0.1;
                    double amp = Math.Sin(localT * Math.PI / 0.1);
                    // 배음 추가로 풍성하게
                    return (Math.Sin(2 * Math.PI * freq * t) * 0.5 +
                            Math.Sin(2 * Math.PI * freq * 2 * t) * 0.25 +
                            Math.Sin(2 * Math.PI * freq * 0.5 * t) * 0.25) * amp * 0.4;
                });
            }
        }

        #endregion

        #region Wave Generation

        /// <summary>
        /// 사각파 계산
        /// </summary>
        private static double SquareWave(double frequency, double t)
        {
            return Math.Sin(2 * Math.PI * frequency * t) >= 0 ? 1.0 : -1.0;
        }

        /// <summary>
        /// 사각파 WAV 파일 생성
        /// </summary>
        private static void CreateSquareWave(string filepath, double duration, double frequency, double amplitude)
        {
            CreateWavFile(filepath, duration, t =>
            {
                double wave = SquareWave(frequency, t);
                double amp = Math.Exp(-t * 50); // 감쇠
                return wave * amplitude * amp;
            });
        }

        /// <summary>
        /// WAV 파일 생성
        /// </summary>
        private static void CreateWavFile(string filepath, double duration, Func<double, double> signalGenerator)
        {
            int numSamples = (int)(SampleRate * duration);
            using var stream = new FileStream(filepath, FileMode.Create);
            using var writer = new BinaryWriter(stream);

            // WAV Header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + numSamples * 2);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write(BitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(numSamples * 2);

            // Data
            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / SampleRate;
                double sample = signalGenerator(t);
                sample = Math.Clamp(sample, -1.0, 1.0);
                short shortSample = (short)(sample * short.MaxValue);
                writer.Write(shortSample);
            }
        }

        #endregion
    }
}
