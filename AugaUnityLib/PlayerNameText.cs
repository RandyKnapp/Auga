using TMPro;
using UnityEngine;

namespace AugaUnity
{
    [RequireComponent(typeof(TMP_Text))]
    public class PlayerNameText : MonoBehaviour
    {
        private TMP_Text _text;

        public virtual void Start()
        {
            _text = GetComponent<TMP_Text>();
            if (_text != null)
            {
                _text.text = Game.instance?.GetPlayerProfile()?.GetName() ?? "";
            }
        }

        public virtual void Update()
        {
            if (_text != null && string.IsNullOrEmpty(_text.text))
            {
                _text.text = Game.instance?.GetPlayerProfile()?.GetName() ?? "";
            }
        }
    }
}
