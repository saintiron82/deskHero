using System;
using System.Windows.Input;
using System.Windows.Media;
using DeskWarrior.Interfaces;
using DeskWarrior.Managers;
using DeskWarrior.Models;

namespace DeskWarrior.ViewModels
{
    /// <summary>
    /// 메인 윈도우 ViewModel (MVVM + Composition 패턴)
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

        // Composed ViewModels
        private readonly GameStateViewModel _gameState;
        private readonly MonsterViewModel _monster;
        private readonly UpgradeViewModel _upgrade;
        private readonly TimerViewModel _timer;

        private int _sessionInputCount;
        private bool _disposed;

        #endregion

        #region Properties - Composed ViewModels

        public GameStateViewModel GameState => _gameState;
        public MonsterViewModel Monster => _monster;
        public UpgradeViewModel Upgrade => _upgrade;
        public TimerViewModel Timer => _timer;

        #endregion

        #region Properties - Game State (Direct Access)

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

        #region Properties - UI Binding (Backwards Compatibility)

        // These properties delegate to composed ViewModels for backwards compatibility
        public string LevelText => _gameState.LevelText;
        public string MaxLevelText => _gameState.MaxLevelText;
        public string GoldText => _gameState.GoldText;

        public string TimerText => _timer.TimerText;
        public Color TimerColor => _timer.TimerColor;

        public string KeyboardPowerText => _upgrade.KeyboardPowerText;
        public string MousePowerText => _upgrade.MousePowerText;
        public string KeyboardUpgradeCost => _upgrade.KeyboardUpgradeCost;
        public string MouseUpgradeCost => _upgrade.MouseUpgradeCost;
        public string KeyboardPowerDisplayText => _upgrade.KeyboardPowerDisplayText;
        public string MousePowerDisplayText => _upgrade.MousePowerDisplayText;

        public string MonsterEmoji => _monster.Emoji;
        public string MonsterName => _monster.Name;
        public string MonsterSkinType => _monster.SkinType;
        public int MonsterCurrentHp => _monster.CurrentHp;
        public int MonsterMaxHp => _monster.MaxHp;
        public double HpRatio => _monster.HpRatio;
        public bool IsBoss => _monster.IsBoss;
        public string HpText => _monster.HpText;

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

        public ICommand UpgradeKeyboardCommand => _upgrade.UpgradeKeyboardCommand;
        public ICommand UpgradeMouseCommand => _upgrade.UpgradeMouseCommand;
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
            // Core Managers 초기화
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

            // Composed ViewModels 초기화
            _gameState = new GameStateViewModel(_gameManager, _saveManager);
            _monster = new MonsterViewModel(_gameManager);
            _upgrade = new UpgradeViewModel(_gameManager, _saveManager, _soundManager);
            _timer = new TimerViewModel(_gameManager);

            // Commands 초기화
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
            _gameState.Update();
            _monster.Update();
            _timer.Update();
            _upgrade.Update();

            // PropertyChanged 전파 (Backwards Compatibility)
            NotifyUIPropertiesChanged();
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

            // Upgrade ViewModel
            _upgrade.UpgradePerformed += OnUpgradePerformed;

            // Composed ViewModels PropertyChanged 전파
            _gameState.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName!);
            _monster.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName!);
            _timer.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName!);
            _upgrade.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName!);
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
            _monster.Update();
            NotifyUIPropertiesChanged();
            DamageDealt?.Invoke(this, e);
        }

        private void OnMonsterDefeated(object? sender, EventArgs e)
        {
            _soundManager.Play(SoundType.Defeat);
            _gameState.Update();
            NotifyUIPropertiesChanged();
            MonsterDefeated?.Invoke(this, e);
        }

        private void OnMonsterSpawned(object? sender, EventArgs e)
        {
            _monster.Update();
            _timer.Update();

            if (_gameManager.CurrentMonster?.IsBoss == true)
            {
                _soundManager.Play(SoundType.BossAppear);
            }

            NotifyUIPropertiesChanged();
            MonsterSpawned?.Invoke(this, e);
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            _timer.Update();
            OnPropertyChanged(nameof(TimerText));
            OnPropertyChanged(nameof(TimerColor));
        }

        private void OnStatsChanged(object? sender, EventArgs e)
        {
            _gameState.Update();
            _upgrade.Update();
            NotifyUIPropertiesChanged();
        }

        private void OnGameOver(object? sender, EventArgs e)
        {
            SaveSession();
            GameOver?.Invoke(this, e);
        }

        private void OnUpgradePerformed(object? sender, EventArgs e)
        {
            _gameState.Update();
            NotifyUIPropertiesChanged();
        }

        private void ExecuteRestartGame(object? parameter)
        {
            _gameManager.RestartGame();
            SessionInputCount = 0;
            RefreshAllUI();
        }

        /// <summary>
        /// Backwards Compatibility를 위한 PropertyChanged 알림
        /// </summary>
        private void NotifyUIPropertiesChanged()
        {
            OnPropertyChanged(nameof(LevelText));
            OnPropertyChanged(nameof(MaxLevelText));
            OnPropertyChanged(nameof(GoldText));
            OnPropertyChanged(nameof(MonsterEmoji));
            OnPropertyChanged(nameof(MonsterName));
            OnPropertyChanged(nameof(MonsterSkinType));
            OnPropertyChanged(nameof(MonsterCurrentHp));
            OnPropertyChanged(nameof(MonsterMaxHp));
            OnPropertyChanged(nameof(HpRatio));
            OnPropertyChanged(nameof(IsBoss));
            OnPropertyChanged(nameof(HpText));
            OnPropertyChanged(nameof(KeyboardPowerText));
            OnPropertyChanged(nameof(MousePowerText));
            OnPropertyChanged(nameof(KeyboardUpgradeCost));
            OnPropertyChanged(nameof(MouseUpgradeCost));
            OnPropertyChanged(nameof(KeyboardPowerDisplayText));
            OnPropertyChanged(nameof(MousePowerDisplayText));
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
