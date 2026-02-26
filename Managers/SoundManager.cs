using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// 사운드 효과 관리 클래스 (카테고리별 사운드팩 시스템)
    /// </summary>
    public class SoundManager : ISoundManager
    {
        #region Fields

        private readonly Dictionary<SoundType, MediaPlayer> _sounds = new();
        private readonly Dictionary<string, SoundPackConfig> _soundPacks = new();
        private readonly Dictionary<string, string> _categoryPackIds = new();
        private readonly Dictionary<string, SoundPackConfig?> _categoryConfigs = new();
        private SoundPackConfig? _fallbackPack;
        private string _currentPackId = "default";
        private bool _enabled = true;
        private double _volume = 0.2;
        private readonly string _soundPacksPath;
        private readonly string _customPacksPath;

        // 사운드 테마
        private readonly Dictionary<string, SoundThemeData> _soundThemes = new();
        private SoundThemeData? _activeTheme;
        private readonly Dictionary<string, double> _themeOverrides = new();
        private string _currentThemeId = "default";

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

        public IReadOnlyDictionary<string, string> CategorySoundPackIds =>
            new ReadOnlyDictionary<string, string>(_categoryPackIds);

        public IReadOnlyList<SoundPackInfo> AvailableSoundPacks =>
            _soundPacks.Values.Select(p => p.ToInfo()).ToList().AsReadOnly();

        public IReadOnlyList<SoundThemeData> AvailableSoundThemes =>
            _soundThemes.Values.ToList().AsReadOnly();

        public string CurrentThemeId => _currentThemeId;

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
            LoadSoundThemes();

            // 기본 초기화: 모든 카테고리에 "default" 팩 적용
            foreach (var category in SoundCategory.AllCategories)
                _categoryPackIds[category] = "default";

            ReloadAllSounds();
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

            // 테마 체크: override 우선, 없으면 테마 기본값
            string key = type.ToString();
            double themeVol;
            if (_themeOverrides.TryGetValue(key, out var overrideVol))
                themeVol = overrideVol;
            else if (_activeTheme != null)
                themeVol = _activeTheme.GetVolume(key);
            else
                themeVol = 1.0;

            // 볼륨 0 이하 = 꺼짐
            if (themeVol <= 0) return;

            if (_sounds.TryGetValue(type, out var player))
            {
                player.Position = TimeSpan.Zero;

                // 최종 볼륨 = 글로벌 × 팩 배율 × 테마 사운드별 볼륨
                string category = SoundCategory.GetCategory(type);
                var config = _categoryConfigs.TryGetValue(category, out var c) ? c : null;
                player.Volume = _volume * (config?.GetVolumeMultiplier(type) ?? 1.0) * themeVol;

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

            string oldPackId = _currentPackId;
            _currentPackId = packId;

            // 모든 카테고리에 일괄 적용 (전역 모드)
            foreach (var category in SoundCategory.AllCategories)
                _categoryPackIds[category] = packId;

            ReloadAllSounds();

            if (oldPackId != packId)
            {
                SoundPackChanged?.Invoke(this, new SoundPackChangedEventArgs
                {
                    OldPackId = oldPackId,
                    NewPackId = packId
                });
            }

            return true;
        }

        public bool ChangeCategorySoundPack(string category, string packId)
        {
            if (!SoundCategory.CategorySoundTypes.ContainsKey(category))
                return false;
            if (!_soundPacks.ContainsKey(packId))
                return false;

            _categoryPackIds[category] = packId;
            _soundPacks.TryGetValue(packId, out var config);
            _categoryConfigs[category] = config;

            // 해당 카테고리의 사운드만 리로드
            foreach (var type in SoundCategory.CategorySoundTypes[category])
            {
                if (_sounds.TryGetValue(type, out var old))
                {
                    old.Stop();
                    old.Close();
                }
                _sounds.Remove(type);
                LoadSoundType(type, config);
            }

            return true;
        }

        public void ApplyCategorySettings(Dictionary<string, string> categoryPacks, string globalPack)
        {
            _currentPackId = globalPack;

            foreach (var category in SoundCategory.AllCategories)
            {
                if (categoryPacks.TryGetValue(category, out var packId)
                    && _soundPacks.ContainsKey(packId))
                {
                    _categoryPackIds[category] = packId;
                }
                else
                {
                    // 카테고리 설정이 없으면 전역 팩 사용
                    _categoryPackIds[category] = _soundPacks.ContainsKey(globalPack) ? globalPack : "default";
                }
            }

            ReloadAllSounds();
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

        public bool ApplySoundTheme(string themeId)
        {
            if (!_soundThemes.TryGetValue(themeId, out var theme))
                return false;

            _currentThemeId = themeId;
            _activeTheme = theme;
            _themeOverrides.Clear();
            return true;
        }

        public void ApplyThemeSettings(string themeId, Dictionary<string, double> overrides)
        {
            if (_soundThemes.TryGetValue(themeId, out var theme))
            {
                _currentThemeId = themeId;
                _activeTheme = theme;
            }

            _themeOverrides.Clear();
            foreach (var kvp in overrides)
                _themeOverrides[kvp.Key] = kvp.Value;
        }

        public void SetSoundTypeOverride(string soundTypeKey, double volume)
        {
            _themeOverrides[soundTypeKey] = volume;
        }

        public void Dispose()
        {
            UnloadAllSounds();
        }

        #endregion

        #region Private Methods

        private void LoadSoundThemes()
        {
            _soundThemes.Clear();
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "SoundThemes.json");
            var file = SoundThemesFile.LoadFromFile(configPath);
            if (file != null)
            {
                foreach (var theme in file.Themes)
                    _soundThemes[theme.Id] = theme;
            }

            _soundThemes.TryGetValue("default", out _activeTheme);
        }

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

        private void ReloadAllSounds()
        {
            UnloadAllSounds();

            // fallback 팩 로드
            _soundPacks.TryGetValue("default", out _fallbackPack);

            // 카테고리별 로드
            foreach (var category in SoundCategory.AllCategories)
            {
                string packId = _categoryPackIds.TryGetValue(category, out var id) ? id : "default";
                _soundPacks.TryGetValue(packId, out var config);
                _categoryConfigs[category] = config;

                foreach (var type in SoundCategory.CategorySoundTypes[category])
                {
                    LoadSoundType(type, config);
                }
            }
        }

        private void LoadSoundType(SoundType type, SoundPackConfig? config)
        {
#pragma warning disable CS0618
            if (type == SoundType.Hit) return;
#pragma warning restore CS0618

            var soundPath = config?.GetSoundPath(type);

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
                    player.Volume = _volume * (config?.GetVolumeMultiplier(type) ?? 1.0);
                    _sounds[type] = player;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load sound: {soundPath} - {ex.Message}");
                }
            }
        }

        private void UnloadAllSounds()
        {
            foreach (var player in _sounds.Values)
            {
                player.Stop();
                player.Close();
            }
            _sounds.Clear();
            _categoryConfigs.Clear();
            _fallbackPack = null;
        }

        private void UpdateAllVolumes()
        {
            foreach (var kvp in _sounds)
            {
                string key = kvp.Key.ToString();
                double themeVol;
                if (_themeOverrides.TryGetValue(key, out var ov))
                    themeVol = ov;
                else if (_activeTheme != null)
                    themeVol = _activeTheme.GetVolume(key);
                else
                    themeVol = 1.0;

                string category = SoundCategory.GetCategory(kvp.Key);
                var config = _categoryConfigs.TryGetValue(category, out var c) ? c : null;
                kvp.Value.Volume = _volume * (config?.GetVolumeMultiplier(kvp.Key) ?? 1.0) * themeVol;
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
                case SoundType.GoldenGoblinAppear:
                    SystemSounds.Question.Play();
                    break;
                case SoundType.GoldenGoblinDefeat:
                    SystemSounds.Exclamation.Play();
                    break;
                case SoundType.GoldenGoblinEscape:
                    SystemSounds.Asterisk.Play();
                    break;
            }
        }

        #endregion
    }
}
