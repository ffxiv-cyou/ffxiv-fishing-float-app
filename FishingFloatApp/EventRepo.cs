using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text.Json;

namespace FishingFloatApp
{
    public interface IEventReceiver
    {
        void HandleEvent(JsonElement e);
    }

    public interface IWorker
    {
        string Name { get; }
        JsonElement? HandleEvent(JsonElement e);
        void Init(IEventRepo repo);
    }

    /// <summary>
    /// 事件仓库，负责管理事件订阅和分发
    /// </summary>
    class EventRepo : IEventRepo
    {
        Dictionary<string, EventHandlerDelegate> handlers { get; } = new Dictionary<string, EventHandlerDelegate>();

        Dictionary<string, List<IEventReceiver>> subscribers { get; } = new Dictionary<string, List<IEventReceiver>>();

        ILogger log { get; }

        public EventRepo(ILogger logger)
        {
            log = logger;
        }

        public void RegisterEventTypes(params string[] types)
        {
            foreach (var item in types)
            {
                if (!subscribers.ContainsKey(item))
                    subscribers[item] = new List<IEventReceiver>();
            }
        }

        public JsonElement? Subscribe(IEventReceiver receiver, JsonElement e)
        {
            if (!e.TryGetProperty("events", out var events))
            {
                log.LogError("events field is not found");
                return null;
            }

            log.LogInformation("subscribing to events: {events}", events);

            foreach (var item in events.EnumerateArray())
            {
                Subscribe(item.ToString(), receiver);
            }

            return new JsonElement();
        }

        public JsonElement? Unsubscribe(IEventReceiver receiver, JsonElement e)
        {
            if (!e.TryGetProperty("events", out var events))
            {
                log.LogError("events field is not found");
                return null;
            }
            foreach (var item in events.EnumerateArray())
            {
                Unsubscribe(item.ToString(), receiver);
            }
            return new JsonElement();
        }

        public void Subscribe(string eventName, IEventReceiver receiver)
        {
            if (!subscribers.ContainsKey(eventName))
            {
                log.LogError("Attempted to subscribe to unregistered event type: {0}", eventName);
                return;
            }

            if (subscribers[eventName].Contains(receiver))
            {
                log.LogWarning("Receiver is already subscribed to event type: {0}", eventName);
                return;
            }
            subscribers[eventName].Add(receiver);
        }

        public void Unsubscribe(string eventName, IEventReceiver receiver)
        {
            if (!subscribers.ContainsKey(eventName))
            {
                log.LogError("Attempted to unsubscribe from unregistered event type: {0}", eventName);
                return;
            }
            subscribers[eventName].Remove(receiver);
        }

        /// <summary>
        /// 初始化插件
        /// </summary>
        /// <param name="plugin"></param>
        public void Init(IEventReceiver plugin)
        {
            RegisterHandler("subscribe", (e) => Subscribe(plugin, e));
            RegisterHandler("unsubscribe", (e) => Unsubscribe(plugin, e));
        }

        /// <summary>
        /// 分发事件
        /// </summary>
        /// <param name="e"></param>
        public void DispatchEvent(JsonElement e)
        {
            if (!e.TryGetProperty("type", out var typeProperty))
            {
                log.LogError("missing type field");
                return;
            }
            var type = typeProperty.GetString();
            if (type == null)
            {
                log.LogError("missing type field");
                return;
            }
            if (!subscribers.ContainsKey(type))
            {
                log.LogError("Received event with unregistered type: {0}", type);
                return;
            }
            foreach (var receiver in subscribers[type])
            {
                receiver.HandleEvent(e);
            }
        }

        /// <summary>
        /// 注册事件处理器
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="handler"></param>
        public void RegisterHandler(string methodName, EventHandlerDelegate handler)
        {
            if (handlers.ContainsKey(methodName))
                log.LogWarning("Overwriting existing handler for method: " + methodName);

            handlers[methodName] = handler;
        }

        /// <summary>
        /// 注册事件处理器
        /// </summary>
        /// <param name="handler"></param>
        public void RegisterHandler(IWorker handler)
        {
            RegisterHandler(handler.Name, handler.HandleEvent);
        }

        public JsonElement? handleCallSync(string data)
        {
            var obj = JsonElement.Parse(data);
            if (!obj.TryGetProperty("call", out var callProperty))
            {
                log.LogError("No call method specified in data: " + data);
                return null;
            }
            var callMethod = callProperty.GetString();
            if (callMethod == null)
            {
                log.LogError("No call method specified in data: " + data);
                return null;
            }
            if (!handlers.ContainsKey(callMethod))
            {
                log.LogError("No handler registered for call method: " + callMethod);
                return null;
            }
            var result = handlers[callMethod](obj);
            return result;
        }
    }
}
