using System;
using System.Linq;
using DeskWarrior.Helpers;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 몬스터 스폰 로직 관리 (일반, 보스, 황금 고블린)
    /// </summary>
    public class MonsterSpawnManager
    {
        #region Fields

        private readonly GameData _gameData;
        private readonly MonsterDataManager _monsterDataManager;
        private readonly GoldenGoblinManager _goldenGoblinManager;
        private readonly StatGrowthManager _statGrowth;
        private readonly Random _random;

        #endregion

        #region Constructor

        public MonsterSpawnManager(
            GameData gameData,
            MonsterDataManager monsterDataManager,
            GoldenGoblinManager goldenGoblinManager,
            StatGrowthManager statGrowth,
            Random random)
        {
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
            _monsterDataManager = monsterDataManager ?? throw new ArgumentNullException(nameof(monsterDataManager));
            _goldenGoblinManager = goldenGoblinManager ?? throw new ArgumentNullException(nameof(goldenGoblinManager));
            _statGrowth = statGrowth ?? throw new ArgumentNullException(nameof(statGrowth));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 몬스터 스폰 (일반, 보스, 또는 황금 고블린)
        /// </summary>
        /// <param name="currentLevel">현재 레벨</param>
        /// <param name="saveManager">저장 매니저 (시간 연장 계산용)</param>
        /// <returns>스폰 결과 (몬스터, 타입, 제한 시간)</returns>
        public MonsterSpawnResult SpawnMonster(int currentLevel, SaveManager? saveManager)
        {
            var balance = _gameData.Balance;
            bool isBoss = currentLevel > 0 && currentLevel % balance.BossInterval == 0;

            // 황금 고블린 스폰 체크 (보스가 아닐 때만)
            if (!isBoss && _goldenGoblinManager.ShouldSpawn())
            {
                return SpawnGoldenGoblin(currentLevel);
            }

            // 일반/보스 몬스터 스폰
            return SpawnRegularMonster(currentLevel, isBoss, saveManager);
        }

        /// <summary>
        /// 현재 스테이지 예상 골드 계산
        /// </summary>
        /// <param name="level">레벨</param>
        /// <returns>예상 골드</returns>
        public int CalculateStageExpectedGold(int level)
        {
            var monsters = _monsterDataManager.GetAllMonsters();
            if (monsters.Count == 0)
            {
                Logger.Log("[WARNING] No monsters loaded in batch system");
                return 10 + level * 2;  // Emergency fallback
            }

            int totalGold = 0;
            foreach (var m in monsters)
            {
                totalGold += m.BaseGold + level * m.GoldGrowth;
            }
            return totalGold / monsters.Count;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 일반/보스 몬스터 스폰
        /// </summary>
        private MonsterSpawnResult SpawnRegularMonster(int currentLevel, bool isBoss, SaveManager? saveManager)
        {
            string species = "";
            string element = "normal";
            MonsterData selectedData;

            // Batch 시스템으로 몬스터 데이터 가져오기
            var flattenedData = _monsterDataManager.GetRandomMonsterData(currentLevel, isBoss);
            selectedData = flattenedData.ToMonsterData();
            species = flattenedData.Species;
            element = flattenedData.Element;

            // 몬스터 생성 (species/element 전달)
            var monster = new Monster(
                selectedData,
                currentLevel,
                isBoss,
                _gameData.Balance.TierHpSystem,
                species,
                element
            );

            // 속성별 특성 적용 (HP, 시간 배속, 저항)
            if (_gameData.ElementProperties.TryGetValue(element, out var elementProps))
            {
                // HP 수정 적용
                monster.MaxHp = (long)(monster.MaxHp * elementProps.HpModifier);
                monster.CurrentHp = monster.MaxHp;

                // 시간 배속, 저항 설정
                monster.TimeScale = elementProps.TimeScale;
                monster.KeyboardResistance = elementProps.KeyboardResistance;
                monster.MouseResistance = elementProps.MouseResistance;
            }

            // 타이머 계산 (영구 스탯 시간 연장 적용)
            var permStats = saveManager?.CurrentSave?.PermanentStats;
            double timeExtend = _statGrowth.GetPermanentStatEffect("time_extend", permStats?.TimeExtendLevel ?? 0);
            int timeLimit = _gameData.Balance.TimeLimit + (int)timeExtend;

            return new MonsterSpawnResult
            {
                Monster = monster,
                SpawnType = MonsterSpawnType.Regular,
                TimeLimit = timeLimit,
                IsGoldenGoblin = false
            };
        }

        /// <summary>
        /// 황금 고블린 스폰
        /// </summary>
        private MonsterSpawnResult SpawnGoldenGoblin(int currentLevel)
        {
            var monster = _goldenGoblinManager.CreateGoldenGoblin(currentLevel);
            int timeLimit = _goldenGoblinManager.Config.TimeLimit;

            return new MonsterSpawnResult
            {
                Monster = monster,
                SpawnType = MonsterSpawnType.GoldenGoblin,
                TimeLimit = timeLimit,
                IsGoldenGoblin = true
            };
        }

        #endregion
    }

    #region Result Types

    /// <summary>
    /// 몬스터 스폰 결과
    /// </summary>
    public class MonsterSpawnResult
    {
        public Monster Monster { get; set; } = null!;
        public MonsterSpawnType SpawnType { get; set; }
        public int TimeLimit { get; set; }
        public bool IsGoldenGoblin { get; set; }
    }

    /// <summary>
    /// 몬스터 스폰 타입
    /// </summary>
    public enum MonsterSpawnType
    {
        Regular,
        GoldenGoblin
    }

    #endregion
}
