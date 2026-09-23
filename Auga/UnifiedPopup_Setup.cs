using System.Collections;
using AugaUnity;
using GUIFramework;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    /// <summary>
    /// Some of the game's unified popups appear as Auga dialogs: a text entry popup (e.g. the build menu's "Add
    /// Category") as Auga's text input dialog, and the build menu's "remove favourite category" question as Auga's
    /// confirm dialog. The popup stack, its visibility and the callbacks stay vanilla's: the vanilla panel is hidden
    /// and the Auga panel, made once under the popup's blocking background, is wired to the popup's callbacks.
    /// Vanilla's body line ("Enter Category Name:") becomes the text field's placeholder; the confirm dialog shows
    /// the header and the body on two lines.
    /// </summary>
    [HarmonyPatch]
    public static class UnifiedPopup_Setup
    {
        private const string PanelName = "AugaTextEntry";
        private const string ConfirmName = "AugaConfirm";
        private static readonly string[] PanelNames = { PanelName, ConfirmName };

        [HarmonyPatch(typeof(UnifiedPopup), "Show")]
        [HarmonyPostfix]
        [UsedImplicitly]
        public static void Show_Postfix(UnifiedPopup __instance, PopupBase popup)
        {
            Transform shown = null;
            Selectable focus = null;
            if (popup is TextEntryPopup textEntry)
            {
                shown = GetPanel(__instance);
                if (shown != null)
                {
                    Wire(__instance, shown, textEntry);
                    focus = shown.Find("TextField")?.GetComponent<GuiInputField>();
                }
            }
            else if (popup is YesNoPopup yesNo && IsRemoveFavoriteCategory(yesNo))
            {
                shown = GetConfirmPanel(__instance);
                if (shown != null)
                {
                    WireConfirm(__instance, shown, yesNo);
                    focus = shown.Find("ButtonYes")?.GetComponent<Button>();   // vanilla puts the focus on Yes as well
                }
            }

            foreach (var name in PanelNames)
            {
                var panel = FindPanel(__instance, name);
                if (panel != null && panel != shown) panel.gameObject.SetActive(false);
            }
            var vanillaPanel = VanillaPanel(__instance);
            if (vanillaPanel != null) vanillaPanel.gameObject.SetActive(shown == null);
            if (shown == null)
                return;
            shown.gameObject.SetActive(true);
            __instance.StartCoroutine(Focus(focus));
        }

        [HarmonyPatch(typeof(UnifiedPopup), "Hide")]
        [HarmonyPostfix]
        [UsedImplicitly]
        public static void Hide_Postfix(UnifiedPopup __instance)
        {
            foreach (var name in PanelNames)
            {
                var panel = FindPanel(__instance, name);
                if (panel != null) panel.gameObject.SetActive(false);
            }
            var vanillaPanel = VanillaPanel(__instance);
            if (vanillaPanel != null) vanillaPanel.gameObject.SetActive(true);
        }

        private static Transform VanillaPanel(UnifiedPopup popup)
        {
            return popup.popupUIParent != null ? popup.popupUIParent.transform.Find("Popup") : null;
        }

        private static Transform FindPanel(UnifiedPopup popup, string name)
        {
            return popup.popupUIParent != null ? popup.popupUIParent.transform.Find(name) : null;
        }

        // ------------------------------------------------------------------ confirm dialog

        private static bool IsRemoveFavoriteCategory(YesNoPopup popup)
        {
            return popup.header == Localization.instance.Localize("$menu_removefavoritecategory");
        }

        /// <summary>Auga's confirm dialog under the popup's blocking background, made on first use.</summary>
        private static Transform GetConfirmPanel(UnifiedPopup popup)
        {
            var existing = FindPanel(popup, ConfirmName);
            if (existing != null)
                return existing;
            var parent = popup.popupUIParent != null ? popup.popupUIParent.transform : null;
            if (parent == null || Auga.Assets.ConfirmDialog == null)
                return null;

            var go = Object.Instantiate(Auga.Assets.ConfirmDialog, parent, false);
            go.name = ConfirmName;
            go.SetActive(false);
            // the popup's blocking background already darkens the screen
            var scrim = go.transform.Find("Scrim");
            if (scrim != null) scrim.gameObject.SetActive(false);
            AddKey(go.transform.Find("ButtonNo"), KeyCode.Escape, "JoyButtonB");
            AddKey(go.transform.Find("ButtonYes"), KeyCode.None, "JoyButtonA");
            return go.transform;
        }

        private static void WireConfirm(UnifiedPopup owner, Transform panel, YesNoPopup popup)
        {
            var text = panel.Find("Text")?.GetComponent<TMP_Text>();
            var yes = panel.Find("ButtonYes")?.GetComponent<Button>();
            var no = panel.Find("ButtonNo")?.GetComponent<Button>();
            if (text == null || yes == null || no == null)
                return;
            text.text = string.IsNullOrEmpty(popup.text) ? popup.header : popup.header + "\n" + popup.text;
            SetLabel(yes, owner.yesText);
            SetLabel(no, owner.noText);
            yes.onClick = new Button.ButtonClickedEvent();
            no.onClick = new Button.ButtonClickedEvent();
            yes.onClick.AddListener(() => popup.yesCallback?.Invoke());
            no.onClick.AddListener(() => popup.noCallback?.Invoke());
        }

        private static void AddKey(Transform button, KeyCode key, string zinputKey)
        {
            if (button == null || button.GetComponent<UIGamePad>() != null)
                return;
            var pad = button.gameObject.AddComponent<UIGamePad>();
            pad.m_keyCode = key;
            pad.m_zinputKey = zinputKey;
        }

        // ------------------------------------------------------------------ text entry

        /// <summary>The Auga text entry panel under the popup's blocking background, made on first use.</summary>
        private static Transform GetPanel(UnifiedPopup popup)
        {
            var existing = FindPanel(popup, PanelName);
            if (existing != null)
                return existing;
            var parent = popup.popupUIParent != null ? popup.popupUIParent.transform : null;
            var template = Auga.Assets.TextInput != null ? Auga.Assets.TextInput.transform.Find("panel") : null;
            if (parent == null || template == null)
                return null;

            var go = Object.Instantiate(template.gameObject, parent, false);
            go.name = PanelName;
            go.SetActive(false);
            var field = go.transform.Find("TextField")?.GetComponent<GuiInputField>();
            if (field != null)
            {
                // the prefab's submit helper re-focuses its field every frame; Enter is handled through onSubmit
                var submit = field.GetComponent<GuiInputFieldSubmit>();
                if (submit != null) Object.DestroyImmediate(submit);
            }
            // Escape / B cancels, as on the vanilla popup's cancel button
            AddKey(go.transform.Find("Cancel"), KeyCode.Escape, "JoyButtonB");
            return go.transform;
        }

        private static void Wire(UnifiedPopup owner, Transform panel, TextEntryPopup popup)
        {
            var topic = panel.Find("DividerMedium/Topic")?.GetComponent<TMP_Text>();
            if (topic != null) topic.text = popup.header;
            var field = panel.Find("TextField")?.GetComponent<GuiInputField>();
            var ok = panel.Find("OK")?.GetComponent<Button>();
            var cancel = panel.Find("Cancel")?.GetComponent<Button>();
            if (field == null || ok == null || cancel == null)
                return;

            var placeholder = field.placeholder as TMP_Text;
            if (placeholder != null) placeholder.text = popup.text;
            SetLabel(ok, owner.okText);
            SetLabel(cancel, owner.cancelText);

            field.text = "";
            field.onValueChanged = new TMP_InputField.OnChangeEvent();
            field.onSubmit = new TMP_InputField.SubmitEvent();
            ok.onClick = new Button.ButtonClickedEvent();
            cancel.onClick = new Button.ButtonClickedEvent();

            bool Valid(string s) => popup.validationCallback?.Invoke(s) ?? true;
            void Submit()
            {
                var s = field.text;
                if (Valid(s))
                    popup.sendResultCallback?.Invoke(s);
            }

            ok.interactable = Valid(field.text);
            field.onValueChanged.AddListener(s => ok.interactable = Valid(s));
            field.onSubmit.AddListener(_ => Submit());
            ok.onClick.AddListener(Submit);
            cancel.onClick.AddListener(() => popup.cancelCallback?.Invoke());
        }

        private static void SetLabel(Button button, string token)
        {
            var label = button.transform.Find("Text")?.GetComponent<TMP_Text>() ?? button.transform.Find("Label")?.GetComponent<TMP_Text>() ?? button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = Localization.instance.Localize(token);
        }

        /// <summary>The element takes the focus once the popup is active, as vanilla does for its own controls.</summary>
        private static IEnumerator Focus(Selectable target)
        {
            yield return null;
            if (target == null || !target.isActiveAndEnabled)
                yield break;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(target.gameObject);
            if (target is GuiInputField field)
                field.ActivateInputField();
        }
    }
}
