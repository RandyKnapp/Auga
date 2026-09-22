using System.Globalization;
using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    /// <summary>
    /// Texts carrying an AlwaysUpper / TitleCase marker get their case fixed whenever text is assigned. TextMeshPro
    /// texts converted from classic Text also carry the UpperCase font style, so the TMP patch mainly serves
    /// TitleCase and texts that were localized on assignment.
    /// </summary>
    public static class TextCase
    {
        public static readonly TextInfo TextInfo = new CultureInfo("en-US").TextInfo;

        public static string Apply(Component component, string value)
        {
            if (component == null || string.IsNullOrEmpty(value))
                return value;
            if (component.GetComponent<AlwaysUpper>() != null)
            {
                if (value.StartsWith("$"))
                    value = Localization.instance.Localize(value);
                return value.ToUpper();
            }
            if (component.GetComponent<TitleCase>() != null)
            {
                if (value.StartsWith("$"))
                    value = Localization.instance.Localize(value);
                return TextInfo.ToTitleCase(value);
            }
            return value;
        }
    }

    [HarmonyPatch(typeof(Text))]
    public static class Text_Patch
    {
        public static TextInfo TextInfo => TextCase.TextInfo;

        [HarmonyPatch(nameof(Text.text), MethodType.Setter)]
        public static bool Prefix(Text __instance, ref string value)
        {
            value = TextCase.Apply(__instance, value);
            return true;
        }
    }

    [HarmonyPatch(typeof(TMP_Text))]
    public static class TMP_Text_Patch
    {
        [HarmonyPatch(nameof(TMP_Text.text), MethodType.Setter)]
        public static bool Prefix(TMP_Text __instance, ref string value)
        {
            value = TextCase.Apply(__instance, value);
            return true;
        }
    }
}
