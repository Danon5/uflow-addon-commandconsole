using System;
using JetBrains.Annotations;

namespace UFlow.Addon.CommandConsole.Runtime
{
    [UsedImplicitly]
    public sealed class FloatParser : IParser
    {
        public Type Type => typeof(float);
        
        public object Parse(string str) => float.Parse(str);

        public bool TryParse(string str, out object value)
        {
            bool canParse = float.TryParse(str, out var floatValue);
            value = floatValue;
            return canParse;
        }
    }
}