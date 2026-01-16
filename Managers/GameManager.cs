using System;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;
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
        private readonly CharacterDataRoot _characterData;
        private readonly DispatcherTimer _timer;
        private readonly GameOverMessageManager _messageManager;
        private readonly SessionTracker _sessionTracker;
        private readonly DamageCalculator _damageCalculator;
        private readonly StatGrowthManager _statGrowth;
        private readonly ComboTracker _comboTracker;
        private readonly Random _random = new();
        private Monster? _currentMonster;
        private SaveManager? _saveManager;
        private PermanentProgressionManager? _permanentProgression;

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

        #endregion

        #region Properties

        public int CurrentLevel { get; private set; } = 1;
        public int Gold { get; private set; }
        public int RemainingTime { get; private set; }
        public Monster? CurrentMonster => _currentMonster;
        public GameData Config => _gameData;
        public GameData GameData => _gameData;
        public System.Collections.Generic.List<HeroData> Heroes => _characterData.Heroes;

        // 인게임 스탯 접근자
        public InGameStats InGameStats => _inGameStats;
        public int KeyboardPower => 1 + (int)_statGrowth.GetInGameStatEffect("keyboard_power", _inGameStats.KeyboardPowerLevel);
        public int MousePower => 1 + (int)_statGrowth.GetInGameStatEffect("mouse_power", _inGameStats.MousePowerLevel);
        public double GoldFlat => _statGrowth.GetInGameStatEffect("gold_flat", _inGameStats.GoldFlatLevel);
        public double GoldMulti => _statGrowth.GetInGameStatEffect("gold_multi", _inGameStats.GoldMultiLevel) / 100.0;
        public double TimeThief => _statGrowth.GetInGameStatEffect("time_thief", _inGameStats.TimeThiefLevel);
        public double ComboFlex => _statGrowth.GetInGameStatEffect("combo_flex", _inGameStats.ComboFlexLevel);
        public double ComboDamage => _statGrowth.GetInGameStatEffect("combo_damage", _inGameStats.ComboDamageLevel) / 100.0;

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

        #endregion

        #region Constructor

        public GameManager()
        {
            // 설정 로드
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "GameData.json");
            _gameData = GameData.LoadFromFile(configPath);

            // 캐릭터 데이터 로드
            var characterDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "CharacterData.json");
            var json = File.ReadAllText(characterDataPath);
            _characterData = JsonSerializer.Deserialize<CharacterDataRoot>(json) ?? new CharacterDataRoot();

            // 메시지 매니저 초기화
            _messageManager = new GameOverMessageManager();

            // 세션 트래커 초기화
            _sessionTracker = new SessionTracker();

            // 데미지 계산기 초기화
            _damageCalculator = new DamageCalculator(_gameData, _random);

            // 스탯 성장 매니저 초기화
            _statGrowth = new StatGrowthManager();

            // 콤보 트래커 초기화
            _comboTracker = new ComboTracker();

            // 타이머 설정 (1초마다)
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
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

            // 크리스탈 획득 이벤트 구독 (세션 트래커에 기록)
            if (_permanentProgression != null)
            {
                _permanentProgression.CrystalEarned += OnCrystalEarned;
            }
        }

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
            _inGameStats.GoldFlatLevel = (int)_statGrowth.GetPermanentStatEffect("start_gold_flat", permStats?.StartGoldFlatLevel ?? 0);
            _inGameStats.GoldMultiLevel = (int)_statGrowth.GetPermanentStatEffect("start_gold_multi", permStats?.StartGoldMultiLevel ?? 0);
            _inGameStats.ComboFlexLevel = (int)_statGrowth.GetPermanentStatEffect("start_combo_flex", permStats?.StartComboFlexLevel ?? 0);
            _inGameStats.ComboDamageLevel = (int)_statGrowth.GetPermanentStatEffect("start_combo_damage", permStats?.StartComboDamageLevel ?? 0);

            CurrentLevel = 1 + (int)_statGrowth.GetPermanentStatEffect("start_level", permStats?.StartLevelLevel ?? 0);
            Gold = (int)_statGrowth.GetPermanentStatEffect("start_gold", permStats?.StartGoldLevel ?? 0);
            _sessionTracker.Reset();

            // 콤보 트래커 리셋 및 설정
            _comboTracker.FullReset();
            _comboTracker.SetComboFlexBonus(ComboFlex);

            SpawnMonster();
        }

        /// <summary>
        /// 키보드 입력 처리
        /// </summary>
        public void OnKeyboardInput()
        {
            if (_currentMonster == null || !_currentMonster.IsAlive) return;

            // 콤보 처리
            int comboStack = _comboTracker.ProcessInput();

            var result = CalculateDamage(KeyboardPower, comboStack);
            ApplyDamage(result.Damage, result.IsCritical, isMouse: false);
        }

        /// <summary>
        /// 마우스 입력 처리
        /// </summary>
        public void OnMouseInput()
        {
            if (_currentMonster == null || !_currentMonster.IsAlive) return;

            // 콤보 처리
            int comboStack = _comboTracker.ProcessInput();

            var result = CalculateDamage(MousePower, comboStack);
            ApplyDamage(result.Damage, result.IsCritical, isMouse: true);
        }

        /// <summary>
        /// 인게임 스탯 업그레이드
        /// </summary>
        public bool UpgradeInGameStat(string statId)
        {
            int currentLevel = GetInGameStatLevel(statId);
            var discountPercent = _saveManager?.CurrentSave?.PermanentStats?.UpgradeCostReduction;
            int cost = _statGrowth.GetInGameUpgradeCost(statId, currentLevel, discountPercent);

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
            var discountPercent = _saveManager?.CurrentSave?.PermanentStats?.UpgradeCostReduction;
            return _statGrowth.GetInGameUpgradeCost(statId, currentLevel, discountPercent);
        }

        /// <summary>
        /// 인게임 스탯 레벨 조회
        /// </summary>
        private int GetInGameStatLevel(string statId) => statId switch
        {
            "keyboard_power" => _inGameStats.KeyboardPowerLevel,
            "mouse_power" => _inGameStats.MousePowerLevel,
            "gold_flat" => _inGameStats.GoldFlatLevel,
            "gold_multi" => _inGameStats.GoldMultiLevel,
            "time_thief" => _inGameStats.TimeThiefLevel,
            "combo_flex" => _inGameStats.ComboFlexLevel,
            "combo_damage" => _inGameStats.ComboDamageLevel,
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
                case "gold_flat": _inGameStats.GoldFlatLevel = level; break;
                case "gold_multi": _inGameStats.GoldMultiLevel = level; break;
                case "time_thief": _inGameStats.TimeThiefLevel = level; break;
                case "combo_flex": _inGameStats.ComboFlexLevel = level; break;
                case "combo_damage": _inGameStats.ComboDamageLevel = level; break;
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

        #endregion

        #region Private Methods

        private DamageResult CalculateDamage(int basePower, int comboStack = 0)
        {
            var permStats = _saveManager?.CurrentSave?.PermanentStats;
            return _damageCalculator.Calculate(basePower, permStats, ComboDamage, comboStack);
        }

        private void ApplyDamage(int damage, bool isCritical, bool isMouse)
        {
            if (_currentMonster == null) return;

            _currentMonster.TakeDamage(damage);

            // 세션 트래커에 기록
            _sessionTracker.RecordDamage(damage, isCritical, isMouse);

            // 데미지 이벤트 발생
            DamageDealt?.Invoke(this, new DamageEventArgs(damage, isCritical, isMouse));

            StatsChanged?.Invoke(this, EventArgs.Empty);

            if (!_currentMonster.IsAlive)
            {
                OnMonsterDefeated();
            }
        }

        private void OnMonsterDefeated()
        {
            if (_currentMonster == null) return;

            // 골드 획득 공식 (STAT_SYSTEM.md 기준)
            // 기본 = 몬스터 기본 골드
            double baseGold = _currentMonster.GoldReward;

            // +가산 = 기본 + gold_flat (인게임) + gold_flat_perm (영구)
            var permStats = _saveManager?.CurrentSave?.PermanentStats;
            double goldFlatPerm = _statGrowth.GetPermanentStatEffect("gold_flat_perm", permStats?.GoldFlatPermLevel ?? 0);
            double goldFlat = baseGold + GoldFlat + goldFlatPerm;

            // ×배수 = +가산 × (1 + gold_multi (인게임) + gold_multi_perm (영구))
            double goldMultiPerm = _statGrowth.GetPermanentStatEffect("gold_multi_perm", permStats?.GoldMultiPermLevel ?? 0) / 100.0;
            int goldReward = (int)(goldFlat * (1.0 + GoldMulti + goldMultiPerm));

            Gold += goldReward;

            // 시간 도둑: 처치 시 시간 추가 (최대 기본 시간까지)
            if (_inGameStats.TimeThiefLevel > 0)
            {
                int baseTimeLimit = _gameData.Balance.TimeLimit + (permStats?.GameOverTimeExtension ?? 0);
                double maxAddTime = _statGrowth.CalculateTimeThiefCap(baseTimeLimit);
                double currentAddTime = Math.Min(TimeThief, maxAddTime);
                RemainingTime = Math.Min(RemainingTime + (int)currentAddTime, baseTimeLimit);
            }

            // 세션 트래커에 킬 기록
            _sessionTracker.RecordKill(_currentMonster.IsBoss, goldReward);

            // 보스 처치 시 크리스탈 드롭 처리
            if (_currentMonster.IsBoss && _permanentProgression != null)
            {
                var dropResult = _permanentProgression.ProcessBossKill(CurrentLevel);
                if (dropResult.Dropped)
                {
                    // UI에 드롭 알림 표시 (이벤트 발생)
                    CrystalDropped?.Invoke(this, dropResult);
                }
            }

            // 타이머 정지
            _timer.Stop();

            // 이벤트 발생
            MonsterDefeated?.Invoke(this, EventArgs.Empty);

            // 다음 레벨
            CurrentLevel++;

            // 즉시 리스폰
            SpawnMonster();
        }

        private void SpawnMonster()
        {
            var balance = _gameData.Balance;
            bool isBoss = CurrentLevel > 0 && CurrentLevel % balance.BossInterval == 0;

            MonsterData selectedData;
            if (isBoss && _characterData.Bosses.Count > 0)
            {
                // 보스 레벨: 랜덤하게 보스 선택
                int bossIndex = _random.Next(_characterData.Bosses.Count);
                selectedData = _characterData.Bosses[bossIndex];
            }
            else if (_characterData.Monsters.Count > 0)
            {
                // 일반 몬스터: 레벨 기반 순환 인덱스
                int monsterIndex = (CurrentLevel - 1) % _characterData.Monsters.Count;
                selectedData = _characterData.Monsters[monsterIndex];
            }
            else
            {
                // 폴백: 기본 데이터
                selectedData = new MonsterData { Id = "monster", Name = "??", BaseHp = 10, HpGrowth = 5, BaseGold = 10, GoldGrowth = 2, Emoji = "👹" };
            }

            _currentMonster = new Monster(selectedData, CurrentLevel, isBoss);

            // 타이머 시작 (영구 스탯 시간 연장 적용)
            var permStats = _saveManager?.CurrentSave?.PermanentStats;
            double timeExtend = _statGrowth.GetPermanentStatEffect("time_extend", permStats?.TimeExtendLevel ?? 0);
            int timeLimit = _gameData.Balance.TimeLimit + (int)timeExtend;
            RemainingTime = timeLimit;
            _timer.Start();

            MonsterSpawned?.Invoke(this, EventArgs.Empty);
            StatsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            RemainingTime--;
            TimerTick?.Invoke(this, EventArgs.Empty);

            if (RemainingTime <= 0)
            {
                // 시간 초과 - 게임 오버 시퀀스 시작
                TriggerGameOver();
            }
        }

        private void TriggerGameOver()
        {
            _timer.Stop();
            // UI에서 애니메이션 재생 후 RestartGame()을 호출하도록 유도
            GameOver?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
