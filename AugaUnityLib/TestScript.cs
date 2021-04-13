using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class TestScript : MonoBehaviour
    {
        private Image _image;

        public void Start()
        {
            _image = GetComponent<Image>();
        }

        public void Update()
        {
            _image.color = Color.Lerp(Color.red, Color.blue, 0.5f * (1 + Mathf.Sin(Time.time)));
        }
    }
}
