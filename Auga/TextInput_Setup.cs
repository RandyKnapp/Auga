using HarmonyLib;

namespace Auga
{
    [HarmonyPatch]
    public static class TextInput_Setup
    {
        [HarmonyPatch(typeof(TextInput), nameof(TextInput.Awake))]
        public static class TextInput_Awake_Patch
        {
            public static bool Prefix(TextInput __instance)
            {
                return !SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.TextInput, "TextInput");
            }
        }

        [HarmonyPatch(typeof(TextInput), nameof(TextInput.Update))]
        [HarmonyPrefix]
        public static bool Update_Prefix(TextInput __instance)
        {
            __instance.m_visibleFrame = TextInput.m_instance.m_panel.gameObject.activeSelf;
            if (!__instance.m_visibleFrame || Console.IsVisible() || Chat.instance.HasFocus())
            {
                return true;
            }

            if (ZInput.GetButtonDown("Inventory"))
            {
                __instance.Hide();
                return false;
            }

            return true;
        }
    }
}
