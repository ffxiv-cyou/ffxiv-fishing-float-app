using Microsoft.Web.WebView2.Core;
using System.Windows;

namespace FishingFloatApp
{
    /// <summary>
    /// PopupWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PopupWindow : Window
    {
        public PopupWindow()
        {
            InitializeComponent();

            webview.CoreWebView2InitializationCompleted += (s, e) =>
            {
                webview.CoreWebView2.DocumentTitleChanged += onTitleChanged;
            };
        }

        private void onTitleChanged(object? sender, object e)
        {
            Title = webview.CoreWebView2.DocumentTitle;
        }

        public void Show(CoreWebView2NewWindowRequestedEventArgs e)
        {
            webview.Source = new Uri(e.Uri);
            if (e.WindowFeatures.HasSize)
            {
                Width = (int)e.WindowFeatures.Width;
                Height = (int)e.WindowFeatures.Height;
            }
            if (e.WindowFeatures.HasPosition)
            {
                Left = (int)e.WindowFeatures.Left;
                Top = (int)e.WindowFeatures.Top;
            }

            var obj = e.GetDeferral();
            webview.EnsureCoreWebView2Async().ContinueWith((Task t) =>
            {
                e.NewWindow = webview.CoreWebView2;
                obj.Complete();
            }, TaskScheduler.FromCurrentSynchronizationContext());

            Show();
        }
    }
}
