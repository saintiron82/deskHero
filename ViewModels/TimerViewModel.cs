using System.Windows.Media;
using DeskWarrior.Managers;

namespace DeskWarrior.ViewModels
{
    /// <summary>
    /// 타이머 UI ViewModel
    /// </summary>
    public class TimerViewModel : ViewModelBase
    {
        private readonly GameManager _gameManager;

        private string _timerText = "30";
        private Color _timerColor = Colors.SkyBlue;

        // 타이머 색상 상수
        private static readonly Color ColorSafe = Color.FromRgb(135, 206, 235);    // 하늘색
        private static readonly Color ColorWarning = Color.FromRgb(255, 200, 100); // 주황색
        private static readonly Color ColorDanger = Color.FromRgb(255, 100, 100);  // 빨간색

        public TimerViewModel(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        #region Properties

        public int RemainingTime => _gameManager.RemainingTime;

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

        #endregion

        #region Methods

        /// <summary>
        /// 타이머 UI 갱신
        /// </summary>
        public void Update()
        {
            int time = _gameManager.RemainingTime;
            TimerText = time.ToString();

            // 타이머 색상 결정
            if (time > 20)
                TimerColor = ColorSafe;
            else if (time > 10)
                TimerColor = ColorWarning;
            else
                TimerColor = ColorDanger;
        }

        #endregion
    }
}
