using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows.Media;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 사운드 효과 관리 클래스
    /// </summary>
    public class SoundManager : IDisposable
    {
        #region Fields

        private readonly Dictionary<string, MediaPlayer> _sounds = new();
        private bool _enabled = true;
        private double _volume = 0.5;

        #endregion

        #region Properties

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public double Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0, 1);
                foreach (var player in _sounds.Values)
                {
                    player.Volume = _volume;
                }
            }
        }

        #endregion

        #region Constructor

        public SoundManager()
        {
            LoadSounds();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 사운드 재생
        /// </summary>
        public void Play(SoundType type)
        {
            if (!_enabled) return;

            string key = type.ToString();
            if (_sounds.TryGetValue(key, out var player))
            {
                player.Position = TimeSpan.Zero;
                player.Volume = _volume;
                player.Play();
            }
            else
            {
                // 사운드 파일 없으면 시스템 사운드 사용
                PlaySystemSound(type);
            }
        }

        public void Dispose()
        {
            foreach (var player in _sounds.Values)
            {
                player.Stop();
                player.Close();
            }
            _sounds.Clear();
        }

        #endregion

        #region Private Methods

        private void LoadSounds()
        {
            var soundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds");
            
            if (!Directory.Exists(soundDir))
            {
                Directory.CreateDirectory(soundDir);
                return;
            }

            // 사운드 파일 로드 시도
            foreach (SoundType type in Enum.GetValues<SoundType>())
            {
                string path = Path.Combine(soundDir, $"{type}.wav");
                if (File.Exists(path))
                {
                    var player = new MediaPlayer();
                    player.Open(new Uri(path));
                    player.Volume = _volume;
                    _sounds[type.ToString()] = player;
                }
            }
        }

        private void PlaySystemSound(SoundType type)
        {
            // 사운드 파일 없을 때 시스템 사운드 폴백
            switch (type)
            {
                case SoundType.Hit:
                    // 짧은 딸깍 소리
                    SystemSounds.Asterisk.Play();
                    break;
                case SoundType.Defeat:
                    SystemSounds.Exclamation.Play();
                    break;
                case SoundType.GameOver:
                    SystemSounds.Hand.Play();
                    break;
                case SoundType.Upgrade:
                    SystemSounds.Beep.Play();
                    break;
                case SoundType.BossAppear:
                    SystemSounds.Question.Play();
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// 사운드 타입
    /// </summary>
    public enum SoundType
    {
        Hit,       // 공격 시
        Defeat,    // 몬스터 처치
        GameOver,  // 하드 리셋
        Upgrade,   // 업그레이드
        BossAppear // 보스 등장
    }
}
