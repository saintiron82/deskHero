using System.Windows;
using DeskWarrior.Helpers;

namespace DeskWarrior
{
    /// <summary>
    /// Application entry point
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Logger.Log("========================================");
            Logger.Log("DeskWarrior Application Starting...");
            Logger.Log($"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            Logger.Log($"OS: {System.Environment.OSVersion}");
            Logger.Log($".NET: {System.Environment.Version}");
            Logger.Log("========================================");

            base.OnStartup(e);

            Logger.Log("Application startup completed");
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
