using AugaUnity;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    // the tooltip vanilla is about to make comes from the copy of its prefab that already has the scroll view
    [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.OnHoverStart))]
    public static class UITooltip_OnHoverStart_Patch
    {
        public static void Prefix(UITooltip __instance)
        {
            __instance.m_tooltipPrefab = TooltipScroll.WithScrollView(__instance.m_tooltipPrefab);
        }
    }

    [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.UpdateTextElements))]
    public static class UITooltip_UpdateTextElements_Patch
    {
        public static bool Prefix(UITooltip __instance)
        {
            if (UITooltip.m_tooltip != null)
            {
                var customTooltip = UITooltip.m_tooltip.GetComponent<ComplexTooltip>();
                if (customTooltip != null)
                {
                    // already there unless the tooltip came from somewhere other than OnHoverStart
                    TooltipScroll.Ensure(customTooltip);

                    var itemTooltip = __instance.GetComponent<ItemTooltip>();
                    if (itemTooltip != null && itemTooltip.Item != null)
                    {
                        customTooltip.SetItem(itemTooltip.Item);
                        return false;
                    }

                    var foodTooltip = __instance.GetComponent<FoodTooltip>();
                    if (foodTooltip != null && foodTooltip.Food != null)
                    {
                        customTooltip.SetFood(foodTooltip.Food);
                        return false;
                    }

                    var statusTooltip = __instance.GetComponent<StatusTooltip>();
                    if (statusTooltip != null && statusTooltip.StatusEffect != null)
                    {
                        customTooltip.SetStatusEffect(statusTooltip.StatusEffect);
                        return false;
                    }

                    var skillTooltip = __instance.GetComponent<SkillTooltip>();
                    if (skillTooltip != null && skillTooltip.Skill != null)
                    {
                        customTooltip.SetSkill(skillTooltip.Skill, __instance);
                        return false;
                    }

                    customTooltip.SetDefault(__instance);
                }
            }

            return true;
        }
    }

    // after vanilla has moved the tooltip to the pointer: a tooltip taller than the screen scrolls instead
    [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.LateUpdate))]
    public static class UITooltip_LateUpdate_Patch
    {
        public static void Postfix(UITooltip __instance)
        {
            var tooltip = UITooltip.m_tooltip;
            if (UITooltip.m_current != __instance || tooltip == null || !tooltip.activeInHierarchy)
                return;

            var scroll = tooltip.GetComponentInChildren<TooltipScroll>();
            if (scroll != null)
                scroll.FitToScreen();
        }
    }
}
