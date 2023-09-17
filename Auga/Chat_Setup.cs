using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch]
    public static class Chat_Setup
    {
        [HarmonyPatch(typeof(Chat), nameof(Chat.Awake))]
        public static class Chat_Awake_Patch
        {
            public static bool Prefix(Chat __instance)
            {
                if (!Auga.AugaChatShow.Value || Auga.HasChatter)
                    return true;
                
                return !SetupHelper.IndirectTwoObjectReplace(__instance.transform, Auga.Assets.AugaChat, "Chat", "Chat_box", "AugaChat");
            }

            public static void Postfix(Chat __instance)
            {
                if (!Auga.AugaChatShow.Value || Auga.HasChatter)
                    return;
                
                if (__instance.m_input != null)
                    __instance.m_input.transform.parent.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.LowerRight, 0, 67);
            }
        }

        [HarmonyPatch(typeof(Chat), nameof(Chat.SetNpcText))]
        public static class Chat_SetNpcText_Patch
        {
            public static void Postfix(Chat __instance)
            {
                if (!Auga.AugaChatShow.Value || Auga.HasChatter)
                    return;
                
                var latestChatMessage = __instance.m_npcTexts.LastOrDefault();
                if (latestChatMessage != null)
                {
                    var text = latestChatMessage.m_textField.text;
                    text = text.Replace("<color=orange>", $"<color={Auga.Colors.Topic}>");
                    text = text.Replace("<color=yellow>", $"<color={Auga.Colors.Emphasis}>");
                    latestChatMessage.m_textField.text = text;
                }
            }
        }
    }
}