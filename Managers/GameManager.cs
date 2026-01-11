using System;
using System.IO;
using System.Windows.Threading;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 게임 상태 및 로직 관리
    /// </summary>
    public class GameManager
    {
        #region Fields

        private readonly GameData _gameData;
        private readonly DispatcherTimer _timer;
        private Monster? _currentMonster;

        #endregion

        #region Events

        public event EventHandler? MonsterDefeated;
        public event EventHandler? MonsterSpawned;
        public event EventHandler? TimerTick;
        public event EventHandler? GameOver;
        public event EventHandler? StatsChanged;

        #endregion

        #region Properties

        public int CurrentLevel { get; private set; } = 1;
        public int Gold { get; private set; }
        public int KeyboardPower { get; private set; } = 1;
        public int MousePower { get; private set; } = 1;
        public int RemainingTime { get; private set; }
        public Monster? CurrentMonster => _currentMonster;
        public GameData Config => _gameData;

        #endregion

        #region Constructor

        public GameManager()
        {
            // 설정 로드
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "GameData.json");
            _gameData = GameData.LoadFromFile(configPath);

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
            
            ApplyDamage(KeyboardPower);
        }

        /// <summary>
        /// 마우스 입력 처리
        /// </summary>
        public void OnMouseInput()
        {
            if (_currentMonster == null || !_currentMonster.IsAlive) return;
            
            ApplyDamage(MousePower);
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

        private void ApplyDamage(int damage)
        {
            if (_currentMonster == null) return;

            _currentMonster.TakeDamage(damage);
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
            _currentMonster = new Monster(
                CurrentLevel,
                balance.BaseHp,
                balance.HpGrowth,
                balance.BossInterval,
                balance.BossHpMultiplier,
                balance.BaseGoldMultiplier
            );

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
                // 시간 초과 - 하드 리셋
                HardReset();
            }
        }

        private void HardReset()
        {
            _timer.Stop();
            
            // 리셋
            CurrentLevel = 1;
            Gold = 0;
            KeyboardPower = 1;
            MousePower = 1;

            GameOver?.Invoke(this, EventArgs.Empty);

            // 새 게임 시작
            SpawnMonster();
        }

        #endregion
    }
}
