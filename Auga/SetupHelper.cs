using UnityEngine;

namespace Auga
{
    public static class SetupHelper
    {
        public static bool DirectObjectReplace(Transform original, GameObject prefab, string originalName)
        {
            if (original.name != originalName)
            {
                return false;
            }

            var parent = original.parent;
            var siblingIndex = original.GetSiblingIndex();
            Object.DestroyImmediate(original.gameObject);

            var newObject = Object.Instantiate(prefab, parent, false);
            newObject.transform.SetSiblingIndex(siblingIndex);
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
            if (primaryOriginal.name != originalName)
            {
                return false;
            }

            var parent = primaryOriginal.parent;
            var secondaryOriginal = parent.Find(secondaryName);
            var secondarySiblingIndex = secondaryOriginal.GetSiblingIndex();
            var primarySiblingINdex = primaryOriginal.GetSiblingIndex();

            Object.DestroyImmediate(secondaryOriginal.gameObject);
            Object.DestroyImmediate(primaryOriginal.gameObject);

            var newPrefab = Object.Instantiate(prefab, parent);
            var secondary = newPrefab.transform.Find(secondaryName);
            var primary = newPrefab.transform.Find(newPrimaryName);

            secondary.SetParent(parent);
            primary.SetParent(parent);
            secondary.SetSiblingIndex(secondarySiblingIndex);
            primary.SetSiblingIndex(primarySiblingINdex);

            Object.Destroy(newPrefab);

            return true;
        }
    }
}
