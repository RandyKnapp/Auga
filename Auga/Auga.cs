using System.Reflection;
using System.Threading;
using Auga.Compat;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn.Utils;
using UnityEngine;

namespace Auga
{
    [BepInPlugin(PluginID, PluginName, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("Menthus.bepinex.plugins.BetterTrader", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("maximods.valheim.multicraft", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("redseiko.valheim.chatter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("redseiko.valheim.searscatalog", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.github.abearcodes.valheim.simplerecycling", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("org.bepinex.plugins.jewelcrafting", BepInDependency.DependencyFlags.SoftDependency)]
    // Full UI replacements that redraw the same screens: BepInEx skips Auga when one of them is installed and says
    // so in the log, instead of two mods fighting over the HUD. Reasons and candidates: Docs/Compatibility.md.
    // Never list a mod that already declares Auga incompatible on its side (ExtraSlots, AzuEPI, MyLittleUI, Almanac).
    [BepInIncompatibility("seneaL.valheim.ui")]          // SeneaL UI
    [BepInIncompatibility("MK_BetterUI")]                // BetterUI Reforged
    [BepInIncompatibility("zenox.betterui")]             // BetterUI (Zenox, the 1.0 rewrite)
    [BepInIncompatibility("Azumatt.MinimalUI")]          // Minimal UI
    [BepInIncompatibility("ZenDragon.AugaLite")]         // AugaLite, a trimmed copy of Auga 1.3.11
    // A UI mod: a server or client without Auga is fine either way. With Auga on both, the server's settings
    // (ValConfig.BindServerConfig) are synced to the client.
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public partial class Auga : BaseUnityPlugin
    {
        public const string PluginID = "randyknapp.mods.auga";
        public const string PluginName = "Project Auga";
        public const string Version = "1.3.13";

        public static readonly AugaAssets Assets = new AugaAssets();
        public static readonly AugaColors Colors = new AugaColors();

        public static bool HasBetterTrader;
        public static bool HasMultiCraft;
        public static bool HasSimpleRecycling;
        public static bool HasChatter;
        public static bool HasSearsCatalog;
        public static bool HasJewelcrafting;

        private static Auga _instance;
        private Harmony _harmony;
        internal ValConfig cfg;

        public static Auga instance => _instance;

        public void Awake()
        {
            _instance = this;
            if (int.TryParse(Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.')[3],out var revision))
            {
                if (revision > 0)
                {
                    Debug.LogWarning($"==============================================================================");
                    Debug.LogWarning($"You are using a PTB version of this mod. It will not work in prior versions.");
                    Debug.LogWarning($"Project Auga - Version {Assembly.GetExecutingAssembly().GetName().Version}");
                    Debug.LogWarning($"Valheim - Version {(global::Version.GetVersionString())}");

                    if ((global::Version.CurrentVersion.m_minor == 217 && global::Version.CurrentVersion.m_patch >= 27 ) || global::Version.CurrentVersion.m_minor > 217)
                    {
                        Debug.LogWarning($"GAME VERSION CHECK - PASSED");
                        Debug.LogWarning($"==============================================================================");
                    }
                    else
                    {
                        Debug.LogError($">>>>>>>>> GAME VERSION MISMATCH - EXITING <<<<<<<<");
                        Debug.LogWarning($"==============================================================================");
                        Thread.Sleep(10000);

                        Destroy(this);
                        return;
                    }
                }
            }

            cfg = new ValConfig(Config);
            // All startup hooks go after the config, which the logging reads.

            LoadDependencies();
            APIManager.Patcher.Patch();
            LoadTranslations();
            LoadAssets();

            ApplyCursor();

            HasBetterTrader = Chainloader.PluginInfos.ContainsKey("Menthus.bepinex.plugins.BetterTrader");
            HasMultiCraft  = Chainloader.PluginInfos.TryGetValue("maximods.valheim.multicraft", out var multiCraftPlugin);
            HasSimpleRecycling  = Chainloader.PluginInfos.TryGetValue("com.github.abearcodes.valheim.simplerecycling", out var recyclingPlugin);
            HasChatter = Chainloader.PluginInfos.TryGetValue("redseiko.valheim.chatter", out var chatterPlugin);
            HasSearsCatalog = Chainloader.PluginInfos.TryGetValue("redseiko.valheim.searscatalog", out var searsPlugin);
            HasJewelcrafting = Chainloader.PluginInfos.TryGetValue("org.bepinex.plugins.jewelcrafting", out var jewelcraftingPlugin);

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginID);

            // Soft-dependency compatibility, patched by reflection and only when that mod is installed.
            if (HasChatter)
                Chatter.Apply(_harmony, chatterPlugin);
            if (HasSearsCatalog)
                SearsCatalog.Apply(_harmony, searsPlugin);
            if (HasJewelcrafting)
                Jewelcrafting.Apply(_harmony, jewelcraftingPlugin);
            if (HasMultiCraft)
                MultiCraft.Apply(_harmony, multiCraftPlugin);
            if (HasSimpleRecycling)
                SimpleRecycling.Apply(_harmony, recyclingPlugin);

            // The config watcher, after ValConfig registered the file it watches.
            ConfigFileWatcher.Initialize();

            // Configs are not written until after they are all wired up, they exist in memory before this. Later
            // changes are written in batches rather than one full rewrite per value (see ValConfig.EnableDeferredSave).
            ValConfig.Save();
            ValConfig.EnableDeferredSave();
        }

        public void OnDestroy()
        {
            // A config write still waiting on its delay would otherwise be lost when the game closes.
            ValConfig.SavePending();
            _instance = null;
        }

        [UsedImplicitly]
        public void Update()
        {
            StatBars.Apply();
        }
    }
}
