using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using DeskWarrior.Interfaces;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    public class LocalizationManager : INotifyPropertyChanged, ILocalizationProvider
    {
        private static LocalizationManager? _instance;
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();

        private Dictionary<string, object> _translations = new();
        private Dictionary<string, object> _fallbackTranslations = new();
        private List<LanguageOption> _availableLanguages = new();
        private string _currentLanguage = "ko-KR";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CurrentLanguage
        {
            get => _currentLanguage;
            private set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged(nameof(CurrentLanguage));
                }
            }
        }

        // Indexer for accessing translations
        public string this[string key]
        {
            get => GetString(key);
        }

        private LocalizationManager()
        {
            LoadAvailableLanguages();

            // Detect system language
            var culture = CultureInfo.CurrentCulture;
            _currentLanguage = culture.Name.StartsWith("ko") ? "ko-KR" : "en-US";
            LoadLanguage(_currentLanguage);
        }

        public void Initialize(string? savedLanguage)
        {
            if (!string.IsNullOrEmpty(savedLanguage))
            {
                _currentLanguage = savedLanguage;
            }
            LoadLanguage(_currentLanguage);
        }

        public void SetLanguage(string languageCode)
        {
            if (_currentLanguage == languageCode) return;

            CurrentLanguage = languageCode;
            LoadLanguage(languageCode);

            // Notify all bindings to refresh
            OnPropertyChanged("Item[]");
        }

        private void LoadLanguage(string languageCode)
        {
            _translations = LoadTranslationDictionary(languageCode);

            // If current language is not English, load English as fallback if not already loaded
            if (languageCode != "en-US" && _fallbackTranslations.Count == 0)
            {
                _fallbackTranslations = LoadTranslationDictionary("en-US");
            }
        }

        private Dictionary<string, object> LoadTranslationDictionary(string languageCode)
        {
            try
            {
                // Try loading from config folder first
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(basePath, "config", "localization", $"{languageCode}.json");

                if (!File.Exists(filePath))
                {
                    // Fallback to embedded resource path
                    filePath = Path.Combine(basePath, $"{languageCode}.json");
                }

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var doc = JsonDocument.Parse(json);
                    return FlattenJson(doc.RootElement, "");
                }
            }
            catch (Exception)
            {
                // Failed to load translations
            }
            return new Dictionary<string, object>();
        }

        private Dictionary<string, object> FlattenJson(JsonElement element, string prefix)
        {
            var result = new Dictionary<string, object>();

            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    string key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                    if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var nested in FlattenJson(prop.Value, key))
                        {
                            result[nested.Key] = nested.Value;
                        }
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var list = new List<string>();
                        foreach (var item in prop.Value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                list.Add(item.GetString() ?? "");
                            }
                        }
                        result[key] = list;
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        result[key] = prop.Value.GetString() ?? "";
                    }
                }
            }

            return result;
        }

        public string GetString(string key)
        {
            // 1. Try current language
            if (_translations.TryGetValue(key, out var value))
            {
                return value as string ?? key;
            }

            // 2. Try fallback language (English)
            if (_fallbackTranslations.TryGetValue(key, out var fallbackValue))
            {
                return fallbackValue as string ?? key;
            }

            // 3. Return key itself
            return key;
        }

        public List<string> GetStringArray(string key)
        {
            // 1. Try current language
            if (_translations.TryGetValue(key, out var value) && value is List<string> list)
            {
                return list;
            }

            // 2. Try fallback language (English)
            if (_fallbackTranslations.TryGetValue(key, out var fallbackValue) && fallbackValue is List<string> fallbackList)
            {
                return fallbackList;
            }

            // 3. Return key generic list
            return new List<string> { key };
        }

        public string GetRandomString(string key)
        {
            var list = GetStringArray(key);
            if (list.Count == 0) return key;
            var random = new Random();
            return list[random.Next(list.Count)];
        }

        /// <summary>
        /// 형식 문자열로 로컬라이즈된 문자열 가져오기
        /// </summary>
        public string Format(string key, params object[] args)
        {
            string template = GetString(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        private void LoadAvailableLanguages()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(basePath, "config", "localization", "languages.json");

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var languages = JsonSerializer.Deserialize<List<LanguageOption>>(json);
                    if (languages != null && languages.Count > 0)
                    {
                        _availableLanguages = languages;
                        return;
                    }
                }
            }
            catch { }

            // Fallback defaults
            _availableLanguages = new List<LanguageOption>
            {
                new LanguageOption { Code = "ko-KR", DisplayName = "한국어 (Korean)" },
                new LanguageOption { Code = "en-US", DisplayName = "English (영어)" }
            };
        }

        public List<LanguageOption> GetAvailableLanguages()
        {
            return _availableLanguages;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 업적 로컬라이즈 텍스트 가져오기 (ILocalizationProvider)
        /// </summary>
        public AchievementLocalization GetAchievementLocalization(AchievementDefinition definition)
        {
            if (definition.Localization.TryGetValue(_currentLanguage, out var loc))
                return loc;
            if (definition.Localization.TryGetValue("en-US", out var fallback))
                return fallback;
            if (definition.Localization.Count > 0)
            {
                foreach (var val in definition.Localization.Values)
                    return val;
            }
            return new AchievementLocalization();
        }
    }

    public class LanguageOption
    {
        public string Code { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}
