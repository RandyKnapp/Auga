using HarmonyLib;
using BepInEx;
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

                // A link: a button in the same row that calls OnLinkClicked instead of opening a tab. Auga keeps it for
                // later inventory screens, and adding it again only updates it, so registering it here is harmless.
                Auga.API.PlayerPanel_AddLink("AugaExample_Link", null, "Example link", OnLinkClicked);
            }
        }

        private static void OnTabSelected(int index)
        {
            Debug.LogWarning("Example Tab Selected");
        }

        private static void OnLinkClicked()
        {
            Debug.LogWarning("Example link clicked");
            MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft, "Example link clicked");
        }
    }
}
