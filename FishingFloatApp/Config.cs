using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FishingFloatApp
{
    public class Config
    {
        [JsonIgnore]
        string FilePath => Path.Combine(UserFolder, "config.json");

        [JsonIgnore]
        public string WebViewUserDataFolder => Path.Combine(UserFolder, "WebView2Data");

        [JsonIgnore]
        public static string UserFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FishingFloatApp"
                );
            }
        }

        [JsonIgnore]
        ILogger Logger { get; }

        [JsonIgnore]
        public bool FirstRun { get; set; } = true;

        public Config()
        {
            Logger = null;
        }

        public Config(ILogger logger)
        {
            this.Logger = logger;
        }

        public static string GetWebview2Version()
        {
            var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            return version;
        }

        public static string GetPcapVersion()
        {
            var installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Npcap", "NPFInstall.exe");
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

        public void EnsureFolder()
        {
            try
            {
                if (!Directory.Exists(UserFolder))
                    Directory.CreateDirectory(UserFolder);

                if (!Directory.Exists(WebViewUserDataFolder))
                    Directory.CreateDirectory(WebViewUserDataFolder);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to create folder at {path}", UserFolder);
            }
        }

        public int Width { get; set; } = 500;
        public int Height { get; set; } = 400;
        public int Left { get; set; } = 200;
        public int Top { get; set; } = 200;
        public string BaseURL { get; set; } = "https://fisher.ffxiv.cyou";

        public bool Debug { get; set; } = false;

        public bool MemoryScanMode { get; set; } = true;

        public void Save()
        {
            lock (this)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    using (var sw = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
                    {
                        JsonSerializer.Serialize(sw, this, options);
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "Failed to save config to {path}", FilePath);
                }
            }
        }

        public void Load()
        {
            if (!File.Exists(FilePath))
                return;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                using (var sr = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
                {
                    var obj = JsonSerializer.Deserialize(sr, typeof(Config), options) as Config;
                    if (obj != null)
                    {
                        Clone(obj);
                        FirstRun = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to load config from {path}", FilePath);
            }
        }

        void Clone(Config config)
        {
            this.Width = config.Width;
            this.Height = config.Height;
            this.Left = config.Left;
            this.Top = config.Top;
            this.BaseURL = config.BaseURL;
            this.Debug = config.Debug;
        }
    }
}