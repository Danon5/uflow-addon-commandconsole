using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace UFlow.Addon.CommandConsole.Runtime
{
    public class CommandConsole : MonoBehaviour, ILogger
    {
        [SerializeField] private RectTransform m_graphics;
        [SerializeField] private GameObject m_linePrefab;
        [SerializeField] private GameObject m_suggestionLinePrefab;
        [SerializeField] private RectTransform m_suggestionsParent;
        [SerializeField] private RectTransform m_contentTransform;
        [SerializeField] private TMP_InputField m_inputField;
        [SerializeField] private TMP_Text m_suggestionFillText;
        [SerializeField] private float m_windowHeight;

        [SerializeField] private Color m_messageLogColor;
        [SerializeField] private Color m_successLogColor;
        [SerializeField] private Color m_warningLogColor;
        [SerializeField] private Color m_errorLogColor;
        
        private readonly List<CommandConsoleLine> m_lines = new();
        private readonly List<string> m_commandHistory = new();
        private readonly string[] m_suggestions = new string[c_max_suggestions];
        private readonly ConsoleSuggestionLine[] m_suggestionLines = new ConsoleSuggestionLine[c_max_suggestions];
        private int m_suggestionCount;
        private int m_historyIndex = -1;
        private int m_suggestionIndex = -1;
        private CommandManager m_manager;
        private bool m_changingOpenState;
        private float m_keyPressStartTime;
        private float m_lastCycleTime;
        private bool m_quickCycling;
        private string m_lastTypedMessage;
        private int m_typedPrefixStart;
        private int m_typedPrefixLength;
        private bool m_atPrefixEnd;
        private int m_oldCaretPos;
        private bool m_menuOpen;
        private bool m_queueFlipOpenState;

        private static CommandConsole s_instance;
        private const int c_command_history_size = 75;
        private const int c_max_suggestions = 5;
        private const float c_quick_cycle_hold_time = .5f;
        private const float c_quick_cycle_delay = .0625f;
        private const int c_key_override_priority = 99;
        private const string c_log_colors_group = "Log Colors";

        protected virtual void Awake()
        {
            m_manager = new CommandManager(this, new List<string>{ typeof(CommandConsole).Assembly.FullName });
            m_inputField.onSubmit.AddListener(OnInputFieldSubmit);
            m_inputField.onValueChanged.AddListener(OnInputFieldChange);
            m_inputField.onValidateInput += OnValidateInput;

            for (int i = 0; i < c_max_suggestions; i++)
                m_suggestionLines[i] = Instantiate(m_suggestionLinePrefab, m_suggestionsParent).GetComponent<ConsoleSuggestionLine>();

            s_instance = this;

            for (int i = 0; i < m_suggestions.Length; i++)
                m_suggestions[i] = "";
        }

        private async UniTask AsyncOpen(bool state)
        {
            if (m_changingOpenState)
            {
                m_queueFlipOpenState = true;
                return;
            }
            m_changingOpenState = true;
            m_menuOpen = state;
            
            m_historyIndex = -1;
            m_suggestionIndex = -1;
            
            /*
            m_cursorUnlocker.SetState(state);
            m_controlBlocker.SetState(state);
            if (state)
                AddGlobalKeyCodeOverride(KeyCode.Tab, c_key_override_priority);
            else
                RemoveGlobalKeyCodeOverride(KeyCode.Tab, c_key_override_priority);
            */
            
            float speed = 2000f;
            if (state)
            {
                m_graphics.gameObject.SetActive(true);
                m_graphics.sizeDelta = new Vector2(m_graphics.sizeDelta.x, 0f);
                while (m_graphics != null && m_graphics.sizeDelta.y < m_windowHeight)
                {
                    m_graphics.sizeDelta = new Vector2(m_graphics.sizeDelta.x,
                                                       Mathf.Min(m_graphics.sizeDelta.y + speed * Time.deltaTime, m_windowHeight));

                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
            }
            else
            {
                while (m_graphics != null && m_graphics.sizeDelta.y > 0f)
                {
                    m_graphics.sizeDelta = new Vector2(m_graphics.sizeDelta.x,
                                                       Mathf.Max(m_graphics.sizeDelta.y - speed * Time.deltaTime, 0f));

                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
                if (m_graphics != null)
                    m_graphics.gameObject.SetActive(false);
            }
            
            m_changingOpenState = false;
            
            if (m_queueFlipOpenState)
            {
                m_queueFlipOpenState = false;
                AsyncOpen(!m_menuOpen).Forget();
            }
        }

        public virtual void SetOpenState(bool state) => AsyncOpen(state).Forget();

        private void UpdateQuickCycleVariables(bool buttonDown)
        {
            if (buttonDown)
                m_keyPressStartTime = Time.time;
            else
            {
                m_lastCycleTime = Time.time;
                m_quickCycling = true;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                SetOpenState(!m_menuOpen);
                if (m_menuOpen)
                    m_inputField.ActivateInputField();
                else
                    m_inputField.text = "";
            }

            if (!m_menuOpen) return;

            if (m_oldCaretPos != m_inputField.caretPosition)
                OnCaretPosChange(m_oldCaretPos, m_inputField.caretPosition);
            m_oldCaretPos = m_inputField.caretPosition;
            
            if (m_suggestionCount > 0 && Input.GetKeyDown(KeyCode.Tab))
            {
                var suggestion = m_suggestionIndex == -1 ? m_suggestions[0] : m_suggestions[m_suggestionIndex];
                CompleteSuggestion(suggestion);
                m_suggestionIndex = -1;
            }
            
            bool upArrowKeyDown = Input.GetKeyDown(KeyCode.UpArrow);
            bool upArrow = Input.GetKey(KeyCode.UpArrow);
            
            bool downArrowKeyDown = Input.GetKeyDown(KeyCode.DownArrow);
            bool downArrow = Input.GetKey(KeyCode.DownArrow);

            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl);

            if (Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow))
            {
                m_quickCycling = false;
            }

            if (upArrowKeyDown || upArrow && 
                (m_quickCycling ||  Time.time > m_keyPressStartTime + c_quick_cycle_hold_time) &&
                Time.time > m_lastCycleTime + c_quick_cycle_delay)
            {
                UpdateQuickCycleVariables(upArrowKeyDown);

                if (m_suggestionCount > 0 && m_suggestionIndex >= 0)
                {
                    if (ctrlPressed)
                    {
                        SetSelectedSuggestion(m_suggestionLines[m_suggestionIndex],
                            null);
                        m_suggestionIndex = -1;
                    }
                    else
                        SetSelectedSuggestion(m_suggestionLines[m_suggestionIndex--], 
                        m_suggestionIndex == -1 ? null : m_suggestionLines[m_suggestionIndex]);
                }
                else if (m_historyIndex < m_commandHistory.Count - 1)
                {
                    m_historyIndex++;

                    if (m_commandHistory.Count > 0)
                        SetTextAndMoveCaret(m_commandHistory[m_historyIndex]);
                }
            }
            else if (downArrowKeyDown || downArrow &&
                     (m_quickCycling || Time.time > m_keyPressStartTime + c_quick_cycle_hold_time) &&
                     Time.time > m_lastCycleTime + c_quick_cycle_delay)
            {
                UpdateQuickCycleVariables(downArrowKeyDown);

                if (m_historyIndex >= 0)
                {
                    m_historyIndex--;
                    if (m_historyIndex != -1 && m_commandHistory.Count > 0)
                        SetTextAndMoveCaret(m_commandHistory[m_historyIndex]);
                    else
                        SetTextAndMoveCaret(m_lastTypedMessage);
                }
                else if (m_suggestionCount > 0 && m_suggestionIndex < m_suggestionCount - 1)
                {
                    if (ctrlPressed)
                    {
                        int newIndex = m_suggestionCount - 1;
                        SetSelectedSuggestion(m_suggestionIndex != -1 ? m_suggestionLines[m_suggestionIndex] : null, 
                            m_suggestionLines[newIndex]);
                        m_suggestionIndex = newIndex;
                    }
                    else
                        SetSelectedSuggestion(m_suggestionIndex != -1 ? m_suggestionLines[m_suggestionIndex] : null, 
                        m_suggestionLines[++m_suggestionIndex]);
                }
            }
            
            if (upArrow || downArrow)
                m_inputField.MoveTextEnd(false);
        }

        private void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.C) && Input.GetKey(KeyCode.LeftControl))
                GUIUtility.systemCopyBuffer = GUIUtility.systemCopyBuffer.RemoveRichTextFormatting();
        }

        private void OnCaretPosChange(int oldPos, int newPos)
        {
            //Debug.Log($"new caret: {oldPos} -> {newPos}");
            UpdatePrefixStart(newPos, m_inputField.text);
        }
        
        private void CompleteSuggestion(string suggestion)
        {
            string completion = suggestion.Substring(m_inputField.caretPosition - m_typedPrefixStart);
            InsertTextAndMoveCaret(completion, m_inputField.caretPosition, true);
        }

        private void SetSelectedSuggestion(ConsoleSuggestionLine oldLine, ConsoleSuggestionLine newLine)
        {
            if (oldLine != null)
                oldLine.SetSelected(false);
            if (newLine != null)
            {
                newLine.SetSelected(true);
                SetDisplayedSuggestionText(newLine.Text);
            }
            else if (m_suggestionCount > 0)
                SetDisplayedSuggestionText(m_suggestions[0]);
        }

        private void SetDisplayedSuggestionText(string suggestion)
        {
            //Debug.Log($"setting suggestion: {suggestion}, at index: {m_typedPrefixStart}");
            
            m_suggestionFillText.text = m_inputField.text.Substring(0, m_typedPrefixStart) + 
                suggestion.SetAlphaRichText("44");// + m_inputField.text.Substring();  
        }

        private void ClearDisplayedSuggestionText() => m_suggestionFillText.text = "";

        private void InternalSetText(string text, bool notify)
        {
            if (notify)
                m_inputField.text = text;
            else
            {
                m_inputField.SetTextWithoutNotify(text);
                UpdateSuggestions();
            }
        }
        
        private void SetTextAndMoveCaret(string text, bool notify = false)
        {
            InternalSetText(text, notify);

            m_inputField.MoveTextEnd(false);
        }

        private void InsertTextAndMoveCaret(string text, int startPos, bool notify = false)
        {
            string newText = m_inputField.text.Insert(startPos, text);
            
            InternalSetText(newText, notify);

            m_inputField.stringPosition = startPos + text.Length;
        }
        
        private char OnValidateInput(string text, int charIndex, char addedChar)
        {
            UpdateCurrentPrefix(text, charIndex, addedChar);
            
            return addedChar;
        }

        private void UpdateCurrentPrefix(string text, int charIndex, char addedChar)
        {
            m_atPrefixEnd = !char.IsWhiteSpace(addedChar) && charIndex == text.Length
                            || (charIndex < text.Length - 1 && char.IsWhiteSpace(text[charIndex]));

            UpdatePrefixStart(charIndex, text);
            
            m_typedPrefixLength = 1 + charIndex - m_typedPrefixStart;
        }

        private void UpdatePrefixStart(int cursorIndex, string text)
        {
            for (int checkIndex = cursorIndex; checkIndex >= 0; checkIndex--)
            {
                if (checkIndex == 0 || char.IsWhiteSpace(text[checkIndex - 1]))
                {
                    m_typedPrefixStart = checkIndex;
                    break;
                }
            }
        }

        private void OnInputFieldChange(string message)
        {
            m_lastTypedMessage = message;
            m_historyIndex = -1;
            
            if (m_atPrefixEnd)
                m_atPrefixEnd = m_inputField.caretPosition == m_typedPrefixStart + m_typedPrefixLength;

            //Debug.Log($"prefix start: {m_typedPrefixStart}, length: {m_typedPrefixLength}, at end: {m_atPrefixEnd}");
            
            UpdateSuggestions();
        }

        private void UpdateSuggestions()
        {
            m_suggestionIndex = -1;

            try
            {
                string prefix;
                if (m_atPrefixEnd)
                    prefix = m_inputField.text.Substring(m_typedPrefixStart, m_typedPrefixLength); //;
                else
                    prefix = "";
                //Debug.Log($"prefix: {prefix}");
                m_suggestionCount = m_manager.SuggestCommands(prefix, m_suggestions);
            }
            catch
            {
               // Debug.Log($"IDIOT: {m_typedPrefixStart}, {m_typedPrefixLength}");
            }

            m_suggestions.SortByLength(m_suggestionCount);
            
            for (int i = 0; i < c_max_suggestions; i++)
            {
                ConsoleSuggestionLine line = m_suggestionLines[i];
                line.Text = i < m_suggestionCount ? m_suggestions[i] : "";
                line.SetSelected(false);
            }

            if (m_suggestionCount > 0)
                SetDisplayedSuggestionText(m_suggestions[0]);
            else
                ClearDisplayedSuggestionText();
        }

        private void OnInputFieldSubmit(string message)
        {
            if (message.Length == 0) return;

            if (m_suggestionIndex != -1)
            {
                CompleteSuggestion(m_suggestions[m_suggestionIndex]);
                m_suggestionIndex = -1;
                m_inputField.ActivateInputField();
                //m_inputField.MoveTextEnd(false);
                return;
            }

            for (int i = m_suggestionCount - 1; i >= 0; i--)
            {
                m_suggestionLines[i].SetSelected(false);
                m_suggestionLines[i].Text = "";
            }
            
            m_historyIndex = -1;

            message = message.Trim();
            AddNewLine(message, true);

            if (m_commandHistory.Count == 0 || m_commandHistory.Count > 0 && m_commandHistory[0] != message)
            {
                m_commandHistory.Insert(0, message);
                if (m_commandHistory.Count > c_command_history_size)
                    m_commandHistory.RemoveAt(m_commandHistory.Count - 1);
            }

            m_manager.SendCommandLine(message);
            m_inputField.text = "";
            m_inputField.ActivateInputField();
        }

        public void Log(string message, LogMessageType type = LogMessageType.Message)
        {
            Color color = type switch
            {
                LogMessageType.Success => m_successLogColor,
                LogMessageType.Warning => m_warningLogColor,
                LogMessageType.Error => m_errorLogColor,
                _ => m_messageLogColor
            };
            AddNewLine(message.GetColoredRichText(color));
        }

        private void AddNewLine(string text, bool addArrow = false)
        {
            CommandConsoleLine newLine = Instantiate(m_linePrefab, m_contentTransform).GetComponent<CommandConsoleLine>();
            newLine.Initialize(text, addArrow);
            m_lines.Add(newLine);
        }

        [Command("clear", "Clears the contents of the command console.", ""), UsedImplicitly]
        public static void Clear()
        {
            for (var i = s_instance.m_lines.Count - 1; i >= 0; i--)
            {                
                Destroy(s_instance.m_lines[i].gameObject);
                s_instance.m_lines.RemoveAt(i);
            }
            s_instance.m_historyIndex = -1;
        }

        [Command("logSuccess", "Logs a success message to the console.", "[string]"), UsedImplicitly]
        public static void LogSuccess(string message, CommandManager console) => console.Log(message, LogMessageType.Success);
        [Command("logWarning", "Logs a warning message to the console.", "[string]"), UsedImplicitly]
        public static void LogWarning(string message, CommandManager console) => console.Log(message, LogMessageType.Warning);
        [Command("logError", "Logs an error message to the console.", "[string]"), UsedImplicitly]
        public static void LogError(string message, CommandManager console) => console.Log(message, LogMessageType.Error);

        /*
        [Command("testicle")]
        public static void Testicle() {}
        [Command("tentacle")]
        public static void Tentacle() {}
        [Command("tentative")]
        public static void Tentative() {}
        [Command("television")]
        public static void Television() {}

        [Command("test", "Tests the command console", "[message]")]
        public static void TestFunc(string message) => Debug.Log(message);
        
        [Command("test", "[message] [message]")]
        public static void TestFunc(string message, string secondMessage) => Debug.Log($"2: {message}\n{secondMessage}");
        */
    }
}