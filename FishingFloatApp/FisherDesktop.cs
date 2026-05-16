using FishingFloatApp.Overlay;
using FishingFloatApp.pages;
using Machina.FFXIV;
using Machina.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using System.Reflection;

namespace FishingFloatApp
{
    class FisherDesktop
    {
        private readonly ILogger log;

        public OverlayPluginApi Api { get; }

        public Config Config { get; }

        EventRepo repo { get; }

        FFXIVNetworkMonitor monitor { get; } = new FFXIVNetworkMonitor();

        PacketWorker packetWorker { get; } = new PacketWorker();

        public FisherDesktop(ILogger logger)
        {
            this.log = logger;

            repo = new EventRepo(log);
            Api = new OverlayPluginApi(log);
            Config = new Config(log);

            Api.CallHandler = repo.handleCallSync;
            repo.Init(Api);

            monitor.WindowName = "最终幻想XIV";
            monitor.MonitorType = NetworkMonitorType.WinPCap;
            monitor.OodleImplementation = Machina.FFXIV.Oodle.OodleImplementation.LibraryTcp;
            monitor.OodlePath = "oo2net_9_win64.dll";
            monitor.UseDeucalion = false;
            monitor.MessageReceivedEventHandler += onMessageReceived;
            monitor.MessageSentEventHandler += onMessageSent;
        }

        private void initOverlayHandler()
        {
            var workers = new IWorker[] {
                new GameVersion(),
                new PluginVersionInfo(),
                new FetchWorker(),
                new OpenBrowserWorker(),
                packetWorker};

            foreach (var item in workers)
                item.Init(repo);
        }

        public void Init()
        {
            Config.EnsureFolder();
            Config.Load();

            initOverlayHandler();
            checkUpdate();
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