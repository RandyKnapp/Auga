using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class InputFieldSubmitEvents: MonoBehaviour
    {
        private InputField m_field;
        
        private HashSet<Action<string>> _submitActions = new HashSet<Action<string>>();
        
        public Action<string> m_onSubmit   
        {
            set { AddListener(value); }
        }

        private void Awake() => m_field = GetComponent<InputField>();

        private void Update()
        {
            if (m_field.text == "" || !Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter) && !ZInput.GetButtonDown("JoyButtonA"))
                return;
            
            foreach (var submitAction in _submitActions)
            {
                submitAction(m_field.text);
            }
            m_field.text = "";
        }

        public void AddListener(Action<string> submitEvent)
        {
            _submitActions.Add(submitEvent);
        }

        public void RemoveListener(Action<string> submitEvent)
        {
            _submitActions.Remove(submitEvent);
        }
    }
}