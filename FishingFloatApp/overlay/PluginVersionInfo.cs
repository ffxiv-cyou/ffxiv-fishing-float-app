using Lotlab.PluginCommon.FFXIV;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Text.Json;
using System.Linq;

namespace FishingFloatApp.Overlay
{
    public class PluginVersionInfo : IWorker
    {
        public PluginVersionInfo()
        {
        }
        string[] workers { get; set; }

        public string Name => "otk::plugin_ver";
        public JsonElement? HandleEvent(JsonElement e)
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();
            return JsonSerializer.SerializeToElement(new
            {
                version = "1.1.1.0", // Compatible with OverlayToolkit v1.1.1
                app_name = assembly.Name?.ToString(),
                app_version = assembly.Version?.ToString(),
                tools = workers
            });
        }
        public void Init(IEventRepo es)
        {
            es.RegisterHandler(this);
        }
        public void SetWorkers(IList<IWorker> workers)
        {
            this.workers = workers.Select(x => x.Name).ToArray();
        }
    }
}
