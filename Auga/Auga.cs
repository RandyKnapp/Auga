using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using fastJSON;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    public class Assets
    {
        public GameObject AugaLogo;
        public GameObject InventoryScreen;
        public Texture2D Cursor;
        public GameObject MenuPrefab;
    }

    [BepInPlugin(PluginID, "Project Auga", Version)]
    public class Auga : BaseUnityPlugin
    {
        public const string PluginID = "mods.randyknapp.auga";
        public const string Version = "1.0.0";

        private static ConfigEntry<bool> _loggingEnabled;
        private static ConfigEntry<LogLevel> _logLevel;

        public static readonly Assets Assets = new Assets();
        private static Auga _instance;
        private Harmony _harmony;

        public void Awake()
        {
            _instance = this;

            LoadDependencies();
            LoadTranslations();
            LoadConfig();
            LoadAssets();

            ApplyCursor();

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginID);
        }

        public void OnDestroy()
        {
            _harmony.UnpatchSelf();
            _instance = null;
        }

        private void LoadDependencies()
        {
            var assembly = Assembly.GetCallingAssembly();
            LoadEmbeddedAssembly(assembly, "fastJSON.dll");
            LoadEmbeddedAssembly(assembly, "Unity.Auga.dll");
        }

        private static void LoadEmbeddedAssembly(Assembly assembly, string assemblyName)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{assembly.GetName().Name}.{assemblyName}");
            if (stream == null)
            {
                LogError($"Could not load embedded assembly ({assemblyName})!");
                return;
            }

            using (stream)
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                Assembly.Load(data);
            }
        }

        private static void LoadTranslations()
        {
            var translationsJsonText = LoadJsonText("translations.json");
            if (string.IsNullOrEmpty(translationsJsonText))
            {
                return;
            }

            var translations = (IDictionary<string, object>)JSON.Parse(translationsJsonText);
            foreach (var translation in translations)
            {
                if (!string.IsNullOrEmpty(translation.Key) && !string.IsNullOrEmpty(translation.Value.ToString()))
                {
                    Localization.instance.AddWord(translation.Key, translation.Value.ToString());
                }
            }
        }

        private void LoadConfig()
        {
            _loggingEnabled = Config.Bind("Logging", "LoggingEnabled", false, "Enable logging");
            _logLevel = Config.Bind("Logging", "LogLevel", LogLevel.Info, "Only log messages of the selected level or higher");
        }

        private static void LoadAssets()
        {
            var assetBundle = LoadAssetBundle("augaassets");
            Assets.AugaLogo = assetBundle.LoadAsset<GameObject>("AugaLogo");
            Assets.InventoryScreen = assetBundle.LoadAsset<GameObject>("Inventory_screen");
            Assets.Cursor = assetBundle.LoadAsset<Texture2D>("Cursor");
            Assets.MenuPrefab = assetBundle.LoadAsset<GameObject>("AugaMenu");
        }

        private static void ApplyCursor()
        {
            Cursor.SetCursor(Assets.Cursor, new Vector2(6, 5), CursorMode.Auto);
        }

        public static AssetBundle LoadAssetBundle(string filename)
        {
            // Optionally load asset bundle from path, if it exists
            var assetBundlePath = GetAssetPath(filename);
            if (!string.IsNullOrEmpty(assetBundlePath))
            {
                return AssetBundle.LoadFromFile(assetBundlePath);
            }

            var assembly = Assembly.GetCallingAssembly();
            var assetBundle = AssetBundle.LoadFromStream(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{filename}"));

            return assetBundle;
        }

        public static string LoadJsonText(string filename)
        {
            var jsonFileName = GetAssetPath(filename);
            return !string.IsNullOrEmpty(jsonFileName) ? File.ReadAllText(jsonFileName) : null;
        }

        public static string GetAssetPath(string assetName)
        {
            var assetFileName = Path.Combine(Paths.PluginPath, "Auga", assetName);
            if (!File.Exists(assetFileName))
            {
                var assembly = typeof(Auga).Assembly;
                assetFileName = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, assetName);
                if (!File.Exists(assetFileName))
                {
                    LogError($"Could not find asset ({assetName})");
                    return null;
                }
            }

            return assetFileName;
        }

        public static void Log(string message)
        {
            if (_loggingEnabled.Value && _logLevel.Value <= LogLevel.Info)
            {
                _instance.Logger.LogInfo(message);
            }
        }

        public static void LogWarning(string message)
        {
            if (_loggingEnabled.Value && _logLevel.Value <= LogLevel.Warning)
            {
                _instance.Logger.LogWarning(message);
            }
        }

        public static void LogError(string message)
        {
            if (_loggingEnabled.Value && _logLevel.Value <= LogLevel.Error)
            {
                _instance.Logger.LogError(message);
            }
        }
    }
}
