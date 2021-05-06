using HarmonyLib;

namespace Auga
{
    [HarmonyPatch]
    public static class EnemyHud_Setup
    {
        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.Awake))]
        public static class EnemyHud_Awake_Patch
        {
            public static bool Prefix(EnemyHud __instance)
            {
                return !SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.EnemyHud, "EnemyHud");
            }
        }
    }
}
