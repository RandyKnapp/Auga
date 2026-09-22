using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class KeyBindDisplay : MonoBehaviour
    {
        public string ZInputId;

        private TMP_Text _text;
        private string _key;

        public virtual void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        public virtual void Start()
        {
            Update();
        }

        public virtual void Update()
        {
            var key = Localization.instance.GetBoundKeyString(ZInputId);
            if (key != _key)
            {
                _key = key;
                _text.text = _key;
            }
        }
    }
}
