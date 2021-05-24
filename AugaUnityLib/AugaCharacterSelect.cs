using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaCharacterSelect : MonoBehaviour
    {
        public CharacterSelectPortrait CharacterPortraitPrefab;
        public RectTransform CharacterList;

        private readonly List<CharacterSelectPortrait> _portraits = new List<CharacterSelectPortrait>();

        public void OnEnable()
        {
            UpdateCharacterList();
        }

        public void UpdateCharacterList()
        {
            foreach (var portrait in _portraits)
            {
                Destroy(portrait.gameObject);
            }
            _portraits.Clear();

            for (var index = 0; index < FejdStartup.instance.m_profiles.Count; index++)
            {
                var profile = FejdStartup.instance.m_profiles[index];
                var portrait = Instantiate(CharacterPortraitPrefab, CharacterList, false);
                portrait.Setup(profile, index);
                _portraits.Add(portrait);
            }
        }
    }

    public class CharacterSelectPortrait : MonoBehaviour
    {
        public RawImage Image;
        public Text CharacterName;
        public Button Button;
        public GameObject Selected;

        private Texture _texture;
        private PlayerProfile _profile;
        private int _index;

        public void Setup(PlayerProfile profile, int index)
        {
            _profile = profile;
            _index = index;
            CharacterName.text = profile.m_playerName;
            Selected.SetActive(_index == FejdStartup.instance.m_profileIndex);
            Button.onClick.AddListener(() => FejdStartup.instance.SetSelectedProfile(_profile.m_filename));


        }

        public void Update()
        {
            Selected.SetActive(_index == FejdStartup.instance.m_profileIndex);
        }

        public void DoRender(VisEquipment visEquip, Camera camera)
        {
            camera.Render();
            SetTexture(camera.targetTexture);
        }

        public void SetTexture(RenderTexture renderTexture)
        {
            if (_texture == null)
            {
                _texture = new Texture2D(renderTexture.width, renderTexture.height, renderTexture.graphicsFormat, renderTexture.mipmapCount, TextureCreationFlags.None);
                Image.texture = _texture;
            }

            Graphics.CopyTexture(renderTexture, _texture);
        }

        [UsedImplicitly]
        public void OnDestroy()
        {
            Destroy(_texture);
        }
    }
}
