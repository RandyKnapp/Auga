using AugaUnity;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

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
                __instance.Replace("hudroot/HotKeyBar", Auga.Assets.Hud, "hudroot/HotKeyBar");

                __instance.m_statusEffectListRoot = null;
                __instance.m_statusEffectTemplate = new GameObject("DummyStatusEffectTemplate", typeof(RectTransform)).RectTransform();
                __instance.Replace("hudroot/StatusEffects", Auga.Assets.Hud);

                __instance.m_saveIcon = __instance.Replace("hudroot/SaveIcon", Auga.Assets.Hud).gameObject;
                __instance.m_badConnectionIcon = __instance.Replace("hudroot/BadConnectionIcon", Auga.Assets.Hud).gameObject;

                var originalDreamTexts = __instance.m_sleepingProgress.GetComponent<SleepText>().m_dreamTexts;
                var loadingScreen = __instance.Replace("LoadingBlack", Auga.Assets.Hud);
                __instance.m_loadingScreen = loadingScreen.GetComponent<CanvasGroup>();
                __instance.m_loadingProgress = loadingScreen.Find("Loading").gameObject;
                __instance.m_sleepingProgress = loadingScreen.Find("Sleeping").gameObject;
                __instance.m_teleportingProgress = loadingScreen.Find("Teleporting").gameObject;
                __instance.m_loadingImage = loadingScreen.Find("Loading/Image").GetComponent<Image>();
                __instance.m_loadingTip = loadingScreen.Find("Loading/Tip").GetComponent<Text>();
                __instance.m_sleepingProgress.GetComponent<SleepText>().m_dreamTexts = originalDreamTexts;

                var minimap = __instance.GetComponentInChildren<Minimap>();
                var originalMiniMapMaterial = minimap.m_mapImageSmall.material;

                var newMiniMap = __instance.Replace("hudroot/MiniMap/small", Auga.Assets.Hud);
                minimap.m_smallRoot = newMiniMap.gameObject;
                minimap.m_mapImageSmall = newMiniMap.GetComponentInChildren<RawImage>();
                minimap.m_mapImageSmall.material = originalMiniMapMaterial;
                minimap.m_pinRootSmall = (RectTransform)newMiniMap.Find("MapMask/map/pin_root");
                minimap.m_biomeNameSmall = newMiniMap.Find("biome/Content").GetComponent<Text>();
                minimap.m_smallShipMarker = (RectTransform)newMiniMap.Find("MapMask/map/ship_marker");
                minimap.m_smallMarker = (RectTransform)newMiniMap.Find("MapMask/map/player_marker");
                minimap.m_windMarker = (RectTransform)newMiniMap.Find("WindIndicator");

                __instance.m_eventBar = __instance.Replace("hudroot/EventBar", Auga.Assets.Hud).gameObject;
                __instance.m_eventName = __instance.m_eventBar.GetComponentInChildren<Text>();

                __instance.m_damageScreen = __instance.Replace("hudroot/Damaged", Auga.Assets.Hud).GetComponent<Image>();

                var newCrosshair = __instance.Replace("hudroot/crosshair", Auga.Assets.Hud);
                __instance.m_crosshair = newCrosshair.Find("crosshair").GetComponent<Image>();
                __instance.m_crosshairBow = newCrosshair.Find("crosshair_bow").GetComponent<Image>();
                __instance.m_hoverName = newCrosshair.Find("HoverName").GetComponent<Text>();
                __instance.m_pieceHealthRoot = (RectTransform) newCrosshair.Find("PieceHealthRoot");
                __instance.m_pieceHealthBar = newCrosshair.Find("PieceHealthRoot/PieceHealthBar").GetComponent<GuiBar>();
                __instance.m_targetedAlert = newCrosshair.Find("Sneak/Alert").gameObject;
                __instance.m_targeted = newCrosshair.Find("Sneak/Detected").gameObject;
                __instance.m_hidden = newCrosshair.Find("Sneak/Hidden").gameObject;
                __instance.m_stealthBar = newCrosshair.Find("Sneak/StealthBar").GetComponent<GuiBar>();
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
                if (Player.m_localPlayer == null || Player.m_localPlayer.IsDead())
                {
                    return;
                }

                for (var index = 0; index < __instance.m_items.Count; ++index)
                {
                    var itemData = __instance.m_items[index];
                    if (itemData.m_gridPos.x < 0 || itemData.m_gridPos.x >= __instance.m_elements.Count)
                    {
                        continue;
                    }

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
