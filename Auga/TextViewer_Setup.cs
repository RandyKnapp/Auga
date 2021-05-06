using HarmonyLib;

namespace Auga
{
    [HarmonyPatch]
    public static class TextViewer_Setup
    {
        [HarmonyPatch(typeof(TextViewer), nameof(TextViewer.Awake))]
        public static class TextViewer_Awake_Patch
        {
            public static bool Prefix(TextViewer __instance)
            {
                return !SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.TextViewerPrefab, "TextViewer");
            }
        }
    }
}
