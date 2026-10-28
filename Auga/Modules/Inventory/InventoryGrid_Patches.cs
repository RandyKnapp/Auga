using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga;

public static class InventoryGrid_Patches
{ 
  [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))] 
  public static class UpdateGuiPatch
  { 
    public static bool Prefix(InventoryGrid __instance, Player player, ItemDrop.ItemData dragItem)
    {
      var transform = __instance.transform as RectTransform;
      var width = __instance.m_inventory.GetWidth();
      var height = __instance.m_inventory.GetHeight();
      if (__instance.m_selected.x >= width - 1)
        __instance.m_selected.x = width - 1;
      if (__instance.m_selected.y >= height - 1)
        __instance.m_selected.y = height - 1;
      if (__instance.m_width != width || __instance.m_height != height)
      {
        __instance.m_width = width;
        __instance.m_height = height;
        foreach (var element in __instance.m_elements)
          UnityEngine.Object.Destroy((UnityEngine.Object) element.m_go);
        __instance.m_elements.Clear();
        var widgetSize = __instance.GetWidgetSize();
        var vector2_1 = new Vector2(transform.rect.width / 2f, 0.0f) - new Vector2(widgetSize.x, 0.0f) * 0.5f;
        for (var _y = 0; _y < height; ++_y)
        {
          for (var _x = 0; _x < width; ++_x)
          {
            var vector2_2 = (Vector2) new Vector3((float) _x * __instance.m_elementSpace, (float) _y * -__instance.m_elementSpace);
            var gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.m_elementPrefab, (Transform) __instance.m_gridRoot);
            (gameObject.transform as RectTransform).anchoredPosition = vector2_1 + vector2_2;
            var componentInChildren = gameObject.GetComponentInChildren<UIInputHandler>();
            componentInChildren.m_onRightDown += new Action<UIInputHandler>(__instance.OnRightClick);
            componentInChildren.m_onLeftDown += new Action<UIInputHandler>(__instance.OnLeftClick);
            var component = gameObject.transform.Find("binding").GetComponent<TMP_Text>();
            if ((bool) (UnityEngine.Object) player && _y == 0)
              component.text = (_x + 1).ToString();
            else
              component.enabled = false;
            __instance.m_elements.Add(new InventoryGrid.Element()
            {
              m_pos = new Vector2i(_x, _y),
              m_go = gameObject,
              m_icon = gameObject.transform.Find("icon").GetComponent<Image>(),
              m_amount = gameObject.transform.Find("amount").GetComponent<TMP_Text>(),
              m_quality = gameObject.transform.Find("quality").GetComponent<TMP_Text>(),
              m_equiped = gameObject.transform.Find("equiped").GetComponent<Image>(),
              m_queued = gameObject.transform.Find("queued").GetComponent<Image>(),
              m_noteleport = gameObject.transform.Find("noteleport").GetComponent<Image>(),
              m_food = gameObject.transform.Find("foodicon").GetComponent<Image>(),
              m_selected = gameObject.transform.Find("selected").gameObject,
              m_tooltip = gameObject.GetComponent<UITooltip>(),
              m_durability = gameObject.transform.Find("durability").GetComponent<GuiBar>()
            });
          }
        }
      }
      foreach (var element in __instance.m_elements)
        element.m_used = false;
      var flag1 = __instance.m_uiGroup.IsActive && ZInput.IsGamepadActive();
      var allItems = __instance.m_inventory.GetAllItems();
      var element1 = flag1 ? __instance.GetElement(__instance.m_selected.x, __instance.m_selected.y, width) : __instance.GetHoveredElement();
      foreach (var itemData in allItems)
      {
        var element2 = __instance.GetElement(itemData.m_gridPos.x, itemData.m_gridPos.y, width);
        element2.m_used = true;
        element2.m_icon.enabled = true;
        element2.m_icon.sprite = itemData.GetIcon();
        element2.m_icon.color = itemData == dragItem ? Color.grey : Color.white;
        var flag2 = itemData.m_shared.m_useDurability && (double) itemData.m_durability < (double) itemData.GetMaxDurability();
        element2.m_durability.gameObject.SetActive(flag2);
        if (flag2)
        {
          if ((double) itemData.m_durability <= 0.0)
          {
            element2.m_durability.SetValue(1f);
            element2.m_durability.SetColor((double) Mathf.Sin(Time.time * 10f) > 0.0 ? Color.red : new Color(0.0f, 0.0f, 0.0f, 0.0f));
          }
          else
          {
            element2.m_durability.SetValue(itemData.GetDurabilityPercentage());
            element2.m_durability.ResetColor();
          }
        }
        element2.m_equiped.enabled = (bool) (UnityEngine.Object) player && itemData.m_equipped;
        element2.m_queued.enabled = (bool) (UnityEngine.Object) player && player.IsEquipActionQueued(itemData);
        element2.m_noteleport.enabled = !itemData.m_shared.m_teleportable && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll);
        if (itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable && ((double) itemData.m_shared.m_food > 0.0 || (double) itemData.m_shared.m_foodStamina > 0.0 || (double) itemData.m_shared.m_foodEitr > 0.0))
        {
          element2.m_food.enabled = true;
          if ((double) itemData.m_shared.m_food < (double) itemData.m_shared.m_foodEitr / 2.0 && (double) itemData.m_shared.m_foodStamina < (double) itemData.m_shared.m_foodEitr / 2.0)
            element2.m_food.color = __instance.m_foodEitrColor;
          else if ((double) itemData.m_shared.m_foodStamina < (double) itemData.m_shared.m_food / 2.0)
            element2.m_food.color = __instance.m_foodHealthColor;
          else if ((double) itemData.m_shared.m_food < (double) itemData.m_shared.m_foodStamina / 2.0)
            element2.m_food.color = __instance.m_foodStaminaColor;
          else
            element2.m_food.color = Color.white;
        }
        else
          element2.m_food.enabled = false;
        if (element1 == element2)
          __instance.CreateItemTooltip(itemData, element2.m_tooltip);
        element2.m_quality.enabled = itemData.m_shared.m_maxQuality > 1;
        if (itemData.m_shared.m_maxQuality > 1)
          element2.m_quality.text = itemData.m_quality.ToString();
        element2.m_amount.enabled = itemData.m_shared.m_maxStackSize > 1;
        if (itemData.m_shared.m_maxStackSize > 1)
          element2.m_amount.text = string.Format("{0}/{1}", (object) itemData.m_stack, (object) itemData.m_shared.m_maxStackSize);
      }
      foreach (var element3 in __instance.m_elements)
      {
        element3.m_selected.SetActive(flag1 && element3.m_pos == __instance.m_selected);
        if (!element3.m_used)
        {
          element3.m_durability.gameObject.SetActive(false);
          element3.m_icon.enabled = false;
          element3.m_amount.enabled = false;
          element3.m_quality.enabled = false;
          element3.m_equiped.enabled = false;
          element3.m_queued.enabled = false;
          element3.m_noteleport.enabled = false;
          element3.m_food.enabled = false;
          element3.m_tooltip.m_text = "";
          element3.m_tooltip.m_topic = "";
        }
      }
      __instance.m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float) height * __instance.m_elementSpace);
      return false;
    }
  } 
}