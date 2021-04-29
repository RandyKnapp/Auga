using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class AugaLauncher : EditorWindow
{
    private const string ValheimPathPref = "Auga.ValheimPath";
    [MenuItem("Auga/Show Launcher Window")]
    public static void Init()
    {
        var window = (AugaLauncher)GetWindow(typeof(AugaLauncher));
        window.Show();
    }

    public void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        var valheimPath = EditorPrefs.GetString(ValheimPathPref);
        valheimPath = EditorGUILayout.TextField("Valheim Path", valheimPath);
        EditorPrefs.SetString(ValheimPathPref, valheimPath);

        if (GUILayout.Button(new GUIContent("..."), GUILayout.Width(18)))
        {
            var startPath = string.IsNullOrEmpty(valheimPath) ? @"C:\Program Files (x86)\Steam\steamapps\common\Valheim\" : valheimPath;
            var newPath = EditorUtility.OpenFolderPanel("Select Location of valheim.exe", startPath, "");
            if (!string.IsNullOrEmpty(newPath))
            {
                valheimPath = Path.GetFullPath(newPath);
            }
            EditorPrefs.SetString(ValheimPathPref, valheimPath);
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(new GUIContent("▶ Build & Launch"), GUILayout.Height(40)))
        {
            BuildAndLaunch();
        }
        if (GUILayout.Button(new GUIContent("Launch"), GUILayout.Height(40)))
        {
            LaunchValheim();
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(new GUIContent("▷ Build Asset Bundles"), GUILayout.Height(40)))
        {
            BuildAssetBundles();
        }

        var assetBundlePath = GetAssetBundleSourcePath();
        GUI.enabled = File.Exists(assetBundlePath);
        if (GUILayout.Button(new GUIContent("Deploy"), GUILayout.Height(40)))
        {
            DeployAssetFile();
        }

        var assetBundleDestinationPath = GetAssetBundleDestinationPath();
        GUI.enabled = File.Exists(assetBundleDestinationPath);
        if (GUILayout.Button(new GUIContent("Clear"), GUILayout.Height(40)))
        {
            RemoveDeployedAssetFile();
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (GUI.Button(position, "", GUIStyle.none))
        {
            GUI.FocusControl(null); 
        }
    }

    [MenuItem("Auga/▶ Build && Launch")]
    public static void BuildAndLaunch()
    {
        EditorUtility.DisplayProgressBar("Auga", "Checking Valheim Path...", 0.1f);
        var valheimPath = EditorPrefs.GetString(ValheimPathPref);
        if (string.IsNullOrEmpty(valheimPath))
        {
            valheimPath = Path.GetFullPath(EditorUtility.OpenFolderPanel("Select Location of valheim.exe", @"C:\Program Files (x86)\Steam\steamapps\common\Valheim\", ""));
            EditorPrefs.SetString(ValheimPathPref, valheimPath);
        }

        EditorUtility.DisplayProgressBar("Auga", "Building Asset Bundles...", 0.3f);
        Thread.Sleep(500);

        BuildAssetBundles();
        DeployAssetFile();
        LaunchValheim();
    }

    [MenuItem("Auga/Launch Valheim")]
    public static void LaunchValheim()
    {
        EditorUtility.DisplayProgressBar("Auga", "Launching Valheim...", 1.0f);
        var proc = new Process {
            StartInfo = {
                FileName = Path.Combine(GetValheimPath(), "valheim.exe"),
                Arguments = "-console"
            }
        };
        proc.Start();
        Thread.Sleep(800);

        EditorUtility.ClearProgressBar();
    }

    private static string GetStagePath()
    {
        const string stageDirName = "AssetBundles";
        var projectRootDir = Path.Combine(Application.dataPath, "..");
        return Path.GetFullPath(Path.Combine(projectRootDir, stageDirName));
    }

    private static string GetValheimPath()
    {
        return EditorPrefs.GetString(ValheimPathPref);
    }

    private static string GetAssetBundleSourcePath()
    {
        return Path.GetFullPath(Path.Combine(GetStagePath(), "augaassets"));
    }

    private static string GetAssetBundleDestinationPath()
    {
        return Path.GetFullPath(Path.Combine(GetValheimPath(), "BepInEx/plugins/Auga/augaassets"));
    }

    [MenuItem("Auga/▷ Build Asset Bundles")]
    public static void BuildAssetBundles()
    {
        var stagePath = GetStagePath();
        BuildPipeline.BuildAssetBundles(stagePath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        Debug.Log("Finished building Auga asset bundles");
    }

    [MenuItem("Auga/Deploy Asset Bundle")]
    public static void DeployAssetFile()
    {
        EditorUtility.DisplayProgressBar("Auga: Build & Launch", "Deploying files...", 0.6f);
        var sourceAssetBundle = GetAssetBundleSourcePath();
        var destAssetBundle = GetAssetBundleDestinationPath();
        if (File.Exists(destAssetBundle))
        {
            FileUtil.ReplaceFile(sourceAssetBundle, destAssetBundle);
        }
        else
        {
            FileUtil.CopyFileOrDirectory(sourceAssetBundle, destAssetBundle);
        }
        Thread.Sleep(1000);
        EditorUtility.ClearProgressBar();
    }

    [MenuItem("Auga/Clear Deployed Asset Bundle")]
    public static void RemoveDeployedAssetFile()
    {
        EditorUtility.DisplayProgressBar("Auga: Build & Launch", "Clearing out deployed files...", 0.45f);
        var destAssetBundle = GetAssetBundleDestinationPath();

        if (File.Exists(destAssetBundle))
        {
            FileUtil.DeleteFileOrDirectory(destAssetBundle);
        }

        Thread.Sleep(500);
        EditorUtility.ClearProgressBar();
    }
}
