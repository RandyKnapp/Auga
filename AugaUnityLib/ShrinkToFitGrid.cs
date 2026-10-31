using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    /// <summary>
    /// Keeps a fixed-column GridLayoutGroup inside the area it was authored to fill, however many cells it holds.
    /// While the cells fit as authored nothing changes. Past that the whole grid is scaled down, taking more columns
    /// when that leaves the cells larger, until the cells would drop below MinScale; from there the grid keeps the
    /// most columns that fit at that size and scrolls. Setup moves the grid into a masked "Viewport" that takes over
    /// its rect and sibling index.
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class ShrinkToFitGrid : MonoBehaviour
    {
        /// <summary>The smallest the cells get before the grid scrolls instead.</summary>
        public float MinScale = 0.7f;
        /// <summary>Kept clear on each side once the grid is wider than authored; the scrollbar sits in it.</summary>
        public float SideMargin = 16f;
        [CanBeNull] public Scrollbar Scrollbar;

        private GridLayoutGroup _grid;
        private RectTransform _content;
        private RectTransform _viewport;
        private ScrollRect _scrollRect;
        private int _baseColumns;
        private float _scrollbarWidth;
        private int _count = -1;
        private Vector2 _area;

        public void Setup([CanBeNull] Scrollbar scrollbar)
        {
            _grid = GetComponent<GridLayoutGroup>();
            _content = (RectTransform)transform;
            _baseColumns = _grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount ? Mathf.Max(1, _grid.constraintCount) : 1;
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

            // The transparent image lets the mouse wheel scroll between the cells too.
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image), typeof(ScrollRect));
            viewport.layer = gameObject.layer;
            viewport.GetComponent<Image>().color = Color.clear;
            _viewport = (RectTransform)viewport.transform;
            _viewport.SetParent(_content.parent, false);
            _viewport.SetSiblingIndex(_content.GetSiblingIndex());
            _viewport.anchorMin = _content.anchorMin;
            _viewport.anchorMax = _content.anchorMax;
            _viewport.pivot = _content.pivot;
            _viewport.anchoredPosition = _content.anchoredPosition;
            _viewport.sizeDelta = _content.sizeDelta;

            // The grid hangs from the viewport's top centre, so it can scale and scroll there.
            _content.SetParent(_viewport, false);
            _content.anchorMin = _content.anchorMax = _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;

            _scrollRect = viewport.GetComponent<ScrollRect>();
            _scrollRect.content = _content;
            _scrollRect.viewport = _viewport;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = false;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 600; // as every Auga scroll view

            Scrollbar = scrollbar;
            if (Scrollbar != null)
            {
                var bar = (RectTransform)Scrollbar.transform;
                _scrollbarWidth = bar.sizeDelta.x;
                bar.SetParent(_viewport, false);
                bar.anchorMin = new Vector2(1f, 0f);
                bar.anchorMax = new Vector2(1f, 1f);
                bar.pivot = new Vector2(1f, 0.5f);
                Scrollbar.direction = Scrollbar.Direction.BottomToTop;
                // the mouse wheel and the bar scroll the grid; gamepad navigation should not stop on the bar
                Scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
                _scrollRect.verticalScrollbar = Scrollbar;
                _scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                Scrollbar.gameObject.SetActive(false);
            }

            _count = -1;
        }

        public void LateUpdate()
        {
            if (_content == null)
            {
                return;
            }

            var count = 0;
            foreach (Transform child in _content)
            {
                if (child.gameObject.activeSelf)
                {
                    count++;
                }
            }

            var area = _viewport.rect.size;
            if (area.x <= 0 || area.y <= 0 || (count == _count && area == _area))
            {
                return;
            }

            _count = count;
            _area = area;
            Fit();
        }

        private void Fit()
        {
            // The authored width always counts as fitting; only wider grids keep to the side margins.
            var width = Mathf.Max(GridWidth(_baseColumns), _area.x - 2 * SideMargin);
            var columns = _baseColumns;
            var maxColumns = _baseColumns;
            var scale = 0f;
            for (var c = _baseColumns; c <= Mathf.Max(_baseColumns, _count); c++)
            {
                if (c > _baseColumns && GridWidth(c) * MinScale > width)
                {
                    break;
                }

                maxColumns = c;
                var s = Mathf.Min(1f, width / GridWidth(c), _area.y / GridHeight(Rows(c)));
                if (s > scale + 0.001f)
                {
                    scale = s;
                    columns = c;
                }
            }

            if (scale < MinScale)
            {
                columns = maxColumns;
                scale = Mathf.Min(1f, width / GridWidth(columns));
            }

            var height = GridHeight(Rows(columns));
            var scroll = height * scale > _area.y + 0.5f;
            _grid.constraintCount = columns;
            _content.localScale = new Vector3(scale, scale, 1f);
            _content.sizeDelta = new Vector2(_area.x / scale, Mathf.Max(height, _area.y / scale));

            _scrollRect.vertical = scroll;
            if (!scroll)
            {
                _scrollRect.StopMovement();
                _content.anchoredPosition = Vector2.zero;
            }

            if (Scrollbar != null)
            {
                // beside the cells, from the top of the first row down
                var bar = (RectTransform)Scrollbar.transform;
                bar.offsetMin = new Vector2(-_scrollbarWidth, 0f);
                bar.offsetMax = new Vector2(0f, -_grid.padding.top * scale);
                Scrollbar.gameObject.SetActive(scroll);
            }
        }

        private int Rows(int columns) => Mathf.Max(1, (_count + columns - 1) / columns);

        private float GridWidth(int columns) => _grid.padding.horizontal + columns * _grid.cellSize.x + (columns - 1) * _grid.spacing.x;

        private float GridHeight(int rows) => _grid.padding.vertical + rows * _grid.cellSize.y + (rows - 1) * _grid.spacing.y;
    }
}
