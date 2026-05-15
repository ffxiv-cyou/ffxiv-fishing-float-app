using Newtonsoft.Json.Linq;
using System.Reflection;

namespace FishingFloatApp.Overlay
{
    public class PluginVersionInfo : IWorker
    {
        public PluginVersionInfo()
        {
        }

        public string Name => "otk::plugin_ver";
        public JToken? HandleEvent(JObject e)
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();
            return JObject.FromObject(new
            {
                version = "1.1.1.0", // Compatible with OverlayToolkit v1.1.1
                app_name = assembly.Name?.ToString(),
                app_version = assembly.Version?.ToString(),
            });
        }
        public void Init(IEventRepo es)
        {
            es.RegisterHandler(this);
        }
    }
}
