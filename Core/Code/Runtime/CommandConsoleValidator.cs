using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UFlow.Addon.CommandConsole.Runtime
{
    [CreateAssetMenu(fileName = "CommandConsoleValidator", 
        menuName = "TextMeshPro/Input Validators/CommandConsoleValidator", order = 1)]
    public class CommandConsoleValidator : TMP_InputValidator
    {
        [SerializeField] private List<char> m_prohibitedCharacters = new List<char>();
        public override char Validate(ref string text, ref int pos, char ch)
        {
            Debug.Log($"char: {ch}");
            foreach (char c in m_prohibitedCharacters)
            {
                if (ch == c)
                    return '\0';
            }

            text = text.Insert(pos, ch.ToString());
            pos++;
            return ch;
        }
    }
}