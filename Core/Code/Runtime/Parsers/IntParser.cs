using System;
using JetBrains.Annotations;

namespace UFlow.Addon.CommandConsole.Runtime
{
    [UsedImplicitly]
    public sealed class IntParser : IParser
    {
        public Type Type => typeof(int);

        public object Parse(string targetString) => int.Parse(targetString);

        public bool TryParse(string str, out object value)
        {
            bool canParse = int.TryParse(str, out int intValue);
            value = intValue;
            return canParse;
        }
    }
}