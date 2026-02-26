using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DeskWarrior.Helpers;
using DeskWarrior.Interfaces;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 게임 상태 및 로직 관리
    /// </summary>
    public class GameManager : IGameManager
    {
        #region Fields

        private readonly GameData _gameData;
        private readonly List<HeroData> _heroes = new();
        private readonly MonsterDataManager _monsterDataManager;
        private readonly GameLoopManager _gameLoopManager;
        private readonly GameOverMessageManager _messageManager;
        private readonly SessionTracker _sessionTracker;
        private readonly DamageCalculator _damageCalculator;
        private readonly StatGrowthManager _statGrowth;
        private readonly ComboTracker _comboTracker;
        private readonly ConsecutiveKeyTracker _consecutiveKeyTracker;
        private readonly CombatManager _combatManager;
        private readonly Random _random = new();
        private readonly GoldenGoblinManager _goldenGoblinManager;
        private readonly MonsterSpawnManager _monsterSpawnManager;
        private Monster? _currentMonster;
        private SaveManager? _saveManager;
        private PermanentProgressionManager? _permanentProgression;
        private CompendiumManager? _compendiumManager;
        private MonsterCollection _monsterCollection = new(); // 도감 시스템
        private CollectionRewards? _collectionRewards; // 도감 보상 설정
        private RewardManager? _rewardManager; // 보상 관리자
        private bool _isGoldenGoblinActive = false; // 황금 고블린 활성화 상태

        // 인게임 스탯 (세션마다 리셋)
        private InGameStats _inGameStats = new();

        #endregion

        #region Events

        public event EventHandler? MonsterDefeated;
        public event EventHandler? MonsterSpawned;
        public event EventHandler? TimerTick;
        public event EventHandler? GameOver;
        public event EventHandler? StatsChanged;
        public event EventHandler<DamageEventArgs>? DamageDealt;
        public event EventHandler<BossDropResult>? CrystalDropped;
        public event EventHandler<GoldenGoblinSpawnEventArgs>? GoldenGoblinSpawned;
        public event EventHandler? GoldenGoblinEscaped;
        public event EventHandler<GoldenGoblinRewardEventArgs>? GoldenGoblinDefeated;

        #endregion

        #region Properties

        public int CurrentLevel { get; private set; } = 1;
        public long Gold { get; private set; }
        public double RemainingTime => _gameLoopManager.RemainingTime;
        public Monster? CurrentMonster => _currentMonster;
        public GameData Config => _gameData;
        public GameData GameData => _gameData;
        public System.Collections.Generic.List<HeroData> Heroes => _heroes;

        /// <summary>
        /// 몬스터 데이터 매니저 (배치 시스템)
        /// </summary>
        public MonsterDataManager MonsterDataManager => _monsterDataManager;

        // 인게임 스탯 접근자
        public InGameStats InGameStats => _inGameStats;

        public int KeyboardPower
        {
            get
            {
                int basePower = 1 + (int)_statGrowth.GetInGameStatEffect("keyboard_power", _inGameStats.KeyboardPowerLevel);
                int baseAttack = (int)_statGrowth.GetPermanentStatEffect("base_attack", _saveManager?.CurrentSave?.PermanentStats?.BaseAttackLevel ?? 0);
                return basePower + baseAttack;
            }
        }

        public int MousePower
        {
            get
            {
                int basePower = 1 + (int)_statGrowth.GetInGameStatEffect("mouse_power", _inGameStats.MousePowerLevel);
                int baseAttack = (int)_statGrowth.GetPermanentStatEffect("base_attack", _saveManager?.CurrentSave?.PermanentStats?.BaseAttackLevel ?? 0);
                return basePower + baseAttack;
            }
        }

        // 콤보 시스템 접근자
        public int CurrentComboStack => _comboTracker.ComboStack;
        public bool IsComboActive => _comboTracker.IsComboActive;

        // Session Stats (위임)
        public long SessionDamage => _sessionTracker.TotalDamage;
        public long SessionTotalGold => _sessionTracker.TotalGold;
        public int SessionKills => _sessionTracker.MonstersKilled;
        public int SessionBossKills => _sessionTracker.BossesKilled;
        public int SessionKeyboardInputs => _sessionTracker.KeyboardInputs;
        public int SessionMouseInputs => _sessionTracker.MouseInputs;
        public int SessionCriticalHits => _sessionTracker.CriticalHits;
        public DateTime SessionStartTime => _sessionTracker.StartTime;
        public int SessionBossDropCrystals => _sessionTracker.SessionBossDropCrystals;
        public int SessionAchievementCrystals => _sessionTracker.SessionAchievementCrystals;
        public int SessionStageClearCrystals => _sessionTracker.SessionStageClearCrystals;
        public System.Collections.Generic.IReadOnlyCollection<DamageRecord> SessionDamageRecords => _sessionTracker.DamageRecords;
        public double SessionCPS => _sessionTracker.CurrentCPS;

        #endregion

        #region Constructor

        public GameManager()
        {
            // 설정 로드
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "GameData.json");
            _gameData = GameData.LoadFromFile(configPath);
            Logger.Log($"[GameData] MaxCps={_gameData.ConsecutiveKeyPenalty.MaxCps}, MouseExempt={_gameData.ConsecutiveKeyPenalty.MouseExempt}");

            // 배치 기반 몬스터 데이터 로드 (필수)
            _monsterDataManager = new MonsterDataManager();
            _monsterDataManager.SetGameData(_gameData);
            _monsterDataManager.LoadBatchIndex();
            _monsterDataManager.LoadAllEnabledBatches();

            if (_monsterDataManager.LoadedBatchCount == 0)
            {
                throw new InvalidOperationException(
                    "Failed to load monster batch data. " +
                    "Please ensure config/monsters/_index.json and batch_01.json exist."
                );
            }

            // Heroes 데이터 로드
            var heroesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "Heroes.json");
            var heroesJson = File.ReadAllText(heroesPath);
            var heroesRoot = JsonSerializer.Deserialize<HeroesRoot>(heroesJson) ?? new HeroesRoot();
            _heroes.AddRange(heroesRoot.Heroes);

            // 메시지 매니저 초기화
            _messageManager = new GameOverMessageManager();

            // 세션 트래커 초기화
            _sessionTracker = new SessionTracker();

            // 스탯 성장 매니저 초기화 (데미지 계산기보다 먼저 초기화)
            _statGrowth = new StatGrowthManager();

            // 데미지 계산기 초기화
            _damageCalculator = new DamageCalculator(_gameData, _random, _statGrowth);

            // 콤보 트래커 초기화
            _comboTracker = new ComboTracker(_gameData.Combo);

            // 연속 키 추적 초기화
            _consecutiveKeyTracker = new ConsecutiveKeyTracker(_gameData.ConsecutiveKeyPenalty);

            // 황금 고블린 매니저 초기화
            _goldenGoblinManager = new GoldenGoblinManager();

            // 몬스터 스폰 매니저 초기화
            _monsterSpawnManager = new MonsterSpawnManager(
                _gameData,
                _monsterDataManager,
                _goldenGoblinManager,
                _statGrowth,
                _random
            );

            // 전투 매니저 초기화
            _combatManager = new CombatManager(
                _gameData,
                _damageCalculator,
                _comboTracker,
                _consecutiveKeyTracker,
                _statGrowth,
                _sessionTracker
            );

            // 전투 매니저 이벤트 구독
            _combatManager.DamageDealt += (sender, e) => DamageDealt?.Invoke(this, e);
            _combatManager.StatsChanged += (sender, e) => StatsChanged?.Invoke(this, e);

            // 도감 보상 설정 로드
            var collectionRewardsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "CollectionRewards.json");
            _collectionRewards = CollectionRewards.LoadFromFile(collectionRewardsPath);

            // 게임 루프 매니저 초기화
            _gameLoopManager = new GameLoopManager();
            _gameLoopManager.TimerTick += (sender, e) => TimerTick?.Invoke(this, e);
            _gameLoopManager.GameOver += (sender, e) => GameOver?.Invoke(this, e);
            _gameLoopManager.GoldenGoblinEscaped += OnGoldenGoblinEscaped;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// SaveManager 및 PermanentProgressionManager 초기화
        /// </summary>
        public void Initialize(SaveManager saveManager)
        {
            _saveManager = saveManager;
            _permanentProgression = new PermanentProgressionManager(saveManager);
            _compendiumManager = new CompendiumManager(saveManager);

            // RewardManager 초기화
            _rewardManager = new RewardManager(
                _permanentProgression,
                _statGrowth,
                _monsterCollection,
                _collectionRewards!,
                _sessionTracker,
                _goldenGoblinManager,
                _monsterSpawnManager,
                saveManager
            );

            // RewardManager 이벤트 구독
            _rewardManager.CrystalDropped += (sender, e) => CrystalDropped?.Invoke(this, e);

            // 황금 고블린 쿨다운 로드
            if (saveManager.CurrentSave != null)
            {
                _goldenGoblinManager.LoadFromSave(saveManager.CurrentSave);
            }

            // 크리스탈 획득 이벤트 구독 (세션 트래커에 기록)
            if (_permanentProgression != null)
            {
                _permanentProgression.CrystalEarned += OnCrystalEarned;
            }
        }

        /// <summary>
        /// CompendiumManager 접근자
        /// </summary>
        public CompendiumManager? CompendiumManager => _compendiumManager;

        /// <summary>
        /// 크리스탈 획득 시 세션 트래커에 기록
        /// </summary>
        private void OnCrystalEarned(object? sender, CrystalEarnedEventArgs e)
        {
            if (e.Source == "boss_kill")
            {
                _sessionTracker.RecordBossDropCrystals(e.Amount);
            }
            else if (e.Source == "stage_clear")
            {
                _sessionTracker.RecordStageClearCrystals(e.Amount);
            }
            else if (e.Source.StartsWith("achievement:") || e.Source.StartsWith("collection_"))
            {
                _sessionTracker.RecordAchievementCrystals(e.Amount);
            }
        }

        /// <summary>
        /// PermanentProgressionManager 접근자
        /// </summary>
        public PermanentProgressionManager? PermanentProgression => _permanentProgression;

        /// <summary>
        /// 게임 시작
        /// </summary>
        public void StartGame()
        {
            var permStats = _saveManager?.CurrentSave?.PermanentStats;

            // 인게임 스탯 리셋 및 시작 보너스 적용
            _inGameStats.Reset();
            _inGameStats.KeyboardPowerLevel = (int)_statGrowth.GetPermanentStatEffect("start_keyboard", permStats?.StartKeyboardLevel ?? 0);
            _inGameStats.MousePowerLevel = (int)_statGrowth.GetPermanentStatEffect("start_mouse", permStats?.StartMouseLevel ?? 0);

            int startLevel = (int)_statGrowth.GetPermanentStatEffect("start_level", permStats?.StartLevelLevel ?? 0);
            int maxLevel = _saveManager?.CurrentSave?.Stats.MaxLevel ?? 0;
            if (maxLevel > 0)
            {
                startLevel = Math.Min(startLevel, maxLevel);
            }
            CurrentLevel = 1 + startLevel;
            Gold = Helpers.SafeMath.ToLong(_statGrowth.GetPermanentStatEffect("start_gold", permStats?.StartGoldLevel ?? 0));
            _sessionTracker.Reset();
            _combatManager.ResetRateLimit();

            // 콤보 트래커 리셋
            _comboTracker.FullReset();

            SpawnMonster();
        }

        /// <summary>
        /// 키보드 입력 처리
        /// </summary>
        public void OnKeyboardInput(int vkCode = 0)
        {
            var permStats = _saveManager?.CurrentSave?.PermanentStats;
            var result = _combatManager.ProcessKeyboardInput(
                _currentMonster,
                KeyboardPower,
                permStats,
                _isGoldenGoblinActive,
                vkCode
            );

            if (result.HasValue && _currentMonster != null && !_currentMonster.IsAlive)
            {
                OnMonsterDefeated();
            }
        }

        /// <summary>
        /// 마우스 입력 처리
        /// </summary>
        public void OnMouseInput(GameMouseButton button = GameMouseButton.None)
        {
            var permStats = _saveManager?.CurrentSave?.PermanentStats;
            var result = _combatManager.ProcessMouseInput(
                _currentMonster,
                MousePower,
                permStats,
                _isGoldenGoblinActive,
                button
            );

            if (result.HasValue && _currentMonster != null && !_currentMonster.IsAlive)
            {
                OnMonsterDefeated();
            }
        }

        /// <summary>
        /// 인게임 스탯 업그레이드
        /// </summary>
        public bool UpgradeInGameStat(string statId)
        {
            int currentLevel = GetInGameStatLevel(statId);
            var discountPercent = _saveManager?.CurrentSave?.PermanentStats?.GetUpgradeCostReduction();
            int baseCost = _statGrowth.GetInGameUpgradeCost(statId, currentLevel, discountPercent);
            int cost = ApplyStageCostMultiplier(baseCost);

            if (!_statGrowth.CanUpgradeInGameStat(statId, currentLevel))
                return false;

            if (Gold >= cost)
            {
                Gold -= cost;
                SetInGameStatLevel(statId, currentLevel + 1);
                StatsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 키보드 공격력 업그레이드 (레거시 호환)
        /// </summary>
        public bool UpgradeKeyboardPower() => UpgradeInGameStat("keyboard_power");

        /// <summary>
        /// 마우스 공격력 업그레이드 (레거시 호환)
        /// </summary>
        public bool UpgradeMousePower() => UpgradeInGameStat("mouse_power");

        /// <summary>
        /// 업그레이드 로드 (레거시 호환)
        /// </summary>
        public void LoadUpgrades(int keyboardPower, int mousePower)
        {
            _inGameStats.KeyboardPowerLevel = keyboardPower > 0 ? keyboardPower - 1 : 0;
            _inGameStats.MousePowerLevel = mousePower > 0 ? mousePower - 1 : 0;
        }

        /// <summary>
        /// 업그레이드 비용 계산 (레거시 호환)
        /// </summary>
        public int CalculateUpgradeCost(int currentLevel)
        {
            return Helpers.SafeMath.CostToInt(_gameData.Upgrade.BaseCost * Math.Pow(_gameData.Upgrade.CostMultiplier, currentLevel - 1));
        }

        /// <summary>
        /// 인게임 스탯 업그레이드 비용 조회
        /// </summary>
        public int GetInGameStatUpgradeCost(string statId)
        {
            int currentLevel = GetInGameStatLevel(statId);
            var discountPercent = _saveManager?.CurrentSave?.PermanentStats?.GetUpgradeCostReduction();
            int baseCost = _statGrowth.GetInGameUpgradeCost(statId, currentLevel, discountPercent);
            return ApplyStageCostMultiplier(baseCost);
        }

        /// <summary>
        /// 스테이지 구간별 업그레이드 비용 배율 적용
        /// 50스테이지마다 비용 2배 증가 (로그라이크 진행 벽)
        /// </summary>
        private int ApplyStageCostMultiplier(int baseCost)
        {
            int interval = _gameData.Balance.UpgradeCostInterval;
            if (interval <= 0) interval = 50;  // 기본값

            int tier = (CurrentLevel - 1) / interval;
            double tierMultiplier = _gameData.Balance.UpgradeCostTierMultiplier;
            if (tierMultiplier <= 1.0) tierMultiplier = 2.0;
            double multiplier = Math.Pow(tierMultiplier, tier);
            return Helpers.SafeMath.CostToInt(baseCost * multiplier);
        }

        /// <summary>
        /// 인게임 스탯 레벨 조회
        /// </summary>
        private int GetInGameStatLevel(string statId) => statId switch
        {
            "keyboard_power" => _inGameStats.KeyboardPowerLevel,
            "mouse_power" => _inGameStats.MousePowerLevel,
            _ => 0
        };

        /// <summary>
        /// 인게임 스탯 레벨 설정
        /// </summary>
        private void SetInGameStatLevel(string statId, int level)
        {
            switch (statId)
            {
                case "keyboard_power": _inGameStats.KeyboardPowerLevel = level; break;
                case "mouse_power": _inGameStats.MousePowerLevel = level; break;
            }
        }

        /// <summary>
        /// 게임 재시작
        /// </summary>
        public void RestartGame()
        {
            // StartGame 호출로 통합 (영구 스탯 시작 보너스 자동 적용)
            StartGame();
        }

        /// <summary>
        /// 현재 세션 데이터 생성 (게임 오버 시 호출)
        /// </summary>
        public SessionStats CreateSessionStats(string endReason = "timeout")
        {
            return _sessionTracker.ToSessionStats(CurrentLevel, endReason);
        }

        /// <summary>
        /// 게임 오버 메시지 생성
        /// </summary>
        /// <param name="deathType">사망 타입 ("boss", "timeout", "normal")</param>
        /// <returns>선택된 메시지</returns>
        public string GetGameOverMessage(string? deathType = null)
        {
            return _messageManager.SelectMessage(
                CurrentLevel,
                SessionTotalGold,
                SessionDamage,
                SessionKills,
                deathType
            );
        }

        /// <summary>
        /// 타이머 일시정지
        /// </summary>
        public void PauseTimer()
        {
            _gameLoopManager.PauseTimer();
        }

        /// <summary>
        /// 타이머 재개
        /// </summary>
        public void ResumeTimer()
        {
            _gameLoopManager.ResumeTimer();
        }

        #endregion

        #region Private Methods

        private void OnMonsterDefeated()
        {
            if (_currentMonster == null || _rewardManager == null) return;

            // 황금 고블린 처치 처리
            if (_isGoldenGoblinActive)
            {
                OnGoldenGoblinDefeatedInternal();
                return;
            }

            // 골드 획득 (RewardManager 위임)
            long goldReward = _rewardManager.CalculateMonsterGoldReward(_currentMonster);
            Gold = Helpers.SafeMath.AddLong(Gold, goldReward);

            // 세션 트래커에 킬 기록
            _sessionTracker.RecordKill(_currentMonster.IsBoss, goldReward);

            // 모든 몬스터 처치 시 크리스탈 지급 (RewardManager 위임)
            _rewardManager.ProcessStageClearReward(CurrentLevel);

            // 황금 고블린 쿨다운 카운터 업데이트
            _goldenGoblinManager.RecordKill(false);
            if (_saveManager?.CurrentSave != null)
            {
                _goldenGoblinManager.SaveToSave(_saveManager.CurrentSave);
            }

            // 도감 처치 기록 (레거시)
            _compendiumManager?.RecordKill(_currentMonster.Id, _currentMonster.TotalDamageTaken);

            // 몬스터 도감 처치 기록 (신규 속성 시스템)
            if (!string.IsNullOrEmpty(_currentMonster.Species) && !string.IsNullOrEmpty(_currentMonster.Element))
            {
                _monsterCollection.RecordKill(_currentMonster.Species, _currentMonster.Element);

                // 종족 도감 완성 체크 및 보상 (RewardManager 위임)
                _rewardManager.CheckAndGrantCollectionRewards(_currentMonster.Species, _currentMonster.Element);
            }

            // 보스 처치 시 크리스탈 지급 (RewardManager 위임)
            if (_currentMonster.IsBoss)
            {
                _rewardManager.ProcessBossKillReward(
                    CurrentLevel,
                    _currentMonster.Element,
                    _gameData.ElementProperties
                );
            }

            // 타이머 정지
            _gameLoopManager.StopTimer();

            // 이벤트 발생
            MonsterDefeated?.Invoke(this, EventArgs.Empty);

            // 다음 레벨
            CurrentLevel++;

            // 즉시 리스폰
            SpawnMonster();
        }

        /// <summary>
        /// 황금 고블린 처치 처리
        /// </summary>
        private void OnGoldenGoblinDefeatedInternal()
        {
            if (_rewardManager == null) return;

            _gameLoopManager.StopTimer();
            _isGoldenGoblinActive = false;

            // 보상 계산 (스폰 시 결정된 배수 사용)
            int predeterminedMultiplier = _currentMonster?.RewardMultiplier ?? 0;
            var (reward, multiplier) = _rewardManager.CalculateGoldenGoblinReward(CurrentLevel, predeterminedMultiplier);

            Gold = Helpers.SafeMath.AddLong(Gold, reward);

            // 세션 트래커에 기록
            _sessionTracker.RecordKill(false, reward);
            _sessionTracker.RecordGoldenGoblinKill(reward);

            // 황금 고블린 쿨다운 리셋 및 통계 업데이트
            _goldenGoblinManager.RecordKill(true);
            if (_saveManager?.CurrentSave != null)
            {
                _goldenGoblinManager.SaveToSave(_saveManager.CurrentSave);
                _saveManager.CurrentSave.GoldenGoblinsCaught++;
                _saveManager.CurrentSave.GoldenGoblinTotalGold = Helpers.SafeMath.AddLong(_saveManager.CurrentSave.GoldenGoblinTotalGold, reward);
            }

            // 처치 이벤트 발생 (등급 정보 포함)
            string? gradeId = _currentMonster?.GradeId;
            GoldenGoblinDefeated?.Invoke(this, new GoldenGoblinRewardEventArgs(reward, multiplier, gradeId));
            MonsterDefeated?.Invoke(this, EventArgs.Empty);

            // 다음 스테이지로 진행
            CurrentLevel++;
            SpawnMonster();
        }

        private void SpawnMonster()
        {
            // MonsterSpawnManager를 통해 몬스터 스폰
            var spawnResult = _monsterSpawnManager.SpawnMonster(CurrentLevel, _saveManager);

            _currentMonster = spawnResult.Monster;
            _isGoldenGoblinActive = spawnResult.IsGoldenGoblin;

            // 도감 조우 기록
            _compendiumManager?.RecordEncounter(_currentMonster.Id);

            // 게임 루프 매니저 초기화 및 타이머 시작
            _gameLoopManager.InitializeForMonster(_currentMonster, spawnResult.TimeLimit, spawnResult.IsGoldenGoblin);

            // 이벤트 발생
            if (spawnResult.IsGoldenGoblin)
            {
                var spawnArgs = new GoldenGoblinSpawnEventArgs(
                    _currentMonster.GradeId,
                    _currentMonster.GradeBackground,
                    _currentMonster.GradeBorderColor,
                    _currentMonster.GradeNameColor,
                    _currentMonster.RewardMultiplier);
                GoldenGoblinSpawned?.Invoke(this, spawnArgs);
            }
            MonsterSpawned?.Invoke(this, EventArgs.Empty);
            StatsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 황금 고블린 도주 처리 (시간 초과)
        /// </summary>
        private void OnGoldenGoblinEscaped(object? sender, EventArgs e)
        {
            _isGoldenGoblinActive = false;

            // 쿨다운 카운터 갱신 (황금 고블린 처치 실패)
            _goldenGoblinManager.RecordKill(false);

            // 저장 데이터 업데이트
            if (_saveManager?.CurrentSave != null)
            {
                _goldenGoblinManager.SaveToSave(_saveManager.CurrentSave);
            }

            // 도주 이벤트 발생
            GoldenGoblinEscaped?.Invoke(this, EventArgs.Empty);

            // 다음 스테이지로 진행 (게임오버 없음)
            CurrentLevel++;
            SpawnMonster();
        }

        #endregion
    }
}
