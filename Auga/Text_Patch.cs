using AugaUnity;
using HarmonyLib;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch(typeof(Text))]
    public static class Text_Patch
    {
        [HarmonyPatch(nameof(Text.text), MethodType.Setter)]
        public static bool Prefix(Text __instance, ref string value)
        {
            if (__instance.GetComponent<AlwaysUpper>() != null && !string.IsNullOrEmpty(value))
            {
                if (value.StartsWith("$"))
                {
                    value = Localization.instance.Localize(value);
                }
                value = value.ToUpper();
            }
            return true;
        }
    }
}
