using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Fishlabs;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    static class MinimapUpdateTranspiler
    {
        private static void CheckForPinSubmit(Minimap instance)
        {
            if (ZInput.GetKeyDown(KeyCode.Return))
            {
                instance.OnPinTextEntered(instance.m_nameInput.text);
            }
        }
        
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var instrs = instructions.ToList();

            var counter = 0;

            CodeInstruction LogMessage(CodeInstruction instruction)
            {
                //Debug.LogFormat($"VAPOK: IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                return instruction;
            }
            
            CodeInstruction FindInstructionWithLabel(List<CodeInstruction> codeInstructions, int index, Label label)
            {
                if (index >= codeInstructions.Count)
                    return null;
                
                if (codeInstructions[index].labels.Contains(label))
                    return codeInstructions[index];
                
                return FindInstructionWithLabel(codeInstructions, index + 1, label);
            }

            var checkForSubmitMethod = AccessTools.DeclaredMethod(typeof(MinimapUpdateTranspiler), nameof(CheckForPinSubmit));
            var inTextInputMethod = AccessTools.DeclaredMethod(typeof(Minimap), nameof(Minimap.InTextInput));
            var getKeyDownMethod = AccessTools.DeclaredMethod(typeof(ZInput), nameof(ZInput.GetKeyDown));
            
            for (int i = 0; i < instrs.Count; ++i)
            {
                if (i > 6 && instrs[i-2].opcode == OpCodes.Call && instrs[i-2].operand.Equals(inTextInputMethod) &&
                    instrs[i+1].opcode == OpCodes.Call && instrs[i+1].operand.Equals(getKeyDownMethod))
                {
                    //Call Method needs Minimap Instance as parameter
                    var ldArgInstruction = new CodeInstruction(OpCodes.Ldarg_0);
                    //Move Any Labels from the instruction position being patched to new instruction.
                    if (instrs[i].labels.Count > 0)
                        instrs[i].MoveLabelsTo(ldArgInstruction);
                    //Output LdArg
                    yield return LogMessage(ldArgInstruction);
                    counter++;

                    //Output Call
                    yield return LogMessage(new CodeInstruction(OpCodes.Call, checkForSubmitMethod));
                    counter++;

                    //Output Current Operation
                    yield return LogMessage(instrs[i]);
                    counter++;
                }
                else
                {
                    yield return LogMessage(instrs[i]);
                    counter++;
                }
            }
        }
    }

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

            var newMiniMap = Hud.instance.Replace("hudroot/MiniMap/small", Auga.Assets.Hud);
            minimap.m_smallRoot = newMiniMap.gameObject;
            minimap.m_mapImageSmall = newMiniMap.GetComponentInChildren<RawImage>();
            minimap.m_pinRootSmall = (RectTransform)newMiniMap.Find("map/small_pin_root");
            minimap.m_pinNameRootSmall = (RectTransform)newMiniMap.Find("map/small_pin_name_root");
            minimap.m_biomeNameSmall = newMiniMap.Find("biome/Content").GetComponent<TextMeshProUGUI>();
            minimap.m_smallShipMarker = (RectTransform)newMiniMap.Find("map/small_ship_marker");
            minimap.m_smallMarker = (RectTransform)newMiniMap.Find("map/small_player_marker");
            minimap.m_windMarker = (RectTransform)newMiniMap.Find("WindIndicator");
            newMiniMap.gameObject.AddComponent<MovableHudElement>().Init("Minimap", TextAnchor.UpperRight, -40, -40);

            var newMap = Hud.instance.Replace("hudroot/MiniMap/large", Auga.Assets.Hud);
            minimap.m_largeRoot = newMap.gameObject;
            minimap.m_mapImageLarge = newMap.GetComponentInChildren<RawImage>();
            minimap.m_pinRootLarge = (RectTransform)newMap.Find("large_map/large_pin_root");
            minimap.m_pinNameRootLarge = (RectTransform)newMap.Find("large_map/large_pin_name_root");
            minimap.m_biomeNameLarge = newMap.Find("biome").GetComponent<TextMeshProUGUI>();
            minimap.m_largeShipMarker = (RectTransform)newMap.Find("large_map/large_ship_marker");
            minimap.m_largeMarker = (RectTransform)newMap.Find("large_map/large_player_marker");
            
            minimap.m_gamepadCrosshair = (RectTransform)newMap.Find("GamepadCrosshair");
            minimap.m_publicPosition = newMap.Find("PublicPanel").GetComponent<Toggle>();
            minimap.m_selectedIcon0 = newMap.Find("IconPanel/Icon0/Selected").GetComponent<Image>();
            minimap.m_selectedIcon1 = newMap.Find("IconPanel/Icon1/Selected").GetComponent<Image>();
            minimap.m_selectedIcon2 = newMap.Find("IconPanel/Icon2/Selected").GetComponent<Image>();
            minimap.m_selectedIcon3 = newMap.Find("IconPanel/Icon3/Selected").GetComponent<Image>();
            minimap.m_selectedIcon4 = newMap.Find("IconPanel/Icon4/Selected").GetComponent<Image>();
            minimap.m_selectedIconBoss = newMap.Find("IconBoss/Selected").GetComponent<Image>();
            minimap.m_selectedIconDeath = newMap.Find("IconDeath/Selected").GetComponent<Image>();
            minimap.m_selectedIcons[Minimap.PinType.Death] = minimap.m_selectedIconDeath;
            minimap.m_selectedIcons[Minimap.PinType.Boss]  = minimap.m_selectedIconBoss;
            minimap.m_selectedIcons[Minimap.PinType.Icon0] = minimap.m_selectedIcon0;
            minimap.m_selectedIcons[Minimap.PinType.Icon1] = minimap.m_selectedIcon1;
            minimap.m_selectedIcons[Minimap.PinType.Icon2] = minimap.m_selectedIcon2;
            minimap.m_selectedIcons[Minimap.PinType.Icon3] = minimap.m_selectedIcon3;
            minimap.m_selectedIcons[Minimap.PinType.Icon4] = minimap.m_selectedIcon4;
            minimap.SelectIcon(Minimap.PinType.Icon0);
            minimap.m_nameInput = newMap.Find("NameField").GetComponent<GuiInputField>();

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

            var mapInputHandler = minimap.m_mapImageLarge.GetComponent<UIInputHandler>();
            mapInputHandler.m_onRightClick += minimap.OnMapRightClick;
            mapInputHandler.m_onMiddleClick += minimap.OnMapMiddleClick;
            mapInputHandler.m_onLeftDown += minimap.OnMapLeftDown;
            mapInputHandler.m_onLeftUp += minimap.OnMapLeftUp;

            Localization.instance.Localize(__instance.transform);
            minimap.Reset();
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

