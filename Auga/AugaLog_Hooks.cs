using System.Collections.Generic;
using AugaUnity;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
    public static class AugaLog_Hooks
    {
        public static void Postfix(Game __instance)
        {
            if (Player.m_localPlayer.m_firstSpawn)
            {
                AugaMessageLog.instance.AddArrivalLog(Player.m_localPlayer);
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    public static class Player_OnDeath_Patch
    {
        public static void Postfix(Player __instance)
        {
            var hitTracker = __instance.RequireComponent<LastHitTracker>();
            var lastHit = hitTracker != null ? hitTracker.LastHit : null;
            AugaMessageLog.instance.AddDeathLog(__instance, lastHit);
        }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.UpdateDespawn))]
    public static class TombStone_UpdateDespawn_Patch
    {
        public static bool Prefix(TombStone __instance)
        {
            if (__instance.m_nview.IsValid() && __instance.IsOwner())
            {
                if (!__instance.m_container.IsInUse() && __instance.m_container.GetInventory().NrOfItems() <= 0)
                {
                    AugaMessageLog.instance.AddTombstoneLog(Player.m_localPlayer);
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownItem))]
    public static class Player_AddKnownItem_Patch
    {
        public static bool Prefix(Player __instance, ItemDrop.ItemData item)
        {
            if (__instance.m_knownMaterial.Contains(item.m_shared.m_name))
            {
                return true;
            }

            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material)
            {
                AugaMessageLog.instance.AddNewMaterialLog(item);
            }
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie)
            {
                AugaMessageLog.instance.AddNewTrophyLog(item);
            }
            else
            {
                AugaMessageLog.instance.AddNewItemLog(item);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdateKnownRecipesList))]
    public static class Player_UpdateKnownRecipesList_Patch
    {
        public static bool Prefix(Player __instance)
        {
            var newRecipes = new List<Recipe>();
            foreach (var recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe.m_enabled && !__instance.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) && __instance.HaveRequirements(recipe, true, 0))
                {
                    newRecipes.Add(recipe);
                }
            }

            if (newRecipes.Count > 0)
            {
                AugaMessageLog.instance.AddNewRecipesLog(newRecipes);
            }

            var pieceTables = new List<PieceTable>();
            var newPieces = new List<Piece>();
            __instance.m_inventory.GetAllPieceTables(pieceTables);
            foreach (var pieceTable in pieceTables)
            {
                foreach (var gameObject in pieceTable.m_pieces)
                {
                    var piece = gameObject.GetComponent<Piece>();
                    if (piece.m_enabled && !__instance.m_knownRecipes.Contains(piece.m_name) && __instance.HaveRequirements(piece, Player.RequirementMode.IsKnown))
                    {
                        newPieces.Add(piece);
                    }
                }
            }

            if (newPieces.Count > 0)
            {
                AugaMessageLog.instance.AddNewPieceLog(newPieces);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownStation))]
    public static class Player_AddKnownStation_Patch
    {
        public static bool Prefix(Player __instance, CraftingStation station)
        {
            if (!__instance.m_knownStations.ContainsKey(station.m_name))
            {
                AugaMessageLog.instance.AddNewStationLog(station);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownBiome))]
    public static class Player_AddKnownBiome_Patch
    {
        public static bool Prefix(Player __instance, Heightmap.Biome biome)
        {
            if (!__instance.m_knownBiome.Contains(biome))
            {
                AugaMessageLog.instance.AddNewBiomeLog(biome);
            }

            return true;
        }
    }

    //Teleport(Player player)
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
    public static class TeleportWorld_Teleport_Patch
    {
        public static void Postfix(TeleportWorld __instance, Player player)
        {
            if (!__instance.TargetFound() || !player.IsTeleportable())
            {
                return;
            }

            AugaMessageLog.instance.AddTeleportLog(__instance.GetText(), player);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    public static class InventoryGui_DoCrafting_Patch
    {
        public static bool Prefix(InventoryGui __instance, Player player)
        {
            if (__instance.m_craftRecipe == null)
            {
                return true;
            }

            var quality = __instance.m_craftUpgradeItem?.m_quality + 1 ?? 1;
            if (quality > __instance.m_craftRecipe.m_item.m_itemData.m_shared.m_maxQuality 
                || !player.HaveRequirements(__instance.m_craftRecipe, false, quality) 
                && !player.NoCostCheat() 
                || (__instance.m_craftUpgradeItem != null 
                    && !player.GetInventory().ContainsItem(__instance.m_craftUpgradeItem) 
                    || __instance.m_craftUpgradeItem == null 
                    && !player.GetInventory().HaveEmptySlot()))
            {
                return true;
            }

            if (__instance.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(__instance.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc))
            {
                return true;
            }

            if (__instance.m_craftUpgradeItem != null)
            {
                AugaMessageLog.instance.AddUpgradeItemLog(__instance.m_craftUpgradeItem, quality);
            }
            else
            {
                AugaMessageLog.instance.AddCraftItemLog(__instance.m_craftRecipe);
            }

            return true;
        }
    }

    //RaiseSkill
    [HarmonyPatch(typeof(Player), nameof(Player.RaiseSkill))]
    public static class Player_RaiseSkill_Patch
    {
        public static Skills.SkillType Skill;
        public static int LevelBefore;

        public static bool Prefix(Player __instance, Skills.SkillType skill)
        {
            Skill = skill;
            LevelBefore = Mathf.FloorToInt(__instance.m_skills.GetSkill(skill)?.m_level ?? 0);
            return true;
        }

        public static void Postfix(Player __instance, Skills.SkillType skill)
        {
            if (Skill != skill)
            {
                return;
            }

            var levelNow = Mathf.FloorToInt(__instance.m_skills.GetSkill(skill)?.m_level ?? 0);
            if (LevelBefore != levelNow)
            {
                AugaMessageLog.instance.AddSkillUpLog(skill, levelNow);
            }
        }
    }

    //Character.ShowPickupMessage
    [HarmonyPatch(typeof(Character), nameof(Character.ShowPickupMessage))]
    public static class Character_ShowPickupMessage_Patch
    {
        public static void Postfix(ItemDrop.ItemData item, int amount)
        {
            if (item != null && amount > 0)
            {
                AugaMessageLog.instance.AddItemPickupLog(item, amount);
            }
        }
    }
}
