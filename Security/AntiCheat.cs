using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DeskWarrior.Security
{
    /// <summary>
    /// 안티-치트 기본 보호
    /// 디버거 감지 및 무결성 검사
    /// </summary>
    public static class AntiCheat
    {
        private static CancellationTokenSource? _monitorCts;
        private static bool _isMonitoring;

        #region Windows API

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

        #endregion

        /// <summary>
        /// 안티-치트 시스템 초기화
        /// RELEASE 빌드에서만 활성화
        /// </summary>
        public static void Initialize(Action? onViolationDetected = null)
        {
            if (!SecurityConfig.SecurityEnabled)
            {
                System.Diagnostics.Debug.WriteLine("[AntiCheat] Disabled in DEBUG mode");
                return;
            }

            // 초기 검사
            if (IsBeingDebugged())
            {
                System.Diagnostics.Debug.WriteLine("[AntiCheat] Debugger detected on startup");
                onViolationDetected?.Invoke();
                return;
            }

            // 백그라운드 모니터링 시작
            StartMonitoring(onViolationDetected);
        }

        /// <summary>
        /// 안티-치트 시스템 종료
        /// </summary>
        public static void Shutdown()
        {
            StopMonitoring();
        }

        /// <summary>
        /// 디버거 연결 여부 확인
        /// </summary>
        public static bool IsBeingDebugged()
        {
            if (!SecurityConfig.SecurityEnabled)
                return false;

            try
            {
                // 방법 1: .NET 관리 코드 체크
                if (Debugger.IsAttached)
                    return true;

                // Windows 전용 체크
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // 방법 2: 네이티브 디버거 체크
                    if (IsDebuggerPresent())
                        return true;

                    // 방법 3: 원격 디버거 체크
                    bool remoteDebugger = false;
                    CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref remoteDebugger);
                    if (remoteDebugger)
                        return true;
                }

                return false;
            }
            catch
            {
                // 예외 발생 시 안전하게 false 반환
                return false;
            }
        }

        /// <summary>
        /// 백그라운드 모니터링 시작
        /// </summary>
        private static void StartMonitoring(Action? onViolationDetected)
        {
            if (_isMonitoring)
                return;

            _isMonitoring = true;
            _monitorCts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                var token = _monitorCts.Token;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(5000, token); // 5초마다 검사

                        if (IsBeingDebugged())
                        {
                            System.Diagnostics.Debug.WriteLine("[AntiCheat] Debugger detected during monitoring");
                            onViolationDetected?.Invoke();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // 예외 무시하고 계속 모니터링
                    }
                }

                _isMonitoring = false;
            });
        }

        /// <summary>
        /// 백그라운드 모니터링 중지
        /// </summary>
        private static void StopMonitoring()
        {
            _monitorCts?.Cancel();
            _monitorCts?.Dispose();
            _monitorCts = null;
            _isMonitoring = false;
        }
    }
}
