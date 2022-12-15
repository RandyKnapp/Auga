using BepInEx.Configuration;
using UnityEngine;

namespace Auga
{
    public class MovableHudElement : MonoBehaviour
    {
        public ConfigEntry<TextAnchor> Anchor;
        public ConfigEntry<Vector2> Position;
        public ConfigEntry<float> Scale;

        public void Init(TextAnchor defaultAnchor, float defaultPositionX, float defaultPositionY)
        {
            Init(gameObject.name, defaultAnchor, defaultPositionX, defaultPositionY);
        }

        public void Init(string nameOverride, TextAnchor defaultAnchor, float defaultPositionX, float defaultPositionY)
        {
            Anchor = Auga.instance.Config.Bind(nameOverride, $"{nameOverride}Anchor", defaultAnchor, $"Anchor for {nameOverride}");
            Position = Auga.instance.Config.Bind(nameOverride, $"{nameOverride}Position", new Vector2(defaultPositionX, defaultPositionY), $"Position for {nameOverride}");
            Scale = Auga.instance.Config.Bind(nameOverride, $"{nameOverride}Scale", 1.0f, $"Uniform scale for {nameOverride}");
        }

        public void Update()
        {
            if (Anchor == null || Position == null || Scale == null)
                return;

            var rectTransform = (RectTransform)transform;
            switch (Anchor.Value)
            {
                case TextAnchor.UpperLeft:
                    rectTransform.pivot = rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0, 1);
                    break;
                case TextAnchor.UpperCenter:
                    rectTransform.pivot = rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 1);
                    break;
                case TextAnchor.UpperRight:
                    rectTransform.pivot = rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(1, 1);
                    break;
                case TextAnchor.MiddleLeft:
                    rectTransform.pivot = rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0, 0.5f);
                    break;
                case TextAnchor.MiddleCenter:
                    rectTransform.pivot = rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
                case TextAnchor.MiddleRight:
                    rectTransform.pivot = rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(1, 0.5f);
                    break;
                case TextAnchor.LowerLeft:
                    rectTransform.pivot = rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0, 0);
                    break;
                case TextAnchor.LowerCenter:
                    rectTransform.pivot = rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0);
                    break;
                case TextAnchor.LowerRight:
                    rectTransform.pivot = rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(1, 0);
                    break;
            }

            rectTransform.anchoredPosition = Position.Value;
            rectTransform.localScale = new Vector3(Scale.Value, Scale.Value);
        }
    }
}
