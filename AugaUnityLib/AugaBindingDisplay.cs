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

            // Valheim now uses Unity's InputSystem: bindings are control paths such as
            // "<Keyboard>/e", "<Keyboard>/numpadDivide" or "<Mouse>/leftButton" instead of KeyCodes.
            var button = ZInput.instance.m_buttons[keyName];
            var path = button.GetActionPath() ?? string.Empty;
            var device = string.Empty;
            var control = path;
            var slash = path.IndexOf('/');
            if (slash >= 0)
            {
                device = path.Substring(0, slash);
                control = path.Substring(slash + 1);
            }

            // an unbound action shows a dash instead of the game's "MISSING KEY BINDING" placeholder
            var localizedKeyString = Localization.instance.GetBoundKeyString(keyName, true);
            if (string.IsNullOrEmpty(localizedKeyString))
            {
                localizedKeyString = "-";
            }

            var showMouse = -1;
            if (device == "<Mouse>")
            {
                switch (control)
                {
                    case "leftButton": showMouse = 0; break;
                    case "rightButton": showMouse = 1; break;
                    case "middleButton": showMouse = 2; break;
                    case "backButton": showMouse = 3; break;
                    case "forwardButton": showMouse = 4; break;
                }
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

            switch (control)
            {
                case "numpadDivide": localizedKeyString = "Num /"; break;
                case "numpadMinus": localizedKeyString = "Num -"; break;
                case "numpadMultiply": localizedKeyString = "Num *"; break;
                case "numpadEquals": localizedKeyString = "Num ="; break;
                case "numpadPeriod": localizedKeyString = "Num ."; break;
                case "numpadPlus": localizedKeyString = "Num +"; break;

                case "leftArrow": localizedKeyString = "←"; break;
                case "rightArrow": localizedKeyString = "→"; break;
                case "upArrow": localizedKeyString = "↑"; break;
                case "downArrow": localizedKeyString = "↓"; break;
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
