using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [RequireComponent(typeof(Player))]
    public class LastHitTracker : MonoBehaviour
    {
        public HitData LastHit;
        protected Player _player;

        public void Awake()
        {
            _player = GetComponent<Player>();
        }

        public virtual void OnDamaged(HitData hitData)
        {
            LastHit = hitData;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnDamaged))]
    public static class Player_OnDamaged_Patch
    {
        public static void Postfix(Player __instance, HitData hit)
        {
            var hitTracker = __instance.RequireComponent<LastHitTracker>();
            hitTracker.OnDamaged(hit);
        }
    }
}
