using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DeskWarrior.Helpers;
using DeskWarrior.Interfaces;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 전역 키보드/마우스 입력을 감지하는 매니저 클래스
    /// IInputHandler 인터페이스 구현 (SOLID: SRP, DIP)
    /// </summary>
    public class GlobalInputManager : IInputHandler
    {
        #region Fields

        private IntPtr _keyboardHookId = IntPtr.Zero;
        private IntPtr _mouseHookId = IntPtr.Zero;
        private Win32Helper.LowLevelKeyboardProc? _keyboardProc;
        private Win32Helper.LowLevelMouseProc? _mouseProc;
        private bool _isRunning;
        private bool _disposed;

        #endregion

        #region Events

        /// <inheritdoc/>
        public event EventHandler<GameInputEventArgs>? OnInput;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public bool IsRunning => _isRunning;

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public void Start()
        {
            if (_isRunning) return;

            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;

            _keyboardHookId = SetKeyboardHook(_keyboardProc);
            _mouseHookId = SetMouseHook(_mouseProc);

            _isRunning = true;
            Debug.WriteLine("[InputManager] Started listening for global input events.");
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (!_isRunning) return;

            if (_keyboardHookId != IntPtr.Zero)
            {
                Win32Helper.UnhookWindowsHookEx(_keyboardHookId);
                _keyboardHookId = IntPtr.Zero;
            }

            if (_mouseHookId != IntPtr.Zero)
            {
                Win32Helper.UnhookWindowsHookEx(_mouseHookId);
                _mouseHookId = IntPtr.Zero;
            }

            _isRunning = false;
            Debug.WriteLine("[InputManager] Stopped listening for global input events.");
        }

        #endregion

        #region Private Methods

        private IntPtr SetKeyboardHook(Win32Helper.LowLevelKeyboardProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return Win32Helper.SetWindowsHookEx(
                Win32Helper.WH_KEYBOARD_LL,
                proc,
                Win32Helper.GetModuleHandle(curModule?.ModuleName),
                0);
        }

        private IntPtr SetMouseHook(Win32Helper.LowLevelMouseProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return Win32Helper.SetWindowsHookEx(
                Win32Helper.WH_MOUSE_LL,
                proc,
                Win32Helper.GetModuleHandle(curModule?.ModuleName),
                0);
        }

        /// <summary>
        /// F1 키 블로킹 여부를 결정하는 콜백 (true면 다른 앱으로 전달 안 함)
        /// </summary>
        public Func<int, bool>? ShouldBlockKey { get; set; }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == Win32Helper.WM_KEYDOWN || msg == Win32Helper.WM_SYSKEYDOWN)
                {
                    var hookStruct = Marshal.PtrToStructure<Win32Helper.KBDLLHOOKSTRUCT>(lParam);
                    var args = new GameInputEventArgs(GameInputType.Keyboard, (int)hookStruct.vkCode);
                    OnInput?.Invoke(this, args);
                    Debug.WriteLine($"[InputManager] Keyboard: VK={hookStruct.vkCode}");

                    // F1 키(112)를 처리했고, 블로킹이 필요하면 다른 앱으로 전달 안 함
                    if (hookStruct.vkCode == 112 && ShouldBlockKey?.Invoke((int)hookStruct.vkCode) == true)
                    {
                        return (IntPtr)1; // 키 이벤트 소비 (다른 앱으로 전달 안 함)
                    }
                }
            }
            return Win32Helper.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                GameMouseButton button = GameMouseButton.None;

                switch (msg)
                {
                    case Win32Helper.WM_LBUTTONDOWN:
                        button = GameMouseButton.Left;
                        break;
                    case Win32Helper.WM_RBUTTONDOWN:
                        button = GameMouseButton.Right;
                        break;
                    case Win32Helper.WM_MBUTTONDOWN:
                        button = GameMouseButton.Middle;
                        break;
                }

                if (button != GameMouseButton.None)
                {
                    var args = new GameInputEventArgs(GameInputType.Mouse, mouseButton: button);
                    OnInput?.Invoke(this, args);
                    Debug.WriteLine($"[InputManager] Mouse: {button}");
                }
            }
            return Win32Helper.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Stop();
            }

            _disposed = true;
        }

        ~GlobalInputManager()
        {
            Dispose(false);
        }

        #endregion
    }
}
