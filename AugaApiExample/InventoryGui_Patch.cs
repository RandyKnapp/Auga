using UnityEngine;
using UnityEngine.UI;

namespace AugaApiExample
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    public static class InventoryGui_Patch
    {
        public static void Postfix(InventoryGui __instance)
        {
            if (Auga.API.IsLoaded())
            {
                Debug.LogWarning("Auga Example Loaded!");
                var tabData = Auga.API.PlayerPanel_AddTab("Example", null, "Example", OnTabSelected);

                var content = tabData.ContentGO;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 10;
                vlg.childControlWidth = false;
                vlg.childControlHeight = false;
                vlg.childScaleWidth = false;
                vlg.childScaleHeight = false;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = false;

                var smallButton = Auga.API.SmallButton_Create(content.transform, "SmallButton", "Test Small Button");
                var rt = (RectTransform)smallButton.transform;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);
            }
        }

        private static void OnTabSelected(int index)
        {
            Debug.LogWarning("Example Tab Selected");
        }
    }
}
