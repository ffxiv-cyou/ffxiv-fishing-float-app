using System.Reflection;
using System.Text.Json;

namespace FishingFloatApp.Overlay
{
    public class PluginVersionInfo : IWorker
    {
        public PluginVersionInfo()
        {
        }

        public string Name => "otk::plugin_ver";
        public JsonElement? HandleEvent(JsonElement e)
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();
            return JsonSerializer.SerializeToElement(new
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
