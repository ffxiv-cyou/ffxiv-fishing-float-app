using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace FishingFloatApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Mutex globalMutex { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
#if DEBUG
            ConsoleHelper.AllocConsole();
            Trace.Listeners.Add(new ConsoleTraceListener());
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

            globalMutex = new Mutex(false, "FisherDesktop", out bool mutexWasCreated);
            if (!mutexWasCreated)
            {
                MessageBox.Show("已经启动了一个实例，请查看右下角通知栏", "没有更多效果了……");
                Shutdown(1);
                return;
            }

            FisherDesktop main = new FisherDesktop(log);
            main.Init();
            Action start = () =>
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
