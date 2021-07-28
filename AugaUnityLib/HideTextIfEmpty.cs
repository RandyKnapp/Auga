using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(Text))]
    public class HideTextIfEmpty : MonoBehaviour
    {
        private Text _text;

        public void OnEnable()
        {
            _text = GetComponent<Text>();
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
