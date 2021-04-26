using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class AugaLauncher : EditorWindow
{
    private const string ValheimPathPref = "Auga.ValheimPath";
    [MenuItem("Mod/Auga Launcher")]
    public static void Init()
    {
        var window = (AugaLauncher)GetWindow(typeof(AugaLauncher));
        window.Show();
    }

    public void OnGUI()
    {
        var valheimPath = EditorPrefs.GetString(ValheimPathPref);
        valheimPath = EditorGUILayout.TextField("Valheim Path", valheimPath);
        EditorPrefs.SetString(ValheimPathPref, valheimPath);

        if (GUILayout.Button(new GUIContent("Select Valheim Path")))
        {
            valheimPath = Path.GetFullPath(EditorUtility.OpenFolderPanel("Select Location of valheim.exe", @"C:\Program Files (x86)\Steam\steamapps\common\Valheim\", ""));
            EditorPrefs.SetString(ValheimPathPref, valheimPath);
        }

        if (GUILayout.Button(new GUIContent("Auga: Build & Launch"), GUILayout.Height(40)))
        {
            BuildAndLaunch();
        }

        EditorUtility.ClearProgressBar();
    }

    public void BuildAndLaunch()
    {
        EditorUtility.DisplayProgressBar("Auga: Build & Launch", "Checking Valheim Path...", 0.1f);
        var valheimPath = EditorPrefs.GetString(ValheimPathPref);
        if (string.IsNullOrEmpty(valheimPath))
        {
            valheimPath = Path.GetFullPath(EditorUtility.OpenFolderPanel("Select Location of valheim.exe", @"C:\Program Files (x86)\Steam\steamapps\common\Valheim\", ""));
            EditorPrefs.SetString(ValheimPathPref, valheimPath);
        }

        EditorUtility.DisplayProgressBar("Auga: Build & Launch", "Building Asset Bundles...", 0.3f);
        Thread.Sleep(1000);

        const string stageDirName = "AssetBundles";
        var projectRootDir = Path.Combine(Application.dataPath, "..");
        var stagePath = Path.Combine(projectRootDir, stageDirName);

        BuildPipeline.BuildAssetBundles(stagePath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        EditorUtility.DisplayProgressBar("Auga: Build & Launch", "Copying files...", 0.6f);
        var sourceAssetBundle = Path.GetFullPath(Path.Combine(stagePath, "augaassets"));
        var destAssetBundle = Path.GetFullPath(Path.Combine(valheimPath, "BepInEx/plugins/Auga/augaassets"));
        if (File.Exists(destAssetBundle))
        {
            FileUtil.ReplaceFile(sourceAssetBundle, destAssetBundle);
        }
        else
        {
            FileUtil.CopyFileOrDirectory(sourceAssetBundle, destAssetBundle);
        }
        Thread.Sleep(1000);

        EditorUtility.DisplayProgressBar("Auga: Build & Launch", "Launching Valheim...", 1.0f);
        Thread.Sleep(1000);

        var proc = new Process();
        proc.StartInfo.FileName = Path.Combine(valheimPath, "valheim.exe");
        proc.Start();

        EditorUtility.ClearProgressBar();
    }
}
