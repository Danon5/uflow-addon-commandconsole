using System;
using JetBrains.Annotations;
using UnityEngine;

namespace UFlow.Addon.CommandConsole.Runtime
{
    [UsedImplicitly]
    public sealed class Vector2Parser : IParser
    {
        public Type Type => typeof(Vector2);
        
        public object Parse(string str)
        {
            string[] nums = str.Split(new char[] {',', '(', ')'}, StringSplitOptions.RemoveEmptyEntries);
            return new Vector2(float.Parse(nums[0]), float.Parse(nums[1]));
        }

        public bool TryParse(string str, out object value)
        {
            value = default;
            string[] nums = str.Split(new char[] {',', '(', ')'}, StringSplitOptions.RemoveEmptyEntries);
            
            if (nums.Length != 2)
                return false;
            if (!float.TryParse(nums[0], out float x) || !float.TryParse(nums[0], out float y))
                return false;
            
            value = new Vector2(x, y);
            return true;
        }
    }
}