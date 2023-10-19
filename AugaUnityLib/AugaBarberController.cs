using System;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaBarberController : MonoBehaviour
    {
        public Button HairLeft;
        public Button HairRight;
        public Button BeardLeft;
        public Button BeardRight;
        public Button Apply;
        public Button Close;
        public Slider HairTone;
        public Slider Blondeness;
        
        private PlayerCustomizaton _playerCustomizaton;
        private void Awake()
        {
            _playerCustomizaton = GetComponent<PlayerCustomizaton>();
        }

        private void Start()
        {
            HairLeft.onClick.AddListener(_playerCustomizaton.OnHairLeft);
            HairRight.onClick.AddListener(_playerCustomizaton.OnHairRight);
            BeardLeft.onClick.AddListener(_playerCustomizaton.OnBeardLeft);
            BeardRight.onClick.AddListener(_playerCustomizaton.OnBeardRight);
            Apply.onClick.AddListener(_playerCustomizaton.OnApply);
            Close.onClick.AddListener(_playerCustomizaton.OnCancel);
            
            _playerCustomizaton.m_beards = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Beard");
            _playerCustomizaton.m_hairs = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Hair");
            _playerCustomizaton.m_beards.RemoveAll((Predicate<ItemDrop>) (x => x.name.Contains("_")));
            _playerCustomizaton.m_hairs.RemoveAll((Predicate<ItemDrop>) (x => x.name.Contains("_")));
            _playerCustomizaton.m_beards.Sort((Comparison<ItemDrop>) ((x, y) => Localization.instance.Localize(x.m_itemData.m_shared.m_name).CompareTo(Localization.instance.Localize(y.m_itemData.m_shared.m_name))));
            _playerCustomizaton.m_hairs.Sort((Comparison<ItemDrop>) ((x, y) => Localization.instance.Localize(x.m_itemData.m_shared.m_name).CompareTo(Localization.instance.Localize(y.m_itemData.m_shared.m_name))));
        }
    }
}