using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DeskWarrior.Helpers;

namespace DeskWarrior.Windows
{
    public partial class DragToggleWindow : Window
    {
        public event EventHandler? ToggleRequested;
        public event EventHandler<DragDeltaEventArgs>? DragDeltaRequested;

        // 드래그 판별용 필드
        private Point _mouseDownScreenPosition;
        private bool _isMouseDown;
        private bool _isDragging;
        private bool _isUnlockedMode = true; // 기본값: 해제 모드

        // 애니메이션 관련 필드
        private bool _isVisible = false;
        private Storyboard? _showStoryboard;
        private Storyboard? _hideStoryboard;

        private const double DragThreshold = 5.0;

        public DragToggleWindow()
        {
            InitializeComponent();

            // Storyboard 캐싱
            _showStoryboard = (Storyboard)Resources["ShowAnimation"];
            _hideStoryboard = (Storyboard)Resources["HideAnimation"];

            // 초기 상태: 숨김
            Opacity = 0;
            SlideTransform.X = 20;
        }

        /// <summary>
        /// 부드러운 애니메이션으로 버튼 표시
        /// </summary>
        public void ShowAnimated()
        {
            if (_isVisible) return;
            _isVisible = true;

            // 숨기기 애니메이션 중지
            _hideStoryboard?.Stop(this);

            // 표시 애니메이션 시작
            _showStoryboard?.Begin(this, true);
        }

        /// <summary>
        /// 부드러운 애니메이션으로 버튼 숨김
        /// </summary>
        public void HideAnimated()
        {
            if (!_isVisible) return;
            _isVisible = false;

            // 표시 애니메이션 중지
            _showStoryboard?.Stop(this);

            // 숨기기 애니메이션 시작
            _hideStoryboard?.Begin(this, true);
        }

        /// <summary>
        /// 현재 표시 상태 확인
        /// </summary>
        public bool IsAnimatedVisible => _isVisible;

        public void UpdateIcon(bool isDragMode)
        {
            _isUnlockedMode = !isDragMode;

            // Path 데이터 변경
            var lockPath = (Geometry)Resources["LockIconPath"];
            var unlockPath = (Geometry)Resources["UnlockIconPath"];
            LockIcon.Data = isDragMode ? lockPath : unlockPath;

            // 색상 변경
            LockIcon.Fill = isDragMode
                ? new SolidColorBrush(Color.FromRgb(0xFF, 0xAA, 0x55))  // 주황색 (잠금)
                : new SolidColorBrush(Color.FromRgb(0xAA, 0xFF, 0xAA)); // 녹색 (해제)

            // 배경색 변경
            ToggleBorder.Background = isDragMode
                ? new SolidColorBrush(Color.FromArgb(0xDD, 0x44, 0x33, 0x00))
                : new SolidColorBrush(Color.FromArgb(0xDD, 0x22, 0x22, 0x33));
        }

        public void UpdatePosition(double mainWindowLeft, double mainWindowTop, double mainWindowWidth)
        {
            Left = mainWindowLeft + mainWindowWidth - Width - 5;
            Top = mainWindowTop + 5;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Logger.Log($"[DragToggle] MouseDown - IsHitTestVisible={IsHitTestVisible}, Visibility={Visibility}, _isUnlockedMode={_isUnlockedMode}");
            _mouseDownScreenPosition = PointToScreen(e.GetPosition(this));
            _isMouseDown = true;
            _isDragging = false;
            ToggleBorder.CaptureMouse();
            e.Handled = true;
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isMouseDown) return;

            if (e.LeftButton == MouseButtonState.Released)
            {
                if (ToggleBorder.IsMouseCaptured)
                    ToggleBorder.ReleaseMouseCapture();
                _isMouseDown = false;
                _isDragging = false;
                return;
            }

            Point currentScreenPosition = PointToScreen(e.GetPosition(this));
            Vector diff = currentScreenPosition - _mouseDownScreenPosition;

            if (Math.Abs(diff.X) > DragThreshold || Math.Abs(diff.Y) > DragThreshold)
            {
                if (_isUnlockedMode)
                {
                    _isDragging = true;
                    DragDeltaRequested?.Invoke(this, new DragDeltaEventArgs(diff.X, diff.Y));
                    _mouseDownScreenPosition = currentScreenPosition;
                }
            }
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Logger.Log($"[DragToggle] MouseUp - _isMouseDown={_isMouseDown}, _isDragging={_isDragging}, _isUnlockedMode={_isUnlockedMode}");

            if (!_isMouseDown) return;

            if (ToggleBorder.IsMouseCaptured)
                ToggleBorder.ReleaseMouseCapture();

            // 해제 모드: 드래그가 아니면 토글
            // 잠금 모드: 항상 토글
            if (!_isDragging || !_isUnlockedMode)
            {
                Logger.Log("[DragToggle] Invoking ToggleRequested");
                ToggleRequested?.Invoke(this, EventArgs.Empty);
            }

            _isMouseDown = false;
            _isDragging = false;
            e.Handled = true;
        }
    }

    public class DragDeltaEventArgs : EventArgs
    {
        public double DeltaX { get; }
        public double DeltaY { get; }

        public DragDeltaEventArgs(double deltaX, double deltaY)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
        }
    }
}
