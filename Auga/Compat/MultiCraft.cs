using System;
using System.Globalization;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine.UI;

namespace Auga.Compat;

// MultiCraft draws its +/- buttons and amount next to the vanilla craft button. Under Auga it drives Auga's own
// crafting controls (API.GetCraftingControls) instead.
public static class MultiCraft
{
    private static Type _multiCraftUiType;

    internal static void Apply(Harmony harmony, PluginInfo plugin)
    {
        var multiCraftPluginType = Assembly.LoadFile(plugin.Location);
        _multiCraftUiType = multiCraftPluginType.GetType("MultiCraft.MultiCraft_UI");
        var multicraftLogicType = multiCraftPluginType.GetType("MultiCraft.MultiCraft_Logic");
        var createCraftButtonSpaceMethod = AccessTools.Method(_multiCraftUiType, "CreateSpaceFromCraftButton");
        var isCraftingMethod = AccessTools.Method(multicraftLogicType, "IsCrafting");
        if (createCraftButtonSpaceMethod != null)
        {
            harmony.Patch(createCraftButtonSpaceMethod, new HarmonyMethod(typeof(MultiCraft), nameof(MultiCraft_UI_CreateSpaceFromCraftButton_Patch)));
            harmony.Patch(isCraftingMethod, new HarmonyMethod(typeof(MultiCraft), nameof(MultiCraft_Logic_IsCrafting_Patch)));
        }
    }

    public static bool MultiCraft_UI_CreateSpaceFromCraftButton_Patch(InventoryGui instance)
    {
        API.GetCraftingControls().Multicraft.SetActive(true);
        var multiCraftUiInstance = AccessTools.Method(_multiCraftUiType, "get_instance").Invoke(null, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty, null, new object[] { }, CultureInfo.InvariantCulture);

        var plusButton = API.GetCraftingControls().PlusButton;
        var plusButtonMethod = AccessTools.Method(_multiCraftUiType, "OnPlusButtonPressed");
        plusButton.GetComponent<Button>().onClick.AddListener(() => plusButtonMethod.Invoke(multiCraftUiInstance, new object[]{}));

        var minusButton = API.GetCraftingControls().MinusButton;
        var minusButtonMethod = AccessTools.Method(_multiCraftUiType, "OnMinusButtonPressed");
        minusButton.GetComponent<Button>().onClick.AddListener(() => minusButtonMethod.Invoke(multiCraftUiInstance, new object[] { }));

        return false;
    }

    public static bool MultiCraft_Logic_IsCrafting_Patch(ref bool __result)
    {
        __result = InventoryGui.instance.m_craftTimer >= 0;
        return false;
    }
}
