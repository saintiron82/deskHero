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
        private readonly Random _random = new();
        private Monster? _currentMonster;
        private SaveManager? _saveManager;
        private PermanentProgressionManager? _permanentProgression;

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
        public int KeyboardPower { get; private set; } = 1;
        public int MousePower { get; private set; } = 1;
        public int RemainingTime { get; private set; }
        public Monster? CurrentMonster => _currentMonster;
        public GameData Config => _gameData;
        public GameData GameData => _gameData;
        public System.Collections.Generic.List<HeroData> Heroes => _characterData.Heroes;

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
        }

        /// <summary>
        /// 게임 시작
        /// </summary>
        public void StartGame()
        {
            var permStats = _saveManager?.CurrentSave?.PermanentStats;

            CurrentLevel = 1 + (permStats?.StartingLevelBonus ?? 0);
            Gold = permStats?.StartingGoldBonus ?? 0;
            KeyboardPower = 1 + (permStats?.StartingKeyboardPower ?? 0);
            MousePower = 1 + (permStats?.StartingMousePower ?? 0);
            _sessionTracker.Reset();
            SpawnMonster();
        }

        /// <summary>
        /// 키보드 입력 처리
        /// </summary>
        public void OnKeyboardInput()
        {
            if (_currentMonster == null || !_currentMonster.IsAlive) return;

            var result = CalculateDamage(KeyboardPower);
            ApplyDamage(result.Damage, result.IsCritical, isMouse: false);
        }

        /// <summary>
        /// 마우스 입력 처리
        /// </summary>
        public void OnMouseInput()
        {
            if (_currentMonster == null || !_currentMonster.IsAlive) return;

            var result = CalculateDamage(MousePower);
            ApplyDamage(result.Damage, result.IsCritical, isMouse: true);
        }

        /// <summary>
        /// 키보드 공격력 업그레이드
        /// </summary>
        public bool UpgradeKeyboardPower()
        {
            int cost = CalculateUpgradeCost(KeyboardPower);
            if (Gold >= cost)
            {
                Gold -= cost;
                KeyboardPower++;
                StatsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 마우스 공격력 업그레이드
        /// </summary>
        public bool UpgradeMousePower()
        {
            int cost = CalculateUpgradeCost(MousePower);
            if (Gold >= cost)
            {
                Gold -= cost;
                MousePower++;
                StatsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 업그레이드 비용 계산
        /// </summary>
        public int CalculateUpgradeCost(int currentLevel)
        {
            return (int)(_gameData.Upgrade.BaseCost * Math.Pow(_gameData.Upgrade.CostMultiplier, currentLevel - 1));
        }

        /// <summary>
        /// 저장된 업그레이드 데이터 로드
        /// </summary>
        public void LoadUpgrades(int keyboardPower, int mousePower)
        {
            KeyboardPower = keyboardPower;
            MousePower = mousePower;
        }

        /// <summary>
        /// 게임 재시작
        /// </summary>
        public void RestartGame()
        {
            // 리셋
            CurrentLevel = 1;
            Gold = 0;
            KeyboardPower = 1;
            MousePower = 1;

            // 세션 트래커 리셋
            _sessionTracker.Reset();

            // 새 게임 시작
            SpawnMonster();
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

        private DamageResult CalculateDamage(int basePower)
        {
            var permStats = _saveManager?.CurrentSave?.PermanentStats;
            return _damageCalculator.Calculate(basePower, permStats);
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

            // 영구 골드 보너스 적용
            var permStats = _saveManager?.CurrentSave?.PermanentStats;
            int goldReward = (int)(_currentMonster.GoldReward * (1.0 + (permStats?.GoldPercentBonus ?? 0)));
            Gold += goldReward;

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
            int timeLimit = _gameData.Balance.TimeLimit + (permStats?.GameOverTimeExtension ?? 0);
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
