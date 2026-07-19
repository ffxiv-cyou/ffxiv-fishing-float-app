using FishingFloatApp.memory;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FishingFloatApp.Overlay
{
    internal class MemoryWorker : IWorker
    {
        SigScanner Scanner { get; set; } = null;

        public MemoryWorker(SigScanner scanner)
        {
            Scanner = scanner;
        }

        public string Name => "otk::memory";

        public JsonElement? HandleEvent(JsonElement req)
        {
            if (!req.TryGetProperty("action", out var actionProp) || actionProp.ValueKind != JsonValueKind.String)
                return JsonHelper.Error("missing 'action' field");

            var action = actionProp.GetString();

            try
            {
                switch (action.ToLower())
                {
                    case "sig_scanner":
                        return SigScan(req);
                    case "read":
                        return Read(req);
                    default:
                        return JsonHelper.Error($"action '{action}' is undefined");
                }
            }
            catch (System.Exception e)
            {
                return JsonHelper.Error(e.Message);
            }
        }

        bool EnsureProcessInited()
        {
            if (Scanner == null)
                return false;

            return true;
        }

        JsonElement? SigScan(JsonElement req)
        {
            if (!req.TryGetProperty("sig", out var sigProp) || sigProp.ValueKind != JsonValueKind.String)
                return JsonHelper.Error("missing 'sig' field");

            var sig = sigProp.GetString();
            if (string.IsNullOrEmpty(sig))
                return JsonHelper.Error("missing 'sig' field");

            Int64 start = 0;
            int offset = 0;
            if (req.TryGetProperty("started_at", out var startProp) && startProp.ValueKind == JsonValueKind.Number)
                start = startProp.GetInt64();

            if (req.TryGetProperty("offset", out var offsetProp) && offsetProp.ValueKind == JsonValueKind.Number)
                offset = offsetProp.GetInt32();

            bool inited = EnsureProcessInited();
            if (!inited)
                return JsonSerializer.SerializeToElement(new SigScanResult());

            SigScanner.SigResult result;
            if (start != 0)
                result = Scanner.FindSignature(sig, new IntPtr(start));
            else
                result = Scanner.FindSignature(sig, offset);

            return JsonSerializer.SerializeToElement(new SigScanResult(result, Scanner.TextBase));
        }

        JsonElement? Read(JsonElement req)
        {
            Int64 address = 0;
            int length = 0;
            if (req.TryGetProperty("addr", out var addrProp) && addrProp.ValueKind == JsonValueKind.Number)
                address = addrProp.GetInt64();
            if (req.TryGetProperty("length", out var lengthProp) && lengthProp.ValueKind == JsonValueKind.Number)
                length = lengthProp.GetInt32();

            if (address == 0)
                return JsonHelper.Error("missing 'addr' field");

            if (length == 0)
                return JsonHelper.Error("missing 'len' field");

            if (length > 0x10000)
                return JsonHelper.Error("data too large!");

            bool inited = EnsureProcessInited();
            if (!inited)
                return JsonSerializer.SerializeToElement(new ReadResult() { addr = address, length = length });

            byte[] buffer = new byte[length];
            length = Scanner.GetBytes(new IntPtr(address), buffer);

            return JsonSerializer.SerializeToElement(new ReadResult(address, length, buffer));
        }

        struct SigScanResult
        {
            [JsonPropertyName("addr")]
            public Int64 addr { get; set; }

            [JsonPropertyName("base")]
            public Int64 textBase { get; set; }

            [JsonPropertyName("bytes")]
            public string bytes { get; set; }
            [JsonPropertyName("mask")]
            public bool[] mask { get; set; }

            public SigScanResult(SigScanner.SigResult result, IntPtr basePtr)
            {
                addr = result.Address.ToInt64();
                bytes = result.Bytes?.ToHexString() ?? "";
                mask = result.Sig.Mask;
                textBase = basePtr.ToInt64();
            }
        }

        struct ReadResult
        {
            [JsonPropertyName("bytes")]
            public string bytes { get; set; }

            [JsonPropertyName("addr")]
            public Int64 addr { get; set; }

            [JsonPropertyName("len")]
            public Int64 length { get; set; }

            [JsonPropertyName("value")]
            public Int64? value { get; set; }

            public ReadResult(Int64 addr, Int64 length, byte[] data)
            {
                this.addr = addr;
                this.length = length;
                bytes = data?.ToHexString() ?? "";
                value = null;

                if (data?.Length == length)
                {
                    if (length == 8)
                        value = BitConverter.ToInt64(data, 0);
                    else if (length == 4)
                        value = BitConverter.ToInt32(data, 0);
                    else if (length == 2)
                        value = BitConverter.ToInt16(data, 0);
                    else if (length == 1)
                        value = data[0];
                }
            }
        }

        public void Init(IEventRepo es)
        {
            es.RegisterHandler(this);
        }
    }

    static class HexExtension
    {
        public static string ToHexString(this byte[] barray)
        {
            char[] c = new char[barray.Length * 2];
            byte b;
            for (int i = 0; i < barray.Length; ++i)
            {
                b = ((byte)(barray[i] >> 4));
                c[i * 2] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                b = ((byte)(barray[i] & 0xF));
                c[i * 2 + 1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }

            return new string(c);
        }
    }
}
