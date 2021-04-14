using JoshH.Extensions;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Author: Josh H.
/// Support: assetstore.joshh@gmail.com
/// </summary>

namespace JoshH.UI
{
    [AddComponentMenu("UI/Effects/UI Gradient")]
    [RequireComponent(typeof(RectTransform))]
    public class UIGradient : BaseMeshEffect
    {
        [Tooltip("How the gradient color will be blended with the graphics color.")]
        [SerializeField] private UIGradientBlendMode blendMode;

        [SerializeField] [Range(0, 1)] private float intensity = 1f;

        [SerializeField] private UIGradientType gradientType;

        //Linear Colors
        [SerializeField] private Color linearColor1 = Color.yellow;
        [SerializeField] private Color linearColor2 = Color.red;

        //Corner Colors
        [SerializeField] private Color cornerColorUpperLeft = Color.red;
        [SerializeField] private Color cornerColorUpperRight = Color.yellow;
        [SerializeField] private Color cornerColorLowerRight = Color.green;
        [SerializeField] private Color cornerColorLowerLeft = Color.blue;

        //Complex Linear
        [SerializeField] private Gradient linearGradient;

        [SerializeField] [Range(0, 360)] private float angle;

        private RectTransform _rectTransform;

        protected RectTransform rectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = transform as RectTransform;
                }
                return _rectTransform;
            }
        }

        public UIGradientBlendMode BlendMode
        {
            get
            {
                return blendMode;
            }

            set
            {
                blendMode = value;
                ForceUpdateGraphic();
            }
        }

        public float Intensity
        {
            get
            {
                return intensity;
            }

            set
            {
                intensity = Mathf.Clamp01(value);
                ForceUpdateGraphic();
            }
        }

        public UIGradientType GradientType
        {
            get
            {
                return gradientType;
            }

            set
            {
                gradientType = value;
                ForceUpdateGraphic();
            }
        }

        public Color LinearColor1
        {
            get
            {
                return linearColor1;
            }

            set
            {
                linearColor1 = value;
                ForceUpdateGraphic();
            }
        }

        public Color LinearColor2
        {
            get
            {
                return linearColor2;
            }

            set
            {
                linearColor2 = value;
                ForceUpdateGraphic();
            }
        }

        public Color CornerColorUpperLeft
        {
            get
            {
                return cornerColorUpperLeft;
            }

            set
            {
                cornerColorUpperLeft = value;
                ForceUpdateGraphic();
            }
        }

        public Color CornerColorUpperRight
        {
            get
            {
                return cornerColorUpperRight;
            }

            set
            {
                cornerColorUpperRight = value;
                ForceUpdateGraphic();
            }
        }

        public Color CornerColorLowerRight
        {
            get
            {
                return cornerColorLowerRight;
            }

            set
            {
                cornerColorLowerRight = value;
                ForceUpdateGraphic();
            }
        }

        public Color CornerColorLowerLeft
        {
            get
            {
                return cornerColorLowerLeft;
            }

            set
            {
                cornerColorLowerLeft = value;
                ForceUpdateGraphic();
            }
        }

        public float Angle
        {
            get
            {
                return angle;
            }

            set
            {
                if (value < 0)
                {
                    angle = (value % 360) + 360;
                }
                else
                {
                    angle = value % 360;
                }
                ForceUpdateGraphic();
            }
        }

        public Gradient LinearGradient
        {
            get
            {
                return linearGradient;
            }

            set
            {
                linearGradient = value;
                ForceUpdateGraphic();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (this.enabled)
            {
                UIVertex vert = new UIVertex();
                if (gradientType == UIGradientType.ComplexLinear)
                {
                    CutMesh(vh);
                }

                for (int i = 0; i < vh.currentVertCount; i++)
                {
                    vh.PopulateUIVertex(ref vert, i);

#if UNITY_2018_1_OR_NEWER
                    Vector2 normalizedPosition = ((Vector2)vert.position - rectTransform.rect.min) / (rectTransform.rect.max - rectTransform.rect.min);
#else
                    Vector2 size = rectTransform.rect.max - rectTransform.rect.min;
                    Vector2 normalizedPosition = Vector2.Scale((Vector2)vert.position - rectTransform.rect.min, new Vector2(1f / size.x, 1f / size.y));
#endif

                    normalizedPosition = RotateNormalizedPosition(normalizedPosition, this.angle);

                    //get color with selected gradient type
                    Color gradientColor = Color.black;
                    if (gradientType == UIGradientType.Linear)
                    {
                        gradientColor = GetColorInGradient(linearColor1, linearColor1, linearColor2, linearColor2, normalizedPosition);
                    }
                    else if (gradientType == UIGradientType.Corner)
                    {
                        gradientColor = GetColorInGradient(cornerColorUpperLeft, cornerColorUpperRight, cornerColorLowerRight, cornerColorLowerLeft, normalizedPosition);
                    }
                    else if (gradientType == UIGradientType.ComplexLinear)
                    {
                        gradientColor = linearGradient.Evaluate(normalizedPosition.y);
                    }
                    vert.color = BlendColor(vert.color, gradientColor, blendMode, intensity);
                    vh.SetUIVertex(vert, i);
                }
            }
        }

        protected void CutMesh(VertexHelper vh)
        {
            var tris = new List<UIVertex>();

            vh.GetUIVertexStream(tris);

            vh.Clear();

            var list = new List<UIVertex>();

            var d = GetCutDirection();

            IEnumerable<float> cuts = linearGradient.alphaKeys.Select(x => { return x.time; });
            cuts = cuts.Union(linearGradient.colorKeys.Select(x => { return x.time; }));

            foreach (var item in cuts)
            {
                list.Clear();
                var point = GetCutOrigin(item);
                if (item < 0.001 || item > 0.999)
                {
                    continue;
                }
                else
                {
                    for (int j = 0; j < tris.Count; j += 3)
                    {
                        CutTriangle(tris, j, list, d, point);
                    }
                }
                tris.Clear();
                tris.AddRange(list);
            }
            vh.AddUIVertexTriangleStream(tris);
        }

        UIVertex UIVertexLerp(UIVertex v1, UIVertex v2, float f)
        {
            UIVertex vert = new UIVertex();

            vert.position = Vector3.Lerp(v1.position, v2.position, f);
            vert.color = Color.Lerp(v1.color, v2.color, f);
            vert.uv0 = Vector2.Lerp(v1.uv0, v2.uv0, f);
            vert.uv1 = Vector2.Lerp(v1.uv1, v2.uv1, f);
            vert.uv2 = Vector2.Lerp(v1.uv2, v2.uv2, f);
            vert.uv3 = Vector2.Lerp(v1.uv3, v2.uv3, f);

            return vert;
        }

        Vector2 GetCutDirection()
        {
            var v = Vector2.up.Rotate(-angle);
            v = new Vector2(v.x / this.rectTransform.rect.size.x,v.y / this.rectTransform.rect.size.y);
            return v.Rotate(90);
        }

        void CutTriangle(List<UIVertex> tris, int idx, List<UIVertex> list, Vector2 cutDirection, Vector2 point)
        {
            var a = tris[idx];
            var b = tris[idx + 1];
            var c = tris[idx + 2];

            float bc = OnLine(b.position, c.position, point, cutDirection);
            float ab = OnLine(a.position, b.position, point, cutDirection);
            float ca = OnLine(c.position, a.position, point, cutDirection);

            if (IsOnLine(ab))
            {
                if (IsOnLine(bc))
                {
                    var pab = UIVertexLerp(a, b, ab);
                    var pbc = UIVertexLerp(b, c, bc);
                    list.AddRange(new List<UIVertex>() { a, pab, c, pab, pbc, c, pab, b, pbc });
                }
                else
                {
                    var pab = UIVertexLerp(a, b, ab);
                    var pca = UIVertexLerp(c, a, ca);
                    list.AddRange(new List<UIVertex>() { c, pca, b, pca, pab, b, pca, a, pab });
                }
            }
            else if (IsOnLine(bc))
            {
                var pbc = UIVertexLerp(b, c, bc);
                var pca = UIVertexLerp(c, a, ca);
                list.AddRange(new List<UIVertex>() { b, pbc, a, pbc, pca, a, pbc, c, pca });
            }
            else
            {
                list.AddRange(tris.GetRange(idx, 3));
            }
        }

        bool IsOnLine(float f)
        {
            return f <= 1 && f > 0;
        }

        /// <summary>
        /// Calculates intersection of two lines.
        /// </summary>
        /// <param name="p1">Point on line 1</param>
        /// <param name="p2">Point on line 1</param>
        /// <param name="o">Point on line 2</param>
        /// <param name="dir">Direction of line 2</param>
        /// <returns>f: lerp(p1,p2,f) is the point of intersection</returns>
        float OnLine(Vector2 p1, Vector2 p2, Vector2 o, Vector2 dir)
        {
            float tmp = (p2.x - p1.x) * dir.y - (p2.y - p1.y) * dir.x;
            if (tmp == 0)
            {
                return -1;
            }
            float mu = ((o.x - p1.x) * dir.y - (o.y - p1.y) * dir.x) / tmp;
            return mu;
        }

        float ProjectedDistance(Vector2 p1, Vector2 p2, Vector2 normal)
        {
            return Vector2.Distance(Vector3.Project(p1, normal), Vector3.Project(p2, normal));
        }

        Vector2 GetCutOrigin(float f)
        {
            var v = Vector2.up.Rotate(-angle);

            v = new Vector2(v.x / this.rectTransform.rect.size.x,v.y / this.rectTransform.rect.size.y);

            Vector3 p1, p2;

            if (angle % 180 < 90)
            {
                p1 = Vector3.Project(Vector2.Scale(rectTransform.rect.size, (Vector2.down + Vector2.left)) * 0.5f, v);
                p2 = Vector3.Project(Vector2.Scale(rectTransform.rect.size,(Vector2.up + Vector2.right)) * 0.5f, v);
            }
            else
            {
                p1 = Vector3.Project(Vector2.Scale(rectTransform.rect.size,(Vector2.up + Vector2.left)) * 0.5f, v);
                p2 = Vector3.Project(Vector2.Scale(rectTransform.rect.size,(Vector2.down + Vector2.right)) * 0.5f, v);
            }
            if (angle % 360 >= 180)
            {
                return Vector2.Lerp(p2, p1, f) + rectTransform.rect.center;
            }
            else
            {
                return Vector2.Lerp(p1, p2, f) + rectTransform.rect.center;
            }
        }

        private Color BlendColor(Color c1, Color c2, UIGradientBlendMode mode, float intensity)
        {
            if (mode == UIGradientBlendMode.Override)
            {
                return Color.Lerp(c1, c2, intensity);
            }
            else if (mode == UIGradientBlendMode.Multiply)
            {
                return Color.Lerp(c1, c1 * c2, intensity);
            }
            else
            {
                Debug.LogErrorFormat("Mode is not supported: {0}", mode);
                return c1;
            }
        }

        /// <summary>
        /// Rotates a position in with coordinates in [0,1]
        /// </summary>
        /// <param name="normalizedPosition">Point to rotate</param>
        /// <param name="angle">Angle to rotate in degrees</param>
        /// <returns>Rotated point</returns>
        private Vector2 RotateNormalizedPosition(Vector2 normalizedPosition, float angle)
        {
            float a = Mathf.Deg2Rad * (angle < 0 ? (angle % 90 + 90) : (angle % 90));
            float scale = Mathf.Sin(a) + Mathf.Cos(a);

            return (normalizedPosition - Vector2.one * 0.5f).Rotate(angle) / scale + Vector2.one * 0.5f;
        }

        /// <summary>
        /// Sets vertices of the referenced Graphic dirty. This triggers a new mesh generation and modification.
        /// </summary>
        public void ForceUpdateGraphic()
        {
            if (this.graphic != null)
            {
                this.graphic.SetVerticesDirty();
            }
        }

        /// <summary>
        /// Calculates color interpolated between 4 corners.
        /// </summary>
        /// <param name="ul">upper left (0,1)</param>
        /// <param name="ur">upper right (1,1)</param>
        /// <param name="lr">lower right (1,0)</param>
        /// <param name="ll">lower left (0,0)</param>
        /// <param name="normalizedPosition">position (x,y) in [0,1]</param>
        /// <returns>interpolated color</returns>
        private Color GetColorInGradient(Color ul, Color ur, Color lr, Color ll, Vector2 normalizedPosition)
        {
            return Color.Lerp(Color.Lerp(ll, lr, normalizedPosition.x), Color.Lerp(ul, ur, normalizedPosition.x), normalizedPosition.y); ;
        }

        public enum UIGradientBlendMode
        {
            Override,
            Multiply
        }

        public enum UIGradientType
        {
            Linear,
            Corner,
            ComplexLinear
        }

#if UNITY_EDITOR
        /// <summary>
        /// Reset is called when the user hits the Reset button in the Inspector's
        /// context menu or when adding the component the first time.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();

            this.linearGradient = new Gradient();

            // Populate the color keys
            var colorKey = new GradientColorKey[3];
            colorKey[0].color = new Color(0.5137255f, 0.2274510f, 0.7058824f);
            colorKey[0].time = 0.0f;
            colorKey[1].color = new Color(0.9921569f, 0.1137255f, 0.1137255f);
            colorKey[1].time = 0.5f;
            colorKey[2].color = new Color(0.9882353f, 0.6901961f, 0.2705882f);
            colorKey[2].time = 1.0f;

            // Populate the alpha keys
            var alphaKey = new GradientAlphaKey[2];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 1.0f;
            alphaKey[1].time = 1.0f;

            this.linearGradient.SetKeys(colorKey, alphaKey);

            ForceUpdateGraphic();
        }
#endif
    }
}