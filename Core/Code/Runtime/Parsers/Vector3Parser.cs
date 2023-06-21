using System;
using JetBrains.Annotations;
using UnityEngine;

namespace UFlow.Addon.CommandConsole.Runtime
{
    [UsedImplicitly]
    public sealed class Vector3Parser : IParser
    {
        public Type Type => typeof(Vector3);
        
        public object Parse(string str)
        {
            string[] nums = str.Split(new char[] {',', '(', ')'}, StringSplitOptions.RemoveEmptyEntries);
            return new Vector3(float.Parse(nums[0]), float.Parse(nums[1]), float.Parse(nums[2]));
        }

        public bool TryParse(string str, out object value)
        {
            value = default;
            string[] nums = str.Split(new char[] {',', '(', ')'}, StringSplitOptions.RemoveEmptyEntries);
            
            if (nums.Length != 3)
                return false;
            if (!float.TryParse(nums[0], out float x) || !float.TryParse(nums[0], out float y) || !float.TryParse(nums[0], out float z))
                return false;
            
            value = new Vector3(x, y, z);
            return true;
        }
    }
}