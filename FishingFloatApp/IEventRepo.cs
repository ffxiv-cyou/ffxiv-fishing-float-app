using Newtonsoft.Json.Linq;

namespace FishingFloatApp
{
    public interface IEventRepo
    {
        void RegisterHandler(string methodName, EventHandlerDelegate handler);
        void RegisterHandler(IWorker handler);
        void DispatchEvent(JObject e);
        void RegisterEventTypes(params string[] types);
    }
}