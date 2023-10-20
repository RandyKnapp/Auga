using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaBarberController : MonoBehaviour
    {
        public Button HairRightButton;
        public Button HairLeftButton;
        public Button BeardRightButton;
        public Button BeardLeftButton;

        private bool _playerFound;
        
        private PlayerCustomizaton _playerCustomizaton;
        private void Awake()
        {
            _playerCustomizaton = GetComponent<PlayerCustomizaton>();
        }

        private void Start()
        {
            _playerCustomizaton.m_beards = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Beard");
            _playerCustomizaton.m_hairs = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Hair");
            _playerCustomizaton.m_beards.RemoveAll(x => x.name.Contains("_"));
            _playerCustomizaton.m_hairs.RemoveAll(x => x.name.Contains("_"));
            _playerCustomizaton.m_beards.Sort((x, y) => Localization.instance.Localize(x.m_itemData.m_shared.m_name).CompareTo(Localization.instance.Localize(y.m_itemData.m_shared.m_name)));
            _playerCustomizaton.m_hairs.Sort((x, y) => Localization.instance.Localize(x.m_itemData.m_shared.m_name).CompareTo(Localization.instance.Localize(y.m_itemData.m_shared.m_name)));
        }

        private void Update()
        {
            if (!_playerFound)
            {
                if (Player.m_localPlayer == null)
                    return;
                _playerFound = true;
            }
            
            var hairSetting = _playerCustomizaton.GetHairIndex();
            var beardSetting = +_playerCustomizaton.GetBeardIndex();

            if (hairSetting == 0)
                HairLeftButton.gameObject.SetActive(false);
            else if (hairSetting >= (_playerCustomizaton.m_hairs.Count -1))
                HairRightButton.gameObject.SetActive(false);
            else
            {
                HairLeftButton.gameObject.SetActive(true);
                HairRightButton.gameObject.SetActive(true);
            }
                
            if (beardSetting == 0)
                BeardLeftButton.gameObject.SetActive(false);
            else if (beardSetting >= (_playerCustomizaton.m_beards.Count -1))
                BeardRightButton.gameObject.SetActive(false);
            else
            {
                BeardLeftButton.gameObject.SetActive(true);
                BeardRightButton.gameObject.SetActive(true);
            }
        }
    }
}