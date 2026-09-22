using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    public static class SetupHelper
    {
        /// <summary>
        /// Snapshot of the Canvas / CanvasScaler / GraphicRaycaster / GuiScaler set-up of a vanilla UI root.
        /// Valheim 1.x makes every UI screen (Menu, Chat, DamageText, EnemyHud, StoreGui, TextInput, TextViewer, ...)
        /// its own root Canvas; the Auga prefabs were built when those were children of one shared canvas and carry
        /// no Canvas of their own, so a replacement root would never render without one.
        /// </summary>
        public sealed class RootCanvasSettings
        {
            public RenderMode RenderMode;
            public bool PixelPerfect;
            public int SortingOrder;
            public int SortingLayerID;
            public bool OverrideSorting;
            public AdditionalCanvasShaderChannels ShaderChannels;
            public CanvasScaler.ScaleMode ScaleMode;
            public Vector2 ReferenceResolution;
            public CanvasScaler.ScreenMatchMode ScreenMatchMode;
            public float MatchWidthOrHeight;
            public float ReferencePixelsPerUnit;
            public float ScaleFactor;
            public bool HasRaycaster;
            public bool IgnoreReversedGraphics;
            public GraphicRaycaster.BlockingObjects BlockingObjects;
            public bool HasGuiScaler;
        }

        public static RootCanvasSettings CaptureRootCanvas(GameObject go)
        {
            if (go == null)
                return null;
            var canvas = go.GetComponent<Canvas>();
            if (canvas == null)
                return null;

            var settings = new RootCanvasSettings
            {
                RenderMode = canvas.renderMode,
                PixelPerfect = canvas.pixelPerfect,
                SortingOrder = canvas.sortingOrder,
                SortingLayerID = canvas.sortingLayerID,
                OverrideSorting = canvas.overrideSorting,
                ShaderChannels = canvas.additionalShaderChannels,
                HasGuiScaler = go.GetComponent<GuiScaler>() != null,
            };
            var scaler = go.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                settings.ScaleMode = scaler.uiScaleMode;
                settings.ReferenceResolution = scaler.referenceResolution;
                settings.ScreenMatchMode = scaler.screenMatchMode;
                settings.MatchWidthOrHeight = scaler.matchWidthOrHeight;
                settings.ReferencePixelsPerUnit = scaler.referencePixelsPerUnit;
                settings.ScaleFactor = scaler.scaleFactor;
            }
            var raycaster = go.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                settings.HasRaycaster = true;
                settings.IgnoreReversedGraphics = raycaster.ignoreReversedGraphics;
                settings.BlockingObjects = raycaster.blockingObjects;
            }
            return settings;
        }

        /// <summary>
        /// Puts <paramref name="go"/> under a new full-screen root canvas configured like the captured vanilla one,
        /// unless it already has a Canvas. The prefab keeps its own RectTransform (it was authored as a child of a
        /// full-screen canvas, so its anchors/offsets stay meaningful); the wrapper takes its place in the parent.
        /// </summary>
        /// <param name="go">The Auga root object that replaces a vanilla root canvas.</param>
        /// <param name="settings">Captured set-up of the vanilla canvas.</param>
        /// <param name="wrap">
        /// false: <paramref name="go"/> becomes the canvas itself and is stretched to the screen, exactly like the
        /// vanilla root canvases (menu, damage text, store, ...). true: a new full-screen wrapper canvas takes its
        /// place and <paramref name="go"/> keeps its authored rect inside it (sized panels such as the chat window).
        /// </param>
        public static void ApplyRootCanvas(GameObject go, RootCanvasSettings settings, bool wrap = false)
        {
            if (go == null || settings == null || go.GetComponent<Canvas>() != null)
                return;

            if (wrap)
            {
                var wrapper = new GameObject(go.name + "Canvas", typeof(RectTransform));
                wrapper.layer = go.layer;
                wrapper.transform.SetParent(go.transform.parent, false);
                wrapper.transform.SetSiblingIndex(go.transform.GetSiblingIndex());
                go.transform.SetParent(wrapper.transform, false);
                go = wrapper;
            }

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = settings.RenderMode;
            canvas.pixelPerfect = settings.PixelPerfect;
            canvas.overrideSorting = settings.OverrideSorting;
            canvas.sortingLayerID = settings.SortingLayerID;
            canvas.sortingOrder = settings.SortingOrder;
            canvas.additionalShaderChannels = settings.ShaderChannels;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = settings.ScaleMode;
            scaler.referenceResolution = settings.ReferenceResolution;
            scaler.screenMatchMode = settings.ScreenMatchMode;
            scaler.matchWidthOrHeight = settings.MatchWidthOrHeight;
            scaler.referencePixelsPerUnit = settings.ReferencePixelsPerUnit;
            scaler.scaleFactor = settings.ScaleFactor;

            // every Auga screen needs to receive clicks, so always add a raycaster
            var raycaster = go.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = settings.HasRaycaster ? settings.IgnoreReversedGraphics : true;
            raycaster.blockingObjects = settings.HasRaycaster ? settings.BlockingObjects : GraphicRaycaster.BlockingObjects.None;

            if (settings.HasGuiScaler)
            {
                go.AddComponent<GuiScaler>();
            }

            // a root canvas must stretch over the screen; prefabs authored as children usually do already
            if (go.transform is RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        /// <summary>Copies the root canvas set-up of <paramref name="template"/> onto <paramref name="newRoot"/> when needed.</summary>
        public static void EnsureRootCanvas(GameObject newRoot, GameObject template)
        {
            ApplyRootCanvas(newRoot, CaptureRootCanvas(template));
        }

        public static bool DirectObjectReplace(Transform original, GameObject prefab, string originalName)
        {
            return DirectObjectReplace(original, prefab, originalName, out _);
        }

        public static bool DirectObjectReplace(Transform original, GameObject prefab, string originalName, out GameObject newObject)
        {
            if (original.name != originalName)
            {
                newObject = null;
                return false;
            }

            var parent = original.parent;
            var siblingIndex = original.GetSiblingIndex();
            var canvasSettings = CaptureRootCanvas(original.gameObject);
            Object.DestroyImmediate(original.gameObject);

            newObject = Object.Instantiate(prefab, parent, false);
            newObject.transform.SetSiblingIndex(siblingIndex);
            ApplyRootCanvas(newObject, canvasSettings);
            return true;
        }

        /// <summary>
        /// Places two child objects from the prefab in-place at the original, and at a sibling of the original.
        /// Call this method in an Awake prefix, and return !result to avoid calling the Awake of the original.
        /// </summary>
        /// <param name="primaryOriginal">Reference to the original object</param>
        /// <param name="prefab">Prefab that contains two children, one with a different name as the original, but will replace it, and one with the same name as the secondary object</param>
        /// <param name="originalName">The original gameObject name of the object to be replaced</param>
        /// <param name="secondaryName">The name of the secondary gameObject to be replaced. This should be the same in both the original and the new prefab</param>
        /// <param name="newPrimaryName">The name of the object in the prefab that will replace the original. It should be different than the originalName</param>
        /// <returns>true if the objects were replaced (this was called on the original), false otherwise (this was called on the replacement)</returns>
        public static bool IndirectTwoObjectReplace(Transform primaryOriginal, GameObject prefab, string originalName, string secondaryName, string newPrimaryName)
        {
            if (primaryOriginal.name.StartsWith("Auga"))
                return false;

            if (primaryOriginal.name != originalName)
            {
                return false;
            }

            if (!prefab)
            {
                Auga.LogWarning($"Prefab for {originalName} converting to {newPrimaryName} for {secondaryName} not found.");
                return false;
            }

            var parent = primaryOriginal.parent;
            if (parent != null)
            {
                var secondaryOriginal = parent.Find(secondaryName);
                if (secondaryOriginal != null)
                {
                    var secondarySiblingIndex = secondaryOriginal.GetSiblingIndex();
                    var primarySiblingIndex = primaryOriginal.GetSiblingIndex();
                    var primaryCanvas = CaptureRootCanvas(primaryOriginal.gameObject);
                    var secondaryCanvas = CaptureRootCanvas(secondaryOriginal.gameObject);

                    Object.DestroyImmediate(secondaryOriginal.gameObject);
                    Object.DestroyImmediate(primaryOriginal.gameObject);

                    var newPrefab = Object.Instantiate(prefab, parent);
                    var secondary = newPrefab.transform.Find(secondaryName);
                    var primary = newPrefab.transform.Find(newPrimaryName);

                    secondary.SetParent(parent);
                    primary.SetParent(parent);
                    secondary.SetSiblingIndex(secondarySiblingIndex);
                    primary.SetSiblingIndex(primarySiblingIndex);
                    ApplyRootCanvas(primary.gameObject, primaryCanvas, wrap: true);
                    ApplyRootCanvas(secondary.gameObject, secondaryCanvas, wrap: true);

                    Object.Destroy(newPrefab);

                    return true;
                }
            }

            return false;
        }
    }
}
