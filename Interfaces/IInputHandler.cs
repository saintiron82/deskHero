using System;
using DeskWarrior.Models;

namespace DeskWarrior.Interfaces
{
    /// <summary>
    /// 입력 처리 인터페이스 (SOLID: DIP, ISP)
    /// </summary>
    public interface IInputHandler : IDisposable
    {
        /// <summary>
        /// 입력 이벤트 발생 시 호출
        /// </summary>
        event EventHandler<GameInputEventArgs>? OnInput;

        /// <summary>
        /// 입력 감지 시작
        /// </summary>
        void Start();

        /// <summary>
        /// 입력 감지 중지
        /// </summary>
        void Stop();

        /// <summary>
        /// 현재 활성화 상태
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 키 블로킹 결정 콜백
        /// </summary>
        Func<int, bool>? ShouldBlockKey { get; set; }
    }
}
