using FishingFloatApp.Overlay;
using Microsoft.Web.WebView2.Core;
using System.Windows;
using System.IO;
using System.Diagnostics;

namespace FishingFloatApp
{
    /// <summary>
    /// WelcomeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        public WelcomeWindow()
        {
            InitializeComponent();
            runCheckes(this, new RoutedEventArgs());
        }

        string? checkIfWebview2IsInstalled()
        {
            var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            return version;
        }

        string? checkNpcapIsInstalled()
        {
            var installPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Npcap", "NPFInstall.exe");
            if (File.Exists(installPath))
            {
                try
                {
                    var version = FileVersionInfo.GetVersionInfo(installPath);
                    return version?.FileVersion;
                }
                catch { }
            }

            return null;
        }

        void runCheckes(object sender, RoutedEventArgs e)
        {
            var webview2Installed = checkIfWebview2IsInstalled();
            Webview2Status.Content = webview2Installed ?? "未安装";
            Webview2Status.Foreground = webview2Installed != null ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            Webview2Link.Visibility = webview2Installed == null ? Visibility.Visible : Visibility.Collapsed;
            var npcapInstalled = checkNpcapIsInstalled();
            NpCapStatus.Content = npcapInstalled ?? "未安装";
            NpCapStatus.Foreground = npcapInstalled != null ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            NpCapLink.Visibility = npcapInstalled == null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void openWebview2Url(object sender, RoutedEventArgs e)
        {
            OpenBrowserWorker.OpenUrl("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
        }

        private void openNpcapUrl(object sender, RoutedEventArgs e)
        {
            OpenBrowserWorker.OpenUrl("https://npcap.com/dist/");
        }

        private void openOverlayUrl(object sender, RoutedEventArgs e)
        {
            OpenBrowserWorker.OpenUrl("https://fisher.ffxiv.cyou/");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
