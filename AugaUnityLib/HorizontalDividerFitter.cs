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
        private Vector2 _lastContentPos;

        public void LateUpdate()
        {
            var rect = ((RectTransform)transform).rect;
            if (_lastRect != rect || _lastContentRect != Content.rect || _lastContentPos != Content.anchoredPosition)
            {
                _lastRect = rect;
                _lastContentRect = Content.rect;
                _lastContentPos = Content.anchoredPosition;

                var width = rect.width;
                var xOffset = Content.anchoredPosition.x;
                var dividerSize = (width - Spacing * 2 - Content.rect.width) / 2;
                // content wider than the row (a tab row full of other mods' links) leaves no room for the lines
                DividerLeft.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0, dividerSize + xOffset));
                DividerRight.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0, dividerSize - xOffset));
            }
        }
    }
}
