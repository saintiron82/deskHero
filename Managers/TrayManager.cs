using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 시스템 트레이 아이콘 관리 클래스
    /// </summary>
    public class TrayManager : IDisposable
    {
        #region Fields

        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private bool _disposed;

        #endregion

        #region Events

        public event EventHandler? DragModeToggled;
        public event EventHandler? SettingsRequested;
        public event EventHandler? ExitRequested;

        #endregion

        #region Properties

        public bool IsDragMode { get; private set; }

        #endregion

        #region Public Methods

        public void Initialize()
        {
            CreateContextMenu();
            CreateNotifyIcon();
        }

        public void SetDragMode(bool enabled)
        {
            IsDragMode = enabled;
            UpdateDragModeMenuItem();
        }

        public void UpdateLanguage()
        {
            var loc = LocalizationManager.Instance;

            // 트레이 아이콘 툴팁
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = loc["ui.tray.title"];
            }

            // 컨텍스트 메뉴 업데이트
            if (_contextMenu != null)
            {
                if (_contextMenu.Items["DragMode"] is ToolStripMenuItem dragItem)
                {
                    dragItem.Text = loc["ui.tray.dragMode"];
                }

                // 설정 메뉴 (인덱스 2)
                if (_contextMenu.Items.Count > 2 && _contextMenu.Items[2] is ToolStripMenuItem settingsItem)
                {
                    settingsItem.Text = loc["ui.tray.settings"];
                }

                // 종료 메뉴 (인덱스 4)
                if (_contextMenu.Items.Count > 4 && _contextMenu.Items[4] is ToolStripMenuItem exitItem)
                {
                    exitItem.Text = loc["ui.tray.exit"];
                }
            }
        }

        #endregion

        #region Private Methods

        private void CreateContextMenu()
        {
            _contextMenu = new ContextMenuStrip();

            // 드래그 모드 토글
            var dragModeItem = new ToolStripMenuItem("📌 드래그 모드")
            {
                CheckOnClick = true,
                Name = "DragMode"
            };
            dragModeItem.Click += (s, e) =>
            {
                IsDragMode = dragModeItem.Checked;
                DragModeToggled?.Invoke(this, EventArgs.Empty);
            };
            _contextMenu.Items.Add(dragModeItem);

            // 구분선
            _contextMenu.Items.Add(new ToolStripSeparator());

            // 설정
            var settingsItem = new ToolStripMenuItem("⚙️ 설정...");
            settingsItem.Click += (s, e) => SettingsRequested?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(settingsItem);

            // 구분선
            _contextMenu.Items.Add(new ToolStripSeparator());

            // 종료
            var exitItem = new ToolStripMenuItem("❌ 종료");
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(exitItem);
        }

        private void CreateNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Text = "DeskWarrior - 트레이 더블클릭 또는 F1키로 드래그 모드",
                Visible = true,
                ContextMenuStrip = _contextMenu,
                Icon = CreateDefaultIcon()
            };

            _notifyIcon.DoubleClick += (s, e) =>
            {
                // 더블클릭 시 드래그 모드 토글
                ToggleDragMode();
            };

            // 시작 시 알림 표시 (비활성화)
            // _notifyIcon.ShowBalloonTip(3000, "DeskWarrior", 
            //     "트레이 아이콘 더블클릭 또는 F1 키로 드래그 모드 전환", 
            //     ToolTipIcon.Info);
        }

        /// <summary>
        /// 드래그 모드 토글 (외부에서 호출 가능)
        /// </summary>
        public void ToggleDragMode()
        {
            IsDragMode = !IsDragMode;
            UpdateDragModeMenuItem();
            DragModeToggled?.Invoke(this, EventArgs.Empty);
            
            // 드래그 모드 전환 알림 (비활성화)
            // _notifyIcon?.ShowBalloonTip(1000, "DeskWarrior", 
            //     IsDragMode ? "드래그 모드 ON - 윈도우 이동 가능" : "드래그 모드 OFF", 
            //     ToolTipIcon.Info);
        }

        private Icon CreateDefaultIcon()
        {
            // 간단하게 기본 시스템 아이콘 사용
            return SystemIcons.Shield;
        }

        private void UpdateDragModeMenuItem()
        {
            if (_contextMenu?.Items["DragMode"] is ToolStripMenuItem item)
            {
                item.Checked = IsDragMode;
            }
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
                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
