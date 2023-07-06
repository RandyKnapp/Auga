using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class ChatWindowController : MonoBehaviour
    {
        public InputField ChatInputField;
        public InputFieldSubmitEvents ChatInputFieldSubmit;
        public TMP_Text ChatDisplayOutput;
        public Scrollbar ChatScrollbar;
        private Chat _chatHandler;
        private float _lastPosition;

        private void Awake()
        {
            _chatHandler = GetComponent<Chat>();
        }

        private void Update()
        {
            if (_chatHandler.m_wasFocused)
            {
                _lastPosition += ZInput.GetAxis("Mouse ScrollWheel");
                _lastPosition = Mathf.Clamp(_lastPosition, 0.0f, ChatScrollbar.size);
            }
            else
            {
                _lastPosition = 0.0f;
            }
            ChatScrollbar.value = _lastPosition;
        }

        void OnEnable()
        {
            ChatInputFieldSubmit.AddListener(AddToChatOutput);
        }

        void OnDisable()
        {
            ChatInputFieldSubmit.RemoveListener(AddToChatOutput);
        }

        void AddToChatOutput(string newText)
        {
            // Set the scrollbar to the bottom when next text is submitted.
            if (_chatHandler.m_wasFocused)
                _chatHandler.SendInput();
            
            ChatScrollbar.value = 0;
        }
    }
}