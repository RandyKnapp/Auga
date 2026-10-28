using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Auga
{
    [Flags]
    public enum ReplaceFlags
    {
        None = 0,
        Instantiate = 1 << 0,
        DestroyOriginal = 1 << 1,
    }

    public static class AugaExtensions
    {
        public static RectTransform RectTransform(this Component c)
        {
            return c.transform as RectTransform;
        }

        public static RectTransform RectTransform(this GameObject go)
        {
            return go.transform as RectTransform;
        }

        public static void HideElement(this Component c)
        {
            if (c != null)
            {
                c.gameObject.SetActive(false);
            }
        }

        public static void HideElementByType<T>(this Component parent) where T : Component
        {
            if (parent == null)
            {
                return;
            }

            var c = parent.GetComponentInChildren<T>();
            HideElement(c);
        }

        public static void HideElementByName(this GameObject parent, string name)
        {
            parent.transform.HideElementByName(name);
        }

        public static void HideElementByName(this Component parent, string name)
        {
            if (parent == null)
            {
                return;
            }

            var c = parent.transform.Find(name);
            HideElement(c);
        }

        public static Transform Replace(this Transform c, string findPath, Transform other, string otherFindPath = null, ReplaceFlags flags = ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal)
        {
            var foundOriginal = c.Find(findPath);
            if (foundOriginal == null)
            {
                return null;
            }

            otherFindPath = otherFindPath ?? findPath;
            var foundOther = other.Find(otherFindPath);
            if (foundOther == null)
            {
                return null;
            }

            var parent = foundOriginal.parent;
            var siblingIndex = foundOriginal.GetSiblingIndex();

            foundOriginal.SetParent(null);

            if ((flags & ReplaceFlags.DestroyOriginal) != 0)
            {
                Object.Destroy(foundOriginal.gameObject);
            }

            if ((flags & ReplaceFlags.Instantiate) != 0)
            {
                foundOther = Object.Instantiate(foundOther, parent);
                foundOther.name = foundOther.name.Replace("(Clone)", "").Replace("(clone)", "");
            }
            else
            {
                foundOther.SetParent(parent);
            }
            foundOther.SetSiblingIndex(siblingIndex);

            return foundOther;
        }

        public static Transform Replace(this Component c, string findPath, GameObject other, string otherFindPath = null, ReplaceFlags flags = ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal)
        {
            return c.transform.Replace(findPath, other.transform, otherFindPath, flags);
        }

        public static Transform Replace(this Transform c, string findPath, GameObject other, string otherFindPath = null, ReplaceFlags flags = ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal)
        {
            return c.Replace(findPath, other.transform, otherFindPath, flags);
        }

        public static Transform Replace(this GameObject go, string findPath, Transform other, string otherFindPath = null, ReplaceFlags flags = ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal)
        {
            return go.transform.Replace(findPath, other, otherFindPath, flags);
        }

        public static Transform Replace(this GameObject go, string findPath, GameObject other, string otherFindPath = null, ReplaceFlags flags = ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal)
        {
            return go.transform.Replace(findPath, other.transform, otherFindPath, flags);
        }

        public static Transform CopyOver(this GameObject go, string findPath, GameObject other, int siblingIndex)
        {
            var foundOther = other.transform.Find(findPath);
            if (foundOther == null)
            {
                return null;
            }

            foundOther = Object.Instantiate(foundOther, go.transform);
            foundOther.name = foundOther.name.Replace("(Clone)", "").Replace("(clone)", "");
            foundOther.SetSiblingIndex(siblingIndex);

            return foundOther;
        }

        public static T RequireComponent<T>(this Component c) where T : Component
        {
            var t = c.GetComponent<T>();
            if (t == null)
            {
                t = c.gameObject.AddComponent<T>();
            }

            return t;
        }

        public static T RequireComponent<T>(this GameObject go) where T : Component
        {
            var t = go.GetComponent<T>();
            if (t == null)
            {
                t = go.AddComponent<T>();
            }

            return t;
        }
    }
}
