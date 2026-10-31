using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.PostProcessing;
using UnityEngine.UI;

namespace AugaUnity
{
    /// <summary>
    /// Takes the character select portraits. Each character sits for its photo as a stand-in at the preview spot, so
    /// it gets the preview's light and background, but only the booth's camera sees the stand-in, and the booth's
    /// camera does not see the preview: what the player is looking at never changes.
    /// </summary>
    public class AugaCharacterSelectPhotoBooth : MonoBehaviour
    {
        public RenderTexture RenderTexture;
        public PostProcessingProfile Profile;

        // Frames the stand-in gets to put its gear on and settle into its idle pose
        private const int SettleFrames = 15;

        private Camera _camera;
        private readonly Vector3 _offset = new Vector3(0, -0.05f, 0);
        private GameObject _standIn;
        private readonly List<Renderer> _renderers = new List<Renderer>();

        [UsedImplicitly]
        public void Awake()
        {
            _camera = CharacterPortraitsController.GetCamera(RenderTexture, Profile);
            _camera.name = "AugaCamera PhotoBooth";
            Camera.onPreCull += HideStandIn;
        }

        [UsedImplicitly]
        public void Start()
        {
            StartCoroutine(PhotoBoothCoroutine());
        }

        [UsedImplicitly]
        public void OnDestroy()
        {
            Camera.onPreCull -= HideStandIn;
            if (_standIn != null)
                Destroy(_standIn);
        }

        // Only the characters without a current portrait sit for one; the list then picks up the new ones.
        public IEnumerator PhotoBoothCoroutine()
        {
            var tookPhotos = false;
            for (var index = 0; FejdStartup.instance != null && index < (FejdStartup.instance.m_profiles?.Count ?? 0); index++)
            {
                if (!NeedsPhoto(FejdStartup.instance.m_profiles[index]))
                    continue;
                yield return TakePhoto(index);
                tookPhotos = true;
            }

            if (tookPhotos)
                RefreshPortraits();
        }

        /// <summary>The portrait list shows the current photos. The preview is left as it is.</summary>
        public static void RefreshPortraits()
        {
            var panel = FejdStartup.instance != null ? FejdStartup.instance.m_selectCharacterPanel : null;
            var select = panel != null ? panel.GetComponentInChildren<AugaCharacterSelect>(true) : null;
            if (select != null && select.isActiveAndEnabled)
                select.UpdatePortraits();
        }

        /// <summary>
        /// A character needs a portrait when it has none yet, or when its save is newer than the portrait: it was
        /// played since (new gear, a visit to the barber).
        /// </summary>
        public static bool NeedsPhoto(PlayerProfile profile)
        {
            var photoPath = GetOutputFilePathForProfile(profile);
            if (!File.Exists(photoPath))
                return true;
            try
            {
                return FileHelpers.GetLastWriteTime(profile.GetPath(), profile.m_fileSource) > File.GetLastWriteTime(photoPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Auga] Could not compare the save of {profile.m_filename} with its portrait: {e.Message}");
                return false;
            }
        }

        public IEnumerator TakePhoto(int profileIndex)
        {
            // one at a time: a new character can come in while the booth is still busy
            while (_standIn != null)
                yield return null;
            var profiles = FejdStartup.instance != null ? FejdStartup.instance.m_profiles : null;
            if (profiles == null || profileIndex < 0 || profileIndex >= profiles.Count)
                yield break;

            var profile = profiles[profileIndex];
            _standIn = CreateStandIn(profile);
            for (var i = 0; i < SettleFrames; i++)
                yield return null;
            // after the frame's animation and skinning, which the stand-in gets even while hidden
            yield return new WaitForEndOfFrame();

            // The stand-in goes whatever happens to the photo (a portrait file another program holds open, say): the
            // next photo waits for it to be gone, and the other characters still get theirs.
            try
            {
                var lookTarget = _standIn != null ? Utils.FindChild(_standIn.transform, "Head") : null;
                if (lookTarget != null)
                    SaveProfilePic(profile, Photograph(lookTarget));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Auga] The portrait of {profile.m_filename} could not be taken: {e.Message}");
            }
            finally
            {
                if (_standIn != null)
                    Destroy(_standIn);
                _standIn = null;
            }
        }

        // What vanilla's SetupCharacterPreview builds, without taking the preview's place
        private static GameObject CreateStandIn(PlayerProfile profile)
        {
            var startup = FejdStartup.instance;
            GameObject standIn;
            ZNetView.m_forceDisableInit = true;
            try
            {
                standIn = Instantiate(startup.m_playerPrefab, startup.m_characterPreviewPoint.position, startup.m_characterPreviewPoint.rotation);
            }
            finally
            {
                ZNetView.m_forceDisableInit = false;
            }
            standIn.name = "AugaPortraitStandIn";
            Destroy(standIn.GetComponent<Rigidbody>());
            foreach (var animator in standIn.GetComponentsInChildren<Animator>())
            {
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // no camera ever shows it on screen
            }
            try
            {
                profile.LoadPlayerData(standIn.GetComponent<Player>());
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Auga] Loading {profile.m_filename} for its portrait failed: {e.Message}");
            }
            return standIn;
        }

        // Every camera but the booth's skips the stand-in, gear that VisEquipment attached during the frame included
        private void HideStandIn(Camera camera)
        {
            if (_standIn != null && camera != _camera)
                SetStandInRendering(false);
        }

        // Hidden renderers would not be skinned, and the booth's one render would then show a stale pose or nothing
        private void SetStandInRendering(bool visible)
        {
            _standIn.GetComponentsInChildren(true, _renderers);
            foreach (var renderer in _renderers)
            {
                renderer.forceRenderingOff = !visible;
                if (renderer is SkinnedMeshRenderer skinned)
                    skinned.updateWhenOffscreen = true;
            }
        }

        // The stand-in shows and the preview hides for this one render only
        private byte[] Photograph(Transform lookTarget)
        {
            _camera.transform.LookAt(lookTarget.position + _offset);

            var hiddenPreview = new List<Renderer>();
            var preview = FejdStartup.instance != null ? FejdStartup.instance.m_playerInstance : null;
            if (preview != null)
            {
                foreach (var renderer in preview.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.forceRenderingOff)
                        continue;   // hidden by someone else, and stays hidden
                    renderer.forceRenderingOff = true;
                    hiddenPreview.Add(renderer);
                }
            }
            SetStandInRendering(true);
            try
            {
                _camera.Render();
            }
            finally
            {
                // the preview the player is looking at comes back even if the render failed
                SetStandInRendering(false);
                foreach (var renderer in hiddenPreview)
                    renderer.forceRenderingOff = false;
            }

            var profilePic = new Texture2D(RenderTexture.width, RenderTexture.height, RenderTexture.graphicsFormat, RenderTexture.mipmapCount, TextureCreationFlags.None);
            var previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = RenderTexture;
                profilePic.ReadPixels(new Rect(0, 0, RenderTexture.width, RenderTexture.height), 0, 0);
                profilePic.Apply();
                return profilePic.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previousActive;
                Destroy(profilePic);
            }
        }

        private void SaveProfilePic(PlayerProfile profile, byte[] bytes)
        {
            var outputFilePath = GetOutputFilePathForProfile(profile);
            // A cloud character's portrait goes in the local "characters" folder, which a player who only ever saved
            // to the cloud does not have
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            File.WriteAllBytes(outputFilePath, bytes);
        }

        public static string GetOutputFilePathForProfile(PlayerProfile profile)
        {
            var outputFileName = profile.m_filename + ".png";
            var outputFilePath = Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + (profile.m_fileSource.IsLocal() ? "/characters_local/" : "/characters/") + outputFileName;
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
        public TMP_Text SourceInfoContent;

        private readonly List<CharacterSelectPortrait> _portraits = new List<CharacterSelectPortrait>();
        private bool _onFirstUpdate;

        // Vanilla shows the panel without refreshing the list, so the source info text still holds what the last
        // refresh and the mods patching it wrote (MyLittleUI's character stats); only the portraits are rebuilt here.
        [UsedImplicitly]
        public void OnEnable()
        {
            _onFirstUpdate = false;
            UpdatePortraits();
        }

        public void UpdateCharacterList()
        {
            UpdatePortraits();
            UpdateSourceInfo();
        }

        public void UpdatePortraits()
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

        // The legacy and cloud-save notices, as vanilla writes them into m_csSourceInfo. The panel shows whenever the
        // text is not empty (LateUpdate), so it also shows text another mod writes there afterwards.
        public void UpdateSourceInfo()
        {
            if (SourceInfoContent == null)
                return;
            SourceInfoContent.text = "";
            if (FejdStartup.instance.m_profileIndex >= 0 && FejdStartup.instance.m_profileIndex < FejdStartup.instance.m_profiles.Count)
            {
                var selectedProfile = FejdStartup.instance.m_profiles[FejdStartup.instance.m_profileIndex];
                if (selectedProfile != null && selectedProfile.m_fileSource == FileHelpers.FileSource.Legacy)
                {
                    SourceInfoContent.text = Localization.instance.Localize("$menu_legacynotice \n\n");
                }
            }

            if (!FileHelpers.CloudStorageSupportedAndEnabled)
            {
                SourceInfoContent.text += Localization.instance.Localize("$menu_cloudsavesdisabled");
            }
        }

        public void LateUpdate()
        {
            if (SourceInfoPanel != null && SourceInfoContent != null)
            {
                var showSourceInfo = !string.IsNullOrWhiteSpace(SourceInfoContent.text);
                if (SourceInfoPanel.activeSelf != showSourceInfo)
                    SourceInfoPanel.SetActive(showSourceInfo);
            }

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
        public TMP_Text CharacterName;
        public Button Button;
        public GameObject Selected;
        public TMP_Text StatsText;
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
            // RawStats is the all-play bucket, filled whether or not the play could earn achievements
            const DifficultyRequirement allPlay = DifficultyRequirement.RawStats;
            StatsText.text = $"{profile.GetStat(PlayerStatType.Deaths, allPlay)}\n{profile.GetStat(PlayerStatType.Builds, allPlay)}\n{profile.GetStat(PlayerStatType.Crafts, allPlay)}";

            var outputFilePath = AugaCharacterSelectPhotoBooth.GetOutputFilePathForProfile(profile);
            if (File.Exists(outputFilePath))
            {
                var bytes = File.ReadAllBytes(outputFilePath);

                _texture = new Texture2D(renderTexture.width, renderTexture.height, renderTexture.graphicsFormat, renderTexture.mipmapCount, TextureCreationFlags.None);
                _texture.LoadImage(bytes);

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

        [UsedImplicitly]
        public void OnDestroy()
        {
            Destroy(_texture);
        }
    }
}
