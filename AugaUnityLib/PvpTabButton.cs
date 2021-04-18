using UnityEngine;

namespace AugaUnity
{
    public class PvpTabButton : TabButton
    {
        public Sprite PvpOnSprite;
        public Sprite PvpOffSprite;

        private bool _pvpEnabled;

        public void Update()
        {
            var player = Player.m_localPlayer;
            if (player != null)
            {
                var pvpEnabled = player.IsPVPEnabled();
                if (_pvpEnabled != pvpEnabled)
                {
                    _pvpEnabled = pvpEnabled;
                    Icon.sprite = _pvpEnabled ? PvpOnSprite : PvpOffSprite;
                }
            }
        }
    }
}
