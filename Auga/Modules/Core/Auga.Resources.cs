using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using Newtonsoft.Json;
using UnityEngine;

namespace Auga
{
    // The files Auga carries inside Auga.dll: the assemblies the prefabs need, the asset bundle and the translations.
    // A loose file of the same name in BepInEx\plugins\Auga\ or next to Auga.dll wins over the embedded one
    // (GetAssetPath), so a newly built bundle or an edited translations.json can be tried without a rebuild.
    public partial class Auga
    {
        private void LoadDependencies()
        {
            // Auga's own assembly, named explicitly: GetCallingAssembly() answered with whatever called Awake, and
            // when Awake is detoured (a Harmony or MonoMod hook from another mod) that is a dynamic assembly, so the
            // embedded resources ("Auga.<file>") were looked up under the wrong name and never found
            var assembly = typeof(Auga).Assembly;
            LoadEmbeddedAssembly(assembly, "ui_lib.dll"); // Fishlabs.GuiInputField shim for the prefabs (see UiLibShim)
            LoadEmbeddedAssembly(assembly, "Unity.Auga.dll");
        }

        private static void LoadEmbeddedAssembly(Assembly assembly, string assemblyName)
        {
            var resourceName = $"{assembly.GetName().Name}.{assemblyName}";
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                LogError($"Could not load embedded assembly ({assemblyName}): no resource '{resourceName}' in {assembly.GetName().Name}. Resources: {string.Join(", ", assembly.GetManifestResourceNames())}");
                return;
            }

            using (stream)
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                Assembly.Load(data);
            }
        }

        // translations.json is a flat object of word -> text, the keys without the "$" Valheim's localization adds.
        private static void LoadTranslations()
        {
            var translationsJsonText = LoadJsonText("translations.json");
            if (string.IsNullOrEmpty(translationsJsonText))
            {
                return;
            }

            Dictionary<string, string> translations;
            try
            {
                translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(translationsJsonText);
            }
            catch (JsonException e)
            {
                LogError($"Could not read translations.json: {e.Message}");
                return;
            }

            foreach (var translation in translations)
            {
                if (!string.IsNullOrEmpty(translation.Key) && !string.IsNullOrEmpty(translation.Value))
                {
                    Localization.instance.AddWord(translation.Key, translation.Value);
                }
            }
        }

        private static void LoadAssets()
        {
            Assets.Load(LoadAssetBundle("augaassets"));
        }

        private static void ApplyCursor()
        {
            Cursor.SetCursor(Assets.Cursor, new Vector2(6, 5), CursorMode.Auto);
        }

        public static AssetBundle LoadAssetBundle(string filename)
        {
            // Optionally load asset bundle from path, if it exists
            var assetBundlePath = GetAssetPath(filename, quiet: true);
            if (!string.IsNullOrEmpty(assetBundlePath))
            {
                return AssetBundle.LoadFromFile(assetBundlePath);
            }

            var stream = OpenEmbeddedFile(filename);
            if (stream == null)
            {
                LogError($"Could not load the embedded asset bundle ({filename}); no such resource in {typeof(Auga).Assembly.GetName().Name}.");
                return null;
            }
            var assetBundle = AssetBundle.LoadFromStream(stream);

            return assetBundle;
        }

        public static string LoadJsonText(string filename)
        {
            var jsonFileName = GetAssetPath(filename, quiet: true);
            if (!string.IsNullOrEmpty(jsonFileName))
            {
                return File.ReadAllText(jsonFileName);
            }

            using var stream = OpenEmbeddedFile(filename);
            if (stream == null)
            {
                LogError($"Could not find {filename}, neither as a file nor embedded in {typeof(Auga).Assembly.GetName().Name}.");
                return null;
            }
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static string GetAssetPath(string assetName)
        {
            return GetAssetPath(assetName, quiet: false);
        }

        private static string GetAssetPath(string assetName, bool quiet)
        {
            var assetFileName = Path.Combine(Paths.PluginPath, "Auga", assetName);
            if (!File.Exists(assetFileName))
            {
                var assembly = typeof(Auga).Assembly;
                assetFileName = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, assetName);
                if (!File.Exists(assetFileName))
                {
                    if (!quiet)
                        LogError($"Could not find asset ({assetName})");
                    return null;
                }
            }

            return assetFileName;
        }

        // "Auga.<file>", see the EmbeddedResource items in Auga.csproj.
        private static Stream OpenEmbeddedFile(string filename)
        {
            var assembly = typeof(Auga).Assembly;   // not GetCallingAssembly(): see LoadDependencies
            return assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{filename}");
        }
    }
}
