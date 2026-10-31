using TMPro;
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
                if (SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.EnemyHud, "EnemyHud"))
                    return false;

                // This is Auga's copy, before vanilla's Awake and every other mod's postfix look at it.
                UseVanillaStarNames(__instance.transform);
                UseVanillaMountNames(__instance.m_baseHudMount);
                EnemyHudBars.Apply(__instance);
                return true;
            }

            // Vanilla's star is a dark star with the lit one inside it, called "star (1)". Auga's is a holder with only
            // the lit star inside, called "star". StarLevelSystem clones the level_2 star and reads both images, and
            // its EnemyHud.Awake postfix throwing on the missing child left Unity's EnemyHud disabled, with no health
            // bars at all. The holder gets a disabled Image to stand in for the dark star, so nothing new is drawn, and
            // the lit star is centred at a fixed size like vanilla's instead of stretched over the holder, so a mod that
            // sizes the two separately gets vanilla's picture. The star layout groups don't size their holders, so
            // Auga's own stars look the same.
            private static void UseVanillaStarNames(Transform root)
            {
                foreach (var star in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (star.name != "star")
                        continue;
                    var lit = star.Find("star") as RectTransform;
                    if (lit == null)
                        continue;
                    lit.name = "star (1)";
                    lit.anchorMin = lit.anchorMax = lit.pivot = new Vector2(0.5f, 0.5f);
                    lit.anchoredPosition = Vector2.zero;
                    lit.sizeDelta = star.sizeDelta;
                    if (star.GetComponent<Image>() == null)
                        star.gameObject.AddComponent<Image>().enabled = false;
                }
            }

            // The mount hud predates vanilla's stamina bar name and number labels. Vanilla's ShowHud throws without
            // them every frame while riding, before it reaches UpdateHuds, so every enemy bar disappeared on a mount.
            // The labels stay hidden: Auga's mount bars have no numbers.
            private static void UseVanillaMountNames(GameObject mountHud)
            {
                if (mountHud == null)
                    return;
                var stamina = mountHud.transform.Find("Stamina");
                if (stamina != null && stamina.Find("stamina_fast") == null)
                {
                    var bar = stamina.Find("health_fast");
                    if (bar != null)
                        bar.name = "stamina_fast";
                    // Vanilla only drives the fast stamina bar, so the slow one would stay full behind it.
                    var trail = stamina.Find("health_slow");
                    if (trail != null)
                        trail.gameObject.SetActive(false);
                }
                AddHiddenText(stamina, "StaminaText");
                AddHiddenText(mountHud.transform.Find("Health"), "HealthText");
            }

            private static void AddHiddenText(Transform parent, string name)
            {
                if (parent == null || parent.Find(name) != null)
                    return;
                var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                text.transform.SetParent(parent, false);
                text.SetActive(false);
            }
        }

        // Vanilla shows only the level_2 and level_3 star rows. Auga's hud also has level_4 to level_6, and level_X
        // with a count for anything higher, for creatures a spawn mod or the spawn command takes past level 3. Creature
        // Level Control and StarLevelSystem draw those stars themselves, so with either installed they are left to it.
        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.ShowHud))]
        public static class EnemyHud_ShowHud_Patch
        {
            private const int FirstExtraStarLevel = 4;
            private const int LastStarRowLevel = 6;

            // ShowHud is called every frame for every creature in range; only the call that creates the hud sets it up
            public static void Prefix(EnemyHud __instance, Character c, out bool __state)
            {
                __state = c != null && !__instance.m_huds.ContainsKey(c);
            }

            public static void Postfix(EnemyHud __instance, Character c, bool __state)
            {
                if (!__state || Auga.HasLevelStarMod || !__instance.m_huds.TryGetValue(c, out var hud) || hud == null || hud.m_gui == null)
                    return;

                var level = c.GetLevel();
                if (level < FirstExtraStarLevel)
                    return;

                // the rows start hidden in every new hud
                var gui = hud.m_gui.transform;
                if (level <= LastStarRowLevel)
                {
                    var row = gui.Find($"level_{level}");
                    if (row != null)
                        row.gameObject.SetActive(true);
                    return;
                }

                var rowX = gui.Find("level_X");
                if (rowX == null)
                    return;
                var count = rowX.GetComponentInChildren<TMP_Text>(true);
                if (count != null)
                    count.text = $"x {level - 1}";
                rowX.gameObject.SetActive(true);
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

                        if (hud.Key.IsBoss())
                        {
                            EnemyHudBars.SyncBossFill(hud.Value.m_healthFast);
                            EnemyHudBars.SyncBossFill(hud.Value.m_healthSlow);
                            EnemyHudBars.SyncBossFill(hud.Value.m_healthFastFriendly);
                        }
                    }
                }
            }
        }
    }
}
