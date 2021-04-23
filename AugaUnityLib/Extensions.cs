using UnityEngine;

namespace AugaUnity
{
    public static class Extensions
    {
        public static T MakeDummy<T>(this MonoBehaviour self, T component, Transform dummyContainer) where T : Component
        {
            if (component == null)
            {
                var dummyGameObject = new GameObject("dummy");
                dummyGameObject.transform.SetParent(dummyContainer);
                return dummyGameObject.AddComponent<T>();
            }
            else
            {
                return component;
            }
        }

        public static GameObject MakeDummy(this MonoBehaviour self, GameObject go, Transform dummyContainer)
        {
            if (go == null)
            {
                var dummyGameObject = new GameObject("dummy");
                dummyGameObject.transform.SetParent(dummyContainer);
                return dummyGameObject;
            }
            else
            {
                return go;
            }
        }
    }
}
