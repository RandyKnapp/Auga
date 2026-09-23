using TMPro;
using UnityEngine;

namespace AugaUnity
{
    /// <summary>
    /// A tile of the compendium's achievements grid. While the achievement is concealed (secret and not yet
    /// unlocked, shown as "Concealed") the name takes a dimmer colour; otherwise it keeps the prefab's colour.
    /// The page controller sets the state whenever it fills the tile.
    /// </summary>
    public class AugaAchievementElement : MonoBehaviour
    {
        public TMP_Text NameText;
        public Color ConcealedColor = new Color(0.64f, 0.59f, 0.54f);   // #A39689

        private Color _normalColor;
        private bool _captured;

        public void SetConcealed(bool concealed)
        {
            if (NameText == null)
                return;
            if (!_captured)
            {
                _normalColor = NameText.color;
                _captured = true;
            }
            NameText.color = concealed ? ConcealedColor : _normalColor;
        }
    }
}
