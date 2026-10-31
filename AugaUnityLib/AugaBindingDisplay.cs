using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace AugaUnity
{
    public class AugaBindingDisplay : MonoBehaviour
    {
        public string AutomaticKeyName;

        public TMP_Text KeybindText;
        public GameObject KeybindBox;
        public TMP_Text LongKeybindText;
        public GameObject LongKeybindBox;
        public GameObject Mouse1;
        public GameObject Mouse2;
        public GameObject Mouse3;
        public GameObject MouseX;
        public TMP_Text MouseXText;

        // What the shown key was worked out from. AutomaticKeyName is shown again only when one of these changes
        // (the player rebinds it, switches language or gamepad), not every frame.
        private string _shownKeyName;
        private bool _shownMissing;
        private string _shownPath;
        private string _shownOverride;
        private GamepadGlyphs _shownGlyph;
        private GamepadType _shownGamepad;
        private int _shownLanguage;

        private static int _languageVersion;
        private static bool _watchingLanguage;

        public void Awake()
        {
            if (!_watchingLanguage)
            {
                _watchingLanguage = true;
                Localization.OnLanguageChange += () => _languageVersion++;
            }
        }

        public void Update()
        {
            if (!string.IsNullOrEmpty(AutomaticKeyName) && !IsShowing(AutomaticKeyName))
            {
                SetBinding(AutomaticKeyName);
            }
        }

        private bool IsShowing(string keyName)
        {
            if (keyName != _shownKeyName)
                return false;
            // a key that was missing is not looked for again until a mod adds it
            if (!ZInput.instance.m_buttons.TryGetValue(keyName, out var button))
                return _shownMissing;
            return !_shownMissing && button.GetActionPath() == _shownPath && button.DisplayNameOverride == _shownOverride
                && ZInput.CurrentGlyph == _shownGlyph && ZInput.ConnectedGamepadType == _shownGamepad && _languageVersion == _shownLanguage;
        }

        public void SetBinding(string keyName)
        {
            if (!ZInput.instance.m_buttons.TryGetValue(keyName, out var button))
            {
                Debug.LogError($"[AugaBindingDisplay.SetBinding] Couldn't find key: {keyName}");
                _shownKeyName = keyName;
                _shownMissing = true;
                return;
            }

            // Valheim now uses Unity's InputSystem: bindings are control paths such as
            // "<Keyboard>/e", "<Keyboard>/numpadDivide" or "<Mouse>/leftButton" instead of KeyCodes.
            var actionPath = button.GetActionPath();
            var path = actionPath ?? string.Empty;
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

            _shownKeyName = keyName;
            _shownMissing = false;
            _shownPath = actionPath;
            _shownOverride = button.DisplayNameOverride;
            _shownGlyph = ZInput.CurrentGlyph;
            _shownGamepad = ZInput.ConnectedGamepadType;
            _shownLanguage = _languageVersion;
        }

        public void SetText(string localizedKeyString, int showMouse = -1)
        {
            // whatever is shown now did not come from AutomaticKeyName, which is shown again on the next Update
            _shownKeyName = null;

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
