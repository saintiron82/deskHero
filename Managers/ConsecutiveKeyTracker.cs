using System;
using System.Diagnostics;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 연속 동일키 입력 추적 및 페널티 계산
    /// 1초 이상 간격이 있으면 카운트 리셋 (1초 할당량 기반)
    /// </summary>
    public class ConsecutiveKeyTracker
    {
        #region Fields

        private readonly ConsecutiveKeyPenaltyConfig _config;
        private int _lastKeyboardVkCode = 0;
        private int _keyboardConsecutiveCount = 0;
        private long _lastKeyboardInputTick = 0;
        private GameMouseButton _lastMouseButton = GameMouseButton.None;
        private int _mouseConsecutiveCount = 0;
        private long _lastMouseInputTick = 0;

        /// <summary>
        /// 연속 카운트에 영향을 주지 않는 VK 코드 (수정자/IME/시스템 키)
        /// 한국어 IME의 VK_PROCESSKEY(229)가 카운터를 리셋하는 버그 방지
        /// </summary>
        private static readonly System.Collections.Generic.HashSet<int> IgnoredVkCodes = new()
        {
            // Modifier keys
            16, 17, 18,       // VK_SHIFT, VK_CONTROL, VK_MENU(ALT)
            160, 161,         // VK_LSHIFT, VK_RSHIFT
            162, 163,         // VK_LCONTROL, VK_RCONTROL
            164, 165,         // VK_LMENU, VK_RMENU
            // IME keys
            229,              // VK_PROCESSKEY (한국어 IME)
            21, 25, 23, 24,   // VK_HANGUL, VK_HANJA, VK_JUNJA, VK_FINAL
            // Lock/Toggle keys
            20, 144, 145,     // VK_CAPITAL, VK_NUMLOCK, VK_SCROLL
            // System keys
            91, 92, 93        // VK_LWIN, VK_RWIN, VK_APPS
        };

        #endregion

        #region Properties

        /// <summary>
        /// 키보드 연속 입력 횟수
        /// </summary>
        public int KeyboardConsecutiveCount => _keyboardConsecutiveCount;

        /// <summary>
        /// 마우스 연속 입력 횟수
        /// </summary>
        public int MouseConsecutiveCount => _mouseConsecutiveCount;

        #endregion

        #region Constructor

        public ConsecutiveKeyTracker(ConsecutiveKeyPenaltyConfig config)
        {
            _config = config;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 키보드 입력 처리 및 페널티 계산
        /// </summary>
        /// <param name="vkCode">Virtual Key Code</param>
        /// <param name="isComboActive">콤보 활성화 여부 (콤보 중엔 페널티 면제)</param>
        /// <returns>데미지 배율 (0.0 ~ 1.0)</returns>
        public double ProcessKeyboardInput(int vkCode, bool isComboActive)
        {
            // 수정자/IME/시스템 키는 연속 카운트에 영향 주지 않음
            if (IgnoredVkCodes.Contains(vkCode))
                return CalculatePenalty(_keyboardConsecutiveCount, isComboActive);

            long now = Stopwatch.GetTimestamp();

            // 1초 이상 간격이 있으면 카운트 리셋
            if (_lastKeyboardInputTick > 0 && (now - _lastKeyboardInputTick) > Stopwatch.Frequency)
            {
                _keyboardConsecutiveCount = 0;
            }
            _lastKeyboardInputTick = now;

            // 같은 키 연속 입력
            if (vkCode == _lastKeyboardVkCode)
            {
                _keyboardConsecutiveCount++;
            }
            // 다른 키 → 카운트 리셋
            else
            {
                _lastKeyboardVkCode = vkCode;
                _keyboardConsecutiveCount = 1;
            }

            return CalculatePenalty(_keyboardConsecutiveCount, isComboActive);
        }

        /// <summary>
        /// 마우스 입력 처리 및 페널티 계산
        /// </summary>
        /// <param name="button">마우스 버튼</param>
        /// <param name="isComboActive">콤보 활성화 여부</param>
        /// <returns>데미지 배율 (0.0 ~ 1.0)</returns>
        public double ProcessMouseInput(GameMouseButton button, bool isComboActive)
        {
            long now = Stopwatch.GetTimestamp();

            // 1초 이상 간격이 있으면 카운트 리셋
            if (_lastMouseInputTick > 0 && (now - _lastMouseInputTick) > Stopwatch.Frequency)
            {
                _mouseConsecutiveCount = 0;
            }
            _lastMouseInputTick = now;

            // 같은 버튼 연속 입력
            if (button == _lastMouseButton)
            {
                _mouseConsecutiveCount++;
            }
            // 다른 버튼 → 카운트 리셋
            else
            {
                _lastMouseButton = button;
                _mouseConsecutiveCount = 1;
            }

            return CalculatePenalty(_mouseConsecutiveCount, isComboActive);
        }

        /// <summary>
        /// 전체 리셋 (게임 재시작 시)
        /// </summary>
        public void Reset()
        {
            _lastKeyboardVkCode = 0;
            _keyboardConsecutiveCount = 0;
            _lastKeyboardInputTick = 0;
            _lastMouseButton = GameMouseButton.None;
            _mouseConsecutiveCount = 0;
            _lastMouseInputTick = 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 페널티 배율 계산
        /// </summary>
        /// <param name="consecutiveCount">연속 입력 횟수</param>
        /// <param name="isComboActive">콤보 활성화 여부</param>
        /// <returns>데미지 배율 (0.0 ~ 1.0)</returns>
        private double CalculatePenalty(int consecutiveCount, bool isComboActive)
        {
            // 기능 비활성화
            if (!_config.Enabled)
                return 1.0;

            // 콤보 중엔 페널티 면제
            if (isComboActive && _config.ComboExempt)
            {
                DeskWarrior.Helpers.Logger.Log($"[ConsecutiveKey] Combo active - penalty exempt (count: {consecutiveCount})");
                return 1.0;
            }

            // 페널티 시작 전 (1~7회)
            if (consecutiveCount <= _config.PenaltyStartCount)
                return 1.0;

            // 페널티 계산: 1.0 - (초과 횟수 × 0.1)
            // 예: 8회 = 1.0 - (1 × 0.1) = 0.9 (90%)
            //     15회 = 1.0 - (8 × 0.1) = 0.2 (20%)
            //     18회+ = 0.0 (0%)
            int excessCount = consecutiveCount - _config.PenaltyStartCount;
            double penalty = 1.0 - (excessCount * _config.PenaltyPerCount);
            double finalPenalty = Math.Max(0.0, penalty);

            DeskWarrior.Helpers.Logger.Log($"[ConsecutiveKey] Count: {consecutiveCount}, Penalty: {finalPenalty:F2} ({finalPenalty * 100:F0}%)");

            return finalPenalty;
        }

        #endregion
    }
}
