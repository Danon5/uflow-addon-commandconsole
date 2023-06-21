using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UFlow.Addon.CommandConsole.Runtime
{
    internal static class Utilities
    {
        public static string SubstringRange(this string text, int startInclusive, int endInclusive) =>
            text.Substring(startInclusive, Mathf.Abs(endInclusive - startInclusive) + 1);
        
        public static string SetAlphaRichText(this string text, string hexAlpha) => $"<alpha=#{hexAlpha}>{text}<alpha=#FF>";
        
        public static string RemoveRichTextFormatting(this string text) => 
            Regex.Replace(text, "<.*?>", string.Empty);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            (list[i], list[j]) = (list[j], list[i]);
        }
        
        public static void SortByLength(this string[] array, int size = -1)
        {
            if (size == -1) size = array.Length;
            if (size > array.Length || array.Length < 2) return;
            for (int i = 0; i < size; i++)
            {
                int ilength = array[i].Length;
                int shortestLength = int.MaxValue, shortestIndex = i;
                for (int j = shortestIndex + 1; j < size; j++)
                {
                    int jLength = array[j].Length;
                    if (jLength < shortestLength && jLength < ilength)
                    {
                        shortestLength = jLength;
                        shortestIndex = j;
                    }
                }
                if (shortestIndex != i)
                    Swap(array, i, shortestIndex);
            }
        }
        
        public static string GetColoredRichText(this string text, Color color)
        {
            return "<color=" + "#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + text + "</color>";
        }
    }
}