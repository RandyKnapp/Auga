using GUIFramework;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace Auga
{
    /// <summary>
    /// Replaces the "connecting..." and "enter password" dialogs shown while joining a server. Both are root
    /// canvases of their own in vanilla now (siblings of IngameGui under PixelFix); the Auga prefabs carry no
    /// Canvas, so without copying the vanilla canvas set-up they never render and a password-protected server
    /// looks like a black screen with no prompt.
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class ZNet_Awake_Patch
    {
        [UsedImplicitly]
        public static void Postfix(ZNet __instance)
        {
            __instance.m_passwordDialog = Replace(__instance.m_passwordDialog, Auga.Assets.PasswordDialog);
            __instance.m_connectingDialog = Replace(__instance.m_connectingDialog, Auga.Assets.ConnectingDialog);

            // the prefab's input field still shows Unity's default placeholder text
            var passwordField = __instance.m_passwordDialog != null ? __instance.m_passwordDialog.GetComponentInChildren<GuiInputField>(true) : null;
            if (passwordField != null && passwordField.placeholder is TMP_Text placeholder)
            {
                placeholder.text = "Enter Password...";
            }
        }

        private static RectTransform Replace(RectTransform vanilla, GameObject prefab)
        {
            if (vanilla == null || prefab == null)
            {
                return vanilla;
            }

            var parent = vanilla.parent;
            var siblingIndex = vanilla.GetSiblingIndex();
            var canvasSettings = SetupHelper.CaptureRootCanvas(vanilla.gameObject);
            Object.Destroy(vanilla.gameObject);

            var replacement = Object.Instantiate(prefab, parent, false);
            replacement.transform.SetSiblingIndex(siblingIndex);
            replacement.SetActive(false);
            SetupHelper.ApplyRootCanvas(replacement, canvasSettings);
            return replacement.GetComponent<RectTransform>();
        }
    }
}
