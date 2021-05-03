using AugaUnity;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch]
    public static class Hud_Setup
    {
        [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
        public static class Hud_Awake_Patch
        {
            public static void Postfix(Hud __instance)
            {
                __instance.Replace("hudroot/HotKeyBar", Auga.Assets.Hud, "hudroot/HotKeyBar", ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal);

                __instance.m_statusEffectListRoot = null;
                __instance.m_statusEffectTemplate = new GameObject("DummyStatusEffectTemplate", typeof(RectTransform)).RectTransform();
                __instance.Replace("hudroot/StatusEffects", Auga.Assets.Hud, "hudroot/StatusEffects", ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal);

                __instance.m_saveIcon = __instance.Replace("hudroot/SaveIcon", Auga.Assets.Hud, "hudroot/SaveIcon", ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal).gameObject;
                __instance.m_badConnectionIcon = __instance.Replace("hudroot/BadConnectionIcon", Auga.Assets.Hud, "hudroot/BadConnectionIcon", ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal).gameObject;
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateStatusEffects))]
        public static class Hud_UpdateStatusEffects_Patch
        {
            public static bool Prefix()
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
        public static class HotkeyBar_UpdateIcons_Patch
        {
            public static void Postfix(HotkeyBar __instance)
            {
                for (var index = 0; index < __instance.m_items.Count; ++index)
                {
                    var itemData = __instance.m_items[index];
                    var element = __instance.m_elements[itemData.m_gridPos.x];
                    var itemTooltip = element.m_go.GetComponent<ItemTooltip>();
                    if (itemTooltip != null)
                    {
                        itemTooltip.Item = itemData;
                    }
                }
            }
        }
    }
}
