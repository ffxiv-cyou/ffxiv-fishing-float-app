using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FishingFloatApp
{
    public class LoggerTraceListener : TraceListener
    {
        private readonly ILogger _logger;

        public LoggerTraceListener(ILogger logger)
        {
            _logger = logger;
        }

        public override void Write(string message)
        {
            _logger.LogDebug(message);
        }

        public override void WriteLine(string message)
        {
            _logger.LogDebug(message);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source,
                                        TraceEventType eventType, int id, string message)
        {
            LogLevel level = LogLevel.Information;
            switch (eventType)
            {
                case TraceEventType.Critical:
                    level = LogLevel.Critical;
                    break;
                case TraceEventType.Error:
                    level = LogLevel.Error;
                    break;
                case TraceEventType.Warning:
                    level = LogLevel.Warning;
                    break;
                case TraceEventType.Information:
                    level = LogLevel.Information;
                    break;
                case TraceEventType.Verbose:
                    level = LogLevel.Debug;
                    break;
                case TraceEventType.Start:
                case TraceEventType.Stop:
                case TraceEventType.Suspend:
                case TraceEventType.Resume:
                case TraceEventType.Transfer:
                default:
                    break;
            }

            _logger.Log(level, id, message, null);
        }
    }
}
