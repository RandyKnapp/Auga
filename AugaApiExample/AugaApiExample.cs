using System.Reflection;
using JetBrains.Annotations;

namespace AugaApiExample
{
    [BepInPlugin(PluginID, "Auga API Example", "1.0.0")]
    [BepInDependency("randyknapp.mods.auga", BepInDependency.DependencyFlags.SoftDependency)]
    public class AugaApiExample : BaseUnityPlugin
    {
        public const string PluginID = "mod.randyknapp.augaapiexample";

        private Harmony _harmony;

        [UsedImplicitly]
        public void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginID);
        }

        [UsedImplicitly]
        public void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
