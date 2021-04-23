using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(Toggle))]
    public class PvpToggle : MonoBehaviour
    {
        public GameObject Enabled;
        public GameObject Disabled;
        public GameObject Inactive;

        protected Toggle _toggle;
        protected bool _canTogglePvp = true;

        protected void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _toggle.onValueChanged.AddListener(OnValueChanged);
        }

        public virtual void Update()
        {
            if (_canTogglePvp != _toggle.interactable)
            {
                _canTogglePvp = _toggle.interactable;
                OnValueChanged(_toggle.isOn);
            }
        }

        private void OnValueChanged(bool toggled)
        {
            Inactive.SetActive(!_toggle.interactable);
            Enabled.SetActive(_toggle.interactable && toggled);
            Disabled.SetActive(_toggle.interactable && !toggled);
        }
    }
}
