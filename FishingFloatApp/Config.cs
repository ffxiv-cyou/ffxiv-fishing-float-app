using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;

namespace FishingFloatApp
{
    public class Config
    {
        [JsonIgnore]
        string FilePath => Path.Combine(UserFolder, "config.json");

        [JsonIgnore]
        public string WebViewUserDataFolder => Path.Combine(UserFolder, "WebView2Data");

        [JsonIgnore]
        public string UserFolder { get; } = "";

        [JsonIgnore]
        ILogger? Logger { get; }

        [JsonIgnore]
        public bool FirstRun { get; set; } = true;

        public Config()
        {
            Logger = null;
        }

        public Config(ILogger logger)
        {
            this.Logger = logger;
            this.UserFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FishingFloatApp"
            );
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

        public void Save()
        {
            lock (this)
            {
                try
                {
                    var serializer = JsonSerializer.CreateDefault();
                    using (var sw = new StreamWriter(FilePath))
                    {
                        serializer.Serialize(sw, this);
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
                var serializer = JsonSerializer.CreateDefault();
                using (var sr = new StreamReader(FilePath))
                {
                    var obj = serializer.Deserialize(sr, typeof(Config)) as Config;
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