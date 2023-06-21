using UnityEngine;

namespace UFlow.Addon.CommandConsole.Runtime
{
    public sealed class DebugLogger : ILogger
    {
        public void Log(string message, LogMessageType type)
        {
            Debug.Log(message);
        }
    }
}