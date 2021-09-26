using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch(typeof(Minimap))]
    public static class Minimap_Setup
    {
        [HarmonyPatch(nameof(Minimap.Start))]
        [HarmonyPostfix]
        public static void Minimap_Start_Postfix(Minimap __instance)
        {
            var minimap = __instance;
            var originalMiniMapMaterial = minimap.m_mapImageSmall.material;
            var originalMiniMapMaterialLarge = minimap.m_mapImageLarge.material;

            var newMiniMap = __instance.Replace("small", Auga.Assets.Hud);
            minimap.m_smallRoot = newMiniMap.gameObject;
            minimap.m_mapImageSmall = newMiniMap.GetComponentInChildren<RawImage>();
            minimap.m_pinRootSmall = (RectTransform)newMiniMap.Find("map/pin_root");
            minimap.m_biomeNameSmall = newMiniMap.Find("biome/Content").GetComponent<Text>();
            minimap.m_smallShipMarker = (RectTransform)newMiniMap.Find("map/ship_marker");
            minimap.m_smallMarker = (RectTransform)newMiniMap.Find("map/player_marker");
            minimap.m_windMarker = (RectTransform)newMiniMap.Find("WindIndicator");

            var newMap = __instance.Replace("large", Auga.Assets.Hud);
            minimap.m_largeRoot = newMap.gameObject;
            minimap.m_mapImageLarge = newMap.GetComponentInChildren<RawImage>();
            minimap.m_pinRootLarge = (RectTransform)newMap.Find("map/pin_root");
            minimap.m_biomeNameLarge = newMap.Find("biome").GetComponent<Text>();
            minimap.m_largeShipMarker = (RectTransform)newMap.Find("map/ship_marker");
            minimap.m_largeMarker = (RectTransform)newMap.Find("map/player_marker");
            minimap.m_gamepadCrosshair = (RectTransform)newMap.Find("GamepadCrosshair");
            minimap.m_publicPosition = newMap.Find("PublicPanel").GetComponent<Toggle>();
            minimap.m_selectedIcon0 = newMap.Find("IconPanel/Icon0/Selected").GetComponent<Image>();
            minimap.m_selectedIcon1 = newMap.Find("IconPanel/Icon1/Selected").GetComponent<Image>();
            minimap.m_selectedIcon2 = newMap.Find("IconPanel/Icon2/Selected").GetComponent<Image>();
            minimap.m_selectedIcon3 = newMap.Find("IconPanel/Icon3/Selected").GetComponent<Image>();
            minimap.m_selectedIcon4 = newMap.Find("IconPanel/Icon4/Selected").GetComponent<Image>();
            minimap.m_selectedIconBoss = newMap.Find("IconBoss/Selected").GetComponent<Image>();
            minimap.m_selectedIconDeath = newMap.Find("IconDeath/Selected").GetComponent<Image>();
            minimap.m_nameInput = newMap.Find("NameField").GetComponent<InputField>();
            minimap.m_sharedMapHint = newMap.Find("SharedPanel").gameObject;
            minimap.m_hints = new List<GameObject> { newMap.Find("PingPanel").gameObject };

            minimap.m_mapImageLarge.material = Object.Instantiate(originalMiniMapMaterialLarge);
            minimap.m_mapImageSmall.material = Object.Instantiate(originalMiniMapMaterial);
            minimap.m_mapImageLarge.material.SetTexture("_MainTex", minimap.m_mapTexture);
            minimap.m_mapImageLarge.material.SetTexture("_MaskTex", minimap.m_forestMaskTexture);
            minimap.m_mapImageLarge.material.SetTexture("_HeightTex", minimap.m_heightTexture);
            minimap.m_mapImageLarge.material.SetTexture("_FogTex", minimap.m_fogTexture);
            minimap.m_mapImageSmall.material.SetTexture("_MainTex", minimap.m_mapTexture);
            minimap.m_mapImageSmall.material.SetTexture("_MaskTex", minimap.m_forestMaskTexture);
            minimap.m_mapImageSmall.material.SetTexture("_HeightTex", minimap.m_heightTexture);
            minimap.m_mapImageSmall.material.SetTexture("_FogTex", minimap.m_fogTexture);
            minimap.m_mapSmallShader = minimap.m_mapImageSmall.material;
            minimap.m_mapLargeShader = minimap.m_mapImageLarge.material;

            SetToggleListener(newMap.transform, "PublicPanel", _ => minimap.OnTogglePublicPosition());
            SetToggleListener(newMap.transform, "SharedPanel", _ =>
            {
                Minimap.instance.OnToggleSharedMapData();
                var sharedMapToggle = Minimap.instance.transform.Find("large/SharedPanel")?.GetComponent<Toggle>();
                if (sharedMapToggle != null)
                    sharedMapToggle.isOn = Minimap.instance.m_showSharedMapData;
            });
            SetButtonListener(newMap.transform, "IconPanel/Icon0", minimap.OnPressedIcon0);
            SetButtonListener(newMap.transform, "IconPanel/Icon1", minimap.OnPressedIcon1);
            SetButtonListener(newMap.transform, "IconPanel/Icon2", minimap.OnPressedIcon2);
            SetButtonListener(newMap.transform, "IconPanel/Icon3", minimap.OnPressedIcon3);
            SetButtonListener(newMap.transform, "IconPanel/Icon4", minimap.OnPressedIcon4);
            SetButtonListener(newMap.transform, "IconBoss", minimap.OnPressedIconBoss);
            SetButtonListener(newMap.transform, "IconDeath", minimap.OnPressedIconDeath);

            SetRightClickListener(newMap.transform, "IconPanel/Icon0", minimap.OnAltPressedIcon0);
            SetRightClickListener(newMap.transform, "IconPanel/Icon1", minimap.OnAltPressedIcon1);
            SetRightClickListener(newMap.transform, "IconPanel/Icon2", minimap.OnAltPressedIcon2);
            SetRightClickListener(newMap.transform, "IconPanel/Icon3", minimap.OnAltPressedIcon3);
            SetRightClickListener(newMap.transform, "IconPanel/Icon4", minimap.OnAltPressedIcon4);
            SetRightClickListener(newMap.transform, "IconBoss", minimap.OnAltPressedIconBoss);
            SetRightClickListener(newMap.transform, "IconDeath", minimap.OnAltPressedIconDeath);
        }

        private static void SetButtonListener(Transform root, string childName, UnityAction listener)
        {
            var button = root.Find(childName).GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(listener);
        }

        private static void SetRightClickListener(Transform root, string childName, UnityAction listener)
        {
            var button = root.Find(childName).GetComponent<MouseClick>();
            button.m_rightClick = new UnityEvent();
            button.m_rightClick.AddListener(listener);
        }

        private static void SetToggleListener(Transform root, string childName, UnityAction<bool> listener)
        {
            var toggle = root.Find(childName).GetComponent<Toggle>();
            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.onValueChanged.AddListener(listener);
        }
    }
}
