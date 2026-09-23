using UnityEngine;
using UnityEngine.EventSystems;

namespace AugaUnity
{
    /// <summary>
    /// Shows a child while this object is the EventSystem's selected object: gamepad and keyboard navigation, and
    /// whatever the game selects when a menu opens (the build menu selects the button of the current piece).
    /// Pooled buttons are deactivated and re-used without a deselect event, so the state is also refreshed on
    /// enable and every frame.
    /// </summary>
    public class SelectedIndicator : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public GameObject Selected;

        private void OnEnable()
        {
            Refresh();
        }

        private void OnDisable()
        {
            Set(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            Set(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            Set(false);
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            var eventSystem = EventSystem.current;
            Set(eventSystem != null && eventSystem.currentSelectedGameObject == gameObject);
        }

        private void Set(bool selected)
        {
            if (Selected != null && Selected.activeSelf != selected)
            {
                Selected.SetActive(selected);
            }
        }
    }
}
