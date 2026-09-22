using Auga.Utilities;
using AugaUnity;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch]
    public static class MessageHud_Setup
    {
        /// <summary>
        /// Replaces the vanilla message HUD ("HudMessage") with the Auga one. The Auga prefab still has the old
        /// two-object layout (an "AugaMessageHud" root carrying the MessageHud component and a separate
        /// "TopLeftMessage" panel with the message log); "HudMessage" is its own root Canvas now and has no
        /// "TopLeftMessage" sibling anymore, so both pieces are placed under a new root canvas here.
        /// </summary>
        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Awake))]
        [HarmonyPrefix]
        public static bool MessageHud_Awake_Prefix(MessageHud __instance)
        {
            if (__instance.name.StartsWith("Auga") || __instance.name != "HudMessage" || !Auga.Assets.MessageHud)
            {
                return true;
            }

            var vanilla = __instance.transform;
            var parent = vanilla.parent;
            var siblingIndex = vanilla.GetSiblingIndex();
            var canvasSettings = SetupHelper.CaptureRootCanvas(vanilla.gameObject);

            var prefabInstance = Object.Instantiate(Auga.Assets.MessageHud, parent);
            var primary = prefabInstance.transform.Find("AugaMessageHud");
            var topLeft = prefabInstance.transform.Find("TopLeftMessage");
            if (primary == null || primary.GetComponent<MessageHud>() == null)
            {
                Auga.LogWarning("Auga message HUD prefab has no AugaMessageHud/MessageHud; keeping the vanilla one.");
                Object.Destroy(prefabInstance);
                return true;
            }

            primary.SetParent(parent, false);
            primary.SetSiblingIndex(siblingIndex);
            var newHud = primary.GetComponent<MessageHud>();
            SerializedFieldHelper.CopyMissingFields(newHud, __instance, primary);
            SetupHelper.ApplyRootCanvas(primary.gameObject, canvasSettings);
            if (topLeft != null)
            {
                // keep the message log inside the new root canvas (it was a sibling of HudMessage before)
                var canvasRoot = primary.GetComponentInParent<Canvas>();
                topLeft.SetParent(canvasRoot != null ? canvasRoot.transform : primary, false);
            }
            Object.Destroy(prefabInstance);

            vanilla.SetParent(null);
            Object.Destroy(vanilla.gameObject);
            return false;
        }

        /// <summary>
        /// Vanilla draws the top-left pickup message in a separate root canvas ("TopLeftMessage", next to
        /// "HudMessage") that only the vanilla MessageHud references. That instance is replaced above before its
        /// Start() could fade the placeholder out, so the prefab's sample text ("You picked up an Axe" with a helmet
        /// icon) would stay on screen forever. The Auga message HUD carries its own log, so drop the vanilla canvas.
        /// </summary>
        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Start))]
        [HarmonyPostfix]
        public static void MessageHud_Start_Postfix(MessageHud __instance)
        {
            if (__instance == null || !__instance.name.StartsWith("Auga") || __instance.transform.parent == null)
                return;

            var vanillaTopLeft = __instance.transform.parent.Find("TopLeftMessage");
            if (vanillaTopLeft != null && !vanillaTopLeft.IsChildOf(__instance.transform) && vanillaTopLeft.GetComponentInChildren<AugaTopLeftMessageController>(true) == null)
            {
                Auga.Log("Removing the vanilla TopLeftMessage canvas; the Auga message HUD has its own log.");
                Object.Destroy(vanillaTopLeft.gameObject);
            }
        }

        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Awake))]
        [HarmonyPostfix]
        public static void MessageHud_Awake_Postfix(MessageHud __instance)
        {
            if (__instance == null || !__instance.name.StartsWith("Auga"))
                return;

            var controller = __instance.GetComponent<AugaTopLeftMessageController>();
            if (controller && controller.LogContainer != null && controller.LogContainer.parent != null)
                controller.LogContainer.parent.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.UpperLeft, 55, -115);
            if (__instance.m_messageCenterText != null)
                __instance.m_messageCenterText.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.MiddleCenter, 0, 150);
        }

        /// <summary>
        /// MessageHud.OnDestroy clears the static instance unconditionally; the vanilla object is destroyed one
        /// frame after the Auga one claimed the instance, which would leave MessageHud.instance null.
        /// </summary>
        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.OnDestroy))]
        public static class MessageHud_OnDestroy_Patch
        {
            public static void Prefix(MessageHud __instance, out MessageHud __state)
            {
                var current = MessageHud.instance;
                __state = current != null && current != __instance ? current : null;
            }

            public static void Postfix(MessageHud __state)
            {
                if (__state != null && MessageHud.instance == null)
                {
                    MessageHud.m_instance = __state;
                }
            }
        }

        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.ShowMessage))]
        [HarmonyPostfix]
        public static void MessageHud_ShowMessage_Postfix(MessageHud __instance, MessageHud.MessageType type, string text, int amount, Sprite icon)
        {
            if (Hud.IsUserHidden())
            {
                return;
            }

            text = Localization.instance.Localize(text);
            if (type == MessageHud.MessageType.TopLeft)
            {
                var controller = __instance.GetComponent<AugaTopLeftMessageController>();
                if (controller != null)
                {
                    controller.AddMessage(text, icon, amount);
                }
            }
        }
    }
}
