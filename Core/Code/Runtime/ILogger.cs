namespace UFlow.Addon.CommandConsole.Runtime
{
    public interface ILogger
    {
        public void Log(string message, LogMessageType type = LogMessageType.Message);
    }
}