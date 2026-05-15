using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace FishingFloatApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IInvoker
    {
        OverlayPluginApi api { get; }

        MainWindowViewModel vm { get; }

        ILogger logger { get; }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public MainWindow(ILogger logger, Config cfg, OverlayPluginApi api)
        {
            this.logger = logger;
            // Create the environment with the resolved path
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", cfg.WebViewUserDataFolder);

            vm = new MainWindowViewModel(cfg);

            this.api = api;
            InitializeComponent();

            webview.NavigationStarting += onWebviewNavStart;
            webview.NavigationCompleted += onWebviewNavComplete;
            webview.CoreWebView2InitializationCompleted += async (s, e) =>
            {
                webview.CoreWebView2.ContextMenuRequested += (s2, e2) => e2.Handled = true; // disable context menu
                webview.CoreWebView2.NewWindowRequested += onWebviewNewWindowRequested;
                webview.CoreWebView2.WebMessageReceived += onMessageReceived;
            };

            DataContext = vm;
        }

        private void onMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.WebMessageAsJson;
                var obj = JsonElement.Parse(json);
                var type = obj.GetProperty("type").GetString();

                if (!vm.Transparent && type == "startWindowDrag")
                {
                    StartNativeDrag();
                }
                else if (type == "contextMenu")
                {
                    ContextMenu? menu = this.FindResource("webview_menu") as ContextMenu;
                    if (menu != null)
                    {
                        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                        menu.PlacementTarget = this;
                        menu.IsOpen = true;
                    }
                }
            }
            catch { }
        }

        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x0002;
        private void StartNativeDrag()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            ReleaseCapture();
            SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }

        private int retryCount = 0;

        private void onWebviewNavComplete(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                if (retryCount < 3)
                {
                    Trace.TraceWarning($"WebView2 navigation failed with error code {e.WebErrorStatus}. Retrying... ({retryCount + 1}/3)");
                    webview.Reload();
                    retryCount++;
                }
                else
                {
                    Trace.TraceError($"WebView2 navigation failed after 3 attempts. Error code: {e.WebErrorStatus}");
                }
            }
        }

        async private void onWebviewNavStart(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Trace.TraceInformation($"WebView2 navigation starting: {e.Uri}");
            api.Initialize(webview.CoreWebView2, this);
            var dragScript = @"(() => {
                    document.addEventListener('mousedown', (e) => {
                        const interactive = e.target.closest('input, textarea, button, a, select');
                        if (interactive) return;
                        if (e.button === 0)
                            window.chrome.webview.postMessage({ type: 'startWindowDrag' });
                        if (e.button === 2)
                            window.chrome.webview.postMessage({ type: 'contextMenu' });
                      });
                })()";
            _ = webview.CoreWebView2.ExecuteScriptAsync(dragScript);
        }

        private void onWebviewNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            var popup = new PopupWindow();
            Invoke(() => popup.Show(e));
        }

        public void Invoke(Action cb)
        {
            Dispatcher.Invoke(cb);
        }

        void ClickExit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ClickSetting(object sender, RoutedEventArgs e)
        {
            CallFishingFloatMethod("openSettingPage");
        }

        private void ClickHistory(object sender, RoutedEventArgs e)
        {
            CallFishingFloatMethod("openHistory");
        }

        private void ClickExport(object sender, RoutedEventArgs e)
        {
            CallFishingFloatMethod("openNoteExportPage");
        }

        private void ClickRefresh(object sender, RoutedEventArgs e)
        {
            webview.ExecuteScriptAsync($@"window.location='{vm.ShowingUri}';window.location.reload();");
        }

        void CallFishingFloatMethod(string name)
        {
            webview.ExecuteScriptAsync($@"window.FishingFloat.{name}()");
        }
    }

    interface IInvoker
    {
        void Invoke(Action cb);
    }
}