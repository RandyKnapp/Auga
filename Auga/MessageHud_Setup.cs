using AugaUnity;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch]
    public static class MessageHud_Setup
    {
        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Awake))]
        [HarmonyPrefix]
        public static bool MessageHud_Awake_Prefix(MessageHud __instance)
        {
            return !SetupHelper.IndirectTwoObjectReplace(__instance.transform, Auga.Assets.MessageHud, "HudMessage", "TopLeftMessage", "AugaMessageHud");
        }

        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.ShowMessage))]
        [HarmonyPostfix]
        public static void MessageHud_ShowMessage_Postfix(MessageHud __instance, MessageHud.MessageType type, string text, int amount, Sprite icon)
        {
            if (Hud.IsUserHidden())
            {
                return;
            }

            text = Localization.instance.Localize(text);
            if (type == MessageHud.MessageType.TopLeft)
            {
                var controller = __instance.GetComponent<AugaTopLeftMessageController>();
                controller.AddMessage(text, icon, amount);
            }
        }
    }
}
