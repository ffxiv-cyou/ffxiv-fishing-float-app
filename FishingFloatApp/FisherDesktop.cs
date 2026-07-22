using FishingFloatApp.memory;
using FishingFloatApp.Overlay;
using FishingFloatApp.pages;
using Machina.FFXIV;
using Machina.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;

namespace FishingFloatApp
{
    class FisherDesktop
    {
        public OverlayPluginApi Api { get; }

        public Config Config { get; }

        EventRepo repo { get; }

        FFXIVNetworkMonitor monitor { get; } = new FFXIVNetworkMonitor();

        PacketWorker packetWorker { get; } = new PacketWorker();

        MemoryScanner Memory { get; set; }

        SigScanner SigScanner { get; set; }

        ILogger MemoryLogger { get; }

        public FisherDesktop(ILoggerFactory logger)
        {
            var overlayLogger = logger.CreateLogger("Overlay");
            MemoryLogger = logger.CreateLogger("Memory");

            repo = new EventRepo(overlayLogger);
            Api = new OverlayPluginApi(overlayLogger, "FisherDesktop");
            Config = new Config(logger.CreateLogger("Config"));
            SigScanner = new SigScanner();
            Memory = new MemoryScanner(MemoryLogger, SigScanner);

            Api.CallHandler = repo.handleCallSync;
            repo.Init(Api);

            monitor.WindowName = "最终幻想XIV";
            monitor.MonitorType = NetworkMonitorType.WinPCap;
            monitor.OodleImplementation = Machina.FFXIV.Oodle.OodleImplementation.LibraryTcp;
            monitor.OodlePath = "oo2net_9_win64.dll";
            monitor.UseDeucalion = false;
            monitor.MessageReceivedEventHandler += onMessageReceived;
            monitor.MessageSentEventHandler += onMessageSent;
            monitor.DecodeFailedEvent += onDecodeFailed;
        }

        private void initOverlayHandler()
        {
            var pluginVer = new PluginVersionInfo();
            var workers = new IWorker[] {
                pluginVer,
                new GameVersion(),
                new FetchWorker(),
                new OpenBrowserWorker(),
                packetWorker,
                new MemoryWorker(SigScanner),
            };

            pluginVer.SetWorkers(workers);

            foreach (var item in workers)
                item.Init(repo);
        }

        bool isUAC()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        bool RestartAsUAC()
        {
            var current = Process.GetCurrentProcess();
            var process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                Verb = "runas",
                FileName = current.MainModule.FileName,
            };
            try
            {
                process.Start();
                return true;
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        public void InitConfig()
        {
            Config.EnsureFolder();
            Config.Load();
        }

        public bool Init()
        {
            if (Config.MemoryScanMode == true)
            {
                if (!isUAC())
                {
                    // 请求UAC重启当前exe
                    if (RestartAsUAC())
                    {
                        return false;
                    }
                }

                // 有可能用户就是不用UAC启动，此时就 fallback 到原有的非内存读取
                if (isUAC())
                    UpdateMemoryScanner();
            }

            initOverlayHandler();
            checkUpdate();

            return true;
        }

        void UpdateMemoryScanner()
        {
            var processes = SigScanner.GetFFXIVProcesses();
            if (processes.Length > 0)
            {
                var process = processes[0];
                SigScanner.Open(process);
            }
            else
            {
                SigScanner.Close();
            }

            Memory.Init();
        }

        MemoryScanner.OodleSizes OodleTcpSize = new MemoryScanner.OodleSizes()
        {
            StateSize = 84104,
            SharedSize = 1048608,
            WindowSize = 0x100000,
        };

        private void onDecodeFailed(FFXIVBundleDecoder decoder, TCPConnection conn, bool tx)
        {
            // Lobby port
            if (conn.RemotePort >= 54992 && conn.RemotePort <= 54994)
                return;

            UpdateMemoryScanner();

            if (Memory == null)
                return;

            if (!Memory.NetworkConnected)
                return;

            // try to figure out which channel...
            bool isChatChannel = false;
            var connections = monitor.Connections;
            foreach (var item in connections) 
            {
                if (item.RemoteIP != conn.RemoteIP || item.RemotePort != conn.RemotePort)
                    continue;

                // chat channel is open AFTER zone channel, so its port may be large
                if (item.LocalPort < conn.LocalPort)
                    isChatChannel = true;
            }
             
            var state = isChatChannel ? Memory.GetChatState(OodleTcpSize) : Memory.GetZoneState(OodleTcpSize);
            if (state == null)
            {
                MemoryLogger.LogWarning($"Failed to get oodle state from memory");
                return;
            }

            monitor.SetOodleState(conn, true, state.Value.Tx.State, state.Value.Tx.Shared, state.Value.Tx.Window);
            monitor.SetOodleState(conn, false, state.Value.Rx.State, state.Value.Rx.Shared, state.Value.Rx.Window);

            var chName = isChatChannel ? "Chat" : "Zone";
            MemoryLogger.LogInformation("Oodle state for {channel}({connection}) is resynced", chName, conn);
        }

        public void Start()
        {
            monitor.Start();
        }

        async void checkUpdate()
        {
            var updater = new Updater("ffxiv-cyou", "ffxiv-fishing-float-app");
            var assembly = Assembly.GetExecutingAssembly().GetName();
            var latest = await updater.CheckUpdate(assembly.Version);
            if (latest != null) 
            {
                // 弹出更新提示
                var vm = new CheckUpdateViewModel(assembly.Version.ToString(), latest.Value);
                var window = new CheckUpdate(vm);
                window.Show();
            }
        }

        private void onMessageReceived(TCPConnection connection, long epoch, byte[] message)
        {
            // TODO: 以后引入 Unscrambler 实现数据包的反混淆。
            //
            // 当前混淆的数据包中，钓鱼需要的数据包只有 StatusEffectList 和 ActorControl 被混淆了。
            // 其中：
            //   - ActorControl: 只有 TargetIcon 类型被混淆，这个没用上;
            //   - StatusEffectList: 混淆了 BuffID，这个在悬浮窗里面做了一些简单的 HACK 处理。
            // 所以暂时不需要引入 Unscrambler. It just works™️
            packetWorker.onNetworkReceived(connection.ToString(), epoch, message);
        }

        private void onMessageSent(TCPConnection connection, long epoch, byte[] message)
        {
            packetWorker.onNetworkSent(connection.ToString(), epoch, message);
        }
    }
}