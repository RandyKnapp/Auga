using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using TMPro;
using ValheimAutoTest;

namespace AugaAutoStart
{
    // Test-only helper: Auga's steps on top of the shared ValheimAutoTest harness (..\..\..\ValheimAutoTest), which
    // starts the game, loads the character and world, runs these steps, writes summary.txt and quits. The harness's
    // own settings are VALHEIM_TEST_*; the four below are Auga's older names for them, still read, and supply Auga's
    // defaults when neither is set. Controlled by environment variables:
    //   AUGA_TEST_CHARACTER  profile filename (default "auga test")                      = VALHEIM_TEST_CHARACTER
    //   AUGA_TEST_WORLD      world name       (default "AugaAutoTest")                   = VALHEIM_TEST_WORLD
    //   AUGA_TEST_SHOTS      folder for screenshots + hierarchy dumps (default %TEMP%\augashots) = VALHEIM_TEST_SHOTS
    //   AUGA_TEST_QUIT       seconds after the UI exercise to quit the game (default 0 = never)  = VALHEIM_TEST_QUIT
    //   AUGA_TEST_DUMP       "1" to dump the vanilla UI hierarchies (before any Awake patch runs)
    //   AUGA_TEST_ROWS       also screenshot the inventory with this many player rows and the container panel shown
    //   AUGA_TEST_EFFECTS    also screenshot the player panel's Active Effects with this many status effects ("11", or "11,24" for several), and check every status effect list showing has one icon per effect
    //   AUGA_TEST_SETTINGS   "1": screenshot every settings tab from the main menu and quit without starting a level
    //   AUGA_TEST_CHARSELECT "1": screenshot the character selection, the new character screen, the manage saves dialog and the remove dialog, then quit
    //   AUGA_TEST_STARTGAME  "1": screenshot the world list, the new world dialog, the server list and the add server dialog, then quit
    //   AUGA_TEST_EPICLOOT_HASAUGA "1": force EpicLoot.HasAuga = true as soon as EpicLoot is loaded (its Auga branches are otherwise off)
    //   AUGA_TEST_ENCHANTING "1": spawn an EpicLoot enchanting table, open its UI and screenshot every tab (after the magic items)
    //   AUGA_TEST_MENUTOUR   "1": also open the changelog, user agreement, credits, settings and cinematics from the main menu (default: only start the game)
    //   AUGA_TEST_MAGIC      "1": spawn EpicLoot magic items of every rarity (magicitem console command), pick them up and screenshot each one's inventory tooltip; or "Rarity:Item;Set:SetID;..." to choose them
    //   AUGA_TEST_CROSSPLAY  "1": host the test world as a password-protected crossplay server (it gets a join code) and log the join code overlay's timeline
    //   AUGA_TEST_ENEMIES    "1": spawn creatures of several levels and a boss, screenshot their health bars full, hurt and nearly empty (after the damage step); "only": just that, right after the first HUD screenshot
    //   AUGA_TEST_STATBARS   "1": give the health bar a fixed length, eat three foods and let two wear off, screenshot each and check the ticks' positions (at the end); "only": just that (with AUGA_TEST_ENEMIES=only as well, both)
    //   AUGA_TEST_FAVORITES  "1": mark test items with Item Favorite Framework (favorite, trash, favorite slots, a custom mark, the wishlist) and screenshot the inventory and their tooltips
    //   AUGA_TEST_LINKS      other mods' links in the tab row under the player name, by link ID ("AugaExample_Link,Clans_Button"): screenshot the row, click each link (and again, to close what it opened) and hide the first one for a moment
    // A missing AUGA_TEST_CHARACTER is created as a new local character.
    [BepInPlugin("augatest.autostart", "Auga AutoStart (test)", "0.2.0")]
    [BepInDependency(AutoTest.Guid)]
    public class Plugin : BaseUnityPlugin
    {
        private static string ShotDir => AutoTest.ShotDir;
        private float _nextSample;
        private static readonly Dictionary<string, string> _lastOverlays = new Dictionary<string, string>();
        private static float _playerSeen = -1f;

        private void Awake()
        {
            // the harness reads its settings only once the main menu is up, after this
            foreach (var (auga, harness, fallback) in new[]
                     {
                         ("AUGA_TEST_CHARACTER", "VALHEIM_TEST_CHARACTER", "auga test"),
                         ("AUGA_TEST_WORLD", "VALHEIM_TEST_WORLD", "AugaAutoTest"),
                         ("AUGA_TEST_SHOTS", "VALHEIM_TEST_SHOTS", Path.Combine(Path.GetTempPath(), "augashots")),
                         ("AUGA_TEST_QUIT", "VALHEIM_TEST_QUIT", ""),
                     })
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(harness))) continue;
                var value = Environment.GetEnvironmentVariable(auga);
                Environment.SetEnvironmentVariable(harness, string.IsNullOrEmpty(value) ? fallback : value);
            }
            // the seed Auga's test world was made with, for a machine that does not have it yet
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VALHEIM_TEST_SEED"))) Environment.SetEnvironmentVariable("VALHEIM_TEST_SEED", "AugaAutoTest");
            if (Environment.GetEnvironmentVariable("AUGA_TEST_CROSSPLAY") == "1")
            {
                Environment.SetEnvironmentVariable("VALHEIM_TEST_HOST", "crossplay");
                Environment.SetEnvironmentVariable("VALHEIM_TEST_PASSWORD", "augatest1");
            }
            AutoTest.AddMenuStep("auga main menu", MenuScreens);
            AutoTest.AddWorldStep("auga ui", Exercise);

            if (Environment.GetEnvironmentVariable("AUGA_TEST_DUMP") == "1")
            {
                var harmony = new Harmony("augatest.autostart");
                var prefix = new HarmonyMethod(typeof(Plugin), nameof(DumpPrefix)) { priority = Priority.First };
                foreach (var t in new[] { typeof(InventoryGui), typeof(Menu), typeof(Hud), typeof(KeyHints), typeof(Chat), typeof(Minimap), typeof(FejdStartup), typeof(StoreGui), typeof(TextViewer), typeof(MessageHud), typeof(EnemyHud), typeof(DamageText), typeof(TextInput), typeof(Settings) })
                {
                    var awake = AccessTools.Method(t, "Awake") ?? AccessTools.Method(t, "Start");
                    if (awake != null) harmony.Patch(awake, prefix: prefix);
                }
                Logger.LogInfo("hierarchy dumps enabled");
            }
            if (Environment.GetEnvironmentVariable("AUGA_TEST_CROSSPLAY") == "1")
            {
                // the join code overlay's timeline: every show / hide of it and of the pause menu, with the time
                var harmony = new Harmony("augatest.joincode");
                harmony.Patch(AccessTools.Method(typeof(JoinCode), "Activate"), postfix: new HarmonyMethod(typeof(Plugin), nameof(LogJoinCodeActivate)));
                harmony.Patch(AccessTools.Method(typeof(JoinCode), "Deactivate"), postfix: new HarmonyMethod(typeof(Plugin), nameof(LogJoinCodeDeactivate)));
                harmony.Patch(AccessTools.Method(typeof(Menu), "Show"), postfix: new HarmonyMethod(typeof(Plugin), nameof(LogMenuShow)));
                harmony.Patch(AccessTools.Method(typeof(Menu), "Hide"), postfix: new HarmonyMethod(typeof(Plugin), nameof(LogMenuHide)));
            }
            Logger.LogInfo($"AugaAutoStart ready, screenshots -> {ShotDir}");
        }

        public static void LogJoinCodeActivate(JoinCode __instance, bool firstSpawn) => Debug.Log($"[AugaAutoStart] t={Time.realtimeSinceStartup:F1}s JoinCode.Activate(firstSpawn={firstSpawn}) code='{__instance.m_joinCode}' root active={__instance.m_root.activeSelf}");
        public static void LogJoinCodeDeactivate() => Debug.Log($"[AugaAutoStart] t={Time.realtimeSinceStartup:F1}s JoinCode.Deactivate");
        public static void LogMenuShow() => Debug.Log($"[AugaAutoStart] t={Time.realtimeSinceStartup:F1}s Menu.Show");
        public static void LogMenuHide() => Debug.Log($"[AugaAutoStart] t={Time.realtimeSinceStartup:F1}s Menu.Hide");

        // Runs before any other Awake prefix/postfix: writes the untouched prefab hierarchy of the instance.
        public static void DumpPrefix(object __instance)
        {
            try
            {
                var c = __instance as Component;
                if (c == null) return;
                var sb = new StringBuilder();
                Dump(c.transform, 0, sb);
                var path = Path.Combine(ShotDir, "hier_" + __instance.GetType().Name + ".txt");
                File.WriteAllText(path, sb.ToString());
                Debug.Log($"[AugaAutoStart] dumped {path}");
            }
            catch (Exception e) { Debug.LogError("[AugaAutoStart] dump failed: " + e); }
        }

        private static void Dump(Transform t, int depth, StringBuilder sb)
        {
            sb.Append(' ', depth * 2).Append(t.name);
            var comps = t.GetComponents<Component>();
            sb.Append("  [");
            for (var i = 0; i < comps.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(comps[i] == null ? "<missing>" : comps[i].GetType().Name);
            }
            sb.Append(']');
            if (!t.gameObject.activeSelf) sb.Append("  (inactive)");
            sb.AppendLine();
            for (var i = 0; i < t.childCount; i++) Dump(t.GetChild(i), depth + 1, sb);
        }

        private bool _epicLootFlagDone;

        /// <summary>AUGA_TEST_EPICLOOT_HASAUGA=1: EpicLoot never assigns its HasAuga flag, so its Auga code paths can only be exercised by forcing it.</summary>
        private void ForceEpicLootHasAuga()
        {
            if (_epicLootFlagDone) return;
            if (Environment.GetEnvironmentVariable("AUGA_TEST_EPICLOOT_HASAUGA") != "1") { _epicLootFlagDone = true; return; }
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "EpicLoot");
            if (assembly == null) return;   // not loaded yet
            _epicLootFlagDone = true;
            var field = assembly.GetType("EpicLoot.EpicLoot")?.GetField("HasAuga", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (field == null) { Debug.LogWarning("[AugaAutoStart] EpicLoot.EpicLoot.HasAuga not found"); return; }
            field.SetValue(null, true);
            Debug.Log("[AugaAutoStart] EpicLoot.HasAuga forced to true");
        }

        private void Update()
        {
            ForceEpicLootHasAuga();
            if (Player.m_localPlayer != null && _playerSeen < 0f) _playerSeen = Time.realtimeSinceStartup;
            if (Player.m_localPlayer != null && _playerSeen >= 0f && Time.realtimeSinceStartup - _playerSeen < 45f && Time.realtimeSinceStartup >= _nextSample)
            {
                // overlay monitor: log every big screen-covering graphic that appears or disappears after spawn
                _nextSample = Time.realtimeSinceStartup + 0.5f;
                Try(SampleOverlays);
            }
        }

        /// <summary>
        /// The manage saves list loads in several asynchronous steps (cloud usage, save cache, list rows, tab switch),
        /// each behind the "please wait" sign with gaps in between: done once the sign has stayed hidden for 1.5 s.
        /// </summary>
        private static IEnumerator WaitForSaveList(FejdStartup fejd)
        {
            var menu = fejd.m_manageSavesMenu;
            var pleaseWait = menu != null && menu.pleaseWait != null ? menu.pleaseWait : null;
            var quiet = 0;
            for (var i = 0; i < 60 && quiet < 3; i++)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                var waiting = menu != null && (menu.pleaseWaitCount > 0 || (pleaseWait != null && pleaseWait.activeInHierarchy));
                quiet = waiting ? 0 : quiet + 1;
                var tabs = menu != null ? menu.GetComponentInChildren<TabHandler>(true) : null;
                Debug.Log($"[AugaAutoStart] manage saves at {(i + 1) * 0.5f:F1}s: sign={(pleaseWait != null ? pleaseWait.name + " self=" + pleaseWait.activeSelf + " hier=" + pleaseWait.activeInHierarchy : "missing")} count={(menu != null ? menu.pleaseWaitCount : -1)} tab={(tabs != null ? tabs.GetActiveTab() : -1)} rows={(menu != null && menu.listRoot != null ? menu.listRoot.childCount : -1)}");
            }
        }

        /// <summary>Logs the average frame rate over two seconds of real time.</summary>
        private static IEnumerator SampleFrameRate(string what)
        {
            var frames = 0;
            var start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 2f)
            {
                frames++;
                yield return null;
            }
            var seconds = Time.realtimeSinceStartup - start;
            Debug.Log($"[AugaAutoStart] frame rate on {what}: {frames / seconds:F1} fps ({frames} frames in {seconds:F2}s)");
        }

        /// <summary>Clicks a button and logs the GUI sound objects alive a moment later (they destroy themselves after playing).</summary>
        private static IEnumerator ClickAndCountSounds(string what, Button button)
        {
            yield return new WaitForSecondsRealtime(1.5f);   // let earlier sounds finish
            var before = new HashSet<int>(UnityEngine.Object.FindObjectsOfType<ZSFX>().Select(s => s.GetInstanceID()));
            Try(() => button.onClick.Invoke());
            yield return null;
            yield return new WaitForSecondsRealtime(0.1f);
            var sounds = UnityEngine.Object.FindObjectsOfType<ZSFX>().Where(s => !before.Contains(s.GetInstanceID())).Select(s => s.name).ToList();
            Debug.Log($"[AugaAutoStart] new sounds after {what}: {sounds.Count} ({string.Join(", ", sounds)}); {before.Count} older sound objects were alive");
        }

        public static void Shot(string name) => AutoTest.Shot(name);

        /// <summary>
        /// The main menu step: the single-screen modes (AUGA_TEST_STARTGAME, AUGA_TEST_CHARSELECT, AUGA_TEST_SETTINGS)
        /// end the run here; AUGA_TEST_MENUTOUR walks the main menu's dialogs, and the harness then starts the game.
        /// </summary>
        private static IEnumerator MenuScreens(FejdStartup fejd)
        {
            var character = AutoTest.Character;

            if (Environment.GetEnvironmentVariable("AUGA_TEST_STARTGAME") == "1")
            {
                // AUGA_TEST_STARTGAME=1: the start game screen only (world list, new world, server list, add server), then quit
                Debug.Log("[AugaAutoStart] OnStartGame (start game only)");
                Try(() => fejd.OnStartGame());
                yield return new WaitForSecondsRealtime(5f);
                Try(() => fejd.SetSelectedProfile(character));
                Try(() => fejd.OnCharacterStart());
                yield return new WaitForSecondsRealtime(2f);
                Shot("02_worldselect");
                yield return new WaitForSecondsRealtime(0.5f);
                Try(() => LogRects(fejd.m_startGamePanel.transform, "WorldPanel"));
                Try(() => fejd.OnWorldNew());
                yield return new WaitForSecondsRealtime(1f);
                Shot("02b_newworld");
                yield return new WaitForSecondsRealtime(0.5f);
                Try(() => fejd.OnNewWorldBack());
                yield return new WaitForSecondsRealtime(0.5f);
                Try(() => fejd.OnServerOptions());
                yield return new WaitForSecondsRealtime(1f);
                Try(() => { if (UnifiedPopup.IsVisible()) UnifiedPopup.Pop(); }); // the first-use disclaimer
                yield return new WaitForSecondsRealtime(0.5f);
                Shot("02e_worldmodifiers");
                yield return new WaitForSecondsRealtime(0.5f);
                Try(() => LogRects(fejd.m_serverOptions.transform, "panel"));
                Try(() => fejd.OnServerOptionsCancel());
                yield return new WaitForSecondsRealtime(0.5f);
                Debug.Log("[AugaAutoStart] join tab");
                Try(() => fejd.m_startGamePanel.transform.GetChild(0).GetComponent<TabHandler>().m_tabs[1].m_button.onClick.Invoke());
                yield return new WaitForSecondsRealtime(4f); // the server lists refresh
                Shot("02c_serverlist");
                yield return new WaitForSecondsRealtime(0.5f);
                Try(() => LogRects(fejd.m_startGamePanel.transform, "JoinPanel"));
                Try(() =>
                {
                    var gui = fejd.m_serverListPanel.GetComponentInChildren<ServerListGui>(true);
                    gui.SetSelectedServer(0, true);
                    gui.OnRemoveServerButton();
                });
                yield return new WaitForSecondsRealtime(1f);
                Shot("02c2_removeserver");
                yield return new WaitForSecondsRealtime(0.5f);
                Try(() => fejd.m_startGamePanel.transform.Find("RemoveServerDialog/ButtonNo").GetComponent<Button>().onClick.Invoke());
                yield return new WaitForSecondsRealtime(0.5f);
                Try(() => fejd.m_serverListPanel.transform.Find("AddServer").GetComponent<Button>().onClick.Invoke());
                yield return new WaitForSecondsRealtime(1f);
                Shot("02d_addserver");
                yield return new WaitForSecondsRealtime(0.5f);
                Try(() => fejd.m_startGamePanel.transform.Find("JoinIP/Cancel").GetComponent<Button>().onClick.Invoke());
                yield return new WaitForSecondsRealtime(0.5f);
                Try(() => fejd.OnStartGameBack());
                yield return new WaitForSecondsRealtime(1f);
                AutoTest.Finish("start game screens only (AUGA_TEST_STARTGAME)", quit: true);
                yield break;
            }

            if (Environment.GetEnvironmentVariable("AUGA_TEST_CHARSELECT") == "1")
            {
                // AUGA_TEST_CHARSELECT=1: only the character selection (list, portraits, remove dialog), then quit
                Debug.Log("[AugaAutoStart] OnStartGame (character select only)");
                Try(() => fejd.OnStartGame());
                yield return new WaitForSecondsRealtime(5f); // the portrait booth photographs profiles without a current portrait first
                yield return SampleFrameRate("character select");
                Shot("01_characterselect");
                Try(() => LogRects(fejd.m_selectCharacterPanel.transform, "Panel"));
                yield return new WaitForSecondsRealtime(1f);
                Debug.Log("[AugaAutoStart] OnCharacterNew");
                Try(() => fejd.OnCharacterNew());
                yield return new WaitForSecondsRealtime(3f); // the portrait grid renders over a few frames
                yield return SampleFrameRate("new character (hair)");
                Shot("01e_newcharacter_hair");
                Try(() => LogRects(fejd.m_newCharacterPanel.transform, "Panel"));
                Try(() => fejd.m_csNewCharacterName.text = "Auga Probe");
                Try(() =>
                {
                    var female = fejd.m_newCharacterPanel.transform.Find("Panel/Content/ToggleGroup/Toggle_Female")?.GetComponent<Toggle>();
                    if (female != null) female.isOn = true;
                });
                Try(() => fejd.m_newCharacterError.SetActive(true)); // the "name exists" label, for its look only
                yield return new WaitForSecondsRealtime(1.5f);
                Shot("01f_newcharacter_female_named");
                yield return new WaitForSecondsRealtime(0.5f); // the capture happens at the end of the frame
                Try(() => fejd.m_newCharacterError.SetActive(false));
                Try(() => fejd.m_newCharacterPanel.GetComponentInChildren<TabHandler>(true).m_tabs[1].m_button.onClick.Invoke());
                yield return new WaitForSecondsRealtime(2f);
                yield return SampleFrameRate("new character (beard)");
                Shot("01g_newcharacter_beard");
                yield return new WaitForSecondsRealtime(0.5f);
                Try(() => fejd.OnNewCharacterCancel());
                yield return new WaitForSecondsRealtime(1.5f);
                Debug.Log("[AugaAutoStart] OnManageSaves (characters)");
                Try(() => fejd.OnManageSaves(1));
                yield return WaitForSaveList(fejd);
                Shot("01c_managesaves_characters");
                yield return new WaitForSecondsRealtime(0.5f); // the capture happens at the end of the frame
                Try(() => LogRects(fejd.m_manageSavesMenu.transform, "Panel"));
                Try(() => LogRects(fejd.m_manageSavesMenu.transform, "SaveList"));
                // the worlds tab, through its button so the TabHandler switches too
                Try(() => fejd.m_manageSavesMenu.GetComponentInChildren<TabHandler>(true).m_tabs[0].m_button.onClick.Invoke());
                yield return WaitForSaveList(fejd);
                Shot("01d_managesaves_worlds");
                yield return new WaitForSecondsRealtime(1f); // the capture happens at the end of the frame
                Try(() => fejd.m_manageSavesMenu.Close());
                yield return new WaitForSecondsRealtime(1f);
                Try(() => fejd.OnCharacterRemove());
                yield return new WaitForSecondsRealtime(1f);
                Shot("01b_removecharacter");
                yield return new WaitForSecondsRealtime(1f);
                Try(() => fejd.OnButtonRemoveCharacterNo());
                yield return new WaitForSecondsRealtime(1f);
                Try(() => fejd.OnSelelectCharacterBack());
                yield return new WaitForSecondsRealtime(1f);
                AutoTest.Finish("character select screens only (AUGA_TEST_CHARSELECT)", quit: true);
                yield break;
            }

            // AUGA_TEST_MENUTOUR=1: also walk the main menu dialogs (changelog, user agreement, credits, settings,
            // cinematics) before starting the game; the default run touches nothing in the main menu but Start.
            if (Environment.GetEnvironmentVariable("AUGA_TEST_MENUTOUR") == "1")
            {
                Debug.Log("[AugaAutoStart] open changelog");
                Try(() => fejd.OnButtonShowChangelog());
                yield return new WaitForSecondsRealtime(1.5f);
                Shot("00d_changelog");
                Try(() => LogRects(fejd.transform, "AugaChangeLog"));
                yield return new WaitForSecondsRealtime(1f);
                Try(() => fejd.OnButtonShowChangelog());
                yield return new WaitForSecondsRealtime(1f);

                Debug.Log("[AugaAutoStart] open user agreement");
                Try(() => fejd.OnButtonEula());
                yield return new WaitForSecondsRealtime(1.5f);
                Shot("00e_eula");
                Try(() => LogRects(fejd.m_eulaWindow.transform, "Popup"));
                yield return new WaitForSecondsRealtime(1f);
                Try(() => fejd.m_eulaWindow.AcceptButton());
                yield return new WaitForSecondsRealtime(1f);

                Debug.Log("[AugaAutoStart] open credits");
                Try(() => fejd.OnCredits());
                yield return new WaitForSecondsRealtime(12f); // the list scrolls in from below
                Shot("00f_credits");
                yield return new WaitForSecondsRealtime(1f);
                Try(() => fejd.OnCreditsBack());
                yield return new WaitForSecondsRealtime(1f);

                Debug.Log("[AugaAutoStart] open settings (main menu)");
                Try(() => fejd.OnButtonSettings());
                yield return new WaitForSecondsRealtime(2f);
                Shot("00b_settings");
                Try(() => DumpSettingsTexts("mainmenu"));
                DumpLive("Settings", Settings.instance);
                var settingsOnly = Environment.GetEnvironmentVariable("AUGA_TEST_SETTINGS") == "1";
                if (settingsOnly && Settings.instance != null)
                {
                    // AUGA_TEST_SETTINGS=1: screenshot every settings tab, then quit without starting a level
                    var handler = Settings.instance.m_tabHandler;
                    var count = handler != null ? handler.m_tabs.Count : 0;
                    for (var i = 0; i < count; i++)
                    {
                        var tab = handler.m_tabs[i];
                        if (tab.m_button == null || !tab.m_button.gameObject.activeSelf) continue;
                        var name = tab.m_page != null ? tab.m_page.name : i.ToString();
                        Debug.Log("[AugaAutoStart] settings tab " + name);
                        Try(() => handler.SetActiveTab(i));
                        yield return new WaitForSecondsRealtime(1f);
                        Shot("00b_settings_" + i + "_" + name);
                        Try(() => LogRects(Settings.instance.transform, "Bottom"));
                        yield return new WaitForSecondsRealtime(1f);
                    }
                    yield return new WaitForSecondsRealtime(1f);
                    Try(() => { var s = Settings.instance; if (s != null) s.OnBack(); });
                    yield return new WaitForSecondsRealtime(1f);
                    AutoTest.Finish("settings tabs only (AUGA_TEST_SETTINGS)", quit: true);
                    yield break;
                }
                Try(() => { var s = Settings.instance; if (s != null) s.OnBack(); });
                yield return new WaitForSecondsRealtime(1f);

                Debug.Log("[AugaAutoStart] open cinematics menu");
                Try(() => fejd.OnCinematics());
                yield return new WaitForSecondsRealtime(1f);
                Shot("00c_cinematics");
                yield return new WaitForSecondsRealtime(1f);
                Try(() => fejd.OnCinematicsBack());
                yield return new WaitForSecondsRealtime(1f);

            }
            // the harness goes on: character select, world, spawn, then Exercise below (AUGA_TEST_CROSSPLAY has it
            // host the world as a password-protected crossplay server, which gives it a join code)
        }

        private static void LogSettingsRenderState(string tag)
        {
            var s = Settings.instance;
            if (s == null) { Debug.Log($"[AugaAutoStart] render[{tag}] no settings instance"); return; }
            Debug.Log($"[AugaAutoStart] render[{tag}] screen={Screen.width}x{Screen.height} settingsRootWorld={s.transform.position} lossy={s.transform.lossyScale} parentWorld={s.transform.parent.position}");
            var menu = Menu.instance;
            if (menu != null && menu.m_root != null)
                Debug.Log($"[AugaAutoStart] render[{tag}] menuRootWorld={menu.m_root.position} lossy={menu.m_root.lossyScale}");
            foreach (var t in s.transform.GetComponentsInParent<Transform>(true))
                Debug.Log($"[AugaAutoStart] render[{tag}] ancestor {t.name} local={t.localPosition} scale={t.localScale} active={t.gameObject.activeSelf}");
            var graphics = s.GetComponentsInChildren<Graphic>(false);
            var shown = 0;
            foreach (var g in graphics)
            {
                var cr = g.canvasRenderer;
                if (shown < 6 && (g.name == "Background" || g.name == "Darken" || g.transform == s.transform || g is TMP_Text))
                {
                    shown++;
                    Debug.Log($"[AugaAutoStart] render[{tag}] {PathOf(g.transform)} type={g.GetType().Name} enabled={g.isActiveAndEnabled} world={g.transform.position} color={g.color} inheritedAlpha={cr.GetInheritedAlpha()} cull={cr.cull} mats={cr.materialCount} mat={(cr.materialCount > 0 && cr.GetMaterial(0) != null ? cr.GetMaterial(0).shader.name : "none")} depth={cr.absoluteDepth} clip={cr.hasRectClipping} popMats={cr.popMaterialCount} rect={((RectTransform)g.transform).rect}");
                }
            }
            Debug.Log($"[AugaAutoStart] render[{tag}] active graphics under settings: {graphics.Length}");
        }

        /// <summary>Logs the placement of every object with the given name under root, plus two levels of children.</summary>
        private static void LogRects(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (t.name != name || !t.gameObject.activeInHierarchy) continue;
                LogRect(t, 0);
                foreach (RectTransform child in t)
                {
                    LogRect(child, 1);
                    foreach (RectTransform grandChild in child) LogRect(grandChild, 2);
                }
            }
        }

        private static readonly List<StatusEffect> TestStatusEffects = new List<StatusEffect>();

        /// <summary>
        /// AUGA_TEST_EFFECTS: tops the player's HUD status effects up to <paramref name="count"/> with plain stat effects
        /// (meads, guardian powers and the like), after taking back the ones a previous call added; 0 only takes them back.
        /// </summary>
        private static void SetTestStatusEffects(int count)
        {
            var seman = Player.m_localPlayer.GetSEMan();
            foreach (var se in TestStatusEffects) seman.RemoveStatusEffect(se, true);
            TestStatusEffects.Clear();
            var shown = new List<StatusEffect>();
            seman.GetHUDStatusEffects(shown);
            var candidates = ObjectDB.instance.m_StatusEffects
                .Where(se => se != null && se.m_icon != null && !se.m_hidden && se.GetType() == typeof(SE_Stats) && !seman.HaveStatusEffect(se))
                .OrderBy(se => se.name)
                .Take(Math.Max(0, count - shown.Count));
            foreach (var se in candidates)
            {
                // some of the game's own stat effects have a start effect without a prefab, which throws once the
                // effect is already on the player
                try { seman.AddStatusEffect(se, true); }
                catch (Exception e) { Debug.LogWarning($"[AugaAutoStart] adding status effect {se.name}: {e.Message}"); }
                if (seman.HaveStatusEffect(se)) TestStatusEffects.Add(se);
            }
            shown.Clear();
            seman.GetHUDStatusEffects(shown);
            Debug.Log($"[AugaAutoStart] status effects: asked {count}, added {TestStatusEffects.Count}, shown {shown.Count}");
        }

        /// <summary>The Active Effects grid of the player panel: its viewport, scale, columns and scrollbar.</summary>
        private static void LogActiveEffectsGrid()
        {
            var right = InventoryGui.instance.transform.Find("root/RightPanel");
            var grid = right != null ? right.GetComponentsInChildren<GridLayoutGroup>(true).FirstOrDefault(g => g.transform.parent != null && g.transform.parent.name == "Viewport") : null;
            if (grid == null) { Debug.Log("[AugaAutoStart] active effects grid: not found"); return; }
            var viewport = (RectTransform)grid.transform.parent;
            var scroll = viewport.GetComponent<ScrollRect>();
            Debug.Log($"[AugaAutoStart] active effects grid: cells={grid.transform.Cast<Transform>().Count(c => c.gameObject.activeSelf)} columns={grid.constraintCount} scrolls={(scroll != null && scroll.vertical)} scrollbar={(scroll != null && scroll.verticalScrollbar != null && scroll.verticalScrollbar.gameObject.activeSelf)}");
            LogRect(viewport, 1);
            LogRect((RectTransform)grid.transform, 2);
            if (scroll != null && scroll.verticalScrollbar != null) LogRect((RectTransform)scroll.verticalScrollbar.transform, 2);
        }

        /// <summary>
        /// Every Auga status effect list that is showing (the HUD's, the player panel's) has one icon per HUD status
        /// effect: the list looks the effects up and hands each icon its own.
        /// </summary>
        private static void CheckStatusEffectLists(string when)
        {
            var shown = new List<StatusEffect>();
            Player.m_localPlayer.GetSEMan().GetHUDStatusEffects(shown);
            var lists = 0;
            foreach (var root in new Component[] { Hud.instance, InventoryGui.instance })
            {
                if (root == null) continue;
                // by type name: this plugin does not reference Unity.Auga
                foreach (var list in root.GetComponentsInChildren<MonoBehaviour>(false).Where(c => c != null && c.GetType().Name == "AugaStatusEffects"))
                {
                    lists++;
                    var container = Traverse.Create(list).Field("EffectsContainer").GetValue<GameObject>();
                    var icons = container.transform.Cast<Transform>()
                        .Where(t => t.gameObject.activeSelf && t.GetComponents<MonoBehaviour>().Any(c => c != null && c.GetType().Name == "PlayerPanelEffectController"))
                        .ToList();
                    var texts = icons.Select(t => string.Join("/", t.GetComponentsInChildren<TMP_Text>(false).Select(x => x.text)));
                    Debug.Log($"[AugaAutoStart] status effects, {when}: {PathOf(list.transform)} shows {icons.Count} icons for {shown.Count} effects: {string.Join("; ", texts)}");
                    if (icons.Count != shown.Count)
                        AutoTest.Fail($"status effects, {when}: {PathOf(list.transform)} shows {icons.Count} icons for {shown.Count} effects");
                }
            }
            if (lists == 0)
                AutoTest.Fail($"status effects, {when}: no Auga status effect list is showing");
        }

        /// <summary>
        /// AugaBindingDisplay shows its binding again only when the binding changes. A copy of one of the key hints'
        /// key boxes (the test character may carry nothing that shows key hints) follows the Jump binding on the HUD;
        /// the binding is overridden, read back from the box, and put back.
        /// </summary>
        private static IEnumerator RebindCheck()
        {
            const string keyName = "Jump";
            GameObject copy = null;
            MonoBehaviour display = null;
            Try(() =>
            {
                bool IsDisplay(MonoBehaviour c) => c != null && c.GetType().Name == "AugaBindingDisplay";
                var template = KeyHints.instance != null ? KeyHints.instance.GetComponentsInChildren<MonoBehaviour>(true).FirstOrDefault(IsDisplay) : null;
                if (template == null || Hud.instance == null) return;
                copy = UnityEngine.Object.Instantiate(template.gameObject, Hud.instance.transform.Find("hudroot"), false);
                copy.name = "AugaAutoStart rebind check";
                copy.SetActive(true);
                display = copy.GetComponents<MonoBehaviour>().First(IsDisplay);
                Traverse.Create(display).Field("AutomaticKeyName").SetValue(keyName);
            });
            if (display == null || !ZInput.instance.m_buttons.TryGetValue(keyName, out var button))
            {
                AutoTest.Fail("rebind check: no key box to copy from the key hints, or no Jump binding");
                yield break;
            }

            string Shown()
            {
                var t = Traverse.Create(display);
                bool On(string field) { var go = t.Field(field).GetValue<GameObject>(); return go != null && go.activeSelf; }
                string Text(string field) => t.Field(field).GetValue<TMP_Text>()?.text;
                return $"key '{(On("KeybindBox") ? Text("KeybindText") : "")}' long '{(On("LongKeybindBox") ? Text("LongKeybindText") : "")}' mouse {(On("Mouse1") ? 1 : 0)}{(On("Mouse2") ? 1 : 0)}{(On("Mouse3") ? 1 : 0)}{(On("MouseX") ? 1 : 0)}";
            }
            // the box's text may be put in capitals
            bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

            yield return null;
            yield return null;
            var before = Shown();
            var bound = Localization.instance.GetBoundKeyString(keyName, true);
            Debug.Log($"[AugaAutoStart] rebind check: {keyName} ('{bound}') shows {before}");
            if (!Same(before, bound.Length == 1 ? $"key '{bound}' long '' mouse 0000" : $"key '' long '{bound}' mouse 0000"))
                AutoTest.Fail($"rebind check: {keyName} shows {before}, not '{bound}'");

            var action = button.ButtonAction;
            var previousOverride = action.bindings[0].overridePath;
            var newPath = button.GetActionPath() == "<Keyboard>/k" ? "<Keyboard>/j" : "<Keyboard>/k";
            Try(() => action.ApplyBindingOverride(0, newPath));
            yield return null;
            yield return null;
            var expected = Localization.instance.GetBoundKeyString(keyName, true);
            var after = Shown();
            Debug.Log($"[AugaAutoStart] rebind check: {keyName} bound to {newPath} ('{expected}') shows {after}");
            if (!Same(after, $"key '{expected}' long '' mouse 0000"))
                AutoTest.Fail($"rebind check: {keyName} bound to {newPath} shows {after}, not the key '{expected}'");

            Try(() =>
            {
                if (previousOverride != null) action.ApplyBindingOverride(0, previousOverride);
                else action.RemoveBindingOverride(0);
            });
            yield return null;
            yield return null;
            var restored = Shown();
            Debug.Log($"[AugaAutoStart] rebind check: {keyName} put back shows {restored}");
            if (!Same(restored, before))
                AutoTest.Fail($"rebind check: {keyName} put back shows {restored} instead of {before}");
            UnityEngine.Object.Destroy(copy);
        }

        private static void LogRect(RectTransform t, int depth)
        {
            var corners = new Vector3[4];
            t.GetWorldCorners(corners);
            var element = t.GetComponent<LayoutElement>();
            Debug.Log($"[AugaAutoStart] rect {new string(' ', depth * 2)}{t.name} active={t.gameObject.activeSelf} rect={t.rect.size} anchored={t.anchoredPosition} anchors={t.anchorMin}-{t.anchorMax} pivot={t.pivot} scale={t.localScale} world=({corners[0].x:F0},{corners[0].y:F0})-({corners[2].x:F0},{corners[2].y:F0}) layoutElement={(element != null ? element.preferredWidth + "x" + element.preferredHeight : "-")}");
        }

        private static void DumpLive(string name, Component c)
        {
            try
            {
                if (c == null) { Debug.Log($"[AugaAutoStart] live {name}: null"); return; }
                var sb = new StringBuilder();
                Dump(c.transform, 0, sb);
                File.WriteAllText(Path.Combine(ShotDir, "auga_hier_" + name + ".txt"), sb.ToString());
            }
            catch (Exception e) { Debug.LogError("[AugaAutoStart] live dump failed: " + e); }
        }

        /// <summary>The world step. The harness has already taken back any tombstone from an earlier run's death (the
        /// later steps need the weapon and hammer) and taken the 10_hud screenshot; it quits after this.</summary>
        private static IEnumerator Exercise()
        {
            if (Environment.GetEnvironmentVariable("AUGA_TEST_ENEMIES") == "only" || Environment.GetEnvironmentVariable("AUGA_TEST_STATBARS") == "only")
            {
                yield return EnemyBars();
                yield return StatBarTicks();
                yield break;
            }
            if (Environment.GetEnvironmentVariable("AUGA_TEST_CROSSPLAY") == "1")
            {
                // the join code overlay: shown on the first spawn, it must fade and hide itself within ten seconds
                // the first-spawn show again, now that the lobby (and the code) exist: it must fade and hide itself
                Try(() => JoinCode.Show(true));
                for (var i = 0; i < 8; i++)
                {
                    Try(() => Debug.Log($"[AugaAutoStart] join code t={i * 3}s: code='{ZPlayFabMatchmaking.JoinCode}' instance={(JoinCode.m_instance != null)} root active={(JoinCode.m_instance != null && JoinCode.m_instance.m_root.activeInHierarchy)} inMenu={(JoinCode.m_instance != null && JoinCode.m_instance.m_inMenu)} visible={(JoinCode.m_instance != null ? JoinCode.m_instance.m_isVisible : -1f):F1} enabled={(JoinCode.m_instance != null && JoinCode.m_instance.isActiveAndEnabled)}"));
                    if (i == 1) Shot("10d_joincode");
                    yield return new WaitForSecondsRealtime(3f);
                }
                Shot("10e_joincode_later");
            }
            DumpLive("Hud", Hud.instance);
            DumpLive("KeyHints", KeyHints.instance);
            DumpLive("Menu", Menu.instance);
            DumpLive("InventoryGui", InventoryGui.instance);
            DumpLive("Chat", Chat.instance);
            DumpLive("Minimap", Minimap.instance);
            DumpLive("MessageHud", MessageHud.instance);
            if (Chat.instance != null && Chat.instance.m_chatWindow != null) DumpLive("ChatWindow", Chat.instance.m_chatWindow.root);
            Try(() => FindTexts("picked up"));
            Try(() => LogKeyHintState("hud"));
            Try(DumpKeyHintDetails);
            Try(() =>
            {
                foreach (var name in new[] { "Attack", "Block", "Remove", "Use", "Jump", "AltPlace" })
                {
                    var def = ZInput.instance.GetButtonDef(name);
                    Debug.Log($"[AugaAutoStart] binding {name}: path='{def?.GetActionPath()}' raw='{def?.GetActionPath(false)}' display='{ZInput.instance.GetBoundKeyString(name)}'");
                }
            });
            Try(() => { foreach (var kv in BigOverlays()) Debug.Log($"[AugaAutoStart] overlay at hud step: {kv.Key}: {kv.Value}"); });
            yield return new WaitForSecondsRealtime(1f);

            // key hints only show with a weapon (or in build mode / inventory): equip the first weapon we carry
            Debug.Log("[AugaAutoStart] equip weapon");
            Try(() =>
            {
                var player = Player.m_localPlayer;
                foreach (var item in player.GetInventory().GetAllItems())
                {
                    if (item.IsWeapon() && item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch)
                    {
                        Debug.Log($"[AugaAutoStart] equipping {item.m_shared.m_name}: {player.EquipItem(item)}");
                        break;
                    }
                }
            });
            yield return new WaitForSecondsRealtime(2f);
            Try(() => LogKeyHintState("armed"));
            Try(() => LogKeyHintRects());
            Shot("10b_keyhints");
            yield return RebindCheck();
            yield return new WaitForSecondsRealtime(1f);

            // build hints (hammer equipped): mouse wheel rows, two-key rows
            ItemDrop.ItemData hammer = null;
            Try(() =>
            {
                foreach (var item in Player.m_localPlayer.GetInventory().GetAllItems())
                {
                    if (item.m_shared.m_buildPieces != null) { hammer = item; break; }
                }
                if (hammer != null) Debug.Log("[AugaAutoStart] equipping hammer: " + Player.m_localPlayer.EquipItem(hammer));
                else Debug.Log("[AugaAutoStart] no hammer in inventory");
            });
            yield return new WaitForSecondsRealtime(2f);
            Try(() => LogKeyHintState("build"));
            Shot("10c_buildhints");
            yield return new WaitForSecondsRealtime(1f);
            // the build menu itself (other mods rebuild it: AAABuildMenu)
            if (hammer != null)
            {
                Debug.Log("[AugaAutoStart] open build menu");
                Try(() => Hud.instance.TogglePieceSelection());
                yield return new WaitForSecondsRealtime(2f);
                Try(() => Debug.Log($"[AugaAutoStart] build menu visible={Hud.IsPieceSelectionVisible()}"));
                Shot("10d_buildmenu");
                yield return new WaitForSecondsRealtime(0.5f);
                Try(() => Hud.HidePieceSelection());
                yield return new WaitForSecondsRealtime(1f);
            }
            Try(() => { if (hammer != null) Player.m_localPlayer.UnequipItem(hammer); });
            yield return new WaitForSecondsRealtime(1f);

            Debug.Log("[AugaAutoStart] open inventory");
            Try(() => InventoryGui.instance.Show(null));
            yield return new WaitForSecondsRealtime(2f);
            Try(() =>
            {
                var inventory = Player.m_localPlayer.GetInventory();
                var grid = InventoryGui.instance.m_playerGrid;
                Debug.Log($"[AugaAutoStart] inventory {inventory.GetWidth()}x{inventory.GetHeight()} items={inventory.NrOfItems()} weight={inventory.GetTotalWeight():F0}; grid {grid.m_width}x{grid.m_height} elements={grid.m_elements.Count} prefab={(grid.m_elementPrefab != null ? grid.m_elementPrefab.name : "null")} gridRoot={(grid.m_gridRoot != null ? grid.m_gridRoot.gameObject.activeInHierarchy.ToString() : "null")} inventoryGridObject={grid.gameObject.activeInHierarchy} visible={InventoryGui.IsVisible()}");
                foreach (var item in inventory.GetAllItems()) Debug.Log($"[AugaAutoStart]   item {item.m_shared.m_name} x{item.m_stack} at {item.m_gridPos}");
                var gui = InventoryGui.instance;
                Debug.Log($"[AugaAutoStart] gui animator visible={InventoryGui.IsVisible()} pieceSelection={Hud.IsPieceSelectionVisible()} menuVisible={Menu.IsVisible()} playerPanelActive={gui.m_player.gameObject.activeInHierarchy} hiddenFrames={gui.m_hiddenFrames}");
                var containers = UnityEngine.Object.FindObjectsOfType<Container>().Count(c => Vector3.Distance(c.transform.position, Player.m_localPlayer.transform.position) < 40f);
                Debug.Log($"[AugaAutoStart] containers within 40 m: {containers}");
                if (grid.m_elements.Count == 0)
                {
                    Debug.Log("[AugaAutoStart] grid empty: calling UpdateInventory directly");
                    gui.UpdateInventory(Player.m_localPlayer);
                    Debug.Log($"[AugaAutoStart] after direct update: elements={grid.m_elements.Count} grid {grid.m_width}x{grid.m_height}");
                }
            });
            Shot("11_inventory");
            yield return new WaitForSecondsRealtime(0.5f);
            // other mods' panels inside the player inventory (Equipment and Quick Slots): where they and their slots landed
            DumpLive("InventoryGuiOpen", InventoryGui.instance);
            Try(() =>
            {
                var player = InventoryGui.instance.m_player;
                LogRect(player, 0);
                LogRect(InventoryGui.instance.m_playerGrid.m_gridRoot, 0);
                foreach (var name in new[] { "EAQS", "EaqsSlotRoot", "Divider", "Paperdolls" })
                    LogRects(player, name);
            });
            // tab clicks: every sound object alive right after a click is logged (one is right, two is the bug).
            // The helper does not reference Auga's Unity library, so the tab buttons are found by hierarchy.
            Try(() => Debug.Log("[AugaAutoStart] set-active-group effects: " + string.Join(", ", InventoryGui.instance.m_setActiveGroupEffects.m_effectPrefabs.Select(e => e.m_prefab != null ? e.m_prefab.name : "null"))));
            var right = InventoryGui.instance.transform.Find("root/RightPanel");
            if (right != null)
            {
                var playerTabs = right.Find("DefaultContent/TabButtonContainer/Tabs")?.GetComponentsInChildren<Button>(false) ?? new Button[0];
                for (var i = 0; i < 2 && playerTabs.Length >= 2; i++)
                {
                    var button = playerTabs[(i + 1) % 2];
                    yield return ClickAndCountSounds("player panel tab " + button.name, button);
                }
                var craftTabs = (right.Find("WorkbenchContent")?.GetComponentsInChildren<Button>(true) ?? new Button[0])
                    .Where(b => b.name.StartsWith("TabButton")).ToArray();
                for (var i = 0; i < 2 && craftTabs.Length >= 2; i++)
                {
                    var button = craftTabs[(i + 1) % 2];
                    yield return ClickAndCountSounds("crafting tab " + button.name, button);
                }
            }
            yield return Links();
            Try(() => LogKeyHintState("inventory"));
            Try(() => LogKeyHintRects());
            yield return new WaitForSecondsRealtime(1f);

            // AUGA_TEST_ROWS: resize the player inventory (as the trader's extra-row purchase does) and show the
            // (empty) container panel so its position relative to the grown player panel can be checked.
            var rowsSetting = Environment.GetEnvironmentVariable("AUGA_TEST_ROWS");
            if (int.TryParse(rowsSetting, out var rows))
            {
                Debug.Log("[AugaAutoStart] set inventory rows " + rows);
                Try(() => Player.m_localPlayer.SetInventorySize(rows));
                Try(() => InventoryGui.instance.m_container.gameObject.SetActive(true));
                yield return new WaitForSecondsRealtime(2f);
                Shot("11b_inventory_rows" + rows);
                yield return new WaitForSecondsRealtime(1f);
                Try(() => InventoryGui.instance.m_container.gameObject.SetActive(false));
                Try(() => Player.m_localPlayer.SetInventorySize(4));
                yield return new WaitForSecondsRealtime(1f);
            }

            // AUGA_TEST_EFFECTS: fill the player's status effects up to each count and screenshot the Active Effects
            // grid of the player panel (the tab clicks above end on it).
            var effectsSetting = Environment.GetEnvironmentVariable("AUGA_TEST_EFFECTS");
            if (!string.IsNullOrEmpty(effectsSetting))
            {
                foreach (var part in effectsSetting.Split(','))
                {
                    if (!int.TryParse(part.Trim(), out var count))
                        continue;
                    Try(() => SetTestStatusEffects(count));
                    yield return new WaitForSecondsRealtime(1f);
                    Shot("11c_effects" + count);
                    Try(() => LogActiveEffectsGrid());
                    Try(() => CheckStatusEffectLists(count + " effects"));
                }
                Try(() => SetTestStatusEffects(0));
                yield return new WaitForSecondsRealtime(1f);
                Try(() => CheckStatusEffectLists("test effects removed"));
            }

            Try(() => InventoryGui.instance.Hide());
            yield return new WaitForSecondsRealtime(1f);

            yield return MagicItems();
            yield return Favorites();

            Debug.Log("[AugaAutoStart] open map");
            Try(() => Minimap.instance.SetMapMode(Minimap.MapMode.Large));
            yield return new WaitForSecondsRealtime(2f);
            Shot("12_map");
            yield return new WaitForSecondsRealtime(1f);
            Try(() => Minimap.instance.SetMapMode(Minimap.MapMode.Small));
            yield return new WaitForSecondsRealtime(1f);

            Debug.Log("[AugaAutoStart] open menu");
            Try(() => Menu.instance.Show());
            yield return new WaitForSecondsRealtime(2f);
            Shot("13_menu");
            yield return new WaitForSecondsRealtime(1f);
            yield return Compendium();
            Debug.Log("[AugaAutoStart] open settings (in game)");
            Try(() => Menu.instance.OnSettings());
            yield return new WaitForSecondsRealtime(2f);
            Try(() =>
            {
                var s0 = Settings.instance;
                if (s0 != null)
                {
                    var bg = s0.GetComponentsInChildren<Graphic>(true).FirstOrDefault(g => g.name == "Background" && g.isActiveAndEnabled);
                    var canvas = bg != null ? bg.canvas : null;
                    Debug.Log($"[AugaAutoStart] settings graphic={(bg != null ? PathOf(bg.transform) : "none")} cull={(bg != null && bg.canvasRenderer.cull)} canvas={(canvas != null ? canvas.name : "null")} root={(canvas != null ? canvas.rootCanvas.name : "?")} mode={(canvas != null ? canvas.renderMode.ToString() : "?")} enabled={(canvas != null && canvas.enabled)} cam={(canvas != null && canvas.worldCamera != null ? canvas.worldCamera.name : "none")} sort={(canvas != null ? canvas.sortingOrder : 0)} override={(canvas != null && canvas.overrideSorting)} pixelRect={(canvas != null ? canvas.pixelRect.ToString() : "?")}");
                    foreach (var c in s0.GetComponentsInParent<Canvas>(true))
                        Debug.Log($"[AugaAutoStart] parent canvas {PathOf(c.transform)} enabled={c.enabled} mode={c.renderMode} sort={c.sortingOrder} override={c.overrideSorting} cam={(c.worldCamera != null ? c.worldCamera.name : "none")} scale={c.scaleFactor}");
                    foreach (var cg in s0.GetComponentsInParent<CanvasGroup>(true))
                        Debug.Log($"[AugaAutoStart] parent canvasgroup {PathOf(cg.transform)} alpha={cg.alpha}");
                }
            });
            Try(() =>
            {
                var s = Settings.instance;
                Debug.Log($"[AugaAutoStart] settings instance={(s != null)} active={(s != null && s.gameObject.activeInHierarchy)} parent={(s != null ? s.transform.parent?.name : "-")} menuInstance={(Menu.instance != null ? Menu.instance.name : "null")} menuRootActive={(Menu.instance != null && Menu.instance.m_root.gameObject.activeInHierarchy)}");
                if (s != null)
                {
                    var r = (RectTransform)s.transform;
                    var cg = s.GetComponent<CanvasGroup>();
                    var pr = s.transform.parent as RectTransform;
                    Debug.Log($"[AugaAutoStart] settings rect={r.rect} anchoredPos={r.anchoredPosition} anchors={r.anchorMin}-{r.anchorMax} scale={r.localScale} alpha={(cg != null ? cg.alpha : -1f)} parentRect={(pr != null ? pr.rect.ToString() : "-")} panelActive={(s.m_settingsPanel != null && s.m_settingsPanel.activeInHierarchy)}");
                }
                DumpLive("MenuWithSettings", Menu.instance);
            });
            Shot("13b_settings");
            yield return new WaitForSecondsRealtime(1f);
            Try(() => LogSettingsRenderState("in game"));
            Try(() => { var s = Settings.instance; if (s != null) s.OnBack(); });
            yield return new WaitForSecondsRealtime(1f);
            Try(() => Menu.instance.Hide());
            yield return new WaitForSecondsRealtime(1f);

            Try(() => LogKeyHintState("after menu"));
            Debug.Log("[AugaAutoStart] open chat");
            Try(() => { Chat.instance.m_input.gameObject.SetActive(true); Chat.instance.m_input.ActivateInputField(); });
            yield return new WaitForSecondsRealtime(2f);
            Shot("14_chat");
            yield return new WaitForSecondsRealtime(1f);

            Debug.Log("[AugaAutoStart] damage self");
            Try(() => { var player = Player.m_localPlayer; if (player.GetHealth() < 15f) player.SetHealth(player.GetMaxHealth()); });
            yield return null;
            Try(() => Player.m_localPlayer.Damage(new HitData { m_damage = { m_damage = 5f } }));
            yield return new WaitForSecondsRealtime(2f);
            Shot("15_damage");

            yield return EnemyBars();
            yield return StatBarTicks();
            Debug.Log("[AugaAutoStart] exercise done");
        }

        // AUGA_TEST_STATBARS: with [StatBars] HealthBarFixedSize above 0 the health bar's ticks spread over the most
        // health the player could reach, which changes with every food. Screenshotted and checked with no food, after
        // three foods and after two of them wore off; a tick away from its place fails the run.
        private static IEnumerator StatBarTicks()
        {
            var setting = Environment.GetEnvironmentVariable("AUGA_TEST_STATBARS");
            if (setting != "1" && setting != "only") yield break;
            Debug.Log("[AugaAutoStart] stat bar ticks");
            BepInEx.Configuration.ConfigEntry<int> fixedSize = null;
            Try(() => BepInEx.Bootstrap.Chainloader.PluginInfos["randyknapp.mods.auga"].Instance.Config.TryGetEntry("StatBars", "HealthBarFixedSize", out fixedSize));
            // by type name: this plugin does not reference Unity.Auga, and Auga loads it itself
            var bar = Hud.instance != null ? Hud.instance.transform.Find("hudroot/HealthBar")?.GetComponents<Component>().FirstOrDefault(c => c != null && c.GetType().Name == "AugaHealthBar") : null;
            if (fixedSize == null || bar == null)
            {
                AutoTest.Fail($"AUGA_TEST_STATBARS: {(fixedSize == null ? "no [StatBars] HealthBarFixedSize setting" : "no Auga health bar at hudroot/HealthBar")}");
                yield break;
            }

            var previous = fixedSize.Value;
            var player = Player.m_localPlayer;
            Try(() => { player.ClearFood(); fixedSize.Value = 300; });
            yield return new WaitForSecondsRealtime(1f);
            CheckFixedLength(bar, 300);
            CheckTicks(bar, "no food");
            Shot("17_statbars_fixed");

            Try(() =>
            {
                foreach (var food in new[] { "CookedMeat", "NeckTailGrilled", "Raspberry" })
                {
                    // EatFood names the food after the item's drop prefab, which a prefab's own item data lacks
                    var prefab = ObjectDB.instance.GetItemPrefab(food);
                    var item = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
                    item.m_dropPrefab = prefab;
                    player.EatFood(item);
                }
            });
            yield return new WaitForSecondsRealtime(1f);
            CheckTicks(bar, "three foods");
            Shot("17b_statbars_fed");

            // the two bigger foods wear off: each forced food update takes a second off every food and removes one that
            // ran out (the world's food rate may stop the clock otherwise)
            Try(() =>
            {
                foreach (var food in player.m_foods.OrderByDescending(f => f.m_item.m_shared.m_food).Take(2))
                    food.m_time = 0.1f;
                player.UpdateFood(0f, true);
                player.UpdateFood(0f, true);
            });
            yield return new WaitForSecondsRealtime(1f);
            CheckTicks(bar, "one food left");
            Shot("17c_statbars_one_food");

            Try(() => fixedSize.Value = previous);
            yield return null;
            CheckFixedLength(bar, previous);
        }

        // Auga hands the [StatBars] settings to the bars when one changes, not every frame
        private static void CheckFixedLength(Component bar, int expected)
        {
            Try(() =>
            {
                var fixedLength = Traverse.Create(bar).Field("FixedLength").GetValue<float>();
                Debug.Log($"[AugaAutoStart] health bar fixed length {fixedLength} (setting {expected})");
                if (Mathf.Abs(fixedLength - expected) > 0.01f)
                    AutoTest.Fail($"AUGA_TEST_STATBARS: the health bar's fixed length is {fixedLength} after the setting changed to {expected}");
            });
        }

        private static void CheckTicks(Component bar, string when)
        {
            Try(() =>
            {
                var t = Traverse.Create(bar);
                var maxPotential = t.Field("MaxPotentialValue").GetValue<float>();
                var spacing = t.Field("UnitsPerTick").GetValue<float>() * (t.Field("MinBackgroundSize").GetValue<float>() + t.Field("FixedLength").GetValue<float>()) / maxPotential;
                var ticks = t.Field("_ticks").GetValue<List<RectTransform>>();
                var shown = ticks.Where(x => x.gameObject.activeSelf).ToList();
                var positions = string.Join(", ", shown.Select(x => x.anchoredPosition.x.ToString("0.#")));
                Debug.Log($"[AugaAutoStart] health bar ticks, {when}: max potential {maxPotential:0.#}, spacing {spacing:0.#}, {shown.Count} ticks at {positions}");
                for (var i = 0; i < shown.Count; i++)
                {
                    if (Mathf.Abs(shown[i].anchoredPosition.x - spacing * (i + 1)) > 0.5f)
                        AutoTest.Fail($"AUGA_TEST_STATBARS: health bar tick {i + 1} at {shown[i].anchoredPosition.x:0.#} instead of {spacing * (i + 1):0.#} ({when})");
                }
            });
        }

        // AUGA_TEST_ENEMIES: creatures of several levels and a boss spawned in front of the camera, their health bars
        // screenshotted full, hurt and nearly empty, then removed again. Their AI is switched off so they stay put and
        // leave the player alone. The boss is marked alerted through the field: SetAlerted would also count it as an
        // active boss in the world's global keys. "only" runs just this step, right after the first HUD screenshot.
        private static IEnumerator EnemyBars()
        {
            var setting = Environment.GetEnvironmentVariable("AUGA_TEST_ENEMIES");
            if (setting != "1" && setting != "only") yield break;
            Debug.Log("[AugaAutoStart] enemy bars");
            var player = Player.m_localPlayer;
            var spawned = new List<Character>();
            Try(() =>
            {
                var forward = Vector3.ProjectOnPlane(GameCamera.instance.transform.forward, Vector3.up).normalized;
                var right = Vector3.Cross(Vector3.up, forward);
                var creatures = new[] { ("Greyling", 1), ("Boar", 2), ("Neck", 3), ("Deer", 5), ("Boar", 8) };
                for (var i = 0; i < creatures.Length; i++)
                {
                    var offset = (i - (creatures.Length - 1) / 2f) * 2.2f;
                    var creature = SpawnFrozen(creatures[i].Item1, player.transform.position + forward * 7f + right * offset, forward);
                    if (creature == null) continue;
                    creature.SetLevel(creatures[i].Item2);
                    spawned.Add(creature);
                }
                var boss = SpawnFrozen("Eikthyr", player.transform.position + forward * 16f, forward);
                if (boss != null) spawned.Add(boss);
            });
            Debug.Log($"[AugaAutoStart] spawned {spawned.Count} characters: {string.Join(", ", spawned.Select(c => $"{c.name} level {c.GetLevel()}"))}");

            // A screenshot is taken at the end of the frame, so nothing changes in the frame after a Shot: destroying
            // the creatures there blanks their names, and switching a setting back shows the old look.
            foreach (var (health, shot) in new[] { (1f, "16_enemy_bars"), (0.55f, "16b_enemy_bars_hurt"), (0.08f, "16c_enemy_bars_low") })
            {
                Try(() => { foreach (var c in spawned) c.SetHealth(c.GetMaxHealth() * health); });
                // the slow bar waits before it drains
                yield return KeepEnemyHudsShown(3f);
                Shot(shot);
                yield return KeepEnemyHudsShown(0.5f);
                if (health != 1f) continue;

                DumpLive("EnemyHudLive", EnemyHud.instance);
                // [EnemyHud] StarsBelowHealthBar off and back on, which moves the stars of the huds already shown
                BepInEx.Configuration.ConfigEntry<bool> starsBelow = null;
                Try(() => BepInEx.Bootstrap.Chainloader.PluginInfos["randyknapp.mods.auga"].Instance.Config.TryGetEntry("EnemyHud", "StarsBelowHealthBar", out starsBelow));
                if (starsBelow == null)
                {
                    Debug.LogWarning("[AugaAutoStart] no [EnemyHud] StarsBelowHealthBar setting");
                    continue;
                }
                Try(() => starsBelow.Value = false);
                yield return KeepEnemyHudsShown(1f);
                Shot("16a_enemy_bars_stars_on_bar");
                yield return KeepEnemyHudsShown(0.5f);
                Try(() => starsBelow.Value = true);
            }

            Try(() => { foreach (var c in spawned) if (c != null) ZNetScene.instance.Destroy(c.gameObject); });
            yield return new WaitForSecondsRealtime(1f);
        }

        // An ordinary creature's bar shows only for a while after the player has hovered it.
        private static IEnumerator KeepEnemyHudsShown(float seconds)
        {
            for (var t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                Try(() => { foreach (var hud in EnemyHud.instance.m_huds.Values) hud.m_hoverTimer = 0f; });
                yield return null;
            }
        }

        private static Character SpawnFrozen(string prefabName, Vector3 position, Vector3 facePlayer)
        {
            var prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (prefab == null)
            {
                Debug.LogWarning($"[AugaAutoStart] no {prefabName} prefab");
                return null;
            }
            position.y = ZoneSystem.instance.GetGroundHeight(position) + 0.2f;
            var go = UnityEngine.Object.Instantiate(prefab, position, Quaternion.LookRotation(-facePlayer));
            var character = go.GetComponent<Character>();
            var ai = go.GetComponent<BaseAI>();
            if (ai != null)
            {
                ai.m_alerted = character != null && character.IsBoss();
                ai.enabled = false;
            }
            return character;
        }

        // AUGA_TEST_MAGIC: EpicLoot's magic items. One item per spec is spawned with the "magicitem" console command
        // and picked up, then each is hovered in the inventory and its tooltip screenshotted.
        private static IEnumerator MagicItems()
        {
            var spec = Environment.GetEnvironmentVariable("AUGA_TEST_MAGIC");
            if (string.IsNullOrEmpty(spec)) yield break;
            var console = global::Console.instance;
            if (console == null || !Terminal.commands.ContainsKey("magicitem"))
            {
                Debug.LogWarning("[AugaAutoStart] no magicitem console command (EpicLoot not loaded?)");
                yield break;
            }

            var specs = new List<KeyValuePair<string, string>>();
            if (spec == "1")
            {
                specs.Add(new KeyValuePair<string, string>("Magic", "SwordBronze"));
                specs.Add(new KeyValuePair<string, string>("Rare", "ShieldBronzeBuckler"));
                specs.Add(new KeyValuePair<string, string>("Epic", "BowFineWood"));
                specs.Add(new KeyValuePair<string, string>("Legendary", "ArmorBronzeChest"));
                specs.Add(new KeyValuePair<string, string>("Mythic", "AxeIron"));
            }
            else
            {
                foreach (var part in spec.Split(';'))
                {
                    var pair = part.Split(':');
                    if (pair.Length == 2) specs.Add(new KeyValuePair<string, string>(pair[0].Trim(), pair[1].Trim()));
                }
            }

            var player = Player.m_localPlayer;
            Try(() => Debug.Log($"[AugaAutoStart] inventory before: {player.GetInventory().NrOfItems()} items, {CountMagicItems(player)} carrying EpicLoot data"));
            Try(LogFonts);
            var before = new HashSet<ItemDrop.ItemData>(player.GetInventory().GetAllItems());
            // EpicLoot registers its commands as cheats, and the game then wants devcommands plus a "confirmcheats"
            // that marks the world and character as cheated for good; running the command's action directly skips
            // that gate and leaves the test world unmarked
            foreach (var s in specs)
            {
                // "Set:<setID>" spawns a whole legendary set (magicitemset); anything else is "magicitem <rarity> <item>"
                var isSet = string.Equals(s.Key, "Set", StringComparison.OrdinalIgnoreCase);
                var command = isSet ? $"magicitemset {s.Value}" : $"magicitem {s.Key} {s.Value} 1";
                if (!Terminal.commands.TryGetValue(isSet ? "magicitemset" : "magicitem", out var consoleCommand))
                {
                    Debug.LogWarning("[AugaAutoStart] no console command for " + command);
                    continue;
                }
                Debug.Log("[AugaAutoStart] console: " + command);
                Try(() => consoleCommand.action(new Terminal.ConsoleEventArgs(command, console, consoleCommand)));
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1.5f);

            Try(() =>
            {
                foreach (var drop in UnityEngine.Object.FindObjectsOfType<ItemDrop>())
                {
                    if (Vector3.Distance(drop.transform.position, player.transform.position) > 10f) continue;
                    var picked = player.Pickup(drop.gameObject, false, false);
                    Debug.Log($"[AugaAutoStart] pickup {drop.name}: {picked}");
                }
            });
            yield return new WaitForSecondsRealtime(1f);

            var magic = player.GetInventory().GetAllItems().Where(i => !before.Contains(i)).ToList();
            foreach (var item in magic)
                Debug.Log($"[AugaAutoStart] new item {item.m_shared.m_name} at {item.m_gridPos} quality={item.m_quality} customData=[{string.Join(", ", item.m_customData.Keys)}]");
            if (magic.Count == 0)
            {
                Debug.LogWarning("[AugaAutoStart] no magic items ended up in the inventory");
                yield break;
            }

            Debug.Log("[AugaAutoStart] open inventory (magic items)");
            Try(() => InventoryGui.instance.Show(null));
            yield return new WaitForSecondsRealtime(1.5f);
            Shot("16_magic_inventory");
            yield return new WaitForSecondsRealtime(0.5f);

            var grid = InventoryGui.instance.m_playerGrid;
            var shotIndex = 0;
            foreach (var item in magic)
            {
                var index = item.m_gridPos.y * grid.m_width + item.m_gridPos.x;
                var element = index >= 0 && index < grid.m_elements.Count ? grid.m_elements[index] : null;
                if (element == null)
                {
                    Debug.LogWarning($"[AugaAutoStart] no grid element for {item.m_shared.m_name} at {item.m_gridPos}");
                    continue;
                }
                Try(() => LogMagicElement(element));
                if (shotIndex == 0 && element.m_tooltip != null && element.m_tooltip.m_tooltipPrefab != null)
                    Try(() => LogTree(element.m_tooltip.m_tooltipPrefab.transform, 0, 3));
                yield return HoverSlot(element, item.m_shared.m_name);
                Try(() => LogTooltip(item));
                var itemName = Localization.instance.Localize(item.m_shared.m_name).Replace(" ", "");
                Shot($"16_tooltip_{shotIndex++}_{itemName}");
                yield return new WaitForSecondsRealtime(1f);
            }

            Try(() => InventoryGui.instance.Hide());
            yield return new WaitForSecondsRealtime(1f);
            Try(() =>
            {
                var weapon = magic.FirstOrDefault(i => i.IsWeapon());
                if (weapon != null) Debug.Log($"[AugaAutoStart] equip magic {weapon.m_shared.m_name}: {player.EquipItem(weapon)}");
            });
            yield return new WaitForSecondsRealtime(1.5f);
            Shot("17_magic_hud");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return EnchantingTable();
            Try(() => RemoveSpawnedItems(player, magic));
        }

        /// <summary>
        /// Hovers an inventory slot (or any other element with a tooltip, such as a link) until its tooltip shows. The
        /// tooltip follows the OS cursor and hides as soon as the cursor is outside the hovered slot, so the cursor is
        /// warped onto the slot.
        /// </summary>
        private static IEnumerator HoverSlot(Component element, string what)
        {
            var rect = (RectTransform)element.transform;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var center = (Vector2)((corners[0] + corners[2]) / 2f);
            var target = center;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var warpTo = target;
                var before = (Vector2)ZInput.pointerPosition;
                Try(() => { if (Mouse.current != null) Mouse.current.WarpCursorPosition(warpTo); });
                yield return null;
                yield return null;
                var pointer = (Vector2)ZInput.pointerPosition;
                if (pointer == before && Mouse.current != null)
                {
                    // the OS sometimes ignores a warp and the game's pointer stays put: it is set on the mouse directly
                    Try(() => InputState.Change(Mouse.current.position, center));
                    yield return null;
                    yield return null;
                    pointer = ZInput.pointerPosition;
                }
                var inside = RectTransformUtility.RectangleContainsScreenPoint(rect, pointer);
                Debug.Log($"[AugaAutoStart] hover attempt {attempt} for {what}: slot centre={center} warped to={warpTo} pointer={pointer} inside={inside} focused={Application.isFocused}");
                if (!inside)
                {
                    target = center + (center - pointer);   // the cursor landed off target: aim past it by the same error
                    continue;
                }
                Try(() => ExecuteEvents.Execute(element.gameObject, new PointerEventData(EventSystem.current) { position = pointer, pointerEnter = element.gameObject }, ExecuteEvents.pointerEnterHandler));
                yield return new WaitForSecondsRealtime(1.5f);   // the tooltip appears half a second after the hover starts
                if (UITooltip.m_tooltip != null && UITooltip.m_tooltip.activeInHierarchy) break;
                Debug.Log($"[AugaAutoStart] hover attempt {attempt}: no tooltip shown (current={(UITooltip.m_current != null ? UITooltip.m_current.name : "null")} hovered={(UITooltip.m_hovered != null ? UITooltip.m_hovered.name : "null")})");
            }
        }

        // AUGA_TEST_LINKS: other mods' links in the tab row under the player name (API.PlayerPanel_AddLink), found by
        // link ID through Auga's API, by reflection since the helper does not reference Auga. The row's buttons are
        // logged in order and screenshotted; each link is clicked, screenshotted and clicked again (links often open
        // and close a window); the first link is hidden and shown again, so the row's re-centring shows.
        private static IEnumerator Links()
        {
            var setting = Environment.GetEnvironmentVariable("AUGA_TEST_LINKS");
            if (string.IsNullOrEmpty(setting)) yield break;
            var api = Type.GetType("Auga.API, Auga");
            if (api == null)
            {
                AutoTest.Fail("AUGA_TEST_LINKS: Auga's API was not found");
                yield break;
            }
            GameObject GetLink(string id) => (GameObject)api.GetMethod("PlayerPanel_GetLink").Invoke(null, new object[] { id });
            void SetVisible(string id, bool visible) => api.GetMethod("PlayerPanel_SetLinkVisible").Invoke(null, new object[] { id, visible });
            void LogRow(string when) => Try(() =>
            {
                var container = InventoryGui.instance.transform.Find("root/RightPanel/DefaultContent/TabButtonContainer");
                var tabs = container != null ? (RectTransform)container.Find("Tabs") : null;
                if (tabs == null)
                {
                    Debug.Log($"[AugaAutoStart] tab row {when}: missing");
                    return;
                }
                string Rect(Transform t) => $"x={((RectTransform)t).anchoredPosition.x:F0} w={((RectTransform)t).rect.width:F0}";
                Debug.Log($"[AugaAutoStart] tab row {when}: row {Rect(tabs)}, lines {string.Join(" / ", container.Cast<Transform>().Where(t => t != tabs).Select(Rect))}; " +
                          string.Join(", ", tabs.Cast<Transform>().Select(t => $"{t.name} ({(t.gameObject.activeSelf ? "" : "hidden, ")}{Rect(t)})")));
            });

            // A screenshot is written well after it is asked for (a second or more was seen), so the state it should
            // show is held for a while after each one before the next change.
            const float settle = 2.5f;
            var ids = setting.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
            LogRow("with links");
            Shot("11c_links");
            yield return new WaitForSecondsRealtime(settle);
            foreach (var id in ids)
            {
                Button button = null;
                Try(() => button = GetLink(id)?.GetComponent<Button>());
                if (button == null)
                {
                    AutoTest.Fail($"AUGA_TEST_LINKS: there is no link '{id}'");
                    continue;
                }
                Debug.Log($"[AugaAutoStart] link {id}: active={button.gameObject.activeInHierarchy}");
                yield return HoverSlot(button, "link " + id);
                Shot("11c_link_" + id + "_tooltip");
                yield return new WaitForSecondsRealtime(settle);
                Try(() => button.onClick.Invoke());
                yield return new WaitForSecondsRealtime(1f);
                Shot("11c_link_" + id);
                yield return new WaitForSecondsRealtime(settle);
                Try(() => button.onClick.Invoke());
                yield return new WaitForSecondsRealtime(1f);
            }

            if (ids.Length == 0) yield break;
            Try(() => SetVisible(ids[0], false));
            yield return new WaitForSecondsRealtime(1f);
            LogRow("with " + ids[0] + " hidden");
            Shot("11c_links_hidden");
            yield return new WaitForSecondsRealtime(settle);
            Try(() => SetVisible(ids[0], true));
            yield return new WaitForSecondsRealtime(1f);
            LogRow("with " + ids[0] + " shown again");
        }

        // AUGA_TEST_FAVORITES: Item Favorite Framework's marks on Auga's slots. Test items are added and marked through
        // IFF's public API, by reflection since the helper does not reference it: favorites, a trash flag, favorited
        // slots (one empty, one holding a favorite), a custom mark with an icon and a wishlisted item, two of them on
        // the hotbar row. The inventory and the tooltips of a favorite, the trash, the wishlisted food and a favorite
        // mead are screenshotted, and one favorite again after it is unmarked while hovered. The objects IFF hung on
        // each slot are logged, then every mark and test item is taken back.
        private static IEnumerator Favorites()
        {
            if (Environment.GetEnvironmentVariable("AUGA_TEST_FAVORITES") != "1") yield break;
            var api = Type.GetType("ItemFavoriteFramework.API, ItemFavoriteFramework");
            if (api == null)
            {
                Debug.LogWarning("[AugaAutoStart] Item Favorite Framework is not loaded");
                yield break;
            }
            object Call(string method, params object[] args) => api.GetMethods().First(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(null, args);
            Try(() => Debug.Log($"[AugaAutoStart] Item Favorite Framework {Call("GetPluginVersion")} (API {Call("GetApiVersion")}) ready={Call("IsReady")}"));

            const string markId = "augatest.mark";
            var player = Player.m_localPlayer;
            var inventory = player.GetInventory();
            var added = new List<ItemDrop.ItemData>();
            var slots = new List<Vector2i>();
            ItemDrop.ItemData Add(string prefab, int stack, int row)
            {
                for (var x = 0; x < inventory.GetWidth(); x++)
                {
                    if (inventory.GetItemAt(x, row) != null || slots.Contains(new Vector2i(x, row))) continue;
                    var item = inventory.AddItem(prefab, stack, 1, 0, 0L, "", new Vector2i(x, row), false);
                    if (item != null) added.Add(item);
                    return item;
                }
                Debug.LogWarning($"[AugaAutoStart] no free slot in row {row} for {prefab}");
                return null;
            }
            void FavoriteSlot(Vector2i pos)
            {
                slots.Add(pos);
                Debug.Log($"[AugaAutoStart] favorite slot {pos}: {Call("SetSlotFavorited", pos.x, pos.y, true)}");
            }

            ItemDrop.ItemData knife = null, torch = null, sword = null, wood = null, resin = null, chest = null, berries = null, mead = null;
            Try(() =>
            {
                knife = Add("KnifeFlint", 1, 0);
                torch = Add("Torch", 1, 0);
                sword = Add("SwordBronze", 1, 1);
                wood = Add("Wood", 20, 1);
                resin = Add("Resin", 5, 1);
                chest = Add("ArmorBronzeChest", 1, 1);
                berries = Add("Raspberry", 3, 1);
                mead = Add("MeadHealthMinor", 2, 1);
                Debug.Log($"[AugaAutoStart] favorite knife: {Call("SetItemFavorited", knife, true)}, sword: {Call("SetItemFavorited", sword, true)}, chest: {Call("SetItemFavorited", chest, true)}, mead: {Call("SetItemFavorited", mead, true)}");
                Debug.Log($"[AugaAutoStart] trash wood: {Call("SetItemTrashFlagged", wood, true)}");
                FavoriteSlot(resin.m_gridPos);
                FavoriteSlot(chest.m_gridPos);
                for (var x = 0; x < inventory.GetWidth(); x++)
                {
                    if (inventory.GetItemAt(x, 2) != null) continue;
                    FavoriteSlot(new Vector2i(x, 2));
                    break;
                }
                Debug.Log($"[AugaAutoStart] custom mark: registered {Call("RegisterMarkType", markId, "", new Color(0.35f, 0.8f, 1f), Call("GetFavoriteIcon"), "")}, on torch {Call("SetCustomMark", torch, markId, true)}");
                Debug.Log($"[AugaAutoStart] wishlist berries: {Call("AddWishlistEntryFromItem", berries)}");
            });
            yield return null;

            Debug.Log("[AugaAutoStart] open inventory (favorites)");
            Try(() => InventoryGui.instance.Show(null));
            yield return new WaitForSecondsRealtime(1.5f);
            Shot("19_favorites_inventory");
            yield return new WaitForSecondsRealtime(0.5f);
            var grid = InventoryGui.instance.m_playerGrid;
            InventoryElement ElementAt(Vector2i pos)
            {
                var index = pos.y * grid.m_width + pos.x;
                return index >= 0 && index < grid.m_elements.Count ? grid.m_elements[index] : null;
            }
            Try(() =>
            {
                foreach (var item in added)
                {
                    var element = ElementAt(item.m_gridPos);
                    if (element != null) LogMarkElement(element, item.m_shared.m_name);
                }
                var empty = ElementAt(slots[slots.Count - 1]);
                if (empty != null) LogMarkElement(empty, "empty favorited slot");
            });

            var hovered = new[] { sword, wood, berries, mead };
            var names = new[] { "favorite", "trash", "wishlist_food", "favorite_mead" };
            for (var i = 0; i < hovered.Length; i++)
            {
                var element = hovered[i] != null ? ElementAt(hovered[i].m_gridPos) : null;
                if (element == null) continue;
                yield return HoverSlot(element, hovered[i].m_shared.m_name);
                var item = hovered[i];
                Try(() => Debug.Log($"[AugaAutoStart] vanilla tooltip text of {item.m_shared.m_name}: '{item.GetTooltip().Replace("\n", " | ")}'"));
                Try(() => LogTooltip(item));
                Shot("19_favorites_tooltip_" + names[i]);
                yield return new WaitForSecondsRealtime(1f);
            }

            // the tooltip of a hovered item has to follow a mark set while it is shown
            if (sword != null)
            {
                var element = ElementAt(sword.m_gridPos);
                yield return HoverSlot(element, sword.m_shared.m_name);
                Try(() => Debug.Log($"[AugaAutoStart] unfavorite sword while hovered: {Call("SetItemFavorited", sword, false)}"));
                yield return new WaitForSecondsRealtime(1f);
                Try(() => LogTooltip(sword));
                Shot("19_favorites_tooltip_unfavorited");
                yield return new WaitForSecondsRealtime(1f);
            }

            Try(() => InventoryGui.instance.Hide());
            yield return new WaitForSecondsRealtime(1f);
            Try(() =>
            {
                foreach (var item in new[] { knife, sword, chest, mead })
                    if (item != null) Call("SetItemFavorited", item, false);
                if (wood != null) Call("SetItemTrashFlagged", wood, false);
                if (torch != null) Call("SetCustomMark", torch, markId, false);
                Call("RemoveWishlistEntries", "Raspberry");
                foreach (var pos in slots) Call("SetSlotFavorited", pos.x, pos.y, false);
                RemoveSpawnedItems(player, added);
            });
        }

        /// <summary>The objects Item Favorite Framework hung on a slot (IFF_*): their images, sibling index and size.</summary>
        private static void LogMarkElement(InventoryElement element, string what)
        {
            var sb = new StringBuilder();
            foreach (var t in element.GetComponentsInChildren<RectTransform>(true))
            {
                if (!t.name.StartsWith("IFF_")) continue;
                var image = t.GetComponent<Image>();
                sb.Append(t.parent == element.transform ? "" : t.parent.name + "/").Append(t.name).Append('#').Append(t.GetSiblingIndex());
                sb.Append(t.gameObject.activeInHierarchy ? "" : "(off)").Append(" size=").Append(t.rect.size);
                if (image != null)
                    sb.Append(" [").Append(image.enabled ? "" : "disabled ").Append(image.sprite != null ? image.sprite.name : "nosprite").Append(' ').Append(ColorUtility.ToHtmlStringRGBA(image.color)).Append(" alpha=").Append(image.canvasRenderer.GetInheritedAlpha().ToString("0.##")).Append(']');
                sb.Append("; ");
            }
            Debug.Log($"[AugaAutoStart] marks on {what} at {element.Position}: {(sb.Length > 0 ? sb.ToString() : "none")}");
            LogMagicElement(element);
        }

        /// <summary>Auga's compendium (pause menu): opened through its controller, every tab screenshotted, closed again.</summary>
        private static IEnumerator Compendium()
        {
            var controllerType = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Unity.Auga")?.GetType("AugaUnity.AugaCompendiumController");
            var controller = controllerType != null && Menu.instance != null ? Menu.instance.GetComponentInChildren(controllerType, true) : null;
            if (controller == null)
            {
                Debug.LogWarning("[AugaAutoStart] no AugaCompendiumController under the menu");
                yield break;
            }
            Debug.Log("[AugaAutoStart] open compendium");
            Try(() => controllerType.GetMethod("ShowCompendium").Invoke(controller, null));
            yield return new WaitForSecondsRealtime(1.5f);
            var tabHandler = controllerType.GetField("TabController")?.GetValue(controller) as TabHandler;
            var count = tabHandler != null ? tabHandler.m_tabs.Count : 0;
            for (var i = 0; i < count; i++)
            {
                var tab = tabHandler.m_tabs[i];
                if (tab.m_button == null || !tab.m_button.gameObject.activeSelf) continue;
                var name = tab.m_page != null ? tab.m_page.name : i.ToString();
                Debug.Log("[AugaAutoStart] compendium tab " + name);
                Try(() => tabHandler.SetActiveTab(i));
                yield return new WaitForSecondsRealtime(1f);
                Shot("13c_compendium_" + i + "_" + name);
                yield return new WaitForSecondsRealtime(0.5f);
            }
            Try(() => controllerType.GetMethod("HideCompendium").Invoke(controller, null));
            yield return new WaitForSecondsRealtime(1f);
        }

        /// <summary>AUGA_TEST_ENCHANTING=1: an EpicLoot enchanting table spawned in front of the player, its UI opened and every tab screenshotted, then removed again.</summary>
        private static IEnumerator EnchantingTable()
        {
            if (Environment.GetEnvironmentVariable("AUGA_TEST_ENCHANTING") != "1") yield break;
            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("piece_enchantingtable") : null;
            if (prefab == null)
            {
                Debug.LogWarning("[AugaAutoStart] no piece_enchantingtable prefab (EpicLoot not loaded?)");
                yield break;
            }
            var player = Player.m_localPlayer;
            var position = player.transform.position + player.transform.forward * 2.5f;
            GameObject table = null;
            Try(() => table = UnityEngine.Object.Instantiate(prefab, position, Quaternion.LookRotation(-player.transform.forward)));
            if (table == null) yield break;
            yield return new WaitForSecondsRealtime(1f);
            var tableComponent = table.GetComponentsInChildren<MonoBehaviour>(true).FirstOrDefault(c => c != null && c.GetType().Name == "EnchantingTable");
            Try(() => Debug.Log($"[AugaAutoStart] enchanting table spawned: {table.name} components=[{string.Join(", ", table.GetComponents<Component>().Select(c => c != null ? c.GetType().Name : "null"))}]"));
            var uiType = tableComponent != null ? tableComponent.GetType().Assembly.GetType("EpicLoot_UnityLib.EnchantingTableUI") : null;
            if (uiType == null)
            {
                Debug.LogWarning("[AugaAutoStart] enchanting table component or UI type not found");
                Try(() => ZNetScene.instance.Destroy(table));
                yield break;
            }
            Debug.Log("[AugaAutoStart] open enchanting table UI");
            Try(() => uiType.GetMethod("Show", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { tableComponent }));
            yield return new WaitForSecondsRealtime(2f);
            var ui = uiType.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null) as Component;
            Debug.Log($"[AugaAutoStart] enchanting UI instance={(ui != null)} shown={(ui != null && ui.gameObject.activeInHierarchy)}");
            Shot("18_enchanting_0");
            yield return new WaitForSecondsRealtime(0.5f);
            if (ui != null)
            {
                DumpLive("EnchantingTableUI", ui);
                var tabHandler = uiType.GetField("TabHandler")?.GetValue(ui) as TabHandler;
                var count = tabHandler != null ? tabHandler.m_tabs.Count : 0;
                for (var i = 1; i < count; i++)
                {
                    var tab = tabHandler.m_tabs[i];
                    var name = tab.m_page != null ? tab.m_page.name : i.ToString();
                    Debug.Log("[AugaAutoStart] enchanting tab " + name);
                    Try(() => tabHandler.SetActiveTab(i));
                    yield return new WaitForSecondsRealtime(1f);
                    Shot("18_enchanting_" + i + "_" + name);
                    yield return new WaitForSecondsRealtime(0.5f);
                }
                Try(() => uiType.GetMethod("Hide", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null, null));
            }
            yield return new WaitForSecondsRealtime(0.5f);
            Try(() => ZNetScene.instance.Destroy(table));
            yield return new WaitForSecondsRealtime(0.5f);
        }

        private const string MagicItemKey = "randyknapp.mods.epicloot#EpicLoot.MagicItemComponent";

        /// <summary>
        /// Items carrying EpicLoot's component data. EpicLoot attaches that component to ordinary equipment as well
        /// (with no magic item inside), so this is a count for the log, never a list of things to delete.
        /// </summary>
        private static int CountMagicItems(Player player)
        {
            return player.GetInventory().GetAllItems().Count(i => i.m_customData != null && i.m_customData.ContainsKey(MagicItemKey));
        }

        /// <summary>Takes the items this run spawned out of the inventory again, so they do not pile up in the test character's save.</summary>
        private static void RemoveSpawnedItems(Player player, List<ItemDrop.ItemData> spawned)
        {
            var inventory = player.GetInventory();
            var removed = 0;
            foreach (var item in spawned)
            {
                if (!inventory.ContainsItem(item)) continue;
                if (item.m_equipped) player.UnequipItem(item);
                inventory.RemoveItem(item);
                removed++;
            }
            Debug.Log($"[AugaAutoStart] removed {removed} of the {spawned.Count} spawned items; {inventory.NrOfItems()} items remain");
        }

        private static void LogFonts()
        {
            var probe = new[] { '\u2023', '\u25C6', '\u25CA', '\u2666', '\u25BE', '\u25B2' };
            foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (font == null) continue;
                var has = string.Join("", probe.Select(c => font.HasCharacter(c) ? c.ToString() : "_"));
                var fallbacks = font.fallbackFontAssetTable != null ? string.Join(", ", font.fallbackFontAssetTable.Where(f => f != null).Select(f => f.name)) : "";
                Debug.Log($"[AugaAutoStart] font {font.name}: glyphs[{has}] population={font.atlasPopulationMode} fallbacks=[{fallbacks}]");
            }
            var globalFallbacks = TMP_Settings.fallbackFontAssets != null ? string.Join(", ", TMP_Settings.fallbackFontAssets.Where(f => f != null).Select(f => f.name)) : "";
            Debug.Log($"[AugaAutoStart] TMP settings fallbacks=[{globalFallbacks}] default={(TMP_Settings.defaultFontAsset != null ? TMP_Settings.defaultFontAsset.name : "null")}");
        }

        private static void LogTree(Transform t, int depth, int maxDepth)
        {
            var rt = t as RectTransform;
            var corners = new Vector3[4];
            if (rt != null) rt.GetWorldCorners(corners);
            var tmp = t.GetComponent<TMP_Text>();
            var extra = tmp != null ? $" font={(tmp.font != null ? tmp.font.name : "null")} text='{ShortText(tmp.text)}'" : "";
            var layoutElement = t.GetComponent<LayoutElement>();
            Debug.Log($"[AugaAutoStart] tree {new string(' ', depth * 2)}{t.name} active={t.gameObject.activeSelf} size={(rt != null ? rt.rect.size : Vector2.zero)} world=({corners[0].x:F0},{corners[0].y:F0})-({corners[2].x:F0},{corners[2].y:F0}) anchors={(rt != null ? rt.anchorMin + "-" + rt.anchorMax : "-")} pivot={(rt != null ? rt.pivot : Vector2.zero)} ignoreLayout={(layoutElement != null && layoutElement.ignoreLayout)}{extra}");
            if (depth >= maxDepth) return;
            for (var i = 0; i < t.childCount; i++) LogTree(t.GetChild(i), depth + 1, maxDepth);
        }

        private static string ShortText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Replace("\n", " | ");
            return text.Length > 50 ? text.Substring(0, 50) + "..." : text;
        }

        private static void LogTooltip(ItemDrop.ItemData item)
        {
            var tooltip = UITooltip.m_tooltip;
            if (tooltip == null)
            {
                Debug.LogWarning($"[AugaAutoStart] tooltip for {item.m_shared.m_name}: none instantiated");
                return;
            }
            Debug.Log($"[AugaAutoStart] tooltip for {item.m_shared.m_name}: {PathOf(tooltip.transform)} active={tooltip.activeInHierarchy} pos={tooltip.transform.position}");
            // Auga builds the scroll view once into a copy of the prefab, and every hover's tooltip is made from it
            var prefab = UITooltip.m_current != null ? UITooltip.m_current.m_tooltipPrefab : null;
            var scrollViews = tooltip.GetComponentsInChildren<ScrollRect>(true).Count(s => s.name == "Scroll View");
            Debug.Log($"[AugaAutoStart] tooltip for {item.m_shared.m_name} made from {(prefab != null ? PathOf(prefab.transform) : "null")}, {scrollViews} scroll view(s)");
            if (scrollViews != 1)
                AutoTest.Fail($"tooltip for {item.m_shared.m_name} has {scrollViews} scroll views");
            // by type name: GetComponent(string) misses types from Unity.Auga, which Auga loads itself
            if (prefab != null && prefab.GetComponents<Component>().Any(c => c != null && c.GetType().Name == "ComplexTooltip") && (prefab.transform.parent == null || prefab.transform.parent.name != "Auga Tooltip Templates"))
                AutoTest.Fail($"tooltip for {item.m_shared.m_name} was made from {PathOf(prefab.transform)}, not Auga's copy with the scroll view");
            LogTree(tooltip.transform, 0, 3);
            foreach (var text in tooltip.GetComponentsInChildren<TMP_Text>(false))
                Debug.Log($"[AugaAutoStart] tooltip text {text.name}: '{text.text.Replace("\n", " | ")}'");
        }

        /// <summary>The children of an inventory slot with their images: EpicLoot adds "magicItem" and "setItem" backgrounds.</summary>
        private static void LogMagicElement(InventoryElement element)
        {
            var sb = new StringBuilder();
            foreach (Transform child in element.transform)
            {
                var image = child.GetComponent<Image>();
                sb.Append(child.name).Append(child.gameObject.activeSelf ? "" : "(off)");
                if (image != null)
                    sb.Append('[').Append(image.enabled ? "" : "disabled ").Append(image.sprite != null ? image.sprite.name : "nosprite").Append(' ').Append(ColorUtility.ToHtmlStringRGBA(image.color)).Append(']');
                sb.Append(' ');
            }
            Debug.Log("[AugaAutoStart] element " + element.name + ": " + sb);
        }

        private static string PathOf(Transform t)
        {
            var sb = new StringBuilder(t.name);
            while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
            return sb.ToString();
        }

        /// <summary>Every active, visible Graphic covering at least 12% of the screen, keyed by path.</summary>
        private static Dictionary<string, string> BigOverlays()
        {
            var result = new Dictionary<string, string>();
            var corners = new Vector3[4];
            foreach (var g in UnityEngine.Object.FindObjectsOfType<Graphic>())
            {
                if (!g.isActiveAndEnabled) continue;
                var canvas = g.canvas;
                if (canvas == null) continue;
                var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                g.rectTransform.GetWorldCorners(corners);
                var min = new Vector2(float.MaxValue, float.MaxValue);
                var max = new Vector2(float.MinValue, float.MinValue);
                foreach (var c in corners)
                {
                    var p = RectTransformUtility.WorldToScreenPoint(cam, c);
                    min = Vector2.Min(min, p); max = Vector2.Max(max, p);
                }
                var w = Mathf.Min(max.x, Screen.width) - Mathf.Max(min.x, 0f);
                var h = Mathf.Min(max.y, Screen.height) - Mathf.Max(min.y, 0f);
                if (w <= 0f || h <= 0f) continue;
                var coverage = w * h / (Screen.width * (float)Screen.height);
                if (coverage < 0.12f) continue;
                var groupAlpha = 1f;
                foreach (var cg in g.GetComponentsInParent<CanvasGroup>(true)) groupAlpha *= cg.alpha;
                var alpha = g.color.a * g.canvasRenderer.GetAlpha() * groupAlpha;
                if (alpha < 0.02f) continue;
                var sprite = (g as Image)?.sprite;
                var path = PathOf(g.transform);
                result[path] = $"{coverage:P0} {g.GetType().Name} alpha={alpha:F2} (color {g.color.a:F2} * renderer {g.canvasRenderer.GetAlpha():F2} * groups {groupAlpha:F2}) sprite={(sprite != null ? sprite.name : "none")} screen=({min.x:F0},{min.y:F0})-({max.x:F0},{max.y:F0}) canvas={canvas.name} sort={canvas.sortingOrder}";
            }
            return result;
        }

        private static void SampleOverlays()
        {
            var now = BigOverlays();
            var t = Time.realtimeSinceStartup - _playerSeen;
            foreach (var kv in now)
                if (!_lastOverlays.ContainsKey(kv.Key)) Debug.Log($"[AugaAutoStart] overlay +{t:F1}s SHOWN {kv.Key}: {kv.Value}");
            foreach (var kv in _lastOverlays)
                if (!now.ContainsKey(kv.Key)) Debug.Log($"[AugaAutoStart] overlay +{t:F1}s GONE  {kv.Key}: {kv.Value}");
            _lastOverlays.Clear();
            foreach (var kv in now) _lastOverlays[kv.Key] = kv.Value;
        }

        private static void FindTexts(string needle)
        {
            foreach (var t in UnityEngine.Object.FindObjectsOfType<TMP_Text>())
                if (t.text != null && t.text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    Debug.Log($"[AugaAutoStart] text '{needle}' in TMP {PathOf(t.transform)} active={t.isActiveAndEnabled} alpha={t.color.a:F2}*{t.canvasRenderer.GetAlpha():F2} text='{t.text}'");
            foreach (var t in UnityEngine.Object.FindObjectsOfType<Text>())
                if (t.text != null && t.text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    Debug.Log($"[AugaAutoStart] text '{needle}' in Text {PathOf(t.transform)} active={t.isActiveAndEnabled} alpha={t.color.a:F2}*{t.canvasRenderer.GetAlpha():F2} text='{t.text}'");
        }

        /// <summary>Everything the key hint converter needs to know about the vanilla KeyHints object.</summary>
        private static void DumpKeyHintDetails()
        {
            var kh = KeyHints.instance;
            if (kh == null) return;
            var sb = new StringBuilder();
            string P(UnityEngine.Object o)
            {
                if (o == null) return "null";
                if (o is GameObject go) return PathOf(go.transform);
                if (o is Component c) return PathOf(c.transform);
                return o.name;
            }
            sb.AppendLine("== KeyHints fields");
            foreach (var f in typeof(KeyHints).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            {
                var v = f.GetValue(kh);
                if (v is UnityEngine.Object o) sb.AppendLine($"{f.Name} = {P(o)}");
                else if (v is Array arr) { var i = 0; foreach (var e in arr) sb.AppendLine($"{f.Name}[{i++}] = {P(e as UnityEngine.Object)}"); }
            }
            sb.AppendLine("== UIInputHint components");
            foreach (var hint in kh.GetComponentsInChildren<UIInputHint>(true))
            {
                sb.AppendLine($"{PathOf(hint.transform)}: gamepad={P(hint.m_gamepadHint)} keyboard={P(hint.m_mouseKeyboardHint)} gamepadMouse={P(hint.m_gamepadMouseHint)}");
                foreach (var ls in hint.m_inputLayoutSettings)
                    sb.AppendLine($"    layoutSetting {P(ls.m_hintObject)} layouts=[{string.Join(",", ls.m_enableForLayout)}]");
            }
            sb.AppendLine("== texts (raw = cached original with tokens)");
            var loc = Localization.instance;
            foreach (var t in kh.GetComponentsInChildren<TMP_Text>(true))
            {
                loc.textMeshStrings.TryGetValue(t, out var raw);
                sb.AppendLine($"{PathOf(t.transform)} active={t.gameObject.activeSelf} text='{t.text}' raw='{raw}' size={t.fontSize} font={(t.font != null ? t.font.name : "?")}");
            }
            foreach (var t in kh.GetComponentsInChildren<Text>(true))
            {
                loc.textStrings.TryGetValue(t, out var raw);
                sb.AppendLine($"{PathOf(t.transform)} [Text] active={t.gameObject.activeSelf} text='{t.text}' raw='{raw}'");
            }
            sb.AppendLine("== images");
            foreach (var img in kh.GetComponentsInChildren<Image>(true))
                sb.AppendLine($"{PathOf(img.transform)} active={img.gameObject.activeSelf} sprite={(img.sprite != null ? img.sprite.name : "none")} size={img.rectTransform.sizeDelta} color={img.color}");
            sb.AppendLine("== layout groups");
            foreach (var lg in kh.GetComponentsInChildren<HorizontalOrVerticalLayoutGroup>(true))
                sb.AppendLine($"{PathOf(lg.transform)} {lg.GetType().Name} spacing={lg.spacing} pad=({lg.padding.left},{lg.padding.right},{lg.padding.top},{lg.padding.bottom}) align={lg.childAlignment} ctrl=({lg.childControlWidth},{lg.childControlHeight}) expand=({lg.childForceExpandWidth},{lg.childForceExpandHeight}) rect={((RectTransform)lg.transform).rect.size} anchors={((RectTransform)lg.transform).anchorMin}-{((RectTransform)lg.transform).anchorMax}");
            File.WriteAllText(Path.Combine(ShotDir, "auga_keyhints_details.txt"), sb.ToString());
        }

        /// <summary>Every text under the open settings screen with its raw localization token (row labels, tab names, tooltips).</summary>
        private static void DumpSettingsTexts(string tag)
        {
            var settings = Settings.instance;
            if (settings == null) { Debug.Log("[AugaAutoStart] settings texts: no Settings.instance"); return; }
            var loc = Localization.instance;
            var sb = new StringBuilder();
            var root = settings.transform;
            foreach (var t in settings.GetComponentsInChildren<TMP_Text>(true))
            {
                loc.textMeshStrings.TryGetValue(t, out var raw);
                var path = PathOf(t.transform);
                var idx = path.IndexOf(root.name, StringComparison.Ordinal);
                if (idx >= 0) path = path.Substring(idx);
                var shown = (t.text ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
                sb.Append(path).Append('\t').Append(t.gameObject.activeInHierarchy ? "active" : "inactive")
                  .Append("\traw='").Append(raw).Append("'\ttext='").Append(shown).AppendLine("'");
            }
            File.WriteAllText(Path.Combine(ShotDir, "auga_settings_texts_" + tag + ".txt"), sb.ToString());
        }

        private static void LogKeyHintRects()
        {
            var kh = KeyHints.instance;
            if (kh == null) return;
            var corners = new Vector3[4];
            var sb = new StringBuilder();
            foreach (var rt in kh.GetComponentsInChildren<RectTransform>(false))
            {
                if (rt != kh.transform && rt.parent != kh.transform && (rt.parent == null || rt.parent.parent != kh.transform)) continue;
                rt.GetWorldCorners(corners);
                var canvas = rt.GetComponentInParent<Canvas>();
                var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                var a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                var b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                var layout = rt.GetComponent<HorizontalOrVerticalLayoutGroup>();
                sb.Append($"{rt.name}[active={rt.gameObject.activeSelf} screen=({a.x:F0},{a.y:F0})-({b.x:F0},{b.y:F0}) anchors={rt.anchorMin}-{rt.anchorMax} pos={rt.anchoredPosition} size={rt.sizeDelta} pivot={rt.pivot} align={(layout != null ? layout.childAlignment.ToString() : "-")}] ");
            }
            Debug.Log("[AugaAutoStart] keyhint rects (screen " + Screen.width + "x" + Screen.height + "): " + sb);
        }

        private static void LogKeyHintState(string tag)
        {
            var kh = KeyHints.instance;
            var chat = Chat.instance;
            var inv = InventoryGui.instance;
            var player = Player.m_localPlayer;
            Debug.Log($"[AugaAutoStart] keyhints[{tag}] instance={(kh != null ? PathOf(kh.transform) : "null")} activeAndEnabled={(kh != null && kh.isActiveAndEnabled)} " +
                      $"enabledSetting={(kh != null ? kh.m_keyHintsEnabled.ToString() : "?")} pref={PlatformPrefs.GetInt("KeyHints", 1)} " +
                      $"chatVisible={(chat != null && chat.IsChatDialogWindowVisible())} chatWindow={(chat != null && chat.m_chatWindow != null ? PathOf(chat.m_chatWindow.transform) + " activeSelf=" + chat.m_chatWindow.gameObject.activeSelf : "null")} " +
                      $"paused={Game.IsPaused()} skills={(inv != null && inv.IsSkillsPanelOpen)} trophies={(inv != null && inv.IsTrophisPanelOpen)} achievements={(inv != null && inv.IsAchievementsPanelOpen)} texts={(inv != null && inv.IsTextPanelOpen)} " +
                      $"weapon={(player != null && player.GetCurrentWeapon() != null ? player.GetCurrentWeapon().m_shared.m_name : "none")} " +
                      $"combatHints={(kh != null && kh.m_combatHints != null ? kh.m_combatHints.activeSelf.ToString() : "null")} rect={(kh != null ? ((RectTransform)kh.transform).anchoredPosition.ToString() : "?")}");
        }

        private static void Try(Action a)
        {
            try { a(); }
            catch (Exception e) { Debug.LogError("[AugaAutoStart] " + e); }
        }
    }
}
