using System;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 게임 입력 이벤트 데이터를 담는 클래스
    /// (System.Windows.Input.InputEventArgs와 충돌 방지를 위해 Game 접두사 사용)
    /// </summary>
    public class GameInputEventArgs : EventArgs
    {
        /// <summary>
        /// 입력 타입 (Keyboard, Mouse)
        /// </summary>
        public GameInputType Type { get; }

        /// <summary>
        /// 키보드 입력 시 가상 키 코드
        /// </summary>
        public int VirtualKeyCode { get; }

        /// <summary>
        /// 마우스 입력 시 버튼 종류
        /// </summary>
        public GameMouseButton MouseButton { get; }

        /// <summary>
        /// 이벤트 발생 시각
        /// </summary>
        public DateTime Timestamp { get; }

        public GameInputEventArgs(GameInputType type, int virtualKeyCode = 0, GameMouseButton mouseButton = GameMouseButton.None)
        {
            Type = type;
            VirtualKeyCode = virtualKeyCode;
            MouseButton = mouseButton;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 게임 입력 타입 열거형
    /// </summary>
    public enum GameInputType
    {
        Keyboard,
        Mouse
    }

    /// <summary>
    /// 게임 마우스 버튼 열거형
    /// </summary>
    public enum GameMouseButton
    {
        None,
        Left,
        Right,
        Middle
    }
}
