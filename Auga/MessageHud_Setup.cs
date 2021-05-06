using HarmonyLib;
using Object = UnityEngine.Object;

namespace Auga
{
    [HarmonyPatch]
    public static class MessageHud_Setup
    {
        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Awake))]
        public static class MessageHud_Awake_Patch
        {
            public static void Postfix(MessageHud __instance)
            {
                if (__instance.name != "HudMessage")
                {
                    return;
                }

                var parent = __instance.transform.parent;
                var topLeftMessageOriginal = parent.Find("TopLeftMessage");
                var topLeftSiblingIndex = topLeftMessageOriginal.GetSiblingIndex();
                var messageHudSiblingIndex = __instance.transform.GetSiblingIndex();

                Object.DestroyImmediate(topLeftMessageOriginal.gameObject);
                Object.DestroyImmediate(__instance.gameObject);

                var augaMessageHudPrefab = Object.Instantiate(Auga.Assets.MessageHud, parent);
                var topLeftMessage = augaMessageHudPrefab.transform.Find("TopLeftMessage");
                var messageHud = augaMessageHudPrefab.transform.Find("AugaMessageHud");

                topLeftMessage.SetParent(parent);
                messageHud.SetParent(parent);
                topLeftMessage.SetSiblingIndex(topLeftSiblingIndex);
                messageHud.SetSiblingIndex(messageHudSiblingIndex);

                Object.Destroy(augaMessageHudPrefab);
            }
        }

        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Start))]
        public static class MessageHud_Start_Patch
        {
            public static void Postfix(MessageHud __instance)
            {
                Auga.LogWarning($"Start: {__instance.name} (static instance={(MessageHud.instance != null ? MessageHud.instance.name : "null")})");
            }
        }
    }
}
