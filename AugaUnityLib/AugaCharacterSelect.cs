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

        [UsedImplicitly]
        public void Update()
        {
            _camera.Render();
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                _profileIndex = 0;
                StartCoroutine(PhotoBoothCoroutine());
            }
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
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;

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
        public RectTransform CharacterList;
        public RenderTexture RenderTexture;

        private readonly List<CharacterSelectPortrait> _portraits = new List<CharacterSelectPortrait>();

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

            for (var index = 0; index < FejdStartup.instance.m_profiles.Count; index++)
            {
                var profile = FejdStartup.instance.m_profiles[index];
                var portrait = Instantiate(CharacterPortraitPrefab, CharacterList, false);
                portrait.Setup(profile, index, RenderTexture);
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

        private Texture2D _texture;
        private PlayerProfile _profile;
        private int _index;

        public void Setup(PlayerProfile profile, int index, RenderTexture renderTexture)
        {
            _profile = profile;
            _index = index;
            CharacterName.text = profile.m_playerName;
            Selected.SetActive(_index == FejdStartup.instance.m_profileIndex);
            Button.onClick.AddListener(() => FejdStartup.instance.SetSelectedProfile(_profile.m_filename));

            var outputFileName = profile.m_filename + ".png";
            var outputFilePath = Utils.GetSaveDataPath() + "/characters/" + outputFileName;
            if (File.Exists(outputFilePath))
            {
                var bytes = File.ReadAllBytes(outputFilePath);

                _texture = new Texture2D(renderTexture.width, renderTexture.height, renderTexture.graphicsFormat, renderTexture.mipmapCount, TextureCreationFlags.None);
                _texture.LoadImage(bytes);

                Image.texture = _texture;
            }
        }

        public void Update()
        {
            Selected.SetActive(_index == FejdStartup.instance.m_profileIndex);
        }

        [UsedImplicitly]
        public void OnDestroy()
        {
            Destroy(_texture);
        }
    }
}
