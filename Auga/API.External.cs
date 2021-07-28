using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    public static class API
    {
        private static readonly Assembly _targetAssembly;

        static API()
        {
            _targetAssembly = LoadAssembly();
            if (_targetAssembly != null)
            {
                var harmony = new Harmony("mods.randyknapp.auga.API");
                foreach (var method in typeof(API).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public).Where(m => m.Name != "IsLoaded" && m.Name != "LoadAssembly"))
                {
                    harmony.Patch(method, transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(API), nameof(Transpiler))));
                }
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _, MethodBase original)
        {
            var parameters = original.GetParameters().Select(p =>
            {
                var type = p.ParameterType;
                if (type.Assembly == Assembly.GetExecutingAssembly() && _targetAssembly != null)
                {
                    type = _targetAssembly.GetType(type.FullName ?? string.Empty);
                }
                return type;
            }).ToArray();

            MethodBase originalMethod = _targetAssembly.GetType("Auga.API").GetMethod(original.Name, parameters);

            for (var i = 0; i < parameters.Length; ++i)
            {
                yield return new CodeInstruction(OpCodes.Ldarg, i);
            }

            yield return new CodeInstruction(OpCodes.Call, originalMethod);
            yield return new CodeInstruction(OpCodes.Ret);
        }

        public static Assembly LoadAssembly()
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "Auga");
        }

        public static string RedText = "#CD2121";

        public static GameObject CreatePanel(Transform parent, Vector2 size, string name, bool withCornerDecoration) => null;
        public static Button MediumButton_Create(Transform parent, string name) => null;
        public static void MediumButton_SetColors(Button button, Color normal, Color highlighted, Color pressed, Color selected, Color disabled) { }
        public static void MediumButton_OverrideColor(Button button, Color color) { }

        public static bool IsLoaded() => LoadAssembly() != null;
        public static bool HasWorkbenchTab(string tabID) => false;
        public static WorkbenchTabData AddWorkbenchTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected) => null;
        public static bool IsTabActive(GameObject tabButton) => false;
        public static Button GetCraftingTabButton() => null;
        public static Button GetUpgradeTabButton() => null;
        public static GameObject Workbench_CreateNewResultsPanel() => null;

        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, Text t, object s, bool localize = true) { }
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, bool localize = true) { }
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, bool localize = true) { }
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, object parenthetical, bool localize = true) { }
        public static void TooltipTextBox_AddUpgradeLine(GameObject tooltipTextBoxGO, object label, object value1, object value2, string color2, bool localize = true) { }

        public static void ComplexTooltip_AddItemTooltipCreatedListener(Action<GameObject, ItemDrop.ItemData> listener) { }
        public static void ComplexTooltip_ClearTextBoxes(GameObject complexTooltipGO) { }
        public static GameObject ComplexTooltip_AddTwoColumnTextBox(GameObject complexTooltipGO) => null;
        public static GameObject ComplexTooltip_AddCenteredTextBox(GameObject complexTooltipGO) => null;
        public static GameObject ComplexTooltip_AddUpgradeLabels(GameObject complexTooltipGO) => null;
        public static GameObject ComplexTooltip_AddUpgradeTwoColumnTextBox(GameObject complexTooltipGO) => null;
        public static GameObject ComplexTooltip_AddCheckBoxTextBox(GameObject complexTooltipGO) => null;
        public static GameObject ComplexTooltip_AddDivider(GameObject complexTooltipGO) => null;
        public static void ComplexTooltip_SetTopic(GameObject complexTooltipGO, string topic) { }
        public static void ComplexTooltip_SetSubtitle(GameObject complexTooltipGO, string topic) { }
        public static void ComplexTooltip_SetDescription(GameObject complexTooltipGO, string desc) { }
        public static void ComplexTooltip_SetItem(GameObject complexTooltipGO, ItemDrop.ItemData item, int quality = -1, int variant = -1) { }
        public static void ComplexTooltip_SetItemNoTextBoxes(GameObject complexTooltipGO, ItemDrop.ItemData item, int quality = -1, int variant = -1) { }
        public static void ComplexTooltip_EnableDescription(GameObject complexTooltipGO, bool enabled) { }

        public static Image RequirementsPanel_GetIcon(GameObject requirementsPanelGO) => null;
        public static void RequirementsPanel_SetWires(GameObject requirementsPanelGO, RequirementWireState[] wireStates, bool canCraft) { }

        public static Text CustomVariantPanel_Enable(string buttonLabel, Action<bool> onShow) => null;
        public static void CustomVariantPanel_Disable() { }
    }
}
