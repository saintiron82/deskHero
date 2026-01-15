using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DeskWarrior.Helpers;

namespace DeskWarrior.ViewControllers
{
    public class WindowInteropController : IDisposable
    {
        private readonly MainWindow _window;
        private IntPtr _hwnd;
        private bool _isManageMode;
        private bool _isModeButtonVisible;
        private System.Windows.Threading.DispatcherTimer? _hoverCheckTimer;

        public bool IsManageMode 
        { 
            get => _isManageMode; 
            set => _isManageMode = value; 
        }

        public WindowInteropController(MainWindow window)
        {
            _window = window;
            InitializeTimers();
        }

        private void InitializeTimers()
        {
            _hoverCheckTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _hoverCheckTimer.Tick += HoverCheckTimer_Tick;
        }

        public void InitializeWindow()
        {
            _hwnd = new WindowInteropHelper(_window).Handle;

            // WndProc 훅 추가 (WM_NCHITTEST 처리용)
            HwndSource source = HwndSource.FromHwnd(_hwnd);
            source.AddHook(WndProc);

            // 태스크바에서 숨기기
            Win32Helper.SetWindowToolWindow(_hwnd);

            // 호버 타이머 시작
            _hoverCheckTimer?.Start();
        }

        public void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isManageMode)
            {
                try
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        _window.DragMove();
                    }
                }
                catch (InvalidOperationException) { }
                catch (Exception ex)
                {
                    Logger.LogError("DragMove Failed", ex);
                }
            }
        }

        public bool IsMouseOverWindow()
        {
            if (Win32Helper.GetCursorPos(out var pt))
            {
                try
                {
                    var localPoint = _window.PointFromScreen(new Point(pt.x, pt.y));
                    return localPoint.X >= 0 && localPoint.X < _window.ActualWidth &&
                           localPoint.Y >= 0 && localPoint.Y < _window.ActualHeight;
                }
                catch { return false; }
            }
            return false;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = -1;
            const int HTCLIENT = 1;

            if (msg == WM_NCHITTEST)
            {
                if (!_isManageMode)
                {
                    // GameOverOverlay가 보이면 클릭 가능해야 함 (게임 오버 시)
                    if (_window.GameOverOverlay.Visibility == Visibility.Visible)
                    {
                        handled = true;
                        return new IntPtr(HTCLIENT);
                    }

                    int x = (short)(lParam.ToInt32() & 0xFFFF);
                    int y = (short)(lParam.ToInt32() >> 16);
                    Point screenPoint = new Point(x, y);
                    Point clientPoint = _window.PointFromScreen(screenPoint);

                    if (IsPointOverModeButton(clientPoint))
                    {
                        handled = true;
                        return new IntPtr(HTCLIENT);
                    }

                    handled = true;
                    return new IntPtr(HTTRANSPARENT);
                }
            }
            return IntPtr.Zero;
        }

        private void HoverCheckTimer_Tick(object? sender, EventArgs e)
        {
            if (_isManageMode) return;

            bool isOver = IsMouseOverWindow();
            if (isOver && !_isModeButtonVisible)
            {
                _isModeButtonVisible = true;
                ShowModeButton();
            }
            else if (!isOver && _isModeButtonVisible)
            {
                _isModeButtonVisible = false;
                HideModeButton();
            }
        }

        private void ShowModeButton()
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
            _window.ModeToggleBorder.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void HideModeButton()
        {
            if (_isManageMode) return;
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            _window.ModeToggleBorder.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private bool IsPointOverModeButton(Point point)
        {
            try
            {
                GeneralTransform transform = _window.ModeToggleBorder.TransformToAncestor(_window);
                Rect bounds = transform.TransformBounds(
                    new Rect(0, 0, _window.ModeToggleBorder.ActualWidth, _window.ModeToggleBorder.ActualHeight));
                return bounds.Contains(point);
            }
            catch { return false; }
        }

        public void ForceShowModeButton()
        {
            _isModeButtonVisible = true;
            // MainWindow에서 직접 UI 조작 (UpdateManageModeUI 호출 시 등)
        }

        public void ForceHideModeButton()
        {
            _isModeButtonVisible = false;
        }

        public void Dispose()
        {
            _hoverCheckTimer?.Stop();
        }
    }
}
