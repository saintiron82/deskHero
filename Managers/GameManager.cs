using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Threading;
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
        private readonly DispatcherTimer _timer;
        private readonly GameOverMessageManager _messageManager;
        private readonly SessionTracker _sessionTracker;
        private readonly DamageCalculator _damageCalculator;
        private readonly StatGrowthManager _statGrowth;
        private readonly ComboTracker _comboTracker;
        private readonly ConsecutiveKeyTracker _consecutiveKeyTracker;
        private readonly Random _random = new();
        private readonly GoldenGoblinManager _goldenGoblinManager;
        private Monster? _currentMonster;
        private SaveManager? _saveManager;
        private PermanentProgressionManager? _permanentProgression;
        private CompendiumManager? _compendiumManager;
        private MonsterCollection _monsterCollection = new(); // 도감 시스템
        private CollectionRewards? _collectionRewards; // 도감 보상 설정
        private bool _isGoldenGoblinActive = false; // 황금 고블린 활성화 상태

        // 입력 속도 제한 (슬라이딩 윈도우 - 1초 내 입력 수 카운트)
        private readonly Queue<long> _rateLimitTicks = new();

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
        public event EventHandler? GoldenGoblinSpawned;
        public event EventHandler? GoldenGoblinEscaped;
        public event EventHandler<GoldenGoblinRewardEventArgs>? GoldenGoblinDefeated;

        #endregion

        #region Properties

        public int CurrentLevel { get; private set; } = 1;
        public int Gold { get; private set; }
        public double RemainingTime { get; private set; }
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
            _comboTracker = new ComboTracker();

            // 연속 키 추적 초기화
            _consecutiveKeyTracker = new ConsecutiveKeyTracker(_gameData.ConsecutiveKeyPenalty);

            // 황금 고블린 매니저 초기화
            _goldenGoblinManager = new GoldenGoblinManager();

            // 도감 보상 설정 로드
            var collectionRewardsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "CollectionRewards.json");
            _collectionRewards = CollectionRewards.LoadFromFile(collectionRewardsPath);

            // 타이머 설정 (0.1초마다)
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += OnTimerTick;
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
            if (e.Source == "boss_drop")
            {
                _sessionTracker.RecordBossDropCrystals(e.Amount);
            }
            else if (e.Source.StartsWith("achievement:"))
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
            Gold = (int)_statGrowth.GetPermanentStatEffect("start_gold", permStats?.StartGoldLevel ?? 0);
            _sessionTracker.Reset();
            _rateLimitTicks.Clear();

            // 콤보 트래커 리셋
            _comboTracker.FullReset();

            SpawnMonster();
        }

        /// <summary>
        /// 키보드 입력 처리
        /// </summary>
        public void OnKeyboardInput(int vkCode = 0)
        {
            if (_currentMonster == null || !_currentMonster.IsAlive) return;

            // 입력 속도 제한 (슬라이딩 윈도우 - 1초 내 입력 수 제한)
            int maxCps = _gameData.ConsecutiveKeyPenalty.MaxCps;
            if (maxCps > 0)
            {
                long now = Stopwatch.GetTimestamp();
                long oneSecondAgo = now - Stopwatch.Frequency;
                while (_rateLimitTicks.Count > 0 && _rateLimitTicks.Peek() < oneSecondAgo)
                    _rateLimitTicks.Dequeue();

                if (_rateLimitTicks.Count >= maxCps)
                {
                    Logger.Log($"[BLOCKED] RateLimit: queue={_rateLimitTicks.Count}/{maxCps}, vkCode={vkCode}");
                    DamageDealt?.Invoke(this, new DamageEventArgs(0, false, false));
                    return;
                }
                _rateLimitTicks.Enqueue(now);
            }

            // CPS 추적 (속도 제한 통과한 유효 입력만)
            _sessionTracker.RecordInput();

            // 콤보 처리
            int comboStack = _comboTracker.ProcessInput();

            // 연속 키 페널티 계산
            double penalty = _consecutiveKeyTracker.ProcessKeyboardInput(vkCode, IsComboActive);

            var result = CalculateDamage(KeyboardPower, comboStack, AttackType.Keyboard, penalty);
            ApplyDamage(result, isMouse: false);
        }

        /// <summary>
        /// 마우스 입력 처리
        /// </summary>
        public void OnMouseInput(GameMouseButton button = GameMouseButton.None)
        {
            if (_currentMonster == null || !_currentMonster.IsAlive) return;

            // 마우스 면제가 아닌 경우만 입력 속도 제한
            if (!_gameData.ConsecutiveKeyPenalty.MouseExempt)
            {
                int maxCps = _gameData.ConsecutiveKeyPenalty.MaxCps;
                if (maxCps > 0)
                {
                    long now = Stopwatch.GetTimestamp();
                    long oneSecondAgo = now - Stopwatch.Frequency;
                    while (_rateLimitTicks.Count > 0 && _rateLimitTicks.Peek() < oneSecondAgo)
                        _rateLimitTicks.Dequeue();

                    if (_rateLimitTicks.Count >= maxCps)
                    {
                        DamageDealt?.Invoke(this, new DamageEventArgs(0, false, true));
                        return;
                    }
                    _rateLimitTicks.Enqueue(now);
                }
            }

            // CPS 추적 (속도 제한 통과한 유효 입력만)
            _sessionTracker.RecordInput();

            // 콤보 처리
            int comboStack = _comboTracker.ProcessInput();

            // 마우스는 연속 페널티 면제
            var result = CalculateDamage(MousePower, comboStack, AttackType.Mouse, 1.0);
            ApplyDamage(result, isMouse: true);
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
            return (int)(_gameData.Upgrade.BaseCost * Math.Pow(_gameData.Upgrade.CostMultiplier, currentLevel - 1));
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
            return (int)(baseCost * multiplier);
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
            _timer.Stop();
        }

        /// <summary>
        /// 타이머 재개
        /// </summary>
        public void ResumeTimer()
        {
            if (_currentMonster != null && _currentMonster.IsAlive && RemainingTime > 0)
            {
                _timer.Start();
            }
        }

        #endregion

        #region Private Methods

        private DamageResult CalculateDamage(int basePower, int comboStack = 0, AttackType attackType = AttackType.Keyboard, double consecutivePenalty = 1.0)
        {
            var permStats = _saveManager?.CurrentSave?.PermanentStats;
            var result = _damageCalculator.Calculate(basePower, permStats, 0, comboStack, attackType, _currentMonster);

            // 연속 키 페널티 적용
            if (consecutivePenalty < 1.0)
            {
                int originalDamage = result.Damage;
                int penalizedDamage = (int)(result.Damage * consecutivePenalty);

                Logger.Log($"[Damage] Original: {originalDamage}, Penalty: {consecutivePenalty:F2}, Final: {penalizedDamage}");

                result = new DamageResult
                {
                    Damage = penalizedDamage,
                    IsCritical = result.IsCritical,
                    IsMultiHit = result.IsMultiHit,
                    IsCombo = result.IsCombo,
                    ComboStack = result.ComboStack,
                    IsResisted = result.IsResisted,
                    BasePower = result.BasePower,
                    BaseAttackBonus = result.BaseAttackBonus,
                    AttackMultiplier = result.AttackMultiplier,
                    CritMultiplier = result.CritMultiplier,
                    UtilityBonus = result.UtilityBonus,
                    ResistanceModifier = result.ResistanceModifier * consecutivePenalty
                };
            }

            return result;
        }

        private void ApplyDamage(DamageResult result, bool isMouse)
        {
            if (_currentMonster == null) return;

            // 황금 고블린은 모든 업그레이드 무시, 1 데미지 고정
            int actualDamage = _isGoldenGoblinActive ? 1 : result.Damage;
            _currentMonster.TakeDamage(actualDamage);

            // 상세 데미지 기록 생성
            var record = new DamageRecord
            {
                BasePower = _isGoldenGoblinActive ? 1 : result.BasePower,
                BaseAttackBonus = _isGoldenGoblinActive ? 0 : result.BaseAttackBonus,
                AttackMultiplier = _isGoldenGoblinActive ? 0 : result.AttackMultiplier,
                IsCritical = _isGoldenGoblinActive ? false : result.IsCritical,
                CritMultiplier = _isGoldenGoblinActive ? 1 : result.CritMultiplier,
                IsMultiHit = _isGoldenGoblinActive ? false : result.IsMultiHit,
                IsCombo = _isGoldenGoblinActive ? false : result.IsCombo,
                ComboStack = _isGoldenGoblinActive ? 0 : result.ComboStack,
                FinalDamage = actualDamage,
                IsMouse = isMouse
            };

            // 세션 트래커에 상세 기록
            _sessionTracker.RecordDamageDetailed(record);

            // 데미지 이벤트 발생
            DamageDealt?.Invoke(this, new DamageEventArgs(actualDamage, result.IsCritical && !_isGoldenGoblinActive, isMouse));

            StatsChanged?.Invoke(this, EventArgs.Empty);

            if (!_currentMonster.IsAlive)
            {
                OnMonsterDefeated();
            }
        }

        private void OnMonsterDefeated()
        {
            if (_currentMonster == null) return;

            // 황금 고블린 처치 처리
            if (_isGoldenGoblinActive)
            {
                OnGoldenGoblinDefeatedInternal();
                return;
            }

            // 골드 획득 공식 (영구 스탯만 사용)
            // 기본 = 몬스터 기본 골드
            double baseGold = _currentMonster.GoldReward;

            // +가산 = 기본 + gold_flat_perm (영구)
            var permStats = _saveManager?.CurrentSave?.PermanentStats;
            double goldFlatPerm = _statGrowth.GetPermanentStatEffect("gold_flat_perm", permStats?.GoldFlatPermLevel ?? 0);
            double goldFlat = baseGold + goldFlatPerm;

            // ×배수 = +가산 × (1 + gold_multi_perm (영구))
            double goldMultiPerm = _statGrowth.GetPermanentStatEffect("gold_multi_perm", permStats?.GoldMultiPermLevel ?? 0) / 100.0;
            int goldReward = (int)(goldFlat * (1.0 + goldMultiPerm));

            Gold += goldReward;

            // 세션 트래커에 킬 기록
            _sessionTracker.RecordKill(_currentMonster.IsBoss, goldReward);

            // ✅ 모든 몬스터 처치 시 1크리스탈 지급
            _permanentProgression?.ProcessStageClear(CurrentLevel);

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

                // 종족 도감 완성 체크 및 보상
                CheckCollectionRewards(_currentMonster.Species, _currentMonster.Element);
            }

            // 보스 처치 시 크리스탈 지급 (100% 확정, 속성별 배율 적용)
            if (_currentMonster.IsBoss && _permanentProgression != null)
            {
                // ✅ 속성별 크리스탈 배율 추출
                var crystalMultipliers = _gameData.ElementProperties.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.CrystalMultiplier
                );

                // ✅ 보스 속성 전달
                var dropResult = _permanentProgression.ProcessBossKill(
                    CurrentLevel,
                    _currentMonster.Element,
                    crystalMultipliers
                );

                // ✅ 100% 지급이므로 조건 불필요
                CrystalDropped?.Invoke(this, dropResult);
            }

            // 타이머 정지
            _timer.Stop();

            // 이벤트 발생
            MonsterDefeated?.Invoke(this, EventArgs.Empty);

            // 다음 레벨
            CurrentLevel++;

            // ❌ 제거: 모든 몬스터 처치 시 1크리스탈은 이미 위(Line 503)에서 지급됨
            // 보스 처치 시 추가 보너스만 Line 523-540에서 지급

            // 즉시 리스폰
            SpawnMonster();
        }

        /// <summary>
        /// 황금 고블린 처치 처리
        /// </summary>
        private void OnGoldenGoblinDefeatedInternal()
        {
            _timer.Stop();
            _isGoldenGoblinActive = false;

            // 보상 계산 (현재 스테이지 예상 골드 기반)
            int expectedGold = CalculateStageExpectedGold(CurrentLevel);
            int reward = _goldenGoblinManager.CalculateReward(expectedGold);
            int multiplier = reward / Math.Max(expectedGold, 1);

            Gold += reward;

            // 세션 트래커에 기록
            _sessionTracker.RecordKill(false, reward);
            _sessionTracker.RecordGoldenGoblinKill(reward);

            // 황금 고블린 쿨다운 리셋 및 통계 업데이트
            _goldenGoblinManager.RecordKill(true);
            if (_saveManager?.CurrentSave != null)
            {
                _goldenGoblinManager.SaveToSave(_saveManager.CurrentSave);
                _saveManager.CurrentSave.GoldenGoblinsCaught++;
                _saveManager.CurrentSave.GoldenGoblinTotalGold += reward;
            }

            // 처치 이벤트 발생
            GoldenGoblinDefeated?.Invoke(this, new GoldenGoblinRewardEventArgs(reward, multiplier));
            MonsterDefeated?.Invoke(this, EventArgs.Empty);

            // 다음 스테이지로 진행
            CurrentLevel++;
            SpawnMonster();
        }

        /// <summary>
        /// 현재 스테이지 예상 골드 계산
        /// </summary>
        private int CalculateStageExpectedGold(int level)
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

        private void SpawnMonster()
        {
            var balance = _gameData.Balance;
            bool isBoss = CurrentLevel > 0 && CurrentLevel % balance.BossInterval == 0;

            // 황금 고블린 스폰 체크 (보스가 아닐 때만)
            if (!isBoss && _goldenGoblinManager.ShouldSpawn())
            {
                SpawnGoldenGoblin();
                return;
            }

            string species = "";
            string element = "normal";
            MonsterData selectedData;

            // Batch 시스템으로 몬스터 데이터 가져오기 (폴백 제거)
            var flattenedData = _monsterDataManager.GetRandomMonsterData(CurrentLevel, isBoss);
            selectedData = flattenedData.ToMonsterData();
            species = flattenedData.Species;
            element = flattenedData.Element;

            // 몬스터 생성 (species/element 전달)
            _currentMonster = new Monster(selectedData, CurrentLevel, isBoss, _gameData.Balance.TierHpSystem, species, element);
            _isGoldenGoblinActive = false;

            // 속성별 특성 적용 (HP, 시간 배속, 저항)
            if (_gameData.ElementProperties.TryGetValue(element, out var elementProps))
            {
                // HP 수정 적용
                _currentMonster.MaxHp = (long)(_currentMonster.MaxHp * elementProps.HpModifier);
                _currentMonster.CurrentHp = _currentMonster.MaxHp;

                // 시간 배속, 저항 설정
                _currentMonster.TimeScale = elementProps.TimeScale;
                _currentMonster.KeyboardResistance = elementProps.KeyboardResistance;
                _currentMonster.MouseResistance = elementProps.MouseResistance;
            }

            // 도감 조우 기록
            _compendiumManager?.RecordEncounter(selectedData.Id);

            // 타이머 시작 (영구 스탯 시간 연장 적용)
            var permStats = _saveManager?.CurrentSave?.PermanentStats;
            double timeExtend = _statGrowth.GetPermanentStatEffect("time_extend", permStats?.TimeExtendLevel ?? 0);
            int timeLimit = _gameData.Balance.TimeLimit + (int)timeExtend;
            RemainingTime = timeLimit;
            _timer.Start();

            MonsterSpawned?.Invoke(this, EventArgs.Empty);
            StatsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 황금 고블린 스폰
        /// </summary>
        private void SpawnGoldenGoblin()
        {
            _currentMonster = _goldenGoblinManager.CreateGoldenGoblin(CurrentLevel);
            _isGoldenGoblinActive = true;

            // 황금 고블린 전용 타이머 (10초 고정)
            RemainingTime = _goldenGoblinManager.Config.TimeLimit;
            _timer.Start();

            // 이벤트 발생
            GoldenGoblinSpawned?.Invoke(this, EventArgs.Empty);
            MonsterSpawned?.Invoke(this, EventArgs.Empty);
            StatsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            // 시간 배속 적용 (Wind 속성 몬스터 등)
            double timeScale = _currentMonster?.TimeScale ?? 1.0;
            RemainingTime -= 0.1 * timeScale;
            TimerTick?.Invoke(this, EventArgs.Empty);

            if (RemainingTime <= 0)
            {
                // 황금 고블린 시간 초과 시 도주 처리 (게임오버 아님)
                if (_isGoldenGoblinActive)
                {
                    OnGoldenGoblinEscaped();
                    return;
                }

                // 일반 시간 초과 - 게임 오버 시퀀스 시작
                TriggerGameOver();
            }
        }

        /// <summary>
        /// 황금 고블린 도주 처리 (시간 초과)
        /// </summary>
        private void OnGoldenGoblinEscaped()
        {
            _timer.Stop();
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

        private void TriggerGameOver()
        {
            _timer.Stop();
            // UI에서 애니메이션 재생 후 RestartGame()을 호출하도록 유도
            GameOver?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 도감 완성 체크 및 보상 지급
        /// </summary>
        private void CheckCollectionRewards(string species, string element)
        {
            if (_collectionRewards == null || _permanentProgression == null) return;

            // 첫 Holy/Dark 조우 마일스톤
            if ((element == "holy" || element == "dark") &&
                _monsterCollection.KillCounts.TryGetValue(species, out var kills) &&
                kills.TryGetValue(element, out var count) && count == 1)
            {
                string milestoneId = $"first_{element}";
                if (!_monsterCollection.HasClaimedReward(milestoneId) &&
                    _collectionRewards.MilestoneRewards.TryGetValue(milestoneId, out var milestone))
                {
                    _permanentProgression.AddCrystals(milestone.Crystals, $"collection_{milestoneId}");
                    _monsterCollection.ClaimReward(milestoneId);
                    _sessionTracker.RecordAchievementCrystals(milestone.Crystals);
                }
            }

            // 종족 도감 완성 보상
            if (_monsterCollection.IsSpeciesComplete(species) &&
                !_monsterCollection.HasClaimedReward($"species_{species}") &&
                _collectionRewards.SpeciesCompletion.TryGetValue(species, out var speciesReward))
            {
                _permanentProgression.AddCrystals(speciesReward.Rewards.Crystals, $"collection_species_{species}");
                _monsterCollection.ClaimReward($"species_{species}");
                _sessionTracker.RecordAchievementCrystals(speciesReward.Rewards.Crystals);

                // TODO: 칭호 및 영구 보너스 적용 (향후 구현)
            }

            // 모든 속성 최소 1마리 처치 마일스톤
            var allElements = new[] { "normal", "fire", "ice", "wind", "holy", "dark" };
            bool hasAllElements = allElements.All(elem =>
                _monsterCollection.EncounteredVariations.Values.Any(set => set.Contains(elem)));

            if (hasAllElements &&
                !_monsterCollection.HasClaimedReward("all_elements_unlocked") &&
                _collectionRewards.MilestoneRewards.TryGetValue("all_elements_unlocked", out var allElementsMilestone))
            {
                _permanentProgression.AddCrystals(allElementsMilestone.Crystals, "collection_all_elements");
                _monsterCollection.ClaimReward("all_elements_unlocked");
                _sessionTracker.RecordAchievementCrystals(allElementsMilestone.Crystals);
            }
        }

        #endregion
    }
}
