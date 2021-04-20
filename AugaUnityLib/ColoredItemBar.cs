using System.Collections.Generic;
using System.Linq;
using JoshH.UI;
using UnityEngine;

namespace AugaUnity
{
    public struct ColorPair
    {
        public string A;
        public string B;
    }

    public class ColoredItemBar : MonoBehaviour
    {
        public UIGradient Gradient;

        protected static Dictionary<string, ColorPair> _colorConfig = new Dictionary<string, ColorPair>()
        {
            { "Needle", new ColorPair { A = "#59927F", B = "#817777" } },
            { "BlackMetal", new ColorPair { A = "#175012", B = "#010401" } },
            { "Silver", new ColorPair { A = "#A6A6A6", B = "#5F5F5F" } },
            { "Obsidian", new ColorPair { A = "#31393E", B = "#000000" } },
            { "Iron", new ColorPair { A = "#4D5960", B = "#10141A" } },
            { "Chitin", new ColorPair { A = "#D8CDB9", B = "#857B5E" } },
            { "Bronze", new ColorPair { A = "#AE9164", B = "#4F4029" } },
            { "Copper", new ColorPair { A = "#AF7454", B = "#411F15" } },
            { "TrollHide", new ColorPair { A = "#559BBD", B = "#0D6DAB" } },
            { "Flint", new ColorPair { A = "#FF80FF", B = "#80FF80" } },
            { "Stone", new ColorPair { A = "#73797A", B = "#393B3E" } },
            { "DeerHide", new ColorPair { A = "#FF80FF", B = "#80FF80" } },
            { "LeatherScraps", new ColorPair { A = "#77583B", B = "#423221" } },
            { "Wood", new ColorPair { A = "#77583B", B = "#423221" } },
        };

        public virtual void Setup(ItemDrop.ItemData item)
        {
            var shouldShow = false;
            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Torch:
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Tool:
                case ItemDrop.ItemData.ItemType.Ammo:
                    shouldShow = true;
                    break;
            }

            if (shouldShow)
            {
                var recipe = ObjectDB.instance.GetRecipe(item);
                if (recipe != null)
                {
                    shouldShow = false;

                    foreach (var entry in _colorConfig)
                    {
                        var itemName = entry.Key;
                        if (recipe.m_resources.ToList().Exists(x => x.m_resItem.name == itemName))
                        {
                            var colors = entry.Value;
                            ColorUtility.TryParseHtmlString(colors.A, out var colorA);
                            Gradient.LinearColor1 = colorA;
                            ColorUtility.TryParseHtmlString(colors.B, out var colorB);
                            Gradient.LinearColor2 = colorB;
                            shouldShow = true;
                            break;
                        }
                    }
                }
                else
                {
                    shouldShow = false;
                }
            }

            gameObject.SetActive(shouldShow);
        }
    }
}
