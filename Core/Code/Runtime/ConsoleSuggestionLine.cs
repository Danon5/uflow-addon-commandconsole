using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UFlow.Addon.CommandConsole.Runtime
{
    public class ConsoleSuggestionLine : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_text;
        [SerializeField] private Image m_image;
        [SerializeField] private Color m_regularColor;
        [SerializeField] private Color m_highlightColor;
        
        public string Text
        {
            get => m_text.text;
            set => m_text.text = value;
        }

        public void SetSelected(bool state)
        {
            if (state)
                m_image.color = m_highlightColor;
            else
                m_image.color = m_regularColor;
        }
    }
}