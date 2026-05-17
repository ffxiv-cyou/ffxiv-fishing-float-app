using FishingFloatApp.Overlay;
using System.Windows;

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

        void runCheckes(object sender, RoutedEventArgs e)
        {
            var webviewVersion = Config.GetWebview2Version();
            Webview2Status.Content = webviewVersion ?? "未安装";
            Webview2Status.Foreground = webviewVersion != null ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            Webview2Link.Visibility = webviewVersion == null ? Visibility.Visible : Visibility.Collapsed;
            var npcapVersion = Config.GetPcapVersion();
            NpCapStatus.Content = npcapVersion ?? "未安装";
            NpCapStatus.Foreground = npcapVersion != null ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            NpCapLink.Visibility = npcapVersion == null ? Visibility.Visible : Visibility.Collapsed;
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

        private void openHelpUrl(object sender, RoutedEventArgs e)
        {
            OpenBrowserWorker.OpenUrl("https://fisher.ffxiv.cyou/web/#/help/app");
        }

        private void closeWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
