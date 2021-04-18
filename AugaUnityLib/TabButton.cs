using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(Button))]
    public class TabButton : MonoBehaviour
    {
        public Color BackgroundColor = Color.white;
        public Color SelectedBackgroundColor = Color.white;
        public Color IconColor = Color.white;
        public Color SelectedIconColor = Color.white;

        public Button Button;
        public Image Background;
        public Image Icon;

        private bool _selected;

        public void Awake()
        {
            SetSelected(false);
            SetColor();
        }

        public void SetSelected(bool selected)
        {
            if (_selected == selected)
            {
                return;
            }

            _selected = selected;
            SetColor();
        }

        private void SetColor()
        {
            Background.color = _selected ? SelectedBackgroundColor : BackgroundColor;
            Icon.color = _selected ? SelectedIconColor : IconColor;
        }
    }
}
