using System;
using System.Windows.Threading;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 게임 루프 및 타이머 관리 (틱 처리, 시간 스케일, 게임 오버)
    /// </summary>
    public class GameLoopManager
    {
        #region Fields

        private readonly DispatcherTimer _timer;
        private Monster? _currentMonster;
        private bool _isGoldenGoblinActive;

        #endregion

        #region Events

        public event EventHandler? TimerTick;
        public event EventHandler? GameOver;
        public event EventHandler? GoldenGoblinEscaped;

        #endregion

        #region Properties

        public double RemainingTime { get; private set; }

        #endregion

        #region Constructor

        public GameLoopManager()
        {
            // 타이머 설정 (0.1초마다)
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += OnTimerTick;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 타이머 시작
        /// </summary>
        public void StartTimer()
        {
            _timer.Start();
        }

        /// <summary>
        /// 타이머 정지
        /// </summary>
        public void StopTimer()
        {
            _timer.Stop();
        }

        /// <summary>
        /// 타이머 일시정지
        /// </summary>
        public void PauseTimer()
        {
            _timer.Stop();
        }

        /// <summary>
        /// 타이머 재개
        /// </summary>
        public void ResumeTimer()
        {
            if (_currentMonster != null && _currentMonster.IsAlive && RemainingTime > 0)
            {
                _timer.Start();
            }
        }

        /// <summary>
        /// 새 몬스터 스폰 시 타이머 초기화 및 시작
        /// </summary>
        public void InitializeForMonster(Monster monster, double timeLimit, bool isGoldenGoblin)
        {
            _currentMonster = monster;
            RemainingTime = timeLimit;
            _isGoldenGoblinActive = isGoldenGoblin;
            _timer.Start();
        }

        #endregion

        #region Private Methods

        private void OnTimerTick(object? sender, EventArgs e)
        {
            // 시간 배속 적용 (Wind 속성 몬스터 등)
            double timeScale = _currentMonster?.TimeScale ?? 1.0;
            RemainingTime -= 0.1 * timeScale;
            TimerTick?.Invoke(this, EventArgs.Empty);

            if (RemainingTime <= 0)
            {
                // 황금 고블린 시간 초과 시 도주 처리 (게임오버 아님)
                if (_isGoldenGoblinActive)
                {
                    TriggerGoldenGoblinEscaped();
                    return;
                }

                // 일반 시간 초과 - 게임 오버 시퀀스 시작
                TriggerGameOver();
            }
        }

        private void TriggerGameOver()
        {
            _timer.Stop();
            // UI에서 애니메이션 재생 후 RestartGame()을 호출하도록 유도
            GameOver?.Invoke(this, EventArgs.Empty);
        }

        private void TriggerGoldenGoblinEscaped()
        {
            _timer.Stop();
            _isGoldenGoblinActive = false;
            GoldenGoblinEscaped?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
