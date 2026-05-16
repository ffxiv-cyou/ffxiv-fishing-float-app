using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace FishingFloatApp
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class OverlayPluginApi : IEventReceiver
    {
        private CoreWebView2 webview { get; set; } = null;

        private ILogger log { get; }
        private IInvoker invoker { get; set; }

        public delegate JsonElement? CallHandlerDelegate(string data);

        [ComVisible(false)]
        public CallHandlerDelegate CallHandler { get; set; }

        public OverlayPluginApi(ILogger logger)
        {
            this.log = logger;
        }

        [ComVisible(false)]
        internal void Initialize(CoreWebView2 webview, IInvoker invoker)
        {
            this.webview = webview;
            this.invoker = invoker;

            webview.AddHostObjectToScript("OverlayPluginApi", this);
            var initScript = @"(() => {
                    window.OverlayPluginApi = {
                        overlayName: null,
                        overlayUuid: null,
                        ready: true,
                        desktopApp: true,
                        sequence: 0,
                        pendingCallbacks: new Map(),
                        callHandler(data, cb) {
                            var id = '';
                            if (cb) {
                                id = new Date().getTime() + '_' + (this.sequence++);
                                this.pendingCallbacks.set(id, cb);
                            }
                            chrome.webview.hostObjects.OverlayPluginApi.callHandler(data, id);
                        },
                        __callback(id, data) {
                            if (this.pendingCallbacks.has(id)) {
                                var cb = this.pendingCallbacks.get(id);
                                cb(JSON.stringify(data));
                                this.pendingCallbacks.delete(id);
                            } else {
                                console.warn('No callback found for id', id);
                            }
                        }
                    };
                })();";
            webview.ExecuteScriptAsync(initScript);
        }

        [ComVisible(false)]
        void Callback(string callbackId, JsonElement? jsonObject)
        {
            if (webview == null)
            {
                log.LogError("Webview is not initialized. Cannot execute callback.");
                return;
            }

            invoker?.Invoke(() =>
            {
                webview.ExecuteScriptAsync($"window.OverlayPluginApi.__callback('{callbackId}', {jsonObject?.ToString() ?? "undefined"});");
            });
        }

        [ComVisible(true)]
        public void callHandler(string data, string callbackId)
        {
            log.LogDebug("received call {callbackId} with data: {data}", callbackId, data);
            Task.Run(() =>
            {
                try
                {
                    var resp = CallHandler?.Invoke(data);
                    if (!string.IsNullOrEmpty(callbackId))
                    {
                        Callback(callbackId, resp);
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error handling callHandler with data: " + data);
                    if (!string.IsNullOrEmpty(callbackId))
                    {
                        Callback(callbackId, JsonSerializer.SerializeToElement(new { error = ex.Message }));
                    }
                }
            });
        }

        [ComVisible(false)]
        public void HandleEvent(JsonElement e)
        {
            if (webview == null)
            {
                log.LogError("Webview is not initialized. Cannot handle event.");
                return;
            }
            invoker?.Invoke(() =>
            {
                webview.ExecuteScriptAsync($"if(window.__OverlayCallback) __OverlayCallback({e.ToString()})");
            });
        }
    }

    public delegate JsonElement? EventHandlerDelegate(JsonElement e);
}
