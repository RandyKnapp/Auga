using System;
using System.Reflection;
using AugaUnity;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Auga.Compat;

// SimpleRecycling adds a "Recycle All" button to containers and a RECYCLE tab to crafting stations. Under Auga the
// button is the one in Auga's container panel, and the tab is an Auga workbench tab driven by the vanilla recipe list.
public static class SimpleRecycling
{
    private static Type _recyclingContainerButtonHolderType;
    private static Type _recyclingStationButtonHolderType;
    private static WorkbenchTabData _recyclingTabData;

    internal static void Apply(Harmony harmony, PluginInfo plugin)
    {
        var pluginType = Assembly.LoadFile(plugin.Location);
        _recyclingContainerButtonHolderType = pluginType.GetType("ABearCodes.Valheim.SimpleRecycling.UI.ContainerRecyclingButtonHolder");
        _recyclingStationButtonHolderType = pluginType.GetType("ABearCodes.Valheim.SimpleRecycling.UI.StationRecyclingTabHolder");
        var containerButtonMethod = AccessTools.Method(_recyclingContainerButtonHolderType, "SetupButton");
        var tabButtonMethod = AccessTools.Method(_recyclingStationButtonHolderType, "SetupTabButton");
        var setActiveMethod = AccessTools.Method(_recyclingStationButtonHolderType, "SetActive");
        var inRecycleTabMethod = AccessTools.Method(_recyclingStationButtonHolderType, "InRecycleTab");
        if (containerButtonMethod != null)
            harmony.Patch(containerButtonMethod, new HarmonyMethod(typeof(SimpleRecycling), nameof(SimpleRecycling_ContainerRecyclingButtonHolder_SetupButton_Patch)));
        if (tabButtonMethod != null)
            harmony.Patch(tabButtonMethod, new HarmonyMethod(typeof(SimpleRecycling), nameof(SimpleRecycling_StationRecyclingTabHolder_SetupTabButton_Patch)));
        if (setActiveMethod != null)
            harmony.Patch(setActiveMethod, new HarmonyMethod(typeof(SimpleRecycling), nameof(SimpleRecycling_StationRecyclingTabHolder_SetActive_Patch)));
        if (inRecycleTabMethod != null)
            harmony.Patch(inRecycleTabMethod, new HarmonyMethod(typeof(SimpleRecycling), nameof(SimpleRecycling_StationRecyclingTabHolder_InRecycleTab_Patch)));
    }

    // SimpleRecycling's holders are components on the BepInEx manager object, the same GameObject every plugin
    // (Auga included) lives on.
    private static Component GetHolder(string name) => Auga.instance.gameObject.GetComponent(name);

    public static bool SimpleRecycling_ContainerRecyclingButtonHolder_SetupButton_Patch()
    {
        var recycleAllButtonGO = InventoryGui.instance.m_container.Find("RecycleAll").gameObject;

        var onRecycleAllPressedMethod = AccessTools.Method(_recyclingContainerButtonHolderType, "OnRecycleAllPressed");
        var setButtonStateMethod = AccessTools.Method(_recyclingContainerButtonHolderType, "SetButtonState");
        var recycleAllButtonFieldRef = AccessTools.FieldRefAccess<Button>(_recyclingContainerButtonHolderType, "_recycleAllButton");
        var textComponentFieldRef = AccessTools.FieldRefAccess<Text>(_recyclingContainerButtonHolderType, "_textComponent");
        var imageComponentFieldRef = AccessTools.FieldRefAccess<Image>(_recyclingContainerButtonHolderType, "_imageComponent");

        var component = GetHolder("ContainerRecyclingButtonHolder");
        var recycleAllButton = recycleAllButtonGO.GetComponent<Button>();
        recycleAllButtonFieldRef(component) = recycleAllButton;
        recycleAllButton.onClick.RemoveAllListeners();
        recycleAllButton.onClick.AddListener(() => { onRecycleAllPressedMethod.Invoke(component, new object[]{}); });

        textComponentFieldRef(component) = recycleAllButton.GetComponentInChildren<Text>();
        imageComponentFieldRef(component) = recycleAllButton.GetComponentInChildren<Image>();
        setButtonStateMethod.Invoke(component, new object[] { false });

        return false;
    }

    public static bool SimpleRecycling_StationRecyclingTabHolder_SetupTabButton_Patch()
    {
        var recyclingTabButtonFieldRef = AccessTools.FieldRefAccess<Button>(_recyclingStationButtonHolderType, "_recyclingTabButtonComponent");
        var recyclingTabButtonGOFieldRef = AccessTools.FieldRefAccess<GameObject>(_recyclingStationButtonHolderType, "_recyclingTabButtonGameObject");
        var updateCraftingPanelMethod = AccessTools.Method(_recyclingStationButtonHolderType, "UpdateCraftingPanel");
        var component = GetHolder("StationRecyclingTabHolder");

        _recyclingTabData = API.Workbench_AddVanillaWorkbenchTab("RECYCLE", Auga.Assets.RecyclingPanelIcon, "Recycle", (_) =>
        {
            updateCraftingPanelMethod.Invoke(component, new object[] { });
        });
        recyclingTabButtonFieldRef(component) = _recyclingTabData.TabButtonGO.GetComponent<Button>();
        recyclingTabButtonGOFieldRef(component) = _recyclingTabData.TabButtonGO;

        return false;
    }

    public static bool SimpleRecycling_StationRecyclingTabHolder_SetActive_Patch()
    {
        return false;
    }

    public static bool SimpleRecycling_StationRecyclingTabHolder_InRecycleTab_Patch(ref bool __result)
    {
        __result = WorkbenchPanelController.instance != null && WorkbenchPanelController.instance.IsTabActiveById("RECYCLE");
        return false;
    }
}
