using UnityEngine;

namespace AugaUnity
{
    public class PvpTabButton : TabButton
    {
        public Sprite PvpOnSprite;
        public Color PvpOnColor;
        public Sprite PvpOffSprite;

        protected bool _pvpEnabled;

        protected bool IsPvpEnabled()
        {
            var player = Player.m_localPlayer;
            if (player != null)
            {
                return player.IsPVPEnabled();
            }

            return false;
        }

        public virtual void Update()
        {
            var pvpEnabled = IsPvpEnabled();
            if (_pvpEnabled != pvpEnabled)
            {
                _pvpEnabled = pvpEnabled;
                Icon.sprite = _pvpEnabled ? PvpOnSprite : PvpOffSprite;
                SetColor();
            }
        }

        public override void SetColor()
        {
            base.SetColor();
            if (_pvpEnabled)
            {
                Icon.color = PvpOnColor;
            }
        }
    }
}
