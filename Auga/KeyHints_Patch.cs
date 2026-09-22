using Auga.Utilities;
using HarmonyLib;

namespace Auga
{
    /// <summary>
    /// Auga replaces hudroot/KeyHints with its own prefab (see Hud_Setup). That prefab predates the radial menu,
    /// bow-draw and build-menu hint objects the current KeyHints class expects (m_radialKeyHints is used without a
    /// null check in Awake). When the Auga instance awakes, the vanilla instance is still alive (its destruction is
    /// deferred), so adopt every serialized object the Auga prefab lacks from it. The localized key texts are
    /// skipped: the game null-checks those, and moving them would leave stray labels in the Auga panel.
    /// </summary>
    [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.Awake))]
    public static class KeyHints_Awake_Patch
    {
        [HarmonyPriority(Priority.First)]
        public static void Prefix(KeyHints __instance)
        {
            var vanilla = KeyHints.instance;
            if (vanilla == null || vanilla == __instance)
            {
                return;
            }

            SerializedFieldHelper.CopyMissingFields(__instance, vanilla, __instance.transform,
                nameof(KeyHints.m_buildMenuKey), nameof(KeyHints.m_buildRotateKey), nameof(KeyHints.m_buildAlternativePlacingKey),
                nameof(KeyHints.m_dodgeKey), nameof(KeyHints.m_cycleSnapKey));
        }
    }

    /// <summary>
    /// KeyHints.OnDestroy clears the static instance unconditionally. The vanilla object is destroyed one frame
    /// after the Auga replacement awoke and claimed the instance, which would leave KeyHints.instance null for
    /// the rest of the session (Settings uses it). Keep the live instance.
    /// </summary>
    [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.OnDestroy))]
    public static class KeyHints_OnDestroy_Patch
    {
        public static void Prefix(KeyHints __instance, out KeyHints __state)
        {
            var current = KeyHints.instance;
            __state = current != null && current != __instance ? current : null;
        }

        public static void Postfix(KeyHints __state)
        {
            if (__state != null && KeyHints.instance == null)
            {
                KeyHints.m_instance = __state;
            }
        }
    }
}
