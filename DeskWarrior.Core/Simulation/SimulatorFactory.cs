using DeskWarrior.Core.Models;
using DeskWarrior.Core.Balance;

namespace DeskWarrior.Core.Simulation;

/// <summary>
/// 시뮬레이터 팩토리 - 설정 파일 로드 및 시뮬레이터 생성
/// </summary>
public static class SimulatorFactory
{
    /// <summary>
    /// 설정 파일에서 BatchSimulator 생성
    /// </summary>
    /// <param name="configPath">config 폴더 경로</param>
    /// <returns>BatchSimulator 인스턴스</returns>
    public static BatchSimulator Create(string configPath)
    {
        // GameData.json 로드
        var gameConfigPath = Path.Combine(configPath, "GameData.json");
        var gameConfig = GameConfig.LoadFromFile(gameConfigPath);

        // InGameStatGrowth.json 로드
        var inGamePath = Path.Combine(configPath, "InGameStatGrowth.json");
        var inGameStats = LoadStatConfigs(inGamePath);

        // PermanentStats.json 로드
        var permanentPath = Path.Combine(configPath, "PermanentStats.json");
        var permanentStats = LoadStatConfigs(permanentPath);

        // CharacterData.json에서 몬스터 기본값 로드
        var monsterConfig = LoadMonsterConfig(Path.Combine(configPath, "CharacterData.json"));

        // BossDrops.json 로드
        var bossDropConfig = LoadBossDropConfig(Path.Combine(configPath, "BossDrops.json"));

        return new BatchSimulator(gameConfig, inGameStats, permanentStats, monsterConfig, bossDropConfig);
    }

    /// <summary>
    /// 단일 엔진 생성 (디버깅용)
    /// </summary>
    public static SimulationEngine CreateEngine(string configPath, int? seed = null)
    {
        var gameConfigPath = Path.Combine(configPath, "GameData.json");
        var gameConfig = GameConfig.LoadFromFile(gameConfigPath);

        var inGamePath = Path.Combine(configPath, "InGameStatGrowth.json");
        var inGameStats = LoadStatConfigs(inGamePath);

        var permanentPath = Path.Combine(configPath, "PermanentStats.json");
        var permanentStats = LoadStatConfigs(permanentPath);

        var monsterConfig = LoadMonsterConfig(Path.Combine(configPath, "CharacterData.json"));

        var bossDropConfig = LoadBossDropConfig(Path.Combine(configPath, "BossDrops.json"));

        return new SimulationEngine(gameConfig, inGameStats, permanentStats, monsterConfig, bossDropConfig, seed);
    }

    /// <summary>
    /// ProgressionSimulator 생성
    /// </summary>
    public static ProgressionSimulator CreateProgressionSimulator(string configPath, int? seed = null)
    {
        var engine = CreateEngine(configPath, seed);
        var costCalculator = CreateCostCalculator(configPath);
        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        return new ProgressionSimulator(engine, costCalculator, random);
    }

    /// <summary>
    /// StatCostCalculator 생성
    /// </summary>
    public static StatCostCalculator CreateCostCalculator(string configPath)
    {
        var permanentPath = Path.Combine(configPath, "PermanentStats.json");
        var statConfigs = LoadPermanentStatConfigs(permanentPath);
        return new StatCostCalculator(statConfigs);
    }

    /// <summary>
    /// PermanentStats.json에서 StatConfig 로드
    /// </summary>
    private static Dictionary<string, StatConfig> LoadPermanentStatConfigs(string path)
    {
        var configs = new Dictionary<string, StatConfig>();

        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                using var doc = System.Text.Json.JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("stats", out var stats))
                {
                    foreach (var stat in stats.EnumerateObject())
                    {
                        if (stat.Name.StartsWith("_")) continue;

                        configs[stat.Name] = new StatConfig
                        {
                            StatName = TryGetString(stat.Value, "stat_name", stat.Name),
                            Category = TryGetString(stat.Value, "category", ""),
                            BaseCost = TryGetDouble(stat.Value, "base_cost", 1),
                            GrowthRate = TryGetDouble(stat.Value, "growth_rate", 0.5),
                            Multiplier = TryGetDouble(stat.Value, "multiplier", 1.5),
                            SoftcapInterval = TryGetInt(stat.Value, "softcap_interval", 10),
                            EffectPerLevel = TryGetDouble(stat.Value, "effect_per_level", 1),
                            MaxLevel = TryGetInt(stat.Value, "max_level", 0),
                            Weight = TryGetDouble(stat.Value, "weight", 1.0)
                        };
                    }
                }
            }
        }
        catch
        {
            // 기본 설정 사용
        }

        return configs;
    }

    private static string TryGetString(System.Text.Json.JsonElement element, string name, string defaultValue)
    {
        if (element.TryGetProperty(name, out var prop))
            return prop.GetString() ?? defaultValue;
        return defaultValue;
    }

    private static Dictionary<string, StatGrowthConfig> LoadStatConfigs(string path)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, StatGrowthConfig>();
        }

        var root = StatGrowthConfigRoot.LoadFromFile(path);
        return root.Stats
            .Where(kvp => !kvp.Key.StartsWith("_"))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static MonsterConfig LoadMonsterConfig(string path)
    {
        // CharacterData.json에서 첫 번째 몬스터의 기본값을 사용
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                using var doc = System.Text.Json.JsonDocument.Parse(json);

                // "Monsters" (Pascal Case) 또는 "monsters" (camelCase) 시도
                System.Text.Json.JsonElement monsters;
                if (!doc.RootElement.TryGetProperty("Monsters", out monsters))
                {
                    doc.RootElement.TryGetProperty("monsters", out monsters);
                }

                if (monsters.ValueKind == System.Text.Json.JsonValueKind.Array && monsters.GetArrayLength() > 0)
                {
                    var first = monsters[0];

                    // Pascal Case 또는 camelCase 프로퍼티 이름 시도
                    int baseHp = TryGetInt(first, "BaseHp", "base_hp", 20);
                    int hpGrowth = TryGetInt(first, "HpGrowth", "hp_growth", 5);
                    int baseGold = TryGetInt(first, "BaseGold", "base_gold", 10);
                    int goldGrowth = TryGetInt(first, "GoldGrowth", "gold_growth", 2);

                    return new MonsterConfig
                    {
                        BaseHp = baseHp,
                        HpGrowth = hpGrowth,
                        BaseGold = baseGold,
                        GoldGrowth = goldGrowth
                    };
                }
            }
        }
        catch
        {
            // 기본값 사용
        }

        return new MonsterConfig { BaseHp = 20, HpGrowth = 5, BaseGold = 10, GoldGrowth = 2 };
    }

    private static int TryGetInt(System.Text.Json.JsonElement element, string name1, string name2, int defaultValue)
    {
        if (element.TryGetProperty(name1, out var prop1))
            return prop1.GetInt32();
        if (element.TryGetProperty(name2, out var prop2))
            return prop2.GetInt32();
        return defaultValue;
    }

    private static BossDropConfig LoadBossDropConfig(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                return new BossDropConfig
                {
                    BaseDropChance = TryGetDouble(root, "base_drop_chance", 0.5),
                    DropChancePerLevel = TryGetDouble(root, "drop_chance_per_level", 0.005),
                    MaxDropChance = TryGetDouble(root, "max_drop_chance", 0.95),
                    BaseCrystalAmount = TryGetInt(root, "base_crystal_amount", 5),
                    CrystalPerLevel = TryGetInt(root, "crystal_per_level", 1),
                    CrystalVariance = TryGetDouble(root, "crystal_variance", 0.2),
                    GuaranteedDropEveryNBosses = TryGetInt(root, "guaranteed_drop_every_n_bosses", 10)
                };
            }
        }
        catch
        {
            // 기본값 사용
        }

        return new BossDropConfig();
    }

    private static int TryGetInt(System.Text.Json.JsonElement element, string name, int defaultValue)
    {
        if (element.TryGetProperty(name, out var prop))
            return prop.GetInt32();
        return defaultValue;
    }

    private static double TryGetDouble(System.Text.Json.JsonElement element, string name, double defaultValue)
    {
        if (element.TryGetProperty(name, out var prop))
            return prop.GetDouble();
        return defaultValue;
    }
}
