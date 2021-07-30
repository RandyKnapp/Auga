using System.Collections.Generic;
using System.Linq;
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
        [Header("Material Configuration")]
        public List<GameObject> MaterialPrefabs;

        private GameObject _currentColorBar;

        public virtual void Setup(ItemDrop.ItemData item)
        {
            var shouldShow = false;
            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Tool:
                    shouldShow = true;
                    break;
            }

            Destroy(_currentColorBar);
            _currentColorBar = null;

            if (shouldShow)
            {
                var recipe = ObjectDB.instance.GetRecipe(item);
                if (recipe != null)
                {
                    shouldShow = false;

                    foreach (var prefab in MaterialPrefabs)
                    {
                        if (prefab == null)
                        {
                            continue;
                        }

                        var itemName = prefab.name;
                        if (recipe.m_resources.ToList().Exists(x => x.m_resItem.name == itemName))
                        {
                            _currentColorBar = Instantiate(prefab, transform, false);
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
