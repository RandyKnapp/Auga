using HarmonyLib;

namespace Auga
{
    [HarmonyPatch]
    public static class MessageHud_Setup
    {
        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Awake))]
        public static class MessageHud_Awake_Patch
        {
            public static bool Prefix(MessageHud __instance)
            {
                return !SetupHelper.IndirectTwoObjectReplace(__instance.transform, Auga.Assets.MessageHud, "HudMessage", "TopLeftMessage", "AugaMessageHud");
            }
        }
    }
}
