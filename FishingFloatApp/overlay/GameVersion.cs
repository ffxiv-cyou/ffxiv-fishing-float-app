using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace FishingFloatApp.Overlay
{
    /// <summary>
    /// 游戏版本
    /// </summary>
    class GameVersion : IWorker
    {
        public string Name => "otk::game_ver";


        public GameVersion()
        {
        }

        public void Init(IEventRepo repo)
        {
            repo.RegisterHandler(this);
        }

        public JsonElement? HandleEvent(JsonElement e)
        {
            JsonElement fallback = JsonSerializer.SerializeToElement(new { });

            IntPtr hWindow = SystemAPI.FindWindow(null, "最终幻想XIV");
            if (hWindow == IntPtr.Zero)
                return fallback;

            _ = SystemAPI.GetWindowThreadProcessId(hWindow, out uint processId);
            if (processId == 0)
                return fallback;

            var process = SystemAPI.OpenProcess(SystemAPI.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (process == IntPtr.Zero)
                return fallback;

            int size = 1024;
            StringBuilder sb = new StringBuilder(size);
            if (!SystemAPI.QueryFullProcessImageName(process, 0, sb, ref size))
                return fallback;

            var gameDir = System.IO.Path.GetDirectoryName(sb.ToString());
            var versionPath = System.IO.Path.Combine(gameDir, "ffxivgame.ver");

            try
            {
                var version = System.IO.File.ReadAllText(versionPath);
                return JsonSerializer.SerializeToElement(new
                {
                    version = version.Trim(),
                    lang = 6,
                });
            }
            catch
            {
                return fallback;
            }
        }
    }
}
