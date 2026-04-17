using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Fixes missing script warnings after rebuilding Unity.Auga.dll.
/// Run via menu: Auga > Fix Missing Scripts
/// </summary>
public static class FixMissingScripts
{
    private const string DllPath = "Assets/ExternalLibraries/Unity.Auga.dll";

    [MenuItem("Auga/Fix Missing Scripts")]
    public static void Fix()
    {
        // Step 1: Reimport the DLL so Unity re-registers all type IDs
        Debug.Log("[FixMissingScripts] Reimporting Unity.Auga.dll...");
        AssetDatabase.ImportAsset(DllPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        // Step 2: Find all prefabs and remove components with missing scripts
        var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToList();

        int totalFixed = 0;
        var report = new List<string>();

        foreach (var path in prefabPaths)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            int fixedInPrefab = RemoveMissingScriptsRecursive(go, path);
            if (fixedInPrefab > 0)
            {
                PrefabUtility.SavePrefabAsset(go);
                totalFixed += fixedInPrefab;
                report.Add($"  {path}: removed {fixedInPrefab} missing script(s)");
            }
        }

        if (totalFixed == 0)
        {
            Debug.Log("[FixMissingScripts] No missing scripts found — all good!");
        }
        else
        {
            Debug.Log($"[FixMissingScripts] Removed {totalFixed} missing script(s):\n" + string.Join("\n", report));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Auga/Show Missing Scripts (dry run)")]
    public static void ShowMissing()
    {
        var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToList();

        bool found = false;
        foreach (var path in prefabPaths)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            var transforms = go.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                var comps = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (comps > 0)
                {
                    Debug.LogWarning($"[MissingScript] {path} → GameObject '{t.name}': {comps} missing script(s)");
                    found = true;
                }
            }
        }

        if (!found)
            Debug.Log("[FixMissingScripts] No missing scripts found in Assets/Prefabs.");
    }

    private static int RemoveMissingScriptsRecursive(GameObject root, string prefabPath)
    {
        int total = 0;
        var transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            if (count > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                total += count;
                Debug.LogWarning($"[FixMissingScripts] Removed {count} missing script(s) from '{t.name}' in {prefabPath}");
            }
        }
        return total;
    }
}
