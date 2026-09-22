using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AugaAutoStart
{
    // Test-only helper. Controlled by environment variables:
    //   AUGA_TEST_CHARACTER  profile filename (default "auga test")
    //   AUGA_TEST_WORLD      world name       (default "AugaTest")
    //   AUGA_TEST_SHOTS      folder for screenshots + hierarchy dumps (default %TEMP%\augashots)
    //   AUGA_TEST_QUIT       seconds after the UI exercise to quit the game (default 0 = never)
    //   AUGA_TEST_DUMP       "1" to dump the vanilla UI hierarchies (before any Awake patch runs)
    //   AUGA_TEST_ROWS       also screenshot the inventory with this many player rows and the container panel shown
    //   AUGA_TEST_SETTINGS   "1": screenshot every settings tab from the main menu and quit without starting a level
    [BepInPlugin("augatest.autostart", "Auga AutoStart (test)", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static string ShotDir;
        private bool _menuStarted;
        private bool _exercised;
        private float _nextSample;
        private static readonly Dictionary<string, string> _lastOverlays = new Dictionary<string, string>();
        private float _menuSeen = -1f;
        private static float _playerSeen = -1f;

        private void Awake()
        {
            ShotDir = Environment.GetEnvironmentVariable("AUGA_TEST_SHOTS");
            if (string.IsNullOrEmpty(ShotDir)) ShotDir = Path.Combine(Path.GetTempPath(), "augashots");
            Directory.CreateDirectory(ShotDir);
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
            Logger.LogInfo($"AugaAutoStart ready, screenshots -> {ShotDir}");
        }

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

        private void Update()
        {
            if (!_menuStarted)
            {
                var fejd = FejdStartup.instance;
                if (fejd != null)
                {
                    if (_menuSeen < 0f)
                    {
                        _menuSeen = Time.realtimeSinceStartup;
                        Debug.Log($"[AugaAutoStart] FejdStartup seen, mainMenu active={fejd.m_mainMenu != null && fejd.m_mainMenu.activeInHierarchy}");
                    }
                    if (Time.realtimeSinceStartup - _menuSeen > 8f)
                    {
                        _menuStarted = true;
                        StartCoroutine(AutoStart(fejd));
                    }
                }
            }

            if (Player.m_localPlayer != null && _playerSeen >= 0f && Time.realtimeSinceStartup - _playerSeen < 45f && Time.realtimeSinceStartup >= _nextSample)
            {
                // overlay monitor: log every big screen-covering graphic that appears or disappears after spawn
                _nextSample = Time.realtimeSinceStartup + 0.5f;
                Try(SampleOverlays);
            }

            if (!_exercised && Player.m_localPlayer != null)
            {
                if (_playerSeen < 0f) _playerSeen = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - _playerSeen > 8f)
                {
                    _exercised = true;
                    StartCoroutine(Exercise());
                }
            }
        }

        public static void Shot(string name)
        {
            var path = Path.Combine(ShotDir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[AugaAutoStart] screenshot {path}");
        }

        private static IEnumerator AutoStart(FejdStartup fejd)
        {
            Shot("00_mainmenu");
            yield return new WaitForSecondsRealtime(1f);
            var character = Environment.GetEnvironmentVariable("AUGA_TEST_CHARACTER");
            if (string.IsNullOrEmpty(character)) character = "auga test";
            var worldName = Environment.GetEnvironmentVariable("AUGA_TEST_WORLD");
            if (string.IsNullOrEmpty(worldName)) worldName = "AugaTest";

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
                Debug.Log("[AugaAutoStart] quitting (settings only)");
                Application.Quit();
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

            Debug.Log("[AugaAutoStart] OnStartGame");
            Try(() => fejd.OnStartGame());
            yield return new WaitForSecondsRealtime(2f);
            Shot("01_characterselect");
            yield return new WaitForSecondsRealtime(1f);

            Try(() => fejd.SetSelectedProfile(character));
            Debug.Log($"[AugaAutoStart] selected profile index {fejd.m_profileIndex} of {fejd.m_profiles?.Count}");
            yield return new WaitForSecondsRealtime(1f);
            Debug.Log("[AugaAutoStart] OnCharacterStart");
            Try(() => fejd.OnCharacterStart());
            yield return new WaitForSecondsRealtime(2f);
            Shot("02_worldselect");
            yield return new WaitForSecondsRealtime(1f);

            Try(() => fejd.m_world = fejd.FindWorld(worldName));
            Debug.Log($"[AugaAutoStart] world '{worldName}' found: {fejd.m_world != null}");
            if (fejd.m_world == null)
            {
                // create a local world with that name (deterministic seed) so later runs reuse it
                Debug.Log($"[AugaAutoStart] creating local world '{worldName}'");
                Try(() =>
                {
                    fejd.OnWorldNew();
                    fejd.m_newWorldName.text = worldName;
                    fejd.m_newWorldSeed.text = "AugaAutoTest";
                    fejd.OnNewWorldDone(true);
                    fejd.m_world = fejd.FindWorld(worldName);
                });
                yield return new WaitForSecondsRealtime(1f);
                Debug.Log($"[AugaAutoStart] world '{worldName}' after create: {fejd.m_world != null}");
                if (fejd.m_world == null) { Quit("no world"); yield break; }
            }
            Debug.Log("[AugaAutoStart] OnWorldStart");
            Try(() => fejd.OnWorldStart());

            // watchdog: if the player never spawns (e.g. the save failed to load), do not hang the test
            var deadline = Time.realtimeSinceStartup + 150f;
            while (Player.m_localPlayer == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (Player.m_localPlayer == null) Quit("player did not spawn within 150s");
        }

        private static void Quit(string why)
        {
            var quit = Environment.GetEnvironmentVariable("AUGA_TEST_QUIT");
            Debug.LogError($"[AugaAutoStart] giving up: {why}");
            if (int.TryParse(quit, out var seconds) && seconds > 0) Application.Quit();
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

        private static IEnumerator Exercise()
        {
            Shot("10_hud");
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
            Try(() => { if (hammer != null) Player.m_localPlayer.UnequipItem(hammer); });
            yield return new WaitForSecondsRealtime(1f);

            Debug.Log("[AugaAutoStart] open inventory");
            Try(() => InventoryGui.instance.Show(null));
            yield return new WaitForSecondsRealtime(2f);
            Shot("11_inventory");
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

            Try(() => InventoryGui.instance.Hide());
            yield return new WaitForSecondsRealtime(1f);

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
            Try(() => Player.m_localPlayer.Damage(new HitData { m_damage = { m_damage = 5f } }));
            yield return new WaitForSecondsRealtime(2f);
            Shot("15_damage");

            Debug.Log("[AugaAutoStart] exercise done");
            var quit = Environment.GetEnvironmentVariable("AUGA_TEST_QUIT");
            if (int.TryParse(quit, out var seconds) && seconds > 0)
            {
                yield return new WaitForSecondsRealtime(seconds);
                Debug.Log("[AugaAutoStart] quitting");
                Application.Quit();
            }
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
