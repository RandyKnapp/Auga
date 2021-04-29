using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch]
    public static class TextViewer_Setup
    {
        [HarmonyPatch(typeof(TextViewer), nameof(TextViewer.Awake))]
        public static class TextViewer_Awake_Patch
        {
            public static void Postfix(TextViewer __instance)
            {
                if (__instance.name != "TextViewer")
                {
                    return;
                }

                var parent = __instance.transform.parent;
                Object.DestroyImmediate(__instance.gameObject);

                var newTextViewer = Object.Instantiate(Auga.Assets.TextViewerPrefab, parent, false).GetComponent<TextViewer>();
                TextViewer.m_instance = newTextViewer;
            }
        }
    }
}
