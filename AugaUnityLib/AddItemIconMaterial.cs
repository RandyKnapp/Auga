using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(Image))]
    public class AddItemIconMaterial : MonoBehaviour
    {
        public static Material IconMaterial;

        public void Start()
        {
            var image = GetComponent<Image>();
            image.material = IconMaterial;
        }
    }
}
