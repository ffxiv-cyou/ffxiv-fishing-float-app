using Lotlab.PluginCommon.FFXIV.Parser;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FishingFloatApp.Overlay
{
    public class PacketWorker : IWorker
    {
        public string Name => "otk::packet";

        NetworkParser Parser { get; } = new NetworkParser();

        public PacketWorker()
        {
        }

        IEventRepo? EventSource { get; set; }

        Dictionary<string, EventListener> Listeners { get; } = new Dictionary<string, EventListener>();

        public void onNetworkSent(string connection, long epoch, byte[] message)
        {
            handlePacket(true, connection, epoch, message);
        }

        public void onNetworkReceived(string connection, long epoch, byte[] message)
        {
            handlePacket(false, connection, epoch, message);
        }

        void handlePacket(bool isSent, string connection, long epoch, byte[] message)
        {
            if (message.Length < 32)
                return;

            var header = Parser.ParseIPCHeader(message);
            if (header == null)
                return;

            var segment = header.Value.segmentHeader;
            bool isSourceEqualTarget = segment.source_actor == segment.target_actor;

            foreach (var item in Listeners)
            {
                bool matched = item.Value.Filters.Length == 0;

                foreach (var filter in item.Value.Filters)
                {
                    if (filter.Match(isSent, message.Length, header.Value.type, isSourceEqualTarget))
                    {
                        matched = true;
                        break;
                    }
                }

                if (matched)
                    dispatchPacket(item.Value, isSent, connection, epoch, message, header.Value);
            }
        }

        void dispatchPacket(EventListener listener, bool isSent, string connection, long epoch, byte[] message, IPCHeader header)
        {
            if (EventSource == null)
                return;

            // Old Behavior: using global type
            var eventType = "otk::packet";

            // New Behavior: using custom event name if provided
            if (!string.IsNullOrEmpty(listener.EventName))
            {
                eventType = listener.EventName;
            }

            EventSource.DispatchEvent(JsonSerializer.SerializeToElement(new
            {
                type = eventType,
                name = listener.Name,
                dir = isSent,
                conn = connection,
                epoch = epoch,
                length = message.Length,
                opcode = header.type,
                msg = Convert.ToBase64String(message)
            }));
        }

        public JsonElement Subscribe(JsonElement req)
        {
            var listener = req.Deserialize<EventListener>();
            if (string.IsNullOrEmpty(listener.Name))
                return JsonHelper.Error("missing 'name' field");

            bool hasListeners = Listeners.ContainsKey(listener.Name);
            Listeners[listener.Name] = listener;

            // Register event type if provided.
            // Only register if this is the first time subscribing with this name, or the old event handler would be overwritten.
            if (!string.IsNullOrEmpty(listener.EventName) && !hasListeners)
            {
                EventSource?.RegisterEventTypes(listener.EventName);
            }

            return JsonSerializer.SerializeToElement(new
            {
                name = listener.Name,
                evt_name = listener.EventName
            });
        }

        public JsonElement Unsubscribe(JsonElement req)
        {
            if (!req.TryGetProperty("name", out var nameProp) || nameProp.ValueKind != JsonValueKind.String)
                return JsonHelper.Error("missing 'name' field");

            var name = nameProp.GetString()!;
            if (!Listeners.ContainsKey(name))
                return JsonHelper.Error($"event {name} is not listen yet");

            Listeners.Remove(name);
            return JsonSerializer.SerializeToElement(new { });
        }

        public JsonElement? HandleEvent(JsonElement req)
        {
            if (!req.TryGetProperty("action", out var actionProp) || actionProp.ValueKind != JsonValueKind.String)
                return JsonHelper.Error("missing 'action' field");

            var action = actionProp.GetString()!;
            switch (action.ToLower())
            {
                case "subscribe":
                    return Subscribe(req);
                case "unsubscribe":
                    return Unsubscribe(req);
                default:
                    return JsonHelper.Error($"unknown action {action}");
            }
        }

        public void Init(IEventRepo es)
        {
            EventSource = es;
            es.RegisterHandler(this);
            es.RegisterEventTypes("otk::packet");
        }

        struct EventListener
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("evt_name")]
            public string EventName { get; set; }

            [JsonPropertyName("filters")]
            public Filter[] Filters { get; set; }
        }

        struct Filter
        {
            [JsonPropertyName("direction")]
            public bool? IsSent { get; set; }
            [JsonPropertyName("length")]
            public int? Length { get; set; }
            [JsonPropertyName("opcode")]
            public int? Opcode { get; set; }
            [JsonPropertyName("self_actor")]
            public bool? SelfActor { get; set; }

            public bool Match(bool isSent, int length, int opcode, bool actorSelf)
            {
                if (IsSent.HasValue && IsSent.Value != isSent)
                    return false;
                if (Length.HasValue && Length.Value != length)
                    return false;
                if (Opcode.HasValue && Opcode.Value != opcode)
                    return false;
                if (SelfActor.HasValue && SelfActor.Value != actorSelf)
                    return false;

                return true;
            }

            public static Filter FromJson(JsonElement token)
            {
                var f = new Filter();
                if (token.TryGetProperty("direction", out var direction))
                {
                    if (direction.ValueKind == JsonValueKind.String)
                    {
                        var dirStr = direction.GetString()!.ToLower();
                        if (dirStr == "send" || dirStr == "sent")
                            f.IsSent = true;
                        else if (dirStr == "recv" || dirStr == "received")
                            f.IsSent = false;
                    }
                    else if (direction.ValueKind == JsonValueKind.True || direction.ValueKind == JsonValueKind.False)
                    {
                        f.IsSent = direction.GetBoolean();
                    }
                }
                if (token.TryGetProperty("length", out var length) && length.ValueKind == JsonValueKind.Number)
                {
                    f.Length = length.GetInt32();
                }
                if (token.TryGetProperty("opcode", out var opcode) && opcode.ValueKind == JsonValueKind.Number)
                {
                    f.Opcode = opcode.GetInt32();
                }
                return f;
            }
        }
    }
}
