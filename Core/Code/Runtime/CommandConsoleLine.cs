using TMPro;
using UnityEngine;

namespace UFlow.Addon.CommandConsole.Runtime
{
    public class CommandConsoleLine : TMP_InputField
    {
        private CommandConsoleLineArrow m_arrow;
        private const float c_text_start_offset_with_arrow = 12;
        protected override void Awake()
        {
            base.Awake();

            m_arrow = GetComponent<CommandConsoleLineArrow>();
        }

        public override float preferredHeight => textComponent.preferredHeight;

        public void Initialize(string text, bool offsetWithArrow)
        {
            this.text = text;
            textViewport.offsetMin = new Vector2(offsetWithArrow ? c_text_start_offset_with_arrow : 0f, 0f);
            m_arrow.arrowText.SetActive(offsetWithArrow);
        }
    }
}