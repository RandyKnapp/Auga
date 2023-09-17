using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AugaUnity
{
    public class ChatWindowController : MonoBehaviour
    {
        public TMP_Text ChatDisplayOutput;
        public Scrollbar ChatScrollbar;
        private Chat _chatHandler;
        private float _lastPosition;

        private void Awake()
        {
            _chatHandler = GetComponent<Chat>();
            _chatHandler.m_input.OnInputSubmit.AddListener(AddToChatOutput);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1) ||
                Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB") ||
                ZInput.GetButtonDown("JoyLStickLeft") || ZInput.GetButtonDown("JoyLStickRight") ||
                ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyLStickDown"))
            {
                EventSystem.current.SetSelectedGameObject(null);
                _chatHandler.m_input.gameObject.SetActive(false);
                _chatHandler.m_focused = false;
            }

            if (!Console.IsVisible() && !TextInput.IsVisible() && !Minimap.InTextInput() && !Menu.IsVisible() &&
                !InventoryGui.IsVisible() && _chatHandler.m_input.gameObject.activeSelf)
            {
                if (!_chatHandler.m_wasFocused)
                {
                    _chatHandler.m_hideTimer = 0.0f;
                    _chatHandler.m_input.ActivateInputField();
                    _lastPosition = 0.0f;
                } else if (_chatHandler.m_wasFocused)
                {
                    _chatHandler.m_hideTimer = 0.0f;
                    _lastPosition += ZInput.GetAxis("Mouse ScrollWheel");
                    _lastPosition = Mathf.Clamp(_lastPosition, 0.0f, ChatScrollbar.size);
                }
            }
            ChatScrollbar.value = _lastPosition;
        }

        void OnEnable()
        {
            //ChatInputFieldSubmit.m_onSubmit = AddToChatOutput;
        }

        void OnDisable()
        {
            //ChatInputFieldSubmit.m_onSubmit = AddToChatOutput;
        }

        void AddToChatOutput(string newText)
        {
            // Set the scrollbar to the bottom when next text is submitted.
            _chatHandler.SendInput();
            ChatScrollbar.value = 0;
        }
    }
}