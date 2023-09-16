using System;
using Fishlabs;
using UnityEngine;

namespace AugaUnity
{
    public class GuiInputFieldSubmit : MonoBehaviour
    {
        public Action<string> m_onSubmit;
        private GuiInputField m_field;

        private void Awake() => m_field = GetComponent<GuiInputField>();

        private void Update()
        {
            m_field.ActivateInputField();
            if (!(m_field.text != "") || !Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter) && !ZInput.GetButtonDown("JoyButtonA"))
                return;
            
            m_onSubmit(m_field.text);
            m_field.text = "";
        }
    }
}