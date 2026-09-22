using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Auga.Utilities
{
    /// <summary>
    /// The Auga prefabs were serialized against an older version of the game's UI components. When Valheim adds
    /// new serialized fields (e.g. Menu.m_skipButton, KeyHints.m_radialKeyHints) those fields are null on the Auga
    /// instance and the game code throws. This copies every UnityEngine.Object-typed field (and arrays/lists of
    /// them) that is unset on <paramref name="target"/> from the vanilla <paramref name="source"/>. Scene objects
    /// that live inside the source hierarchy are re-parented under <paramref name="reparentTo"/> first, so they
    /// survive the destruction of the vanilla object.
    /// </summary>
    public static class SerializedFieldHelper
    {
        public static void CopyMissingFields<T>(T target, T source, Transform reparentTo, params string[] skip) where T : Component
        {
            CopyMissingFields(target, source, _ => reparentTo, skip);
        }

        /// <summary>
        /// Same as above, but <paramref name="reparentTo"/> picks the new parent for each adopted scene object
        /// (it receives the object about to be moved). Use it when the vanilla hierarchy keeps some objects under
        /// a root that is toggled with the screen (e.g. Menu.m_root): those must stay hidden with the Auga root
        /// too, otherwise they render permanently.
        /// </summary>
        public static void CopyMissingFields<T>(T target, T source, Func<Transform, Transform> reparentTo, params string[] skip) where T : Component
        {
            if (target == null || source == null || reparentTo == null)
                return;

            var skipSet = new HashSet<string>(skip ?? Array.Empty<string>());
            var sourceRoot = source.transform;
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var type = typeof(T); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            {
                foreach (var field in type.GetFields(flags | BindingFlags.DeclaredOnly))
                {
                    if (field.IsStatic || skipSet.Contains(field.Name))
                        continue;
                    if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                        continue;

                    var fieldType = field.FieldType;
                    if (typeof(Object).IsAssignableFrom(fieldType))
                    {
                        var current = field.GetValue(target) as Object;
                        if (current != null)
                            continue;
                        var value = field.GetValue(source) as Object;
                        if (value == null)
                            continue;
                        Debug.Log($"[Auga] {typeof(T).Name}.{field.Name}: adopting '{value.name}' ({value.GetType().Name}) from vanilla");
                        field.SetValue(target, Adopt(value, sourceRoot, reparentTo));
                    }
                    else if (fieldType.IsArray && typeof(Object).IsAssignableFrom(fieldType.GetElementType()))
                    {
                        var current = field.GetValue(target) as Array;
                        if (current != null && current.Length > 0)
                            continue;
                        var value = field.GetValue(source) as Array;
                        if (value == null || value.Length == 0)
                            continue;
                        var copy = Array.CreateInstance(fieldType.GetElementType(), value.Length);
                        for (var i = 0; i < value.Length; i++)
                            copy.SetValue(Adopt(value.GetValue(i) as Object, sourceRoot, reparentTo), i);
                        field.SetValue(target, copy);
                    }
                }
            }
        }

        /// <summary>
        /// Returns <paramref name="value"/>, moving it (or rather the top-most ancestor that is still inside
        /// <paramref name="sourceRoot"/>) under <paramref name="reparentTo"/> when it is a scene object of the source.
        /// Assets (prefabs, scriptable objects, sprites...) are returned unchanged.
        /// </summary>
        private static Object Adopt(Object value, Transform sourceRoot, Func<Transform, Transform> reparentTo)
        {
            if (value == null)
                return value;

            Transform t = null;
            if (value is GameObject go) t = go.transform;
            else if (value is Component c) t = c.transform;
            if (t == null || !t.IsChildOf(sourceRoot) || t == sourceRoot)
                return value;

            // move just this object (with its own children); moving a larger ancestor would drag the whole
            // vanilla panel along.
            var newParent = reparentTo(t);
            if (newParent != null)
            {
                t.SetParent(newParent, false);
            }
            return value;
        }
    }
}
