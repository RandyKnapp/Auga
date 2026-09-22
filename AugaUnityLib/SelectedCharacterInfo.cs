using System.Linq;
using System.Text;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class SelectedCharacterInfo : MonoBehaviour
    {
        public TMP_Text TextBox;

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
            _sb.AppendLine($"   Deaths: {profile.GetStat(PlayerStatType.Deaths)}, Builds: {profile.GetStat(PlayerStatType.Builds)}, Crafts: {profile.GetStat(PlayerStatType.Crafts)}");

            var worldNames = SaveSystem.GetWorldList().Where(x => profile.m_worldData.ContainsKey(x.m_uid)).Select(x => x.m_name);
            _sb.AppendLine($"   Local Worlds: {string.Join(", ", worldNames)}");

            TextBox.text = _sb.ToString();
        }
    }
}
