using System.Text.Json;
using System.Text.Json.Serialization;
using DeskWarrior.Core.Models;

namespace DeskWarrior.Core.Simulation;

internal sealed class SimMonsterBatchProvider
{
    private readonly string _configPath;
    private readonly Random _random;
    private readonly GameConfig _gameConfig;

    private BatchIndex? _batchIndex;
    private readonly Dictionary<int, BatchData> _loadedBatches = new();
    private List<FlattenedMonsterData>? _allMonsters;
    private List<FlattenedMonsterData>? _allBosses;

    public SimMonsterBatchProvider(GameConfig gameConfig, Random random, string configPath)
    {
        _gameConfig = gameConfig;
        _random = random;
        _configPath = configPath;
    }

    public bool TryLoad()
    {
        LoadBatchIndex();
        if (_batchIndex == null)
            return false;

        LoadAllEnabledBatches();
        return _loadedBatches.Count > 0;
    }

    internal FlattenedMonsterData? GetRandomMonsterData(int level, bool isBoss)
    {
        var list = isBoss ? GetAllBosses() : GetAllMonsters();
        if (list.Count == 0)
            return null;

        if (_gameConfig.MonsterSpawning.UseWeightedSelection)
        {
            return SelectByWeight(list);
        }

        int index = (level - 1) % list.Count;
        if (index < 0) index = 0;
        return list[index];
    }

    internal int CalculateStageExpectedGold(int level)
    {
        var monsters = GetAllMonsters();
        if (monsters.Count == 0)
            return 0;

        long totalGold = 0;
        foreach (var monster in monsters)
        {
            totalGold += monster.BaseGold + level * monster.GoldGrowth;
        }

        return (int)(totalGold / monsters.Count);
    }

    private void LoadBatchIndex()
    {
        var indexPath = Path.Combine(_configPath, "_index.json");
        if (!File.Exists(indexPath))
        {
            _batchIndex = null;
            return;
        }

        var json = File.ReadAllText(indexPath);
        _batchIndex = JsonSerializer.Deserialize<BatchIndex>(json);
    }

    private BatchData? LoadBatch(int batchId)
    {
        if (_loadedBatches.TryGetValue(batchId, out var cached))
        {
            return cached;
        }

        var entry = _batchIndex?.Batches.FirstOrDefault(b => b.BatchId == batchId);
        if (entry == null)
        {
            return null;
        }

        var batchPath = Path.Combine(_configPath, entry.File);
        if (!File.Exists(batchPath))
        {
            return null;
        }

        var json = File.ReadAllText(batchPath);
        var batchData = JsonSerializer.Deserialize<BatchData>(json);
        if (batchData != null)
        {
            _loadedBatches[batchId] = batchData;
        }

        return batchData;
    }

    private void LoadAllEnabledBatches()
    {
        if (_batchIndex == null)
        {
            return;
        }

        foreach (var entry in _batchIndex.Batches.Where(b => b.Enabled))
        {
            LoadBatch(entry.BatchId);
        }
    }

    private List<FlattenedMonsterData> GetAllMonsters()
    {
        if (_allMonsters != null)
            return _allMonsters;

        _allMonsters = new List<FlattenedMonsterData>();

        foreach (var batch in _loadedBatches.Values)
        {
            foreach (var monster in batch.Monsters)
            {
                foreach (var (element, variation) in monster.Variations)
                {
                    _allMonsters.Add(FlattenMonster(monster, element, variation, batch.BatchId));
                }
            }
        }

        return _allMonsters;
    }

    private List<FlattenedMonsterData> GetAllBosses()
    {
        if (_allBosses != null)
            return _allBosses;

        _allBosses = new List<FlattenedMonsterData>();

        foreach (var batch in _loadedBatches.Values)
        {
            foreach (var boss in batch.Bosses)
            {
                foreach (var (element, variation) in boss.Variations)
                {
                    _allBosses.Add(FlattenMonster(boss, element, variation, batch.BatchId));
                }
            }
        }

        return _allBosses;
    }

    private FlattenedMonsterData FlattenMonster(BatchMonsterEntry entry, string element, MonsterVariation variation, int batchId)
    {
        int baseHp = (int)(entry.BaseStats.BaseHp * variation.HpModifier);
        int baseGold = (int)(entry.BaseStats.BaseGold * variation.GoldModifier);

        int speciesWeight = entry.SpawnWeight;
        int elementWeight = 100;
        if (_gameConfig.MonsterSpawning.ElementWeights.TryGetValue(element, out var elementWeightValue))
        {
            elementWeight = elementWeightValue;
        }

        double batchWeight = 1.0;
        var batchEntry = _batchIndex?.Batches.FirstOrDefault(b => b.BatchId == batchId);
        if (batchEntry != null)
        {
            batchWeight = batchEntry.ActivationWeight;
        }

        int finalWeight = (int)(speciesWeight * elementWeight * batchWeight);

        return new FlattenedMonsterData
        {
            Element = element,
            BaseHp = baseHp,
            HpGrowth = entry.BaseStats.HpGrowth,
            BaseGold = baseGold,
            GoldGrowth = entry.BaseStats.GoldGrowth,
            IsBoss = entry.IsBoss,
            BatchId = batchId,
            SpawnWeight = entry.SpawnWeight,
            FinalWeight = finalWeight
        };
    }

    private FlattenedMonsterData SelectByWeight(List<FlattenedMonsterData> list)
    {
        int totalWeight = list.Sum(m => m.FinalWeight);
        if (totalWeight <= 0)
        {
            return list[_random.Next(list.Count)];
        }

        int roll = _random.Next(totalWeight);
        int cumulative = 0;

        foreach (var monster in list)
        {
            cumulative += monster.FinalWeight;
            if (roll < cumulative)
            {
                return monster;
            }
        }

        return list.Last();
    }

    private sealed class BatchIndex
    {
        [JsonPropertyName("batches")]
        public List<BatchIndexEntry> Batches { get; set; } = new();
    }

    private sealed class BatchIndexEntry
    {
        [JsonPropertyName("batch_id")]
        public int BatchId { get; set; }

        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("activation_weight")]
        public double ActivationWeight { get; set; } = 1.0;
    }

    private sealed class BatchData
    {
        [JsonPropertyName("batch_id")]
        public int BatchId { get; set; }

        [JsonPropertyName("monsters")]
        public List<BatchMonsterEntry> Monsters { get; set; } = new();

        [JsonPropertyName("bosses")]
        public List<BatchMonsterEntry> Bosses { get; set; } = new();
    }

    private sealed class BatchMonsterEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("species")]
        public string Species { get; set; } = string.Empty;

        [JsonPropertyName("is_boss")]
        public bool IsBoss { get; set; }

        [JsonPropertyName("base_stats")]
        public MonsterBaseStats BaseStats { get; set; } = new();

        [JsonPropertyName("spawn_weight")]
        public int SpawnWeight { get; set; } = 100;

        [JsonPropertyName("variations")]
        public Dictionary<string, MonsterVariation> Variations { get; set; } = new();
    }

    private sealed class MonsterBaseStats
    {
        [JsonPropertyName("base_hp")]
        public int BaseHp { get; set; }

        [JsonPropertyName("hp_growth")]
        public int HpGrowth { get; set; }

        [JsonPropertyName("base_gold")]
        public int BaseGold { get; set; }

        [JsonPropertyName("gold_growth")]
        public int GoldGrowth { get; set; }
    }

    private sealed class MonsterVariation
    {
        [JsonPropertyName("hp_modifier")]
        public double HpModifier { get; set; } = 1.0;

        [JsonPropertyName("gold_modifier")]
        public double GoldModifier { get; set; } = 1.0;
    }

    internal sealed class FlattenedMonsterData
    {
        public string Element { get; set; } = "normal";
        public int BaseHp { get; set; }
        public int HpGrowth { get; set; }
        public int BaseGold { get; set; }
        public int GoldGrowth { get; set; }
        public bool IsBoss { get; set; }
        public int BatchId { get; set; }
        public int SpawnWeight { get; set; }
        public int FinalWeight { get; set; } = 100;
    }
}
