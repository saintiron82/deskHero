using System;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 콤보 시스템 (리듬 기반)
    /// 연속 입력의 시간 간격이 일정할 때 콤보 발동
    /// </summary>
    public class ComboTracker
    {
        #region Constants

        private const double BASE_TOLERANCE = 0.01; // 기본 허용 오차 ±0.01초
        private const double COMBO_EXPIRE_TIME = 3.0; // 콤보 유지 시간 3초

        #endregion

        #region Fields

        private DateTime _lastInputTime;
        private double _lastInterval; // 이전 입력 간격
        private int _comboStack = 0; // 0 = 없음, 1-3 = 스택
        private double _comboFlexBonus = 0.0; // combo_flex 스탯 보너스

        #endregion

        #region Properties

        /// <summary>
        /// 현재 콤보 스택 (0-3)
        /// </summary>
        public int ComboStack => _comboStack;

        /// <summary>
        /// 콤보 활성화 여부
        /// </summary>
        public bool IsComboActive => _comboStack > 0;

        #endregion

        #region Public Methods

        /// <summary>
        /// 콤보 유연성 보너스 설정
        /// </summary>
        public void SetComboFlexBonus(double flexBonus)
        {
            _comboFlexBonus = flexBonus;
        }

        /// <summary>
        /// 입력 처리 및 콤보 판정
        /// </summary>
        /// <returns>현재 콤보 스택 (0-3)</returns>
        public int ProcessInput()
        {
            var now = DateTime.UtcNow;

            // 첫 입력
            if (_lastInputTime == default)
            {
                _lastInputTime = now;
                return 0;
            }

            // 현재 입력 간격
            double currentInterval = (now - _lastInputTime).TotalSeconds;

            // 콤보 만료 체크 (3초 경과)
            if (currentInterval > COMBO_EXPIRE_TIME)
            {
                Reset();
                _lastInputTime = now;
                return 0;
            }

            // 리듬 판정 (두 번째 입력부터)
            if (_lastInterval > 0)
            {
                double tolerance = BASE_TOLERANCE + _comboFlexBonus;
                double intervalDiff = Math.Abs(currentInterval - _lastInterval);

                // 리듬 일치 → 콤보 스택 증가
                if (intervalDiff <= tolerance)
                {
                    _comboStack = Math.Min(_comboStack + 1, 3); // 최대 3스택
                }
                // 리듬 깨짐 → 콤보 해제
                else
                {
                    Reset();
                }
            }
            // 첫 리듬 시작 (두 번째 입력)
            else
            {
                _comboStack = 1; // 첫 콤보 발동
            }

            _lastInterval = currentInterval;
            _lastInputTime = now;

            return _comboStack;
        }

        /// <summary>
        /// 콤보 리셋
        /// </summary>
        public void Reset()
        {
            _comboStack = 0;
            _lastInterval = 0;
        }

        /// <summary>
        /// 전체 리셋 (게임 재시작 시)
        /// </summary>
        public void FullReset()
        {
            Reset();
            _lastInputTime = default;
        }

        #endregion
    }
}
