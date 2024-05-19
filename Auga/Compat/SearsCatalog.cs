using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Auga.Compat;

public static class SearsCatalog
{
    public static Assembly SearsCatalogType;
    public static Type HudPatch;
    
    [HarmonyPriority(Priority.Last)]
    public static void AwakePostfix_Patch(ref Hud __instance)
    {
        __instance.m_pieceListRoot.RectTransform().localPosition = new Vector3(__instance.m_pieceListRoot.RectTransform().localPosition.x+3, __instance.m_pieceListRoot.RectTransform().localPosition.y-3, __instance.m_pieceListRoot.RectTransform().localPosition.z);
    }

}