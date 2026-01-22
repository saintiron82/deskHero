using System.Windows.Input;

namespace DeskWarrior.ViewModels
{
    /// <summary>
    /// GameOverOverlay ViewModel
    /// </summary>
    public class GameOverViewModel : ViewModelBase
    {
        #region Fields

        private string _gameOverMessage = "";
        private string _levelText = "0";
        private string _killsText = "0";
        private string _goldText = "0";
        private string _damageText = "0";
        private int _bossDropCrystals;
        private int _achievementCrystals;
        private int _totalCrystalsEarned;
        private long _currentCrystalBalance;
        private string _countdownText = "10초 후 닫힘";
        private bool _showBossDropLine;
        private bool _showAchievementLine;

        #endregion

        #region Properties

        public string GameOverMessage
        {
            get => _gameOverMessage;
            set => SetProperty(ref _gameOverMessage, value);
        }

        public string LevelText
        {
            get => _levelText;
            set => SetProperty(ref _levelText, value);
        }

        public string KillsText
        {
            get => _killsText;
            set => SetProperty(ref _killsText, value);
        }

        public string GoldText
        {
            get => _goldText;
            set => SetProperty(ref _goldText, value);
        }

        public string DamageText
        {
            get => _damageText;
            set => SetProperty(ref _damageText, value);
        }

        public int BossDropCrystals
        {
            get => _bossDropCrystals;
            set
            {
                SetProperty(ref _bossDropCrystals, value);
                ShowBossDropLine = value > 0;
            }
        }

        public int AchievementCrystals
        {
            get => _achievementCrystals;
            set
            {
                SetProperty(ref _achievementCrystals, value);
                ShowAchievementLine = value > 0;
            }
        }

        public int TotalCrystalsEarned
        {
            get => _totalCrystalsEarned;
            set => SetProperty(ref _totalCrystalsEarned, value);
        }

        public long CurrentCrystalBalance
        {
            get => _currentCrystalBalance;
            set => SetProperty(ref _currentCrystalBalance, value);
        }

        public string CountdownText
        {
            get => _countdownText;
            set => SetProperty(ref _countdownText, value);
        }

        public bool ShowBossDropLine
        {
            get => _showBossDropLine;
            set => SetProperty(ref _showBossDropLine, value);
        }

        public bool ShowAchievementLine
        {
            get => _showAchievementLine;
            set => SetProperty(ref _showAchievementLine, value);
        }

        #endregion

        #region Commands

        public ICommand? ShopCommand { get; set; }
        public ICommand? CloseCommand { get; set; }

        #endregion
    }
}
