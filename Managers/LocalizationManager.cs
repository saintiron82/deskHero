using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace DeskWarrior.Managers
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        private static LocalizationManager? _instance;
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();

        private Dictionary<string, object> _translations = new();
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
                    _translations = FlattenJson(doc.RootElement, "");
                }
                else
                {
                    // Fallback: load default translations
                    LoadDefaultTranslations(languageCode);
                }
            }
            catch (Exception)
            {
                LoadDefaultTranslations(languageCode);
            }
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
            if (_translations.TryGetValue(key, out var value))
            {
                return value as string ?? key;
            }
            return key; // Fallback: return key itself
        }

        public List<string> GetStringArray(string key)
        {
            if (_translations.TryGetValue(key, out var value) && value is List<string> list)
            {
                return list;
            }
            return new List<string> { key };
        }

        public string GetRandomString(string key)
        {
            var list = GetStringArray(key);
            if (list.Count == 0) return key;
            var random = new Random();
            return list[random.Next(list.Count)];
        }

        private void LoadDefaultTranslations(string languageCode)
        {
            _translations = new Dictionary<string, object>();

            if (languageCode == "ko-KR")
            {
                LoadKoreanDefaults();
            }
            else
            {
                LoadEnglishDefaults();
            }
        }

        private void LoadKoreanDefaults()
        {
            // Common
            _translations["ui.common.level"] = "레벨";
            _translations["ui.common.gold"] = "골드";
            _translations["ui.common.damage"] = "데미지";
            _translations["ui.common.close"] = "닫기";
            _translations["ui.common.kills"] = "처치";

            // Main UI
            _translations["ui.main.keyboardAtk"] = "⌨️ 공격력";
            _translations["ui.main.mouseAtk"] = "🖱️ 공격력";
            _translations["ui.main.upgradeKeyboard"] = "⌨️ 공격력 UP";
            _translations["ui.main.upgradeMouse"] = "🖱️ 공격력 UP";
            _translations["ui.main.stats"] = "📊 통계";
            _translations["ui.main.settings"] = "⚙️ 설정";
            _translations["ui.main.exit"] = "❌ 종료";

            // Settings
            _translations["ui.settings.title"] = "⚙️ SETTINGS";
            _translations["ui.settings.opacity"] = "배경 투명도";
            _translations["ui.settings.volume"] = "사운드 볼륨";
            _translations["ui.settings.language"] = "언어 / Language";
            _translations["ui.settings.close"] = "CLOSE";

            // Statistics
            _translations["ui.statistics.title"] = "통계";
            _translations["ui.statistics.tabs.overview"] = "개요";
            _translations["ui.statistics.tabs.sessions"] = "세션";
            _translations["ui.statistics.tabs.history"] = "기록"; // New
            _translations["ui.statistics.tabs.achievements"] = "업적";

            _translations["ui.statistics.overview.currentSession"] = "현재 세션";
            _translations["ui.statistics.overview.bestSessionRecords"] = "역대 최고 기록"; // New
            _translations["ui.statistics.overview.cumulativeStats"] = "누적 통계"; // New
            _translations["ui.statistics.overview.maxStats"] = "최고 세션 통계 (기간 내)"; // New
            _translations["ui.statistics.overview.lifetimeRecords"] = "누적 기록";
            _translations["ui.statistics.overview.efficiency"] = "효율";

            _translations["ui.statistics.labels.level"] = "레벨";
            _translations["ui.statistics.labels.damage"] = "데미지";
            _translations["ui.statistics.labels.gold"] = "골드";
            _translations["ui.statistics.labels.kills"] = "처치";
            _translations["ui.statistics.labels.maxLevel"] = "최고 레벨";
            _translations["ui.statistics.labels.totalDamage"] = "총 데미지";
            _translations["ui.statistics.labels.maxHit"] = "최대 타격";
            _translations["ui.statistics.labels.monsterKills"] = "몬스터 처치";
            _translations["ui.statistics.labels.bossesDefeated"] = "보스";
            _translations["ui.statistics.labels.totalGoldEarned"] = "총 골드";
            _translations["ui.statistics.labels.totalPlaytime"] = "총 플레이시간";
            _translations["ui.statistics.labels.totalInputs"] = "총 입력 횟수";
            _translations["ui.statistics.labels.criticalHits"] = "크리티컬";
            _translations["ui.statistics.labels.consecutiveDays"] = "연속 일수";
            _translations["ui.statistics.labels.duration"] = "지속시간";
            _translations["ui.statistics.labels.levelProgress"] = "레벨 추이 (7일)";
            _translations["ui.statistics.labels.inputRatio"] = "입력 비율";
            _translations["ui.statistics.labels.keyboard"] = "키보드";
            _translations["ui.statistics.labels.mouse"] = "마우스";
            _translations["ui.statistics.labels.achievements"] = "업적 달성"; // New
            _translations["ui.statistics.labels.timeRange"] = "기간 선택"; // New

            _translations["ui.statistics.format.days"] = "{0}일";

            _translations["ui.statistics.sessions.bestSession"] = "최고 세션";
            _translations["ui.statistics.sessions.noSessionsYet"] = "기록 없음";
            _translations["ui.statistics.sessions.currentVsAverage"] = "현재 vs 평균";
            _translations["ui.statistics.sessions.recentSessions"] = "최근 세션";
            _translations["ui.statistics.sessions.noSessionsRecorded"] = "아직 기록된 세션이 없습니다";

            _translations["ui.statistics.achievements.loading"] = "업적 로딩 중...";
            _translations["ui.statistics.achievements.categories.combat"] = "전투";
            _translations["ui.statistics.achievements.categories.progression"] = "성장";
            _translations["ui.statistics.achievements.categories.wealth"] = "재화";
            _translations["ui.statistics.achievements.categories.engagement"] = "활동";
            _translations["ui.statistics.achievements.categories.secret"] = "비밀";

            // Game Over
            _translations["ui.gameover.title"] = "도전 실패";
            _translations["ui.gameover.maxLevel"] = "최고 레벨:";
            _translations["ui.gameover.goldEarned"] = "획득 골드:";
            _translations["ui.gameover.damageDealt"] = "가한 데미지:";
            _translations["ui.gameover.newLife"] = "⚠️ 새 생명 시작";
            _translations["ui.gameover.autoRestart"] = "{0}초 후 자동 재시작";
            _translations["ui.gameover.messages"] = new List<string>
            {
                "타임아웃! 다음엔 더 빠르게!",
                "시간이 부족했나봐요. 재도전!",
                "좋은 시도였어요! 다시 한 번!",
                "이번 라운드는 여기까지! 화이팅!",
                "타이머가 만료되었습니다. 리트라이!"
            };

            // Toast
            _translations["ui.toast.achievementUnlocked"] = "업적 해금!";

            // Tray
            _translations["ui.tray.title"] = "DeskWarrior - 트레이 더블클릭 또는 F1키로 드래그 모드";
            _translations["ui.tray.dragMode"] = "📌 드래그 모드";
            _translations["ui.tray.settings"] = "⚙️ 설정...";
            _translations["ui.tray.exit"] = "❌ 종료";
        }

        private void LoadEnglishDefaults()
        {
            // Common
            _translations["ui.common.level"] = "Level";
            _translations["ui.common.gold"] = "Gold";
            _translations["ui.common.damage"] = "Damage";
            _translations["ui.common.close"] = "Close";
            _translations["ui.common.kills"] = "Kills";

            // Main UI
            _translations["ui.main.keyboardAtk"] = "⌨️ Atk";
            _translations["ui.main.mouseAtk"] = "🖱️ Atk";
            _translations["ui.main.upgradeKeyboard"] = "⌨️ ATK UP";
            _translations["ui.main.upgradeMouse"] = "🖱️ ATK UP";
            _translations["ui.main.stats"] = "📊 Stats";
            _translations["ui.main.settings"] = "⚙️ Settings";
            _translations["ui.main.exit"] = "❌ Exit";

            // Settings
            _translations["ui.settings.title"] = "⚙️ SETTINGS";
            _translations["ui.settings.opacity"] = "Background Opacity";
            _translations["ui.settings.volume"] = "Sound Volume";
            _translations["ui.settings.language"] = "Language / 언어";
            _translations["ui.settings.close"] = "CLOSE";

            // Statistics
            _translations["ui.statistics.title"] = "STATISTICS";
            _translations["ui.statistics.tabs.overview"] = "OVERVIEW";
            _translations["ui.statistics.tabs.sessions"] = "SESSIONS";
            _translations["ui.statistics.tabs.history"] = "HISTORY"; // New
            _translations["ui.statistics.tabs.achievements"] = "ACHIEVEMENTS";

            _translations["ui.statistics.overview.currentSession"] = "CURRENT SESSION";
            _translations["ui.statistics.overview.bestSessionRecords"] = "BEST SESSION RECORDS"; // New
            _translations["ui.statistics.overview.cumulativeStats"] = "CUMULATIVE STATS"; // New
            _translations["ui.statistics.overview.maxStats"] = "MAX SESSION STATS (In Range)"; // New
            _translations["ui.statistics.overview.lifetimeRecords"] = "LIFETIME RECORDS";
            _translations["ui.statistics.overview.efficiency"] = "EFFICIENCY";

            _translations["ui.statistics.labels.level"] = "Level";
            _translations["ui.statistics.labels.damage"] = "Damage";
            _translations["ui.statistics.labels.gold"] = "Gold";
            _translations["ui.statistics.labels.kills"] = "Kills";
            _translations["ui.statistics.labels.maxLevel"] = "Max Level";
            _translations["ui.statistics.labels.totalDamage"] = "Total Damage";
            _translations["ui.statistics.labels.maxHit"] = "Max Hit";
            _translations["ui.statistics.labels.monsterKills"] = "Monster Kills";
            _translations["ui.statistics.labels.bossesDefeated"] = "Bosses Defeated";
            _translations["ui.statistics.labels.totalGoldEarned"] = "Total Gold Earned";
            _translations["ui.statistics.labels.totalPlaytime"] = "Total Playtime";
            _translations["ui.statistics.labels.totalInputs"] = "Total Inputs";
            _translations["ui.statistics.labels.criticalHits"] = "Critical Hits";
            _translations["ui.statistics.labels.consecutiveDays"] = "Consecutive Days";
            _translations["ui.statistics.labels.duration"] = "Duration";
            _translations["ui.statistics.labels.levelProgress"] = "Level Progress (7 Days)";
            _translations["ui.statistics.labels.inputRatio"] = "Input Ratio";
            _translations["ui.statistics.labels.keyboard"] = "Keyboard";
            _translations["ui.statistics.labels.mouse"] = "Mouse";
            _translations["ui.statistics.labels.achievements"] = "ACHIEVEMENTS"; // New
            _translations["ui.statistics.labels.timeRange"] = "TIME RANGE"; // New

            _translations["ui.statistics.format.days"] = "{0} days";

            _translations["ui.statistics.sessions.bestSession"] = "BEST SESSION";
            _translations["ui.statistics.sessions.noSessionsYet"] = "No sessions yet";
            _translations["ui.statistics.sessions.currentVsAverage"] = "CURRENT VS AVERAGE";
            _translations["ui.statistics.sessions.recentSessions"] = "RECENT SESSIONS";
            _translations["ui.statistics.sessions.noSessionsRecorded"] = "No sessions recorded yet";

            _translations["ui.statistics.achievements.loading"] = "Loading achievements...";
            _translations["ui.statistics.achievements.categories.combat"] = "COMBAT";
            _translations["ui.statistics.achievements.categories.progression"] = "PROGRESSION";
            _translations["ui.statistics.achievements.categories.wealth"] = "WEALTH";
            _translations["ui.statistics.achievements.categories.engagement"] = "ENGAGEMENT";
            _translations["ui.statistics.achievements.categories.secret"] = "SECRET";

            // Game Over
            _translations["ui.gameover.title"] = "CHALLENGE FAILED";
            _translations["ui.gameover.maxLevel"] = "Max Level:";
            _translations["ui.gameover.goldEarned"] = "Gold Earned:";
            _translations["ui.gameover.damageDealt"] = "Damage Dealt:";
            _translations["ui.gameover.newLife"] = "⚠️ BEGIN NEW LIFE";
            _translations["ui.gameover.autoRestart"] = "Auto Restart in {0}s";
            _translations["ui.gameover.messages"] = new List<string>
            {
                "Time's up! Try to be faster next time!",
                "Ran out of time. Retry!",
                "Good try! Give it another shot!",
                "This round ends here! Fight on!",
                "Timer expired. Retry!"
            };

            // Toast
            _translations["ui.toast.achievementUnlocked"] = "ACHIEVEMENT UNLOCKED!";

            // Tray
            _translations["ui.tray.title"] = "DeskWarrior - Double-click tray or press F1 for drag mode";
            _translations["ui.tray.dragMode"] = "📌 Drag Mode";
            _translations["ui.tray.settings"] = "⚙️ Settings...";
            _translations["ui.tray.exit"] = "❌ Exit";
        }

        public List<LanguageOption> GetAvailableLanguages()
        {
            return new List<LanguageOption>
            {
                new LanguageOption { Code = "ko-KR", DisplayName = "한국어 (Korean)" },
                new LanguageOption { Code = "en-US", DisplayName = "English (영어)" }
            };
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LanguageOption
    {
        public string Code { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}
