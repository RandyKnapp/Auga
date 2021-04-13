using System.IO;
using System.Reflection;
using AugaUnity;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    public class Assets
    {
        public GameObject AugaLogo;
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
            LoadConfig();
            LoadAssets();

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
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{assembly.GetName().Name}.AugaUnityLib.dll");
            if (stream != null)
            {
                using (stream)
                {
                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    Assembly.Load(data);
                }
            }
        }

        private void LoadConfig()
        {
            _loggingEnabled = Config.Bind("Logging", "LoggingEnabled", false, "Enable logging");
            _logLevel = Config.Bind("Logging", "LogLevel", LogLevel.Info, "Only log messages of the selected level or higher");
        }

        private void LoadAssets()
        {
            var assetBundle = LoadAssetBundle("augaassets");
            Assets.AugaLogo = assetBundle.LoadAsset<GameObject>("AugaLogo");
        }

        public static AssetBundle LoadAssetBundle(string filename)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assetBundle = AssetBundle.LoadFromStream(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{filename}"));

            return assetBundle;
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
