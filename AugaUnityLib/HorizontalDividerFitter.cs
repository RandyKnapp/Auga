using UnityEngine;

namespace AugaUnity
{
    [ExecuteInEditMode]
    public class HorizontalDividerFitter : MonoBehaviour
    {
        public RectTransform Content;
        public RectTransform DividerLeft;
        public RectTransform DividerRight;
        public float Spacing;

        private Rect _lastRect;
        private Rect _lastContentRect;

        public void LateUpdate()
        {
            var rect = (transform as RectTransform).rect;
            if (_lastRect != rect || _lastContentRect != Content.rect)
            {
                _lastRect = rect;
                _lastContentRect = Content.rect;

                var width = rect.width;
                var dividerSize = (width - Spacing * 2 - Content.rect.width) / 2;
                DividerLeft.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, dividerSize);
                DividerRight.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, dividerSize);
            }
        }
    }
}
