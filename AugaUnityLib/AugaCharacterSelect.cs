using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        public void Awake()
        {
            _camera = CharacterPortraitsController.GetCamera(RenderTexture, Profile);
            _camera.name = "AugaCamera PhotoBooth";

            _profileIndex = 0;
        }

        public void Start()
        {
            // SetupCharacterPreview создаёт Heightmap-объекты которые требуют WorldGenerator.
            // В главном меню WorldGenerator не инициализирован → Heightmap.Generate крашится
            // и сломанные объекты остаются в Heightmap.m_heightmaps → ClutterSystem спам.
            // Пропускаем фотосессию если WorldGenerator недоступен.
            if (WorldGenerator.instance == null)
            {
                Debug.LogWarning("[Auga] PhotoBooth: WorldGenerator not available, skipping character photos.");
                TakingPhotos = false;
                return;
            }
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
            for (int i = 0; i < 15; i++)
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
            var bytes = ImageConversionReflection.EncodeToPNG(profilePic);

            SaveProfilePic(profile, bytes);
            
            Destroy(profilePic);
        }

        private void SaveProfilePic(PlayerProfile profile, byte[] bytes)
        {
            var outputFilePath = GetOutputFilePathForProfile(profile);
            File.WriteAllBytes(outputFilePath, bytes);
        }

        public static string GetOutputFilePathForProfile(PlayerProfile profile)
        {
            var outputFileName = profile.m_filename + ".png";
            var outputFilePath = Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + PlayerProfile.GetCharacterFolder(profile.m_fileSource) + outputFileName;
            return outputFilePath;
        }
    }

    public class AugaCharacterSelect : MonoBehaviour
    {
        public CharacterSelectPortrait CharacterPortraitPrefab;
        public Scrollbar ScrollBar;
        public RectTransform CharacterList;
        public RenderTexture RenderTexture;
        public GameObject SourceInfoPanel;
        public Text SourceInfoContent;

        private readonly List<CharacterSelectPortrait> _portraits = new List<CharacterSelectPortrait>();
        private bool _onFirstUpdate;

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

            var showSourceInfoPanel = !FileHelpers.CloudStorageEnabled;
            SourceInfoContent.text = "";
            if (FejdStartup.instance.m_profileIndex >= 0 && FejdStartup.instance.m_profileIndex < FejdStartup.instance.m_profiles.Count)
            {
                var selectedProfile = FejdStartup.instance.m_profiles[FejdStartup.instance.m_profileIndex];
                if (selectedProfile != null && selectedProfile.m_fileSource == FileHelpers.FileSource.Legacy)
                {
                    SourceInfoContent.text = Localization.instance.Localize("$menu_legacynotice \n\n");
                }
            }

            if (!FileHelpers.CloudStorageEnabled)
            {
                SourceInfoContent.text += Localization.instance.Localize("$menu_cloudsavesdisabled");
            }

            SourceInfoPanel.gameObject.SetActive(showSourceInfoPanel);
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
        public Image LocalSave;
        public Image LegacySave;
        public Image CloudSave;

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
            StatsText.text = $"{profile.m_playerStats.m_stats[PlayerStatType.Deaths]}\n{profile.m_playerStats.m_stats[PlayerStatType.Builds]}\n{profile.m_playerStats.m_stats[PlayerStatType.Crafts]}";

            var outputFilePath = AugaCharacterSelectPhotoBooth.GetOutputFilePathForProfile(profile);
            if (File.Exists(outputFilePath))
            {
                var bytes = File.ReadAllBytes(outputFilePath);

                _texture = new Texture2D(renderTexture.width, renderTexture.height, renderTexture.graphicsFormat, renderTexture.mipmapCount, TextureCreationFlags.None);
                ImageConversionReflection.LoadImage(_texture, bytes);

                Image.texture = _texture;
            }

            LocalSave?.gameObject.SetActive(profile.m_fileSource == FileHelpers.FileSource.Local);
            LegacySave?.gameObject.SetActive(profile.m_fileSource == FileHelpers.FileSource.Legacy);
            CloudSave?.gameObject.SetActive(profile.m_fileSource == FileHelpers.FileSource.Cloud || profile.m_fileSource == FileHelpers.FileSource.Auto);

            Update();
        }

        public void Update()
        {
            var selected = _index == FejdStartup.instance.m_profileIndex;
            Selected.SetActive(selected);
            CharacterName.color = selected ? NameColorSelected : _originalNameColor;
            StatsText.color = selected ? StatsTextColorSelected : _originalStatsTextColor;
        }

        public void OnDestroy()
        {
            Destroy(_texture);
        }
    }

    /// <summary>
    /// Reflection-based wrapper for UnityEngine.ImageConversion methods.
    /// Avoids a direct compile-time reference to UnityEngine.ImageConversionModule.dll,
    /// which in Unity 6 defines ReadOnlySpan&lt;byte&gt; overloads that break .NET Framework builds (CS1705/CS7069).
    /// At runtime Unity always loads the module, so reflection is safe.
    /// </summary>
    internal static class ImageConversionReflection
    {
        private static MethodInfo _encodeToPng;
        private static MethodInfo _loadImage;
        private static bool _resolved;

        private static void EnsureResolved()
        {
            if (_resolved) return;
            _resolved = true;
            var imageConvType = System.Type.GetType(
                "UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
            if (imageConvType == null) return;

            _encodeToPng = imageConvType.GetMethod(
                "EncodeToPNG",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Texture2D) },
                null);

            _loadImage = imageConvType.GetMethod(
                "LoadImage",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) },
                null);

            // Fallback: 2-arg overload without the markNonReadable flag
            if (_loadImage == null)
            {
                _loadImage = imageConvType.GetMethod(
                    "LoadImage",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(Texture2D), typeof(byte[]) },
                    null);
            }
        }

        public static byte[] EncodeToPNG(Texture2D texture)
        {
            EnsureResolved();
            if (_encodeToPng != null)
                return (byte[])_encodeToPng.Invoke(null, new object[] { texture });

            Debug.LogError("[ImageConversionReflection] EncodeToPNG not found via reflection");
            return null;
        }

        public static void LoadImage(Texture2D texture, byte[] data)
        {
            EnsureResolved();
            if (_loadImage != null)
            {
                var paramCount = _loadImage.GetParameters().Length;
                if (paramCount == 3)
                    _loadImage.Invoke(null, new object[] { texture, data, false });
                else
                    _loadImage.Invoke(null, new object[] { texture, data });
                return;
            }
            Debug.LogError("[ImageConversionReflection] LoadImage not found via reflection");
        }
    }
}
