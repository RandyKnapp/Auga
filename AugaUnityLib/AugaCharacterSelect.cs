using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using UnityStandardAssets.ImageEffects;

namespace AugaUnity
{
    public class AugaCharacterSelect : MonoBehaviour
    {
        private const float FOV = 11;

        public CharacterSelectPortrait CharacterPortraitPrefab;
        public RectTransform CharacterList;
        public RenderTexture RenderTexture;

        private Camera _camera;
        private Transform _lookTarget;
        private readonly Vector3 _offset = new Vector3(0, -0.05f, 0);

        private readonly List<CharacterSelectPortrait> _portraits = new List<CharacterSelectPortrait>();

        [UsedImplicitly]
        public void Awake()
        {
            _camera = Instantiate(FejdStartup.instance.m_mainCamera.GetComponent<Camera>());
            _camera.transform.position = FejdStartup.instance.m_cameraMarkerCharacter.position;
            _camera.fieldOfView = FOV;
            _camera.targetTexture = RenderTexture;
            _camera.GetComponent<DepthOfField>().enabled = false;
            _camera.enabled = false;

            _camera.transform.position = FejdStartup.instance.m_cameraMarkerCharacter.position;
            _camera.transform.rotation = FejdStartup.instance.m_cameraMarkerCharacter.rotation;
        }

        [UsedImplicitly]
        public void Start()
        {
            _lookTarget = Utils.FindChild(FejdStartup.instance.m_playerInstance.transform, "Head");
            _camera.transform.LookAt(_lookTarget.position + _offset);
        }

        [UsedImplicitly]
        public void Update()
        {
            _camera.transform.LookAt(_lookTarget.position + _offset);
        }

        [UsedImplicitly]
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

            //PlayerProfile selectedProfile = null;
            for (var index = 0; index < FejdStartup.instance.m_profiles.Count; index++)
            {
                var profile = FejdStartup.instance.m_profiles[index];
                //if (index == FejdStartup.instance.m_profileIndex)
                //{
                //    selectedProfile = profile;
                //}

                var portrait = Instantiate(CharacterPortraitPrefab, CharacterList, false);
                portrait.Setup(profile, index, _camera);
                _portraits.Add(portrait);
            }

            /*if (selectedProfile != null)
            {
                FejdStartup.instance.SetupCharacterPreview(selectedProfile);
            }*/

            _lookTarget = Utils.FindChild(FejdStartup.instance.m_playerInstance.transform, "Head");
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

        public void Setup(PlayerProfile profile, int index, Camera camera)
        {
            _profile = profile;
            _index = index;
            CharacterName.text = profile.m_playerName;
            Selected.SetActive(_index == FejdStartup.instance.m_profileIndex);
            Button.onClick.AddListener(() => FejdStartup.instance.SetSelectedProfile(_profile.m_filename));

            //FejdStartup.instance.SetupCharacterPreview(_profile);
            //DoRender(camera);
        }

        public void Update()
        {
            Selected.SetActive(_index == FejdStartup.instance.m_profileIndex);
        }

        public void DoRender(Camera camera)
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
