using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace AugaUnity
{
    public class RandomLoadingImage : MonoBehaviour
    {
        public Sprite[] Images;
        public Image TargetImage;

        public void Start()
        {
            if (TargetImage == null || Images.Length == 0)
                return;

            Images = Images.Where(x => x != null).ToArray();

            var imageIndex = Random.Range(0, Images.Length);
            TargetImage.sprite = Images[imageIndex];
        }
    }
}
