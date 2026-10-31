using System;
using System.IO;
using Auga.Common;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace Auga
{
    // Every setting Auga binds, from the shared Jotunn template's ValConfig.
    //
    // Section and key names are the ones Auga has always used, so an existing randyknapp.mods.auga.cfg keeps its
    // values. Two kinds of entry:
    //   * client settings, per machine: the look of the HUD, logging. Bound with cfg.Bind.
    //   * server settings, bound with BindServerConfig: marked IsAdminOnly, which makes Jotunn's
    //     SynchronizationManager push the server's value to every client that joins a server running Auga, and
    //     lock the entry for non-admins in the Configuration Manager. On a server without Auga, or in single
    //     player, the local value applies.
    // The movable HUD elements bind their own sections later, at runtime (BindLate, from MovableHudElement).
    internal class ValConfig
    {
        public static ConfigFile cfg;

        // Client config entries
        public static ConfigEntry<bool> LoggingEnabled;
        public static ConfigEntry<LogLevel> LogLevel;

        public static ConfigEntry<bool> HealthBarShow;
        public static ConfigEntry<int> HealthBarFixedSize;
        public static ConfigEntry<StatBarTextDisplayMode> HealthBarTextDisplay;
        public static ConfigEntry<StatBarTextPosition> HealthBarTextPosition;
        public static ConfigEntry<bool> HealthBarShowTicks;

        public static ConfigEntry<bool> StaminaBarShow;
        public static ConfigEntry<int> StaminaBarFixedSize;
        public static ConfigEntry<StatBarTextDisplayMode> StaminaBarTextDisplay;
        public static ConfigEntry<StatBarTextPosition> StaminaBarTextPosition;
        public static ConfigEntry<bool> StaminaBarShowTicks;

        public static ConfigEntry<bool> EitrBarShow;
        public static ConfigEntry<int> EitrBarFixedSize;
        public static ConfigEntry<StatBarTextDisplayMode> EitrBarTextDisplay;
        public static ConfigEntry<StatBarTextPosition> EitrBarTextPosition;
        public static ConfigEntry<bool> EitrBarShowTicks;

        public static ConfigEntry<bool> AugaChatShow;

        public static ConfigEntry<bool> EnemyHudStarsBelowBar;

        // Server synced config entries
        public static ConfigEntry<bool> UseAugaTrash;
        public static ConfigEntry<float> ConfigPollIntervalSeconds;

        // How long a burst of setting changes has to go quiet before the config file is written.
        private const float SaveDelaySeconds = 1f;
        private static bool savePending = false;

        public ValConfig(ConfigFile cf)
        {
            cfg = cf;
            // Off for the whole session. Binding writes nothing; the plugin writes once when Awake finishes,
            // then EnableDeferredSave batches every later change.
            cfg.SaveOnConfigSet = false;
            CreateConfigValues(cf);

            ConfigFileWatcher.Register(cfg.ConfigFilePath, OnMainConfigFileChanged);
        }

        // An admin editing the file on a running server: reload it, and Jotunn sends the new server values to the
        // connected clients. A client must not reload: Jotunn has already replaced its in-memory server values with
        // the server's, and a reload would clobber them with whatever this machine happens to have on disk.
        private static void OnMainConfigFileChanged(string _)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer() == false) { return; }
            Auga.Log("Configuration file has been changed, reloading settings.");
            cfg.Reload();
        }

        // Called once plugin setup has finished binding.
        //
        // With SaveOnConfigSet on, BepInEx rewrites the whole file synchronously for each value that changes, and a
        // server config sync or an admin closing the Configuration Manager after a few edits changes several
        // entries in one frame. Changes now mark the file dirty and it is written once, after they settle.
        internal static void EnableDeferredSave()
        {
            cfg.SaveOnConfigSet = false;
            cfg.SettingChanged += (_, _) => SchedulePendingSave();
        }

        private static void SchedulePendingSave()
        {
            savePending = true;
            ConfigChangeDebouncer.Schedule(cfg, SavePending, SaveDelaySeconds);
        }

        // Writes the file if anything changed since the last write. The plugin also calls this on shutdown, since a
        // write still waiting on its delay would otherwise be lost when the game closes.
        internal static void SavePending()
        {
            if (savePending == false) { return; }
            savePending = false;
            try
            {
                Save();
            }
            catch (IOException e)
            {
                // Most likely another program has the file open. Leave it pending for the next change or shutdown.
                savePending = true;
                Auga.LogWarning($"Could not write the config file, will retry: {e.Message}");
            }
        }

        // Writes the file and re-stamps the watcher, so our own write is not taken for an external edit and reloaded
        // on the next poll.
        internal static void Save()
        {
            cfg.Save();
            ConfigFileWatcher.RefreshStamp(cfg.ConfigFilePath);
        }

        // A client setting bound after Awake (the movable HUD elements, when the HUD is built). Binding does not
        // raise SettingChanged, so a new entry would otherwise not reach the file until something else changed.
        internal static ConfigEntry<T> BindLate<T>(string section, string key, T defaultValue, string description)
        {
            var isNew = cfg.ContainsKey(new ConfigDefinition(section, key)) == false;
            var entry = cfg.Bind(section, key, defaultValue, description);
            if (isNew) { SchedulePendingSave(); }
            return entry;
        }

        private void CreateConfigValues(ConfigFile Config)
        {
            LoggingEnabled = Config.Bind("Logging", "LoggingEnabled", false, "Enable logging");
            LogLevel = Config.Bind("Logging", "LogLevel", BepInEx.Logging.LogLevel.Info, "Only log messages of the selected level or higher");

            HealthBarShow = Config.Bind("StatBars", "HealthBarShow", true, "If false, hides the health bar completely.");
            HealthBarFixedSize = Config.Bind("StatBars", "HealthBarFixedSize", 0, "If greater than 0, forces the health bar to be that many pixels long, regardless of the player's max health.");
            HealthBarTextDisplay = Config.Bind("StatBars", "HealthBarTextDisplay", StatBarTextDisplayMode.JustValue, "Changes how the label of the health bar is displayed.");
            HealthBarTextPosition = Config.Bind("StatBars", "HealthBarTextPosition", StatBarTextPosition.Center, "Changes where the label of the health bar is displayed.");
            HealthBarShowTicks = Config.Bind("StatBars", "HealthBarShowTicks", true, "Show a faint line on the bar every 25 units");

            StaminaBarShow = Config.Bind("StatBars", "StaminaBarShow", true, "If false, hides the stamina bar completely.");
            StaminaBarFixedSize = Config.Bind("StatBars", "StaminaBarFixedSize", 0, "If greater than 0, forces the stamina bar to be that many pixels long, regardless of the player's max stamina.");
            StaminaBarTextDisplay = Config.Bind("StatBars", "StaminaBarTextDisplay", StatBarTextDisplayMode.JustValue, "Changes how the label of the stamina bar is displayed.");
            StaminaBarTextPosition = Config.Bind("StatBars", "StaminaBarTextPosition", StatBarTextPosition.Center, "Changes where the label of the stamina bar is displayed.");
            StaminaBarShowTicks = Config.Bind("StatBars", "StaminaBarShowTicks", true, "Show a faint line on the bar every 25 units");

            EitrBarShow = Config.Bind("StatBars", "EitrBarShow", true, "If false, hides the eitr bar completely.");
            EitrBarFixedSize = Config.Bind("StatBars", "EitrBarFixedSize", 0, "If greater than 0, forces the eitr bar to be that many pixels long, regardless of the player's max eitr. Eitr bar still hides if max eitr is zero.");
            EitrBarTextDisplay = Config.Bind("StatBars", "EitrBarTextDisplay", StatBarTextDisplayMode.JustValue, "Changes how the label of the eitr bar is displayed.");
            EitrBarTextPosition = Config.Bind("StatBars", "EitrBarTextPosition", StatBarTextPosition.Center, "Changes where the label of the eitr bar is displayed.");
            // The key has always been "Eitr", not "EitrBarShowTicks"; renaming it would reset players' setting.
            EitrBarShowTicks = Config.Bind("StatBars", "Eitr", true, "Show a faint line on the bar every 25 units");
            // the bars take these when the HUD is built and again whenever one changes (a file reload raises it too)
            Config.SettingChanged += (_, e) =>
            {
                if (e.ChangedSetting.Definition.Section == "StatBars")
                    StatBars.Apply();
            };

            AugaChatShow = Config.Bind("AugaChat", "Show Auga Chat. Disable to use other mods. (Requires Restart)", true, "If false, disables the Auga Chat window display");

            EnemyHudStarsBelowBar = Config.Bind("EnemyHud", "StarsBelowHealthBar", true, "Show an enemy's level stars just below its health bar. If false, they are drawn on the bar, which leaves room for mods that write text below it.");
            EnemyHudStarsBelowBar.SettingChanged += (_, _) => EnemyHudBars.ApplyStarSetting();

            // Server synced. The trash button deletes items outright, which is a server's call rather than a display
            // preference.
            UseAugaTrash = BindServerConfig("Options", "UseAugaTrash", false, "Enable Auga's built in trash button. Click on the button while holding an item or part of a stack with the mouse. On a server running Auga, the server's setting applies to everyone.");
            UseAugaTrash.SettingChanged += (_, _) => InventoryPanel_Patches.ApplyTrashSetting();

            // Read by Common/ConfigFileWatcher, so it is bound rather than hardcoded: an admin editing the file wants
            // a short interval, a busy server a long one.
            ConfigPollIntervalSeconds = BindServerConfig("Config", "Config Poll Interval", 30f, "Seconds between checks for edits to this mod's config file. On a server, an edited file is reloaded and its server settings are sent to every player. Lower reacts faster to a hand edit, higher does less disk work.", true, 1f, 300f);
        }

        // Every overload below marks the entry IsAdminOnly, which is what makes Jotunn's SynchronizationManager push
        // the server's value to every client. Use cfg.Bind directly for anything that is genuinely per-machine (UI,
        // logging, client-side toggles).
        //
        // The numeric overloads come in two shapes: pass a min and max for the common bounded case, or pass an
        // AcceptableValueBase (null for unbounded) when a range is wrong for the setting.

        /// <summary>
        /// Binds a server-synced bool.
        /// </summary>
        public static ConfigEntry<bool> BindServerConfig(string category, string key, bool value, string description, AcceptableValueBase acceptableValues = null, bool advanced = false)
        {
            return cfg.Bind(category, key, value,
                new ConfigDescription(description,
                    acceptableValues,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        /// <summary>
        /// Binds a server-synced int constrained to a range.
        /// </summary>
        public static ConfigEntry<int> BindServerConfig(string category, string key, int value, string description, bool advanced = false, int valMin = 0, int valMax = 150)
        {
            return cfg.Bind(category, key, value,
                new ConfigDescription(description,
                    new AcceptableValueRange<int>(valMin, valMax),
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        /// <summary>
        /// Binds a server-synced int with an explicit constraint. Pass null for an unbounded value.
        /// </summary>
        public static ConfigEntry<int> BindServerConfig(string category, string key, int value, string description, AcceptableValueBase acceptableValues, bool advanced = false)
        {
            return cfg.Bind(category, key, value,
                new ConfigDescription(description,
                    acceptableValues,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        /// <summary>
        /// Binds a server-synced float constrained to a range.
        /// </summary>
        public static ConfigEntry<float> BindServerConfig(string category, string key, float value, string description, bool advanced = false, float valMin = 0, float valMax = 150)
        {
            return cfg.Bind(category, key, value,
                new ConfigDescription(description,
                    new AcceptableValueRange<float>(valMin, valMax),
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        /// <summary>
        /// Binds a server-synced float with an explicit constraint. Pass null for an unbounded value.
        /// </summary>
        public static ConfigEntry<float> BindServerConfig(string category, string key, float value, string description, AcceptableValueBase acceptableValues, bool advanced = false)
        {
            return cfg.Bind(category, key, value,
                new ConfigDescription(description,
                    acceptableValues,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        /// <summary>
        /// Binds a server-synced string, optionally restricted to a fixed list of values.
        /// </summary>
        public static ConfigEntry<string> BindServerConfig(string category, string key, string value, string description, AcceptableValueList<string> acceptableValues = null, bool advanced = false)
        {
            return cfg.Bind(category, key, value,
                new ConfigDescription(description,
                    acceptableValues,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }
    }
}
