using System;
using JetBrains.Annotations;

namespace UFlow.Addon.CommandConsole.Runtime
{
    [UsedImplicitly]
    public sealed class StringParser : IParser
    {
        public Type Type => typeof(string);
        
        public object Parse(string str) => str;
        
        public bool TryParse(string str, out object value)
        {
            value = str;
            return true;
        }
    }
}