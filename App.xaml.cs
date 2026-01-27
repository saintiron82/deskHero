using System;
using System.Windows;
using System.Windows.Threading;
using DeskWarrior.Helpers;
using DeskWarrior.Managers;
using DeskWarrior.Models;

namespace DeskWarrior
{
    /// <summary>
    /// Application entry point
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 전역 예외 핸들러 등록
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            Logger.Log("========================================");
            Logger.Log("DeskWarrior Application Starting...");
            Logger.Log($"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            Logger.Log($"OS: {System.Environment.OSVersion}");
            Logger.Log($".NET: {System.Environment.Version}");
            Logger.Log("========================================");

            // 의존성 주입: AchievementDefinition에 LocalizationProvider 설정
            AchievementDefinition.LocalizationProvider = LocalizationManager.Instance;

            base.OnStartup(e);

            Logger.Log("Application startup completed");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Logger.LogError("[FATAL] Unhandled AppDomain Exception", ex);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.LogError("[FATAL] Unhandled Dispatcher Exception", e.Exception);
            e.Handled = false; // 앱 종료 허용
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Log("========================================");
            Logger.Log("DeskWarrior Application Exiting...");
            Logger.Log($"Exit Code: {e.ApplicationExitCode}");
            Logger.Log("========================================");

            base.OnExit(e);
        }
    }
}
