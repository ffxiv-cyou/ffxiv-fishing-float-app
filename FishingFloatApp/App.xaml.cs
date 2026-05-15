using Microsoft.Extensions.Logging;
using System.Windows;

namespace FishingFloatApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
#if DEBUG
            ConsoleHelper.AllocConsole();
#endif
            var logFactory = LoggerFactory.Create((v) =>
            {
                v.AddConsole();
                v.SetMinimumLevel(LogLevel.Debug);
            });

            ILogger log = logFactory.CreateLogger("FisherDesktop");
            log.LogInformation("Application starting...");

            // allow auto play audio
            Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--autoplay-policy=no-user-gesture-required");

            FisherDesktop main = new FisherDesktop(log);
            main.Init();
#if DEBUG
            main.Config.FirstRun = true;
#endif
            var start = () =>
            {
                main.Start();

                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                MainWindow mw = new MainWindow(log, main.Config, main.Api);
                mw.Show();
            };

            bool depenceniesMissing = false;
            if (Config.GetWebview2Version() == null)
                depenceniesMissing = true;
            if (Config.GetPcapVersion() == null)
                depenceniesMissing = true;

            if (main.Config.FirstRun || depenceniesMissing)
            {
                Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                WelcomeWindow welcome = new WelcomeWindow();
                welcome.ShowDialog();
            }
            start();
        }
    }

}
