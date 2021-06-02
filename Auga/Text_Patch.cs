using System.Globalization;
using AugaUnity;
using HarmonyLib;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch(typeof(Text))]
    public static class Text_Patch
    {
        public static TextInfo TextInfo = new CultureInfo("en-US").TextInfo;

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
            else if (__instance.GetComponent<TitleCase>() != null && !string.IsNullOrEmpty(value))
            {
                if (value.StartsWith("$"))
                {
                    value = Localization.instance.Localize(value);
                }
                value = TextInfo.ToTitleCase(value);
            }
            return true;
        }
    }
}
