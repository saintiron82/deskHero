using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 배치 기반 몬스터 데이터 로드 및 관리
    /// </summary>
    public class MonsterDataManager
    {
        #region Fields

        private readonly string _configPath;
        private readonly Random _random = new();
        private GameData? _gameData;
        private BatchIndex? _batchIndex;
        private readonly Dictionary<int, BatchData> _loadedBatches = new();
        private List<FlattenedMonsterData>? _allMonsters;
        private List<FlattenedMonsterData>? _allBosses;
        private string _currentLanguage = "ko-KR";

        #endregion

        #region Constructor

        public MonsterDataManager(string? configBasePath = null)
        {
            _configPath = configBasePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "monsters");
        }

        #endregion

        #region Properties

        /// <summary>
        /// 현재 언어 코드
        /// </summary>
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    // 언어 변경 시 캐시된 몬스터 목록 갱신 필요
                    _allMonsters = null;
                    _allBosses = null;
                }
            }
        }

        /// <summary>
        /// 로드된 배치 수
        /// </summary>
        public int LoadedBatchCount => _loadedBatches.Count;

        /// <summary>
        /// 활성화된 배치 ID 목록
        /// </summary>
        public IReadOnlyList<int> EnabledBatchIds => _batchIndex?.Batches
            .Where(b => b.Enabled)
            .Select(b => b.BatchId)
            .ToList() ?? new List<int>();

        #endregion

        #region Public Methods

        /// <summary>
        /// GameData 설정 (가중치 계산에 사용)
        /// </summary>
        public void SetGameData(GameData gameData)
        {
            _gameData = gameData;
            // GameData 변경 시 캐시 무효화
            _allMonsters = null;
            _allBosses = null;
        }

        /// <summary>
        /// 배치 인덱스 로드
        /// </summary>
        public void LoadBatchIndex()
        {
            var indexPath = Path.Combine(_configPath, "_index.json");
            if (!File.Exists(indexPath))
            {
                // 인덱스 파일이 없으면 레거시 모드로 폴백
                _batchIndex = null;
                return;
            }

            var json = File.ReadAllText(indexPath);
            _batchIndex = JsonSerializer.Deserialize<BatchIndex>(json);
        }

        /// <summary>
        /// 특정 배치 로드
        /// </summary>
        public BatchData? LoadBatch(int batchId)
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

        /// <summary>
        /// 모든 활성화된 배치 로드
        /// </summary>
        public void LoadAllEnabledBatches()
        {
            if (_batchIndex == null)
            {
                LoadBatchIndex();
            }

            if (_batchIndex == null) return;

            foreach (var entry in _batchIndex.Batches.Where(b => b.Enabled))
            {
                LoadBatch(entry.BatchId);
            }
        }

        /// <summary>
        /// 모든 몬스터 목록 가져오기 (평탄화)
        /// </summary>
        public List<FlattenedMonsterData> GetAllMonsters()
        {
            if (_allMonsters != null)
            {
                return _allMonsters;
            }

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

        /// <summary>
        /// 모든 보스 목록 가져오기 (평탄화)
        /// </summary>
        public List<FlattenedMonsterData> GetAllBosses()
        {
            if (_allBosses != null)
            {
                return _allBosses;
            }

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

        /// <summary>
        /// 레벨 기반 랜덤 몬스터 가져오기 (FlattenedMonsterData 반환)
        /// </summary>
        /// <param name="level">현재 레벨</param>
        /// <param name="isBoss">보스 여부</param>
        /// <returns>FlattenedMonsterData (Species, Element 포함)</returns>
        public FlattenedMonsterData GetRandomMonsterData(int level, bool isBoss)
        {
            var list = isBoss ? GetAllBosses() : GetAllMonsters();
            if (list.Count == 0)
            {
                // 폴백 기본 몬스터
                return new FlattenedMonsterData
                {
                    Id = "monster_unknown",
                    Species = "unknown",
                    Element = "normal",
                    Name = "???",
                    BaseHp = 10,
                    HpGrowth = 5,
                    BaseGold = 10,
                    GoldGrowth = 2,
                    Emoji = "👹"
                };
            }

            // 가중치 기반 선택 (feature flag로 제어)
            if (_gameData?.MonsterSpawning.UseWeightedSelection ?? false)
            {
                return SelectByWeight(list);
            }
            else
            {
                // 레거시 순환 방식
                int index = (level - 1) % list.Count;
                return list[index];
            }
        }

        /// <summary>
        /// 레벨 기반 랜덤 몬스터 가져오기 (하위 호환용)
        /// </summary>
        /// <param name="level">현재 레벨</param>
        /// <param name="isBoss">보스 여부</param>
        /// <returns>MonsterData (하위 호환용)</returns>
        public MonsterData GetRandomMonster(int level, bool isBoss)
        {
            return GetRandomMonsterData(level, isBoss).ToMonsterData();
        }

        /// <summary>
        /// 특정 ID의 몬스터 가져오기
        /// </summary>
        public FlattenedMonsterData? GetMonsterById(string id)
        {
            var all = GetAllMonsters().Concat(GetAllBosses());
            return all.FirstOrDefault(m => m.Id == id);
        }

        /// <summary>
        /// 특정 종(species)의 모든 변형 가져오기
        /// </summary>
        public List<FlattenedMonsterData> GetMonstersBySpecies(string species)
        {
            var all = GetAllMonsters().Concat(GetAllBosses());
            return all.Where(m => m.Species == species).ToList();
        }

        /// <summary>
        /// 레거시 CharacterDataRoot로 변환 (하위 호환)
        /// </summary>
        public CharacterDataRoot ToCharacterDataRoot()
        {
            return new CharacterDataRoot
            {
                Monsters = GetAllMonsters().Select(m => m.ToMonsterData()).ToList(),
                Bosses = GetAllBosses().Select(m => m.ToMonsterData()).ToList(),
                Heroes = new List<HeroData>() // Heroes는 별도 관리
            };
        }

        /// <summary>
        /// 캐시 초기화
        /// </summary>
        public void ClearCache()
        {
            _allMonsters = null;
            _allBosses = null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 배치 몬스터 데이터를 평탄화된 형태로 변환
        /// </summary>
        private FlattenedMonsterData FlattenMonster(BatchMonsterEntry entry, string element, MonsterVariation variation, int batchId)
        {
            // HP와 골드에 모디파이어 적용
            int baseHp = (int)(entry.BaseStats.BaseHp * variation.HpModifier);
            int baseGold = (int)(entry.BaseStats.BaseGold * variation.GoldModifier);

            // 최종 가중치 계산
            int speciesWeight = entry.SpawnWeight;
            int elementWeight = _gameData?.MonsterSpawning.ElementWeights
                .GetValueOrDefault(element, 100) ?? 100;

            var batchEntry = _batchIndex?.Batches.FirstOrDefault(b => b.BatchId == batchId);
            double batchWeight = batchEntry?.ActivationWeight ?? 1.0;

            int finalWeight = (int)(speciesWeight * elementWeight * batchWeight);

            return new FlattenedMonsterData
            {
                Id = $"{entry.Id}_{element}",
                Species = entry.Species,
                Element = element,
                Name = variation.GetName(_currentLanguage),
                Description = variation.GetDescription(_currentLanguage),
                Sprite = variation.Sprite,
                Emoji = variation.Emoji,
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

        /// <summary>
        /// 가중치 기반 랜덤 선택
        /// </summary>
        private FlattenedMonsterData SelectByWeight(List<FlattenedMonsterData> list)
        {
            int totalWeight = list.Sum(m => m.FinalWeight);
            if (totalWeight == 0)
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

        #endregion
    }
}
