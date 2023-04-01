using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch]
    public static class EnemyHud_Setup
    {
        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.Awake))]
        public static class EnemyHud_Awake_Patch
        {
            public static bool Prefix(EnemyHud __instance)
            {
                return !SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.EnemyHud, "EnemyHud");
            }
        }

        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.ShowHud))]
        public static class EnemyHud_ShowHud_Patch
        {
            public static void Postfix(EnemyHud __instance, Character c)
            {
                if (c == null || __instance.m_huds.TryGetValue(c, out EnemyHud.HudData _))
                {
                    return;
                }

                var hud = __instance.m_huds.LastOrDefault();
                if (hud.Key != null && hud.Value != null)
                {
                    const int maxLevelForStarDisplays = 6;
                    const int firstStarDisplayLevel = 2;
                    const int lastStarDisplayLevel = 3;

                    var level = c.GetLevel();
                    if (level > lastStarDisplayLevel)
                    {
                        var hudGui = hud.Value.m_gui;
                        for (var i = firstStarDisplayLevel; i <= maxLevelForStarDisplays; i++)
                        {
                            var levelDisplay = hudGui.transform.Find($"level_{level}");
                            if (levelDisplay != null)
                            {
                                levelDisplay.gameObject.SetActive(i == level);
                            }
                        }

                        var levelDisplayX = hudGui.transform.Find("level_X");
                        if (levelDisplayX)
                        {
                            var useExtendedLevel = level > maxLevelForStarDisplays;
                            if (useExtendedLevel)
                            {
                                var levelXDisplayName = $"level_{level}";
                                var newLevelXDisplay = hudGui.transform.Find(levelXDisplayName);
                                if (newLevelXDisplay == null)
                                {
                                    newLevelXDisplay = Object.Instantiate(levelDisplayX, levelDisplayX.parent, false);
                                    newLevelXDisplay.name = levelXDisplayName;
                                }

                                var text = levelDisplayX.GetComponentInChildren<Text>();
                                text.text = $"x {level - 1}";
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.UpdateHuds))]
        [HarmonyAfter("org.bepinex.plugins.creaturelevelcontrol")]
        public static class EnemyHud_UpdateHuds_Patch
        {
            public static void Postfix(EnemyHud __instance)
            {
                foreach (var hud in __instance.m_huds)
                {
                    if (hud.Key != null && hud.Value != null && hud.Value.m_gui != null)
                    {
                        var name = hud.Value.m_gui.transform.Find("Name");
                        if (name != null)
                        {
                            var rt = (RectTransform)name;
                            rt.anchoredPosition = new Vector2(0, 38.5f);
                        }
                    }
                }
            }
        }
    }
}
