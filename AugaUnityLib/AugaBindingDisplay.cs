using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaBindingDisplay : MonoBehaviour
    {
        public string AutomaticKeyName;

        public Text KeybindText;
        public GameObject KeybindBox;
        public Text LongKeybindText;
        public GameObject LongKeybindBox;
        public GameObject Mouse1;
        public GameObject Mouse2;
        public GameObject Mouse3;
        public GameObject MouseX;
        public Text MouseXText;

        public void Update()
        {
            if (!string.IsNullOrEmpty(AutomaticKeyName))
            {
                SetBinding(AutomaticKeyName);
            }
        }

        public void SetBinding(string keyName)
        {
            if (!ZInput.instance.m_buttons.ContainsKey(keyName))
            {
                Debug.LogError($"[AugaBindingDisplay.SetBinding] Couldn't find key: {keyName}");
                return;
            }

            var localizedKeyString = Localization.instance.GetBoundKeyString(keyName);

            // Detect mouse button index from the localized key string
            var showMouse = -1;
            if (localizedKeyString == "Mouse0" || localizedKeyString == "LMB") showMouse = 0;
            else if (localizedKeyString == "Mouse1" || localizedKeyString == "RMB") showMouse = 1;
            else if (localizedKeyString == "Mouse2" || localizedKeyString == "MMB") showMouse = 2;
            else if (localizedKeyString == "Mouse3") showMouse = 3;
            else if (localizedKeyString == "Mouse4") showMouse = 4;
            else if (localizedKeyString == "Mouse5") showMouse = 5;
            else if (localizedKeyString == "Mouse6") showMouse = 6;

            // Normalize display strings
            switch (localizedKeyString)
            {
                case "Equals": localizedKeyString = "="; break;
                case "BackQuote": localizedKeyString = "`"; break;
            }

            if (localizedKeyString.StartsWith("Keypad"))
            {
                localizedKeyString = localizedKeyString
                    .Replace("Keypad", "Num")
                    .Replace("Divide", "/")
                    .Replace("Minus", "-")
                    .Replace("Multiply", "*")
                    .Replace("Equals", "=")
                    .Replace("Period", ".")
                    .Replace("Plus", "+");
            }
            else if (localizedKeyString.StartsWith("Alpha"))
            {
                localizedKeyString = localizedKeyString.Replace("Alpha", "");
            }
            else
            {
                switch (localizedKeyString)
                {
                    case "LeftArrow":  localizedKeyString = "←"; break;
                    case "RightArrow": localizedKeyString = "→"; break;
                    case "UpArrow":    localizedKeyString = "↑"; break;
                    case "DownArrow":  localizedKeyString = "↓"; break;
                }
            }

            SetText(localizedKeyString, showMouse);
        }

        public void SetText(string localizedKeyString, int showMouse = -1)
        {
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
