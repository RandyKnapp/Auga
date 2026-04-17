using System;
using TMPro;
using UnityEngine;

namespace AugaUnity
{
    public class GuiInputFieldSubmit : MonoBehaviour
    {
        public Action<string> m_onSubmit;
        private TMP_InputField m_field;

        private void Awake() => m_field = GetComponent<TMP_InputField>();

        private void Update()
        {
            if (m_field == null) return;
            m_field.ActivateInputField();
            if (!(m_field.text != "") || !Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter) && !ZInput.GetButtonDown("JoyButtonA"))
                return;

            m_onSubmit?.Invoke(m_field.text);
            m_field.text = "";
        }
    }
}