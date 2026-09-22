using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class HideTextIfEmpty : MonoBehaviour
    {
        private TMP_Text _text;

        public void OnEnable()
        {
            _text = GetComponent<TMP_Text>();
            HideIfEmpty();
        }

        public void Update()
        {
            HideIfEmpty();
        }

        private void HideIfEmpty()
        {
            _text.enabled = !string.IsNullOrEmpty(_text.text);
        }
    }
}
