using System;

namespace UFlow.Addon.CommandConsole.Runtime
{
    public interface IParser
    {
        Type Type { get; }
        
        object Parse(string str);
        bool TryParse(string str, out object value);
    }
}