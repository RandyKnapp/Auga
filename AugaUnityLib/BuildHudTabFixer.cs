using UnityEngine;

namespace AugaUnity
{
    public class BuildHudTabFixer : MonoBehaviour
    {
        public RectTransform TabContainer;

        public void Update()
        {
            while (transform.childCount > 3)
            {
                transform.GetChild(3).SetParent(TabContainer);
            }
        }
    }
}
