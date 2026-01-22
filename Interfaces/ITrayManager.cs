using System;

namespace DeskWarrior.Interfaces
{
    /// <summary>
    /// 트레이 관리자 인터페이스 (SOLID: DIP, ISP)
    /// </summary>
    public interface ITrayManager : IDisposable
    {
        #region Events

        event EventHandler? SettingsRequested;
        event EventHandler? ExitRequested;

        #endregion

        #region Methods

        void Initialize();
        void UpdateLanguage();

        #endregion
    }
}
