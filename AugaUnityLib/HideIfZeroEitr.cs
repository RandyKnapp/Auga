using JetBrains.Annotations;
using UnityEngine;

namespace AugaUnity
{
    public class HideIfZeroEitr : MonoBehaviour
    {
        public GameObject Target;

        [UsedImplicitly]
        public void Update()
        {
            if (!Target || !Player.m_localPlayer)
                return;

            Target.SetActive(Player.m_localPlayer.GetMaxEitr() > 0);
        }
    }
}
