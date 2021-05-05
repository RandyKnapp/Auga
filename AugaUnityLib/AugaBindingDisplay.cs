using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaBindingDisplay : MonoBehaviour
    {
        public Text KeybindText;
        public GameObject KeybindBox;
        public Text LongKeybindText;
        public GameObject LongKeybindBox;
        public GameObject Mouse1;
        public GameObject Mouse2;
        public GameObject Mouse3;
        public GameObject MouseX;
        public Text MouseXText;

        public void SetBinding(string keyName)
        {
            var keycode = ZInput.instance.m_buttons[keyName].m_key;
            var localizedKeyString = Localization.instance.GetBoundKeyString(keyName);

            var showMouse = -1;
            switch (keycode)
            {
                case KeyCode.Mouse0: showMouse = 0; break;
                case KeyCode.Mouse1: showMouse = 1; break;
                case KeyCode.Mouse2: showMouse = 2; break;
                case KeyCode.Mouse3: showMouse = 3; break;
                case KeyCode.Mouse4: showMouse = 4; break;
                case KeyCode.Mouse5: showMouse = 5; break;
                case KeyCode.Mouse6: showMouse = 6; break;
            }

            switch (localizedKeyString)
            {
                case "Equals": localizedKeyString = "="; break;
                case "BackQuote": localizedKeyString = "`"; break;
            }

            if (localizedKeyString.StartsWith("Keypad"))
            {
                localizedKeyString = localizedKeyString.Replace("Keypad", "Num");
            }
            else if (localizedKeyString.StartsWith("Alpha"))
            {
                localizedKeyString = localizedKeyString.Replace("Alpha", "");
            }

            switch (keycode)
            {
                case KeyCode.KeypadDivide: localizedKeyString = localizedKeyString.Replace("Divide", "/"); break;
                case KeyCode.KeypadMinus: localizedKeyString = localizedKeyString.Replace("Minus", "-"); break;
                case KeyCode.KeypadMultiply: localizedKeyString = localizedKeyString.Replace("Multiply", "*"); break;
                case KeyCode.KeypadEquals: localizedKeyString = localizedKeyString.Replace("Equals", "="); break;
                case KeyCode.KeypadPeriod: localizedKeyString = localizedKeyString.Replace("Period", "."); break;
                case KeyCode.KeypadPlus: localizedKeyString = localizedKeyString.Replace("Plus", "+"); break;

                case KeyCode.LeftArrow: localizedKeyString = "←"; break;
                case KeyCode.RightArrow: localizedKeyString = "→"; break;
                case KeyCode.UpArrow: localizedKeyString = "↑"; break;
                case KeyCode.DownArrow: localizedKeyString = "↓"; break;
            }

            if (char.IsPunctuation((char)keycode))
            {
                localizedKeyString = ((char)keycode).ToString();
            }

            var isOneCharLong = localizedKeyString.Length == 1;
            (isOneCharLong ? KeybindText : LongKeybindText).text = localizedKeyString;
            KeybindBox.SetActive(showMouse < 0 && isOneCharLong);
            LongKeybindBox.SetActive(showMouse < 0 && !isOneCharLong);

            Mouse1.SetActive(showMouse == 0);
            Mouse2.SetActive(showMouse == 1);
            Mouse3.SetActive(showMouse == 2);
            MouseX.SetActive(showMouse > 2);
            MouseXText.text = (showMouse + 1).ToString();
        }
    }
}
