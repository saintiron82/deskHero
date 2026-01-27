using System;
using System.Windows.Input;
using DeskWarrior.Interfaces;
using DeskWarrior.Managers;

namespace DeskWarrior.ViewModels
{
    /// <summary>
    /// 업그레이드 UI ViewModel
    /// </summary>
    public class UpgradeViewModel : ViewModelBase
    {
        private readonly GameManager _gameManager;
        private readonly SaveManager _saveManager;
        private readonly SoundManager _soundManager;

        private string _keyboardPowerText = "1";
        private string _mousePowerText = "1";
        private string _keyboardUpgradeCost = "100";
        private string _mouseUpgradeCost = "100";

        public UpgradeViewModel(GameManager gameManager, SaveManager saveManager, SoundManager soundManager)
        {
            _gameManager = gameManager ?? throw new ArgumentNullException(nameof(gameManager));
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            _soundManager = soundManager ?? throw new ArgumentNullException(nameof(soundManager));

            UpgradeKeyboardCommand = new RelayCommand(ExecuteUpgradeKeyboard, CanUpgradeKeyboard);
            UpgradeMouseCommand = new RelayCommand(ExecuteUpgradeMouse, CanUpgradeMouse);
        }

        #region Properties

        public int KeyboardPower => _gameManager.KeyboardPower;
        public int MousePower => _gameManager.MousePower;

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

        public string KeyboardPowerDisplayText => LocalizationManager.Instance.Format("ui.main.attackFormat.keyboard", (_gameManager?.KeyboardPower ?? 1).ToString("N0"));
        public string MousePowerDisplayText => LocalizationManager.Instance.Format("ui.main.attackFormat.mouse", (_gameManager?.MousePower ?? 1).ToString("N0"));

        #endregion

        #region Commands

        public ICommand UpgradeKeyboardCommand { get; }
        public ICommand UpgradeMouseCommand { get; }

        #endregion

        #region Events

        public event EventHandler? UpgradePerformed;

        #endregion

        #region Methods

        /// <summary>
        /// 업그레이드 비용 갱신
        /// </summary>
        public void Update()
        {
            KeyboardPowerText = $"{_gameManager.KeyboardPower:N0}";
            MousePowerText = $"{_gameManager.MousePower:N0}";
            KeyboardUpgradeCost = $"{_gameManager.CalculateUpgradeCost(_gameManager.KeyboardPower):N0}";
            MouseUpgradeCost = $"{_gameManager.CalculateUpgradeCost(_gameManager.MousePower):N0}";

            OnPropertyChanged(nameof(KeyboardPowerDisplayText));
            OnPropertyChanged(nameof(MousePowerDisplayText));
        }

        private void ExecuteUpgradeKeyboard(object? parameter)
        {
            if (_gameManager.UpgradeKeyboardPower())
            {
                _soundManager.Play(SoundType.Upgrade);
                _saveManager.UpdateUpgrades(_gameManager.KeyboardPower, _gameManager.MousePower);
                Update();
                UpgradePerformed?.Invoke(this, EventArgs.Empty);
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
                Update();
                UpgradePerformed?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool CanUpgradeMouse(object? parameter)
        {
            return _gameManager.Gold >= _gameManager.CalculateUpgradeCost(_gameManager.MousePower);
        }

        #endregion
    }
}
