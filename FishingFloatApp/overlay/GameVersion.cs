using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Text;

namespace FishingFloatApp.Overlay
{
    /// <summary>
    /// 游戏版本
    /// </summary>
    class GameVersion : IWorker
    {
        public string Name => "otk::game_ver";

        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? sClass, string? sWindow);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, [Out, MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpExeName, ref uint lpdwSize);

        const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        public GameVersion()
        {
        }

        public void Init(IEventRepo repo)
        {
            repo.RegisterHandler(this);
        }

        public JToken? HandleEvent(JObject e)
        {
            JToken fallback = "{}";

            IntPtr hWindow = FindWindow(null, "最终幻想XIV");
            if (hWindow == IntPtr.Zero)
                return fallback;

            _ = GetWindowThreadProcessId(hWindow, out uint processId);
            if (processId == 0)
                return fallback;

            var process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (process == IntPtr.Zero)
                return fallback;

            StringBuilder sb = new StringBuilder();
            uint size = 1024;
            if (!QueryFullProcessImageName(process, 0, sb, ref size))
                return fallback;

            var gameDir = System.IO.Path.GetDirectoryName(sb.ToString());
            var versionPath = System.IO.Path.Combine(gameDir, "ffxivgame.ver");

            try
            {
                var version = System.IO.File.ReadAllText(versionPath);
                return JObject.FromObject(new
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
