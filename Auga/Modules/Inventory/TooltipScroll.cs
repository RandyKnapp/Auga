using System.Collections.Generic;
using AugaUnity;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    /// <summary>
    /// Keeps Auga's hover tooltip on the screen. Everything below the title (the text boxes, the description and the
    /// lower boxes) sits in a scroll view that is as tall as its content until the whole tooltip would be taller than
    /// the screen. From there on it stays that tall, shows a scrollbar and scrolls with the mouse wheel or the right
    /// stick.
    /// </summary>
    /// <remarks>
    /// Vanilla moves the tooltip to the pointer every frame and pushes its panel back inside the screen, and the push
    /// stays on the panel. A panel taller than the screen was pushed down one frame and back up the next, so a long
    /// tooltip (an EpicLoot item with its effects and a set) flickered over the whole screen.
    ///
    /// The scroll view is named "Scroll View" because EpicLoot builds its own scroll view into every tooltip unless it
    /// finds one by that name, and to build it takes the title and a text box template out of this tooltip.
    /// </remarks>
    public class TooltipScroll : MonoBehaviour, ILayoutElement
    {
        // kept clear around the tooltip
        private const float ScreenMargin = 10f;
        // the least of the body a tooltip shows, however tall its title
        private const float MinBodyHeight = 100f;
        // ZInput scales the wheel so that one notch reads 0.15
        private const float WheelNotch = 0.15f;
        private const float WheelStep = 60f;
        private const float StickDeadzone = 0.3f;
        private const float StickSpeed = 1000f;

        // serialized so that a copy of the tooltip points at its own parts
        [SerializeField] private RectTransform _root;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private RectTransform _content;
        // the tallest the body may be; -1 until the tooltip is first fitted to the screen
        private float _limit = -1f;
        private bool _measured;
        // where the tooltip's layout puts the panel
        private Vector2 _panelHome;
        private static readonly Vector3[] Corners = new Vector3[4];

        // Vanilla makes a new tooltip from its prefab on every hover and destroys it afterwards, and building the
        // scroll view into each one cost a scrollbar, a dozen components and a layout pass per hover. It is built
        // once into a copy of each tooltip prefab instead, kept under an inactive holder, and the UITooltip makes its
        // tooltips from that copy (UITooltip_OnHoverStart_Patch). Prefabs without a ComplexTooltip map to
        // themselves.
        private static readonly Dictionary<GameObject, GameObject> Templates = new Dictionary<GameObject, GameObject>();
        private static Transform _templateHolder;

        /// <summary>The copy of a tooltip prefab with the scroll view built in, made the first time it is asked for.</summary>
        public static GameObject WithScrollView(GameObject prefab)
        {
            if (prefab == null)
                return null;
            if (Templates.TryGetValue(prefab, out var template) && template != null)
                return template;

            template = prefab;
            if (prefab.GetComponent<ComplexTooltip>() != null && prefab.GetComponentInChildren<TooltipScroll>(true) == null)
            {
                if (_templateHolder == null)
                {
                    var holder = new GameObject("Auga Tooltip Templates");
                    holder.SetActive(false);
                    DontDestroyOnLoad(holder);
                    _templateHolder = holder.transform;
                }

                var copy = Instantiate(prefab, _templateHolder, false);
                copy.name = prefab.name;
                Ensure(copy.GetComponent<ComplexTooltip>());
                if (copy.GetComponentInChildren<TooltipScroll>(true) != null)
                {
                    template = copy;
                    Templates[copy] = copy;
                }
                else
                {
                    Destroy(copy);
                }
            }

            Templates[prefab] = template;
            return template;
        }

        /// <summary>Moves the body of a tooltip into a scroll view, once.</summary>
        public static void Ensure(ComplexTooltip tooltip)
        {
            if (tooltip.GetComponentInChildren<TooltipScroll>(true) != null)
                return;

            var body = tooltip.TextBoxContainer;
            var panel = body != null ? body.parent as RectTransform : null;
            var panelLayout = panel != null ? panel.GetComponent<VerticalLayoutGroup>() : null;
            if (panelLayout == null)
                return;

            var view = NewRect("Scroll View", panel);
            view.SetSiblingIndex(body.GetSiblingIndex());
            view.pivot = new Vector2(0f, 1f);
            view.sizeDelta = new Vector2(body.rect.width, 0f);

            // stacked the way the panel stacked them
            var content = NewRect("Content", view);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0f, 1f);
            content.sizeDelta = Vector2.zero;
            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = panelLayout.spacing;
            contentLayout.childAlignment = panelLayout.childAlignment;
            contentLayout.childControlWidth = panelLayout.childControlWidth;
            contentLayout.childControlHeight = panelLayout.childControlHeight;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // the text boxes and everything after them
            while (panel.childCount > view.GetSiblingIndex() + 1)
                panel.GetChild(view.GetSiblingIndex() + 1).SetParent(content, false);

            // in the panel's right margin, beside the text boxes rather than over them
            Scrollbar scrollbar = null;
            if (Auga.Assets.ScrollBar != null)
            {
                var bar = (RectTransform)Instantiate(Auga.Assets.ScrollBar, view, false).transform;
                bar.name = "Scrollbar";
                bar.anchorMin = new Vector2(1f, 0f);
                bar.anchorMax = Vector2.one;
                bar.pivot = new Vector2(0.5f, 0.5f);
                bar.anchoredPosition = new Vector2(panelLayout.padding.right / 2f, 0f);
                bar.sizeDelta = new Vector2(AugaPanelRestyler.ScrollbarWidth, 0f);
                scrollbar = bar.GetComponent<Scrollbar>();
                scrollbar.direction = Scrollbar.Direction.BottomToTop;
                AugaPanelRestyler.NormalizeHandle(scrollbar);
                // nothing on a tooltip is clicked; the pointer stays on the slot under it
                foreach (var graphic in bar.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
            }

            // clips the body, but not the scrollbar in the margin
            view.gameObject.AddComponent<RectMask2D>().padding = new Vector4(0f, 0f, -panelLayout.padding.right, 0f);
            var scrollRect = view.gameObject.AddComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = view;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            var scroll = view.gameObject.AddComponent<TooltipScroll>();
            scroll._root = (RectTransform)tooltip.transform;
            scroll._panel = panel;
            scroll._content = content;

            // the description moved, and is put back here when another mod moves it
            tooltip.RememberLayout();
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>
        /// Caps the body so that the whole tooltip fits on the screen, then moves the tooltip onto the screen. Called
        /// every frame the tooltip shows, after vanilla moved it to the pointer.
        /// </summary>
        public void FitToScreen()
        {
            if (_root == null || _panel == null)
                return;

            if (!_measured)
            {
                // last laid out while it showed something else, or never
                LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
                _panelHome = _panel.anchoredPosition;
                _measured = true;
            }

            var scale = _panel.lossyScale.y;
            if (scale <= 0f)
                return;

            // the title and the panel's padding: everything that does not scroll
            var fixedHeight = _panel.rect.height - ((RectTransform)transform).rect.height;
            var limit = Mathf.Max(MinBodyHeight, Screen.height / scale - 2f * ScreenMargin - fixedHeight);
            if (Mathf.Abs(limit - _limit) >= 1f)
            {
                _limit = limit;
                LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
            }

            // Vanilla's push onto the screen moves the panel and stays on it, and any layout pass puts the panel back
            // where the layout has it. The push goes on the root instead, which vanilla places afresh every frame.
            _panel.anchoredPosition = _panelHome;
            _panel.GetWorldCorners(Corners);
            var margin = ScreenMargin * scale;
            var push = Vector3.zero;
            if (Corners[2].x > Screen.width - margin) push.x = Screen.width - margin - Corners[2].x;
            if (Corners[0].x + push.x < margin) push.x = margin - Corners[0].x;
            if (Corners[0].y < margin) push.y = margin - Corners[0].y;
            if (Corners[2].y + push.y > Screen.height - margin) push.y = Screen.height - margin - Corners[2].y;
            _root.position += push;
        }

        // each hover starts at the top of the body, measured again
        private void OnEnable()
        {
            _measured = false;
            if (_content != null)
                _content.anchoredPosition = Vector2.zero;
        }

        private void Update()
        {
            if (_content == null)
                return;
            var overflow = _content.rect.height - ((RectTransform)transform).rect.height;
            if (overflow < 1f)
                return;

            // wheel up and stick up both scroll toward the title
            var move = -Mathf.Clamp(ZInput.GetMouseScrollWheel() / WheelNotch, -1f, 1f) * WheelStep;
            if (ZInput.IsGamepadActive())
            {
                var stick = ZInput.GetJoyRightStickY();
                if (Mathf.Abs(stick) > StickDeadzone)
                    move += stick * StickSpeed * Time.unscaledDeltaTime;
            }
            if (move == 0f)
                return;

            var position = _content.anchoredPosition;
            position.y = Mathf.Clamp(position.y + move, 0f, overflow);
            _content.anchoredPosition = position;
        }

        // as tall as the content, up to the limit
        public float preferredHeight
        {
            get
            {
                var height = _content != null ? LayoutUtility.GetPreferredHeight(_content) : 0f;
                return _limit < 0f ? height : Mathf.Min(height, _limit);
            }
        }

        public float minHeight => -1f;
        public float flexibleHeight => -1f;
        public float minWidth => -1f;
        public float preferredWidth => -1f;
        public float flexibleWidth => -1f;
        public int layoutPriority => 1;
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
    }
}
