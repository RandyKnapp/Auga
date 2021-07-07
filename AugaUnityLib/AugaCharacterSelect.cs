using System.Collections;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.PostProcessing;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaCharacterSelectPhotoBooth : MonoBehaviour
    {
        public RenderTexture RenderTexture;
        public PostProcessingProfile Profile;

        public static bool TakingPhotos;

        private Camera _camera;
        private Transform _lookTarget;
        private readonly Vector3 _offset = new Vector3(0, -0.05f, 0);

        private int _profileIndex;

        [UsedImplicitly]
        public void Awake()
        {
            _camera = CharacterPortraitsController.GetCamera(RenderTexture, Profile);
            _camera.name = "AugaCamera PhotoBooth";

            _profileIndex = 0;
        }

        [UsedImplicitly]
        public void Start()
        {
            StartCoroutine(PhotoBoothCoroutine());
        }

        public IEnumerator PhotoBoothCoroutine()
        {
            TakingPhotos = true;
            while (FejdStartup.instance != null && FejdStartup.instance.m_profiles?.Count > 0)
            {
                if (_profileIndex < FejdStartup.instance.m_profiles.Count)
                {
                    yield return TakePhoto(_profileIndex);
                    _profileIndex++;
                }
                else if (_profileIndex == FejdStartup.instance.m_profiles.Count)
                {
                    FejdStartup.instance.UpdateCharacterList();
                    TakingPhotos = false;
                    yield break;
                }

                yield return null;
            }

            TakingPhotos = false;
        }

        public IEnumerator TakePhoto(int profileIndex)
        {
            var profile = FejdStartup.instance.m_profiles[profileIndex];
            FejdStartup.instance.SetupCharacterPreview(profile);

            //yield return new WaitForSeconds(1);
            for (int i = 0; i < 30; i++)
            {
                yield return null;
            }

            _lookTarget = Utils.FindChild(FejdStartup.instance.m_playerInstance.transform, "Head");
            _camera.transform.LookAt(_lookTarget.position + _offset);

            _camera.Render();
            var profilePic = new Texture2D(RenderTexture.width, RenderTexture.height, RenderTexture.graphicsFormat, RenderTexture.mipmapCount, TextureCreationFlags.None);

            RenderTexture.active = RenderTexture;
            profilePic.ReadPixels(new Rect(0, 0, RenderTexture.width, RenderTexture.height), 0, 0);
            profilePic.Apply();

            var outputFileName = profile.m_filename + ".png";
            var outputFilePath = Utils.GetSaveDataPath() + "/characters/" + outputFileName;
            var bytes = profilePic.EncodeToPNG();

            File.WriteAllBytes(outputFilePath, bytes);
            Destroy(profilePic);
        }
    }

    public class AugaCharacterSelect : MonoBehaviour
    {
        public CharacterSelectPortrait CharacterPortraitPrefab;
        public Scrollbar ScrollBar;
        public RectTransform CharacterList;
        public RenderTexture RenderTexture;

        private readonly List<CharacterSelectPortrait> _portraits = new List<CharacterSelectPortrait>();
        private bool _onFirstUpdate;

        [UsedImplicitly]
        public void OnEnable()
        {
            _onFirstUpdate = false;
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
                portrait.Setup(profile, index, RenderTexture);
                _portraits.Add(portrait);
            }
        }

        public void LateUpdate()
        {
            if (!_onFirstUpdate)
            {
                _onFirstUpdate = true;

                var currentIndex = FejdStartup.instance.m_profileIndex;
                if (currentIndex >= 0 && currentIndex < _portraits.Count && _portraits.Count > 1)
                {
                    ScrollBar.value = 1.0f - (currentIndex / (_portraits.Count - 1.0f));
                }
            }
        }
    }

    public class CharacterSelectPortrait : MonoBehaviour
    {
        public RawImage Image;
        public Text CharacterName;
        public Button Button;
        public GameObject Selected;
        public Text StatsText;

        public Color NameColorSelected;
        public Color StatsTextColorSelected;

        private Texture2D _texture;
        private PlayerProfile _profile;
        private int _index;
        private Color _originalNameColor;
        private Color _originalStatsTextColor;

        public void Awake()
        {
            _originalNameColor = CharacterName.color;
            _originalStatsTextColor = StatsText.color;
            Update();
        }

        public void Setup(PlayerProfile profile, int index, RenderTexture renderTexture)
        {
            _profile = profile;
            _index = index;
            CharacterName.text = profile.m_playerName;
            Button.onClick.AddListener(() => FejdStartup.instance.SetSelectedProfile(_profile.m_filename));
            StatsText.text = $"{profile.m_playerStats.m_deaths}\n{profile.m_playerStats.m_builds}\n{profile.m_playerStats.m_crafts}";

            var outputFileName = profile.m_filename + ".png";
            var outputFilePath = Utils.GetSaveDataPath() + "/characters/" + outputFileName;
            if (File.Exists(outputFilePath))
            {
                var bytes = File.ReadAllBytes(outputFilePath);

                _texture = new Texture2D(renderTexture.width, renderTexture.height, renderTexture.graphicsFormat, renderTexture.mipmapCount, TextureCreationFlags.None);
                _texture.LoadImage(bytes);

                Image.texture = _texture;
            }

            Update();
        }

        public void Update()
        {
            var selected = _index == FejdStartup.instance.m_profileIndex;
            Selected.SetActive(selected);
            CharacterName.color = selected ? NameColorSelected : _originalNameColor;
            StatsText.color = selected ? StatsTextColorSelected : _originalStatsTextColor;
        }

        [UsedImplicitly]
        public void OnDestroy()
        {
            Destroy(_texture);
        }
    }
}
