using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Media;
using DeskWarrior.Helpers;
using DeskWarrior.Interfaces;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 사운드 효과 관리 클래스 (사운드팩 시스템)
    /// </summary>
    public class SoundManager : ISoundManager
    {
        #region Fields

        private readonly Dictionary<SoundType, MediaPlayer> _sounds = new();
        private readonly Dictionary<string, SoundPackConfig> _soundPacks = new();
        private SoundPackConfig? _currentPack;
        private SoundPackConfig? _fallbackPack;
        private string _currentPackId = "default";
        private bool _enabled = true;
        private double _volume = 0.2;
        private readonly string _soundPacksPath;
        private readonly string _customPacksPath;

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
                UpdateAllVolumes();
            }
        }

        public string CurrentSoundPackId => _currentPackId;

        public IReadOnlyList<SoundPackInfo> AvailableSoundPacks =>
            _soundPacks.Values.Select(p => p.ToInfo()).ToList().AsReadOnly();

        #endregion

        #region Events

        public event EventHandler<SoundPackChangedEventArgs>? SoundPackChanged;

        #endregion

        #region Constructor

        public SoundManager()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _soundPacksPath = Path.Combine(baseDir, "Assets", "Sounds", "SoundPacks");
            _customPacksPath = Path.Combine(baseDir, "Assets", "Sounds", "Custom");

            EnsureDirectoriesExist();
            RefreshSoundPacks();
            LoadSoundPack("default");
        }

        #endregion

        #region Public Methods

        public void Play(SoundType type)
        {
            if (!_enabled) return;

            // 레거시 Hit -> KeyboardHit 매핑
#pragma warning disable CS0618
            if (type == SoundType.Hit)
                type = SoundType.KeyboardHit;
#pragma warning restore CS0618

            if (_sounds.TryGetValue(type, out var player))
            {
                player.Position = TimeSpan.Zero;

                // 볼륨 배율 적용
                double volumeMultiplier = _currentPack?.GetVolumeMultiplier(type) ?? 1.0;
                player.Volume = _volume * volumeMultiplier;

                player.Play();
            }
            else
            {
                PlaySystemSound(type);
            }
        }

        public bool ChangeSoundPack(string packId)
        {
            if (!_soundPacks.ContainsKey(packId))
                return false;

            if (packId == _currentPackId)
                return true;

            string oldPackId = _currentPackId;

            UnloadCurrentPack();

            if (LoadSoundPack(packId))
            {
                _currentPackId = packId;
                SoundPackChanged?.Invoke(this, new SoundPackChangedEventArgs
                {
                    OldPackId = oldPackId,
                    NewPackId = packId
                });
                return true;
            }

            // 실패 시 이전 팩 복원
            LoadSoundPack(oldPackId);
            return false;
        }

        public void RefreshSoundPacks()
        {
            _soundPacks.Clear();

            // 빌트인 팩 스캔
            ScanBuiltinPacks();

            // 커스텀 팩 스캔
            ScanCustomPacks();

            // 기본 팩이 없으면 프로시저럴 생성
            if (!_soundPacks.ContainsKey("default"))
            {
                GenerateDefaultPacks();
            }
        }

        public void Dispose()
        {
            UnloadCurrentPack();
        }

        #endregion

        #region Private Methods

        private void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(_soundPacksPath);
            Directory.CreateDirectory(_customPacksPath);
        }

        private void ScanBuiltinPacks()
        {
            if (!Directory.Exists(_soundPacksPath)) return;

            foreach (var dir in Directory.GetDirectories(_soundPacksPath))
            {
                var packJsonPath = Path.Combine(dir, "pack.json");
                if (File.Exists(packJsonPath))
                {
                    try
                    {
                        var config = SoundPackConfig.LoadFromFile(packJsonPath);
                        if (config != null)
                        {
                            config.FolderPath = dir;
                            config.IsBuiltin = true;
                            _soundPacks[config.Id] = config;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load sound pack: {dir} - {ex.Message}");
                    }
                }
            }
        }

        private void ScanCustomPacks()
        {
            if (!Directory.Exists(_customPacksPath)) return;

            foreach (var dir in Directory.GetDirectories(_customPacksPath))
            {
                var packJsonPath = Path.Combine(dir, "pack.json");
                if (File.Exists(packJsonPath))
                {
                    try
                    {
                        var config = SoundPackConfig.LoadFromFile(packJsonPath);
                        if (config != null)
                        {
                            config.FolderPath = dir;
                            config.IsBuiltin = false;
                            config.IsCustom = true;

                            // 커스텀 팩 ID 충돌 방지
                            string customId = $"custom_{config.Id}";
                            config.Id = customId;
                            _soundPacks[customId] = config;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load custom sound pack: {dir} - {ex.Message}");
                    }
                }
            }
        }

        private void GenerateDefaultPacks()
        {
            // 4개의 기본 사운드팩 생성
            GenerateSoundPack("default", "Default", "기본", SoundStyle.Default);
            GenerateSoundPack("mechanical", "Mechanical Keyboard", "기계식 키보드", SoundStyle.Mechanical);
            GenerateSoundPack("soft", "Soft", "부드러운", SoundStyle.Soft);
            GenerateSoundPack("8bit", "8-bit Retro", "8비트 레트로", SoundStyle.EightBit);
        }

        private void GenerateSoundPack(string id, string name, string nameKo, SoundStyle style)
        {
            var packPath = Path.Combine(_soundPacksPath, id);
            Directory.CreateDirectory(packPath);

            // 사운드 파일 생성
            SoundGenerator.GenerateSoundPack(packPath, style);

            // pack.json 생성
            var config = new SoundPackConfig
            {
                Id = id,
                Name = name,
                NameKo = nameKo,
                Description = $"{name} sound pack",
                DescriptionKo = $"{nameKo} 사운드팩",
                Author = "DeskHero",
                IsBuiltin = true,
                FolderPath = packPath
            };
            config.SetupDefaultMappings();
            config.SaveToFile(Path.Combine(packPath, "pack.json"));

            _soundPacks[id] = config;
        }

        private bool LoadSoundPack(string packId)
        {
            if (!_soundPacks.TryGetValue(packId, out var config))
                return false;

            _currentPack = config;

            // fallback 팩 로드 (default)
            if (packId != "default" && _soundPacks.TryGetValue("default", out var defaultPack))
            {
                _fallbackPack = defaultPack;
            }

            foreach (SoundType type in Enum.GetValues<SoundType>())
            {
#pragma warning disable CS0618
                if (type == SoundType.Hit) continue;
#pragma warning restore CS0618

                var soundPath = config.GetSoundPath(type);

                // fallback 사용
                if (string.IsNullOrEmpty(soundPath) || !File.Exists(soundPath))
                {
                    soundPath = _fallbackPack?.GetSoundPath(type);
                }

                if (!string.IsNullOrEmpty(soundPath) && File.Exists(soundPath))
                {
                    try
                    {
                        var player = new MediaPlayer();
                        player.Open(new Uri(soundPath));
                        player.Volume = _volume * config.GetVolumeMultiplier(type);
                        _sounds[type] = player;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load sound: {soundPath} - {ex.Message}");
                    }
                }
            }

            return true;
        }

        private void UnloadCurrentPack()
        {
            foreach (var player in _sounds.Values)
            {
                player.Stop();
                player.Close();
            }
            _sounds.Clear();
            _currentPack = null;
            _fallbackPack = null;
        }

        private void UpdateAllVolumes()
        {
            foreach (var kvp in _sounds)
            {
                double volumeMultiplier = _currentPack?.GetVolumeMultiplier(kvp.Key) ?? 1.0;
                kvp.Value.Volume = _volume * volumeMultiplier;
            }
        }

        private void PlaySystemSound(SoundType type)
        {
            switch (type)
            {
                case SoundType.KeyboardHit:
                case SoundType.MouseClick:
                case SoundType.Critical:
                    SystemSounds.Asterisk.Play();
                    break;
                case SoundType.Defeat:
                case SoundType.BossDefeat:
                    SystemSounds.Exclamation.Play();
                    break;
                case SoundType.GameOver:
                    SystemSounds.Hand.Play();
                    break;
                case SoundType.Upgrade:
                case SoundType.LevelUp:
                case SoundType.OfflineReward:
                    SystemSounds.Beep.Play();
                    break;
                case SoundType.BossAppear:
                    SystemSounds.Question.Play();
                    break;
            }
        }

        #endregion
    }
}
