using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class SelectedCharacterInfo : MonoBehaviour
    {
        public Text TextBox;

        private string _currentProfile;
        private readonly StringBuilder _sb = new StringBuilder();

        [UsedImplicitly]
        public void Update()
        {
            var selectedProfile = FejdStartup.instance.m_profiles[FejdStartup.instance.m_profileIndex];
            if (_currentProfile != selectedProfile.GetName())
            {
                _currentProfile = selectedProfile.GetName();

                SetPlayerInfo(selectedProfile);
            }
        }

        private void SetPlayerInfo(PlayerProfile profile)
        {
            _sb.Clear();

            _sb.AppendLine($"{profile.m_playerName}:");
            _sb.AppendLine($"   Deaths: {profile.m_playerStats.m_deaths}, Builds: {profile.m_playerStats.m_builds}, Crafts: {profile.m_playerStats.m_crafts}");

            var worldNames = World.GetWorldList().Where(x => profile.m_worldData.ContainsKey(x.m_uid)).Select(x => x.m_name);
            _sb.AppendLine($"   Local Worlds: {string.Join(", ", worldNames)}");

            TextBox.text = _sb.ToString();
        }
    }
}
