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
                version = assembly.Version?.ToString(),
                variant = assembly.Name?.ToString(),
            });
        }
        public void Init(IEventRepo es)
        {
            es.RegisterHandler(this);
        }
    }
}
