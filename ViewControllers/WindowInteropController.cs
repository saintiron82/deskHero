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

        public WindowInteropController(MainWindow window)
        {
            _window = window;
        }

        public void InitializeWindow()
        {
            _hwnd = new WindowInteropHelper(_window).Handle;

            // WndProc 훅 추가 (WM_NCHITTEST 처리용)
            HwndSource source = HwndSource.FromHwnd(_hwnd);
            source.AddHook(WndProc);

            // 태스크바에서 숨기기
            Win32Helper.SetWindowToolWindow(_hwnd);
        }

        public void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 창 드래그는 InfoBar에서 직접 처리
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
                // GameOverOverlay가 보이면 클릭 가능
                if (_window.GameOverOverlay.Visibility == Visibility.Visible)
                {
                    handled = true;
                    return new IntPtr(HTCLIENT);
                }

                // 마우스 좌표 계산
                int x = (short)(lParam.ToInt32() & 0xFFFF);
                int y = (short)(lParam.ToInt32() >> 16);
                Point screenPoint = new Point(x, y);
                Point clientPoint = _window.PointFromScreen(screenPoint);

                // InfoBar, UpgradePanel, UtilityPanel, PowerInfoBar 영역만 클릭 가능
                if (IsPointOverInfoBar(clientPoint) ||
                    IsPointOverUpgradePanel(clientPoint) ||
                    IsPointOverUtilityPanel(clientPoint) ||
                    IsPointOverPowerInfoBar(clientPoint))
                {
                    handled = true;
                    return new IntPtr(HTCLIENT);
                }

                // 나머지 영역: 클릭 통과 (투명)
                handled = true;
                return new IntPtr(HTTRANSPARENT);
            }
            return IntPtr.Zero;
        }

        private bool IsPointOverInfoBar(Point point)
        {
            try
            {
                var actualW = _window.GoldInfoBarTop.ActualWidth;
                var actualH = _window.GoldInfoBarTop.ActualHeight;

                GeneralTransform transform = _window.GoldInfoBarTop.TransformToAncestor(_window);
                Rect bounds = transform.TransformBounds(
                    new Rect(0, 0, actualW, actualH));

                return bounds.Contains(point);
            }
            catch
            {
                return false;
            }
        }

        private bool IsPointOverUpgradePanel(Point point)
        {
            try
            {
                if (_window.UpgradePanel.Visibility != Visibility.Visible)
                    return false;

                var actualW = _window.UpgradePanel.ActualWidth;
                var actualH = _window.UpgradePanel.ActualHeight;

                GeneralTransform transform = _window.UpgradePanel.TransformToAncestor(_window);
                Rect bounds = transform.TransformBounds(
                    new Rect(0, 0, actualW, actualH));

                return bounds.Contains(point);
            }
            catch
            {
                return false;
            }
        }

        private bool IsPointOverUtilityPanel(Point point)
        {
            try
            {
                if (_window.UtilityPanel.Visibility != Visibility.Visible)
                    return false;

                var actualW = _window.UtilityPanel.ActualWidth;
                var actualH = _window.UtilityPanel.ActualHeight;

                GeneralTransform transform = _window.UtilityPanel.TransformToAncestor(_window);
                Rect bounds = transform.TransformBounds(
                    new Rect(0, 0, actualW, actualH));

                return bounds.Contains(point);
            }
            catch
            {
                return false;
            }
        }

        private bool IsPointOverPowerInfoBar(Point point)
        {
            try
            {
                if (_window.PowerInfoBar.Visibility != Visibility.Visible)
                    return false;

                var actualW = _window.PowerInfoBar.ActualWidth;
                var actualH = _window.PowerInfoBar.ActualHeight;

                GeneralTransform transform = _window.PowerInfoBar.TransformToAncestor(_window);
                Rect bounds = transform.TransformBounds(
                    new Rect(0, 0, actualW, actualH));

                return bounds.Contains(point);
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
