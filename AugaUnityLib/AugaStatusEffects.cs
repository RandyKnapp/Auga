using System.Collections.Generic;
using UnityEngine;

namespace AugaUnity
{
    public class AugaStatusEffects : MonoBehaviour
    {
        public GameObject EffectsContainer;
        public PlayerPanelEffectController EffectPrefab;

        protected readonly List<StatusEffect> _playerStatusEffects = new List<StatusEffect>();
        protected readonly List<PlayerPanelEffectController> _statusEffects = new List<PlayerPanelEffectController>();

        public virtual void Update()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            UpdateStatusEffects(player);
        }

        // The effects are looked up once a frame here and each icon is handed its own, rather than every icon
        // looking the whole list up again for itself.
        public virtual void UpdateStatusEffects(Player player)
        {
            _playerStatusEffects.Clear();
            player.GetSEMan().GetHUDStatusEffects(_playerStatusEffects);

            while (_statusEffects.Count < _playerStatusEffects.Count)
            {
                var effect = Instantiate(EffectPrefab, EffectsContainer.transform, false);
                effect.Index = _statusEffects.Count;
                _statusEffects.Add(effect);
            }

            foreach (var effect in _statusEffects)
            {
                effect.Show(effect.Index < _playerStatusEffects.Count ? _playerStatusEffects[effect.Index] : null);
            }
        }
    }
}
