using System;
using System.Windows.Input;
using System.Windows.Media;
using DeskWarrior.Interfaces;
using DeskWarrior.Managers;
using DeskWarrior.Models;

namespace DeskWarrior.ViewModels
{
    /// <summary>
    /// 메인 윈도우 ViewModel (MVVM 패턴)
    /// </summary>
    public class MainViewModel : ViewModelBase, IDisposable
    {
        #region Fields

        private readonly IInputHandler _inputHandler;
        private readonly TrayManager _trayManager;
        private readonly SaveManager _saveManager;
        private readonly GameManager _gameManager;
        private readonly SoundManager _soundManager;
        private readonly AchievementManager _achievementManager;

        private int _sessionInputCount;
        private bool _disposed;

        // Monster UI
        private string _monsterEmoji = "";
        private string _monsterName = "";
        private string _monsterSkinType = "";
        private int _monsterCurrentHp;
        private int _monsterMaxHp;
        private double _hpRatio = 1.0;
        private bool _isBoss;

        // Game UI
        private string _levelText = "Lv.1";
        private string _maxLevelText = "(Best: 1)";
        private string _goldText = "0";
        private string _timerText = "30";
        private Color _timerColor = Colors.SkyBlue;
        private string _keyboardPowerText = "1";
        private string _mousePowerText = "1";
        private string _keyboardUpgradeCost = "100";
        private string _mouseUpgradeCost = "100";

        #endregion

        #region Properties - Game State

        public int CurrentLevel => _gameManager.CurrentLevel;
        public int Gold => _gameManager.Gold;
        public int KeyboardPower => _gameManager.KeyboardPower;
        public int MousePower => _gameManager.MousePower;
        public int RemainingTime => _gameManager.RemainingTime;
        public Monster? CurrentMonster => _gameManager.CurrentMonster;
        public GameData GameConfig => _gameManager.GameData;

        public int SessionInputCount
        {
            get => _sessionInputCount;
            private set => SetProperty(ref _sessionInputCount, value);
        }

        #endregion

        #region Properties - UI Binding

        public string LevelText
        {
            get => _levelText;
            private set => SetProperty(ref _levelText, value);
        }

        public string MaxLevelText
        {
            get => _maxLevelText;
            private set => SetProperty(ref _maxLevelText, value);
        }

        public string GoldText
        {
            get => _goldText;
            private set => SetProperty(ref _goldText, value);
        }

        public string TimerText
        {
            get => _timerText;
            private set => SetProperty(ref _timerText, value);
        }

        public Color TimerColor
        {
            get => _timerColor;
            private set => SetProperty(ref _timerColor, value);
        }

        public string KeyboardPowerText
        {
            get => _keyboardPowerText;
            private set => SetProperty(ref _keyboardPowerText, value);
        }

        public string MousePowerText
        {
            get => _mousePowerText;
            private set => SetProperty(ref _mousePowerText, value);
        }

        public string KeyboardUpgradeCost
        {
            get => _keyboardUpgradeCost;
            private set => SetProperty(ref _keyboardUpgradeCost, value);
        }

        public string MouseUpgradeCost
        {
            get => _mouseUpgradeCost;
            private set => SetProperty(ref _mouseUpgradeCost, value);
        }

        // Monster Properties
        public string MonsterEmoji
        {
            get => _monsterEmoji;
            private set => SetProperty(ref _monsterEmoji, value);
        }

        public string MonsterName
        {
            get => _monsterName;
            private set => SetProperty(ref _monsterName, value);
        }

        public string MonsterSkinType
        {
            get => _monsterSkinType;
            private set => SetProperty(ref _monsterSkinType, value);
        }

        public int MonsterCurrentHp
        {
            get => _monsterCurrentHp;
            private set => SetProperty(ref _monsterCurrentHp, value);
        }

        public int MonsterMaxHp
        {
            get => _monsterMaxHp;
            private set => SetProperty(ref _monsterMaxHp, value);
        }

        public double HpRatio
        {
            get => _hpRatio;
            private set => SetProperty(ref _hpRatio, value);
        }

        public bool IsBoss
        {
            get => _isBoss;
            private set => SetProperty(ref _isBoss, value);
        }

        public string HpText => $"{MonsterCurrentHp:N0}/{MonsterMaxHp:N0}";

        #endregion

        #region Properties - Managers (읽기 전용 노출)

        public SaveManager SaveManager => _saveManager;
        public GameManager GameManager => _gameManager;
        public AchievementManager AchievementManager => _achievementManager;
        public TrayManager TrayManager => _trayManager;
        public SoundManager SoundManager => _soundManager;
        public IInputHandler InputHandler => _inputHandler;

        #endregion

        #region Commands

        public ICommand UpgradeKeyboardCommand { get; }
        public ICommand UpgradeMouseCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand OpenStatsCommand { get; }
        public ICommand RestartGameCommand { get; }

        #endregion

        #region Events

        public event EventHandler<DamageEventArgs>? DamageDealt;
        public event EventHandler? MonsterDefeated;
        public event EventHandler? MonsterSpawned;
        public event EventHandler? GameOver;
        public event EventHandler<GameInputEventArgs>? InputReceived;
        public event Action? SettingsRequested;
        public event Action? StatsRequested;

        #endregion

        #region Constructor

        public MainViewModel()
        {
            // Managers 초기화
            _inputHandler = new GlobalInputManager();
            _trayManager = new TrayManager();
            _saveManager = new SaveManager();
            _gameManager = new GameManager();
            _soundManager = new SoundManager();
            _achievementManager = new AchievementManager(_saveManager);

            // GameManager에 SaveManager 연결 (PermanentProgressionManager 자동 생성)
            _gameManager.Initialize(_saveManager);

            // AchievementManager에 PermanentProgressionManager 연결
            if (_gameManager.PermanentProgression != null)
            {
                _achievementManager.Initialize(_gameManager.PermanentProgression);
            }

            // Commands 초기화
            UpgradeKeyboardCommand = new RelayCommand(ExecuteUpgradeKeyboard, CanUpgradeKeyboard);
            UpgradeMouseCommand = new RelayCommand(ExecuteUpgradeMouse, CanUpgradeMouse);
            OpenSettingsCommand = new RelayCommand(_ => SettingsRequested?.Invoke());
            OpenStatsCommand = new RelayCommand(_ => StatsRequested?.Invoke());
            RestartGameCommand = new RelayCommand(ExecuteRestartGame);

            // 이벤트 구독
            SubscribeToEvents();

            // 초기 UI 업데이트
            RefreshAllUI();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 게임 시작
        /// </summary>
        public void StartGame()
        {
            _gameManager.StartGame();
            _inputHandler.Start();
            RefreshAllUI();
        }

        /// <summary>
        /// 입력 시작
        /// </summary>
        public void StartInput()
        {
            _inputHandler.Start();
        }

        /// <summary>
        /// 입력 중지
        /// </summary>
        public void StopInput()
        {
            _inputHandler.Stop();
        }

        /// <summary>
        /// 트레이 초기화
        /// </summary>
        public void InitializeTray()
        {
            _trayManager.Initialize();
        }

        /// <summary>
        /// 저장된 데이터 로드 및 적용
        /// </summary>
        public void LoadSavedData()
        {
            var upgrades = _saveManager.CurrentSave.Upgrades;
            _gameManager.LoadUpgrades(upgrades.KeyboardPower, upgrades.MousePower);
            RefreshAllUI();
        }

        /// <summary>
        /// 현재 상태 저장
        /// </summary>
        public void SaveCurrentState()
        {
            _saveManager.UpdateUpgrades(_gameManager.KeyboardPower, _gameManager.MousePower);
            _saveManager.Save();
        }

        /// <summary>
        /// 세션 저장
        /// </summary>
        public void SaveSession()
        {
            var sessionStats = _gameManager.CreateSessionStats();
            _saveManager.SaveSession(sessionStats);
            _achievementManager.CheckAllAchievements();
        }

        /// <summary>
        /// 모든 UI 새로고침
        /// </summary>
        public void RefreshAllUI()
        {
            UpdateGameUI();
            UpdateMonsterUI();
            UpdateTimerUI();
            UpdateUpgradeCosts();
        }

        #endregion

        #region Private Methods - Event Handlers

        private void SubscribeToEvents()
        {
            // Input Handler
            _inputHandler.OnInput += OnInputReceived;

            // Game Manager
            _gameManager.DamageDealt += OnDamageDealt;
            _gameManager.MonsterDefeated += OnMonsterDefeated;
            _gameManager.MonsterSpawned += OnMonsterSpawned;
            _gameManager.TimerTick += OnTimerTick;
            _gameManager.StatsChanged += OnStatsChanged;
            _gameManager.GameOver += OnGameOver;

            // Tray Manager
            _trayManager.SettingsRequested += (s, e) => SettingsRequested?.Invoke();
        }

        private void OnInputReceived(object? sender, GameInputEventArgs e)
        {
            SessionInputCount++;

            if (e.Type == GameInputType.Keyboard)
            {
                _gameManager.OnKeyboardInput();
            }
            else
            {
                _gameManager.OnMouseInput();
            }

            InputReceived?.Invoke(this, e);
        }

        private void OnDamageDealt(object? sender, DamageEventArgs e)
        {
            _soundManager.Play(SoundType.Hit);
            UpdateMonsterUI();
            DamageDealt?.Invoke(this, e);
        }

        private void OnMonsterDefeated(object? sender, EventArgs e)
        {
            _soundManager.Play(SoundType.Defeat);
            UpdateGameUI();
            MonsterDefeated?.Invoke(this, e);
        }

        private void OnMonsterSpawned(object? sender, EventArgs e)
        {
            UpdateMonsterUI();
            UpdateTimerUI();

            if (_gameManager.CurrentMonster?.IsBoss == true)
            {
                _soundManager.Play(SoundType.BossAppear);
            }

            MonsterSpawned?.Invoke(this, e);
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            UpdateTimerUI();
        }

        private void OnStatsChanged(object? sender, EventArgs e)
        {
            UpdateGameUI();
            UpdateUpgradeCosts();
        }

        private void OnGameOver(object? sender, EventArgs e)
        {
            SaveSession();
            GameOver?.Invoke(this, e);
        }

        #endregion

        #region Private Methods - UI Updates

        private void UpdateGameUI()
        {
            LevelText = $"Lv.{_gameManager.CurrentLevel}";

            int bestLevel = Math.Max(_gameManager.CurrentLevel, _saveManager.CurrentSave.Stats.MaxLevel);
            MaxLevelText = $"(Best: {bestLevel})";

            GoldText = $"{_gameManager.Gold:N0}";
            KeyboardPowerText = $"{_gameManager.KeyboardPower:N0}";
            MousePowerText = $"{_gameManager.MousePower:N0}";
        }

        private void UpdateMonsterUI()
        {
            var monster = _gameManager.CurrentMonster;
            if (monster == null) return;

            MonsterEmoji = monster.Emoji;
            MonsterName = monster.Name;
            MonsterSkinType = monster.SkinType;
            MonsterCurrentHp = monster.CurrentHp;
            MonsterMaxHp = monster.MaxHp;
            HpRatio = monster.HpRatio;
            IsBoss = monster.IsBoss;

            OnPropertyChanged(nameof(HpText));
        }

        private void UpdateTimerUI()
        {
            int time = _gameManager.RemainingTime;
            TimerText = time.ToString();

            // 타이머 색상
            if (time > 20)
                TimerColor = Color.FromRgb(135, 206, 235); // 하늘색
            else if (time > 10)
                TimerColor = Color.FromRgb(255, 200, 100); // 주황색
            else
                TimerColor = Color.FromRgb(255, 100, 100); // 빨간색
        }

        private void UpdateUpgradeCosts()
        {
            KeyboardUpgradeCost = $"{_gameManager.CalculateUpgradeCost(_gameManager.KeyboardPower):N0}";
            MouseUpgradeCost = $"{_gameManager.CalculateUpgradeCost(_gameManager.MousePower):N0}";
        }

        #endregion

        #region Private Methods - Commands

        private void ExecuteUpgradeKeyboard(object? parameter)
        {
            if (_gameManager.UpgradeKeyboardPower())
            {
                _soundManager.Play(SoundType.Upgrade);
                _saveManager.UpdateUpgrades(_gameManager.KeyboardPower, _gameManager.MousePower);
                UpdateGameUI();
                UpdateUpgradeCosts();
            }
        }

        private bool CanUpgradeKeyboard(object? parameter)
        {
            return _gameManager.Gold >= _gameManager.CalculateUpgradeCost(_gameManager.KeyboardPower);
        }

        private void ExecuteUpgradeMouse(object? parameter)
        {
            if (_gameManager.UpgradeMousePower())
            {
                _soundManager.Play(SoundType.Upgrade);
                _saveManager.UpdateUpgrades(_gameManager.KeyboardPower, _gameManager.MousePower);
                UpdateGameUI();
                UpdateUpgradeCosts();
            }
        }

        private bool CanUpgradeMouse(object? parameter)
        {
            return _gameManager.Gold >= _gameManager.CalculateUpgradeCost(_gameManager.MousePower);
        }

        private void ExecuteRestartGame(object? parameter)
        {
            _gameManager.RestartGame();
            SessionInputCount = 0;
            RefreshAllUI();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            _inputHandler.Stop();
            _inputHandler.Dispose();
            _trayManager.Dispose();
            _soundManager.Dispose();

            _disposed = true;
        }

        #endregion
    }
}
