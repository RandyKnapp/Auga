using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class MatchColor : MonoBehaviour
    {
        public Graphic Graphic;
        public Graphic ToMatch;
        public Color IfNotActiveColor = Color.white;

        public void Update()
        {
            if (ToMatch != null && ToMatch.gameObject.activeInHierarchy)
            {
                Graphic.color = ToMatch.color;
            }
            else
            {
                Graphic.color = IfNotActiveColor;
            }
        }
    }

    public class ColorIfNotActive : MonoBehaviour
    {
        public Graphic Graphic;
        public GameObject Other;
        public Color IfNotActiveColor = Color.white;

        private Color _original;

        public void Start()
        {
            _original = Graphic.color;
        }

        public void Update()
        {
            if (Other == null || !Other.gameObject.activeInHierarchy)
            {
                Graphic.color = IfNotActiveColor;
            }
            else
            {
                Graphic.color = _original;
            }
        }
    }
}
