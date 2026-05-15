using System.Diagnostics;
using System.Text.Json;

namespace FishingFloatApp.Overlay
{
    /// <summary>
    /// 在默认浏览器中打开指定URL的Worker
    /// </summary>
    class OpenBrowserWorker : IWorker
    {
        string IWorker.Name => "otk::open_browser";
        JsonElement? IWorker.HandleEvent(JsonElement req)
        {
            if (!req.TryGetProperty("url", out var urlProp) || urlProp.ValueKind != JsonValueKind.String)
                return JsonHelper.Error("Missing 'url' field");

            var url = urlProp.GetString();
            if (string.IsNullOrEmpty(url))
                return JsonHelper.Error("Missing 'url' field");

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                return JsonHelper.Error("Invalid URL");

            var uri = new Uri(url);
            var allowedSchemes = new[] { "http", "https" };

            if (!allowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase))
                return JsonHelper.Error("Only http/https URLs are allowed");

            try
            {
                OpenUrl(url);
                return JsonSerializer.SerializeToElement(new
                {
                    url = url,
                });
            }
            catch (Exception ex)
            {
                return JsonHelper.Error(ex.Message);
            }
        }

        public static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        void IWorker.Init(IEventRepo repo)
        {
            repo.RegisterHandler(this);
        }
    }
}
