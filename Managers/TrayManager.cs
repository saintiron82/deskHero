using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using DeskWarrior.Interfaces;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 시스템 트레이 아이콘 관리 클래스
    /// </summary>
    public class TrayManager : ITrayManager
    {
        #region Fields

        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private bool _disposed;

        #endregion

        #region Events

        public event EventHandler? SettingsRequested;
        public event EventHandler? ExitRequested;

        #endregion

        #region Public Methods

        public void Initialize()
        {
            CreateContextMenu();
            CreateNotifyIcon();
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
                // 설정 메뉴 (인덱스 0)
                if (_contextMenu.Items.Count > 0 && _contextMenu.Items[0] is ToolStripMenuItem settingsItem)
                {
                    settingsItem.Text = loc["ui.tray.settings"];
                }

                // 종료 메뉴 (인덱스 2)
                if (_contextMenu.Items.Count > 2 && _contextMenu.Items[2] is ToolStripMenuItem exitItem)
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
                Text = "DeskWarrior",
                Visible = true,
                ContextMenuStrip = _contextMenu,
                Icon = CreateDefaultIcon()
            };
        }

        private Icon CreateDefaultIcon()
        {
            // 간단하게 기본 시스템 아이콘 사용
            return SystemIcons.Shield;
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
