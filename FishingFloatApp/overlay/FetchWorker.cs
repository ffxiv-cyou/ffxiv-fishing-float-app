using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FishingFloatApp.Overlay
{
    public static class JsonHelper
    {
        public static JsonElement Error(string message)
        {
            return JsonSerializer.SerializeToElement(new
            {
                error = message
            });
        }
    }

    public class FetchWorker : IWorker
    {
        public string Name => "otk::fetch";

        public async Task<JsonElement> Fetch(JsonElement req)
        {
            var request = JsonSerializer.Deserialize<FetchRequest>(req);
            if (string.IsNullOrEmpty(request?.resource))
                return JsonHelper.Error("Missing resource field");

            var msg = new HttpRequestMessage();
            msg.RequestUri = new Uri(request.resource);

            if (request.options != null)
                request.options.Value.ApplyToRequest(msg);

            var httpClient = new HttpClient();
            var resp = await httpClient.SendAsync(msg);
            return await FromResponse(resp);
        }

        static async Task<JsonElement> FromResponse(HttpResponseMessage msg)
        {
            var headersObj = new Dictionary<string, string>();
            foreach (var header in msg.Headers)
                headersObj[header.Key] = string.Join(", ", header.Value);

            return JsonSerializer.SerializeToElement(new FetchResponse(msg, await msg.Content.ReadAsStringAsync()));
        }

        public JsonElement? HandleEvent(JsonElement token)
        {
            var req = Fetch(token);
            req.Wait(3000);

            if (req.Exception != null)
                return JsonHelper.Error(req.Exception.Message);

            return req.Result;
        }

        public void Init(IEventRepo es)
        {
            es.RegisterHandler(this);
        }

        class FetchRequest
        {
            public string resource = "";
            public RequestInit? options;
        }

        struct RequestInit
        {
            public string body;

            public Dictionary<string, string> headers;

            public string method;

            public string referrer;

            public void ApplyToRequest(HttpRequestMessage req)
            {
                if (string.IsNullOrEmpty(method))
                    method = "GET";

                req.Method = new HttpMethod(method);

                if (body != null)
                    req.Content = new StringContent(body);
                else if (method != "HEAD" && method != "GET")
                    req.Content = new ByteArrayContent(Array.Empty<byte>());

                if (!string.IsNullOrEmpty(referrer))
                    req.Headers.Referrer = new Uri(referrer);

                if (headers != null)
                {
                    foreach (var item in headers)
                    {
                        if (item.Key == "Content-Type" && req.Content != null)
                        {
                            req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(item.Value);
                            continue;
                        }

                        req.Headers.Add(item.Key, item.Value);
                    }
                }
            }
        }
    
        class FetchResponse
        {
            public Dictionary<string, string> headers { get; set; }
            public bool ok { get; set; }
            public int status { get; set; }
            public string statusText { get; set; }
            public string type { get; set; }
            public string url { get; set; }
            public string body { get; set; }

            public FetchResponse(HttpResponseMessage msg, string body)
            {
                headers = new Dictionary<string, string>();
                foreach (var item in msg.Headers)
                {
                    headers.Add(item.Key, string.Join(", ", item.Value));
                }

                ok = msg.IsSuccessStatusCode;
                status = (int)msg.StatusCode;
                statusText = msg.StatusCode.ToString();
                type = "basic";
                url = msg.RequestMessage?.RequestUri?.ToString() ?? "";
                this.body = body;
            }
        }
    }
}
