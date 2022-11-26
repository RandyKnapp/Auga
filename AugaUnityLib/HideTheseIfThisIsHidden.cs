using JetBrains.Annotations;
using UnityEngine;

namespace AugaUnity
{
    public class HideTheseIfThisIsHidden : MonoBehaviour
    {
        public GameObject IfThisIsHidden;
        public GameObject[] HideThese;

        [UsedImplicitly]
        public void Update()
        {
            if (!IfThisIsHidden)
                return;

            var isActive = IfThisIsHidden.activeSelf;
            foreach (var go in HideThese)
            {
                go.SetActive(isActive);
            }
        }
    }
}
