using System;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    public class DamageEventArgs : EventArgs
    {
        public int Damage { get; }
        public bool IsCritical { get; }
        public bool IsMouse { get; }

        public DamageEventArgs(int damage, bool isCritical, bool isMouse)
        {
            Damage = damage;
            IsCritical = isCritical;
            IsMouse = isMouse;
        }
    }
    /// <summary>
    /// 게임 상태 및 로직 관리
    /// </summary>
    public class GameManager
    {
        #region Fields

        private readonly GameData _gameData;
        private readonly CharacterDataRoot _characterData;
        private readonly DispatcherTimer _timer;
        private readonly GameOverMessageManager _messageManager;
        private readonly Random _random = new();
        private Monster? _currentMonster;

        #endregion

        #region Events

        public event EventHandler? MonsterDefeated;
        public event EventHandler? MonsterSpawned;
        public event EventHandler? TimerTick;
        public event EventHandler? GameOver;
        public event EventHandler? StatsChanged;
        public event EventHandler<DamageEventArgs>? DamageDealt;

        #endregion

        #region Properties

        public int CurrentLevel { get; internal set; } = 1;
        public int Gold { get; internal set; }
        public int KeyboardPower { get; internal set; } = 1;
        public int MousePower { get; internal set; } = 1;
        public int RemainingTime { get; private set; }
        public Monster? CurrentMonster => _currentMonster;
        public GameData Config => _gameData;
        public GameData GameData => _gameData;
        public System.Collections.Generic.List<HeroData> Heroes => _characterData.Heroes;

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
        /// 게임 시작
        /// </summary>
        public void StartGame()
        {
            CurrentLevel = 1;
            Gold = 0;
            KeyboardPower = 1;
            MousePower = 1;
            SpawnMonster();
        }

        /// <summary>
        /// 키보드 입력 처리
        /// </summary>
        public void OnKeyboardInput()
        {
            if (_currentMonster == null || !_currentMonster.IsAlive) return;
            
            int damage = CalculateDamage(KeyboardPower, out bool isCritical);
            ApplyDamage(damage, isCritical, isMouse: false);
        }

        /// <summary>
        /// 마우스 입력 처리
        /// </summary>
        public void OnMouseInput()
        {
            if (_currentMonster == null || !_currentMonster.IsAlive) return;
            
            int damage = CalculateDamage(MousePower, out bool isCritical);
            ApplyDamage(damage, isCritical, isMouse: true);
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

        #endregion

        #region Private Methods

        private int CalculateDamage(int basePower, out bool isCritical)
        {
            isCritical = false;
            double damage = basePower;

            // 크리티컬 확률 체크
            if (new Random().NextDouble() < _gameData.Balance.CriticalChance)
            {
                isCritical = true;
                damage *= _gameData.Balance.CriticalMultiplier;
            }

            return (int)damage;
        }

        public long SessionDamage { get; private set; }
        public long SessionTotalGold { get; private set; }
        public int SessionKills { get; private set; }
        public int SessionBossKills { get; private set; }
        public int SessionKeyboardInputs { get; private set; }
        public int SessionMouseInputs { get; private set; }
        public int SessionCriticalHits { get; private set; }
        public DateTime SessionStartTime { get; private set; } = DateTime.Now;

        private void ApplyDamage(int damage, bool isCritical, bool isMouse)
        {
            if (_currentMonster == null) return;

            _currentMonster.TakeDamage(damage);

            // 세션 스탯 누적
            SessionDamage += damage;

            // 크리티컬 히트 카운트
            if (isCritical)
            {
                SessionCriticalHits++;
            }

            // 입력 타입별 카운트
            if (isMouse)
            {
                SessionMouseInputs++;
            }
            else
            {
                SessionKeyboardInputs++;
            }

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

            // 골드 획득
            Gold += _currentMonster.GoldReward;
            SessionTotalGold += _currentMonster.GoldReward;

            // 킬 카운트 증가
            SessionKills++;

            // 보스 킬 체크
            if (_currentMonster.IsBoss)
            {
                SessionBossKills++;
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

            // 타이머 시작
            RemainingTime = _gameData.Balance.TimeLimit;
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

        public void RestartGame()
        {
            // 리셋
            CurrentLevel = 1;
            Gold = 0;
            KeyboardPower = 1;
            MousePower = 1;

            // 세션 스탯 리셋
            SessionDamage = 0;
            SessionTotalGold = 0;
            SessionKills = 0;
            SessionBossKills = 0;
            SessionKeyboardInputs = 0;
            SessionMouseInputs = 0;
            SessionCriticalHits = 0;
            SessionStartTime = DateTime.Now;

            // 새 게임 시작
            SpawnMonster();
        }

        /// <summary>
        /// 현재 세션 데이터 생성 (게임 오버 시 호출)
        /// </summary>
        public SessionStats CreateSessionStats(string endReason = "timeout")
        {
            return new SessionStats
            {
                StartTime = SessionStartTime,
                EndTime = DateTime.Now,
                MaxLevel = CurrentLevel,
                TotalDamage = SessionDamage,
                TotalGold = (int)SessionTotalGold,
                MonstersKilled = SessionKills,
                BossesKilled = SessionBossKills,
                KeyboardInputs = SessionKeyboardInputs,
                MouseInputs = SessionMouseInputs,
                EndReason = endReason
            };
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
    }
}
