using System.Text.Json;

namespace FishingFloatApp
{
    public interface IEventRepo
    {
        void RegisterHandler(string methodName, EventHandlerDelegate handler);
        void RegisterHandler(IWorker handler);
        void DispatchEvent(JsonElement e);
        void RegisterEventTypes(params string[] types);
    }
}