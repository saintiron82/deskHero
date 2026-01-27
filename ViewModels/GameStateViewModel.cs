using System;
using DeskWarrior.Managers;

namespace DeskWarrior.ViewModels
{
    /// <summary>
    /// 게임 상태 ViewModel (레벨, 골드 등)
    /// </summary>
    public class GameStateViewModel : ViewModelBase
    {
        private readonly GameManager _gameManager;
        private readonly SaveManager _saveManager;

        private string _levelText = "Lv.1";
        private string _maxLevelText = "(Best: 1)";
        private string _goldText = "0";

        public GameStateViewModel(GameManager gameManager, SaveManager saveManager)
        {
            _gameManager = gameManager ?? throw new ArgumentNullException(nameof(gameManager));
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
        }

        #region Properties

        public int CurrentLevel => _gameManager.CurrentLevel;
        public int Gold => _gameManager.Gold;

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

        #endregion

        #region Methods

        /// <summary>
        /// UI 갱신
        /// </summary>
        public void Update()
        {
            var loc = LocalizationManager.Instance;
            LevelText = loc.Format("ui.common.levelFormat", _gameManager.CurrentLevel);

            int bestLevel = Math.Max(_gameManager.CurrentLevel, _saveManager.CurrentSave.Stats.MaxLevel);
            MaxLevelText = loc.Format("ui.common.bestFormat", bestLevel);

            GoldText = $"{_gameManager.Gold:N0}";
        }

        #endregion
    }
}
