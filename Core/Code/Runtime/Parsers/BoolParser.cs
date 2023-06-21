using System;
using JetBrains.Annotations;

namespace UFlow.Addon.CommandConsole.Runtime
{
    [UsedImplicitly]
    public sealed class BoolParser : IParser
    {
        public Type Type => typeof(bool);
        
        public object Parse(string str) => bool.Parse(str);

        public bool TryParse(string str, out object value)
        {
            bool canParse = bool.TryParse(str, out var boolValue);
            value = boolValue;
            return canParse;
        }
    }
}