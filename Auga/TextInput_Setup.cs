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
    }
}
