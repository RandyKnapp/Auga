using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.PostProcessing;
using UnityEngine.UI;
using UnityStandardAssets.ImageEffects;

namespace AugaUnity
{
    public enum PortraitMode
    {
        Hair,
        Beard
    }

    /// <summary>
    /// The grid of hair (or beard) portraits on the new character screen. Each portrait is a photo of the preview
    /// player wearing one item, taken with a private camera. Photos are cached: they are retaken only when the
    /// model itself changes (body, skin, hair colour, the item in the other slot), and then a few per frame
    /// (<see cref="RendersPerFrame"/>) round-robin until every portrait is current again. Photos are taken at the
    /// end of the frame, once the model has settled for a few frames: a body mesh swapped this frame has no valid
    /// skinning data yet for an extra camera render (Unity then draws nothing and logs a mesh data mismatch).
    /// </summary>
    public class CharacterPortraitsController : MonoBehaviour
    {
        public CharacterPortrait PortraitPrefab;
        public RectTransform PortraitList;
        public RenderTexture RenderTexture;
        public PortraitMode Mode;
        public PostProcessingProfile Profile;
        [Tooltip("How many out-of-date portraits are re-rendered per frame")]
        public int RendersPerFrame = 5;
        [Tooltip("Frames the model gets to settle after a change before its portraits are retaken")]
        public int SettleFrames = 5;

        private Camera _camera;
        private Transform _lookTarget;
        private GameObject _playerInstance;
        private PortraitMode _currentMode;
        private const float FOV = 11;
        private readonly Vector3 _offset = new Vector3(0, -0.05f, 0);
        private PlayerCustomizaton _playerCustomizaton;
        private readonly List<CharacterPortrait> _characterPortraits = new List<CharacterPortrait>();
        private readonly List<bool> _stale = new List<bool>();
        private int _staleCount;
        private int _cursor;
        private int _lookChangedFrame = -1;
        private Coroutine _renderLoop;
        private (int model, Vector3 skin, Vector3 hair, int otherItem) _renderedLook;
        private readonly Renderer[] _noRenderers = Array.Empty<Renderer>();

        [UsedImplicitly]
        public void Awake()
        {
            _playerCustomizaton = FejdStartup.instance.m_newCharacterPanel.GetComponent<PlayerCustomizaton>();

            _camera = GetCamera(RenderTexture, Profile);
            _camera.name = "AugaCamera NewCharPortraits";

            _currentMode = Mode;
        }

        [UsedImplicitly]
        public void OnEnable()
        {
            _renderLoop = StartCoroutine(RenderLoop());
        }

        [UsedImplicitly]
        public void OnDisable()
        {
            if (_renderLoop != null)
                StopCoroutine(_renderLoop);
            _renderLoop = null;
        }

        public void SwitchToHairMode()
        {
            Mode = PortraitMode.Hair;
        }

        public void SwitchToBeardMode()
        {
            Mode = PortraitMode.Beard;
        }

        public static Camera GetCamera(RenderTexture renderTexture, PostProcessingProfile profile)
        {
            var camera = Instantiate(FejdStartup.instance.m_mainCamera.GetComponent<Camera>());
            camera.fieldOfView = FOV;
            camera.targetTexture = renderTexture;
            camera.GetComponent<DepthOfField>().enabled = false;
            camera.enabled = false;

            camera.transform.position = FejdStartup.instance.m_cameraMarkerCharacter.position;
            camera.transform.rotation = FejdStartup.instance.m_cameraMarkerCharacter.rotation;

            var postProcessing = camera.GetComponent<PostProcessingBehaviour>();
            postProcessing.profile = profile;

            return camera;
        }

        public void InitializeChraracterPortraits()
        {
            foreach (var characterPortrait in _characterPortraits)
            {
                Destroy(characterPortrait.gameObject);
            }
            _characterPortraits.Clear();
            _stale.Clear();
            _cursor = 0;

            var count = Mode == PortraitMode.Hair ? _playerCustomizaton.m_hairs.Count : _playerCustomizaton.m_beards.Count;
            for (var i = 0; i < count; i++)
            {
                var characterPortrait = Instantiate(PortraitPrefab, PortraitList);
                var index = i;
                characterPortrait.Button.onClick.AddListener(() => OnPortraitClick(index));
                characterPortrait.Setup(_playerCustomizaton, Mode, index);
                _characterPortraits.Add(characterPortrait);
                _stale.Add(true);
            }
            _staleCount = count;
            _lookChangedFrame = Time.frameCount;
        }

        /// <summary>Every portrait is retaken (a few per frame) once the model has settled.</summary>
        public void MarkAllStale()
        {
            for (var i = 0; i < _stale.Count; i++)
                _stale[i] = true;
            _staleCount = _stale.Count;
            _lookChangedFrame = Time.frameCount;
        }

        public void OnPortraitClick(int index)
        {
            switch (Mode)
            {
                case PortraitMode.Hair:
                    _playerCustomizaton.SetHair(index);
                    break;

                default:
                    _playerCustomizaton.SetBeard(index);
                    break;
            }
        }

        // LateUpdate: after PlayerCustomizaton has pushed the sliders into the model; the photos follow at the end of the frame
        public void LateUpdate()
        {
            var startup = FejdStartup.instance;
            var playerInstance = startup != null ? startup.m_playerInstance : null;
            if (playerInstance == null || _playerCustomizaton == null || _playerCustomizaton.m_hairs == null)
                return;

            if (playerInstance != _playerInstance)
            {
                _playerInstance = playerInstance;
                _lookTarget = Utils.FindChild(playerInstance.transform, "Head");
                InitializeChraracterPortraits();
            }
            else if (_characterPortraits.Count == 0 || _currentMode != Mode)
            {
                InitializeChraracterPortraits();
            }
            _currentMode = Mode;

            var player = _playerCustomizaton.GetPlayer();
            if (player == null || _lookTarget == null)
                return;
            var visEquip = player.m_visEquipment;

            // the part of the model every portrait shares: a change there dates all of them
            var look = (visEquip.m_modelIndex, visEquip.m_skinColor, visEquip.m_hairColor,
                Mode == PortraitMode.Hair ? visEquip.m_currentBeardItemHash : visEquip.m_currentHairItemHash);
            if (!look.Equals(_renderedLook))
            {
                _renderedLook = look;
                MarkAllStale();
            }

            var currentIndex = Mode == PortraitMode.Hair ? _playerCustomizaton.GetHairIndex() : _playerCustomizaton.GetBeardIndex();
            for (var index = 0; index < _characterPortraits.Count; index++)
            {
                _characterPortraits[index].Selected.SetActive(index == currentIndex);
            }
        }

        private IEnumerator RenderLoop()
        {
            var endOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return endOfFrame;
                if (_staleCount == 0 || _lookTarget == null || Time.frameCount - _lookChangedFrame < Mathf.Max(0, SettleFrames))
                    continue;
                var player = _playerCustomizaton != null ? _playerCustomizaton.GetPlayer() : null;
                if (player == null)
                    continue;
                RenderStalePortraits(player.m_visEquipment);
            }
        }

        /// <summary>Retakes up to <see cref="RendersPerFrame"/> stale portraits, continuing round-robin where the last frame stopped.</summary>
        private void RenderStalePortraits(VisEquipment visEquip)
        {
            _camera.transform.LookAt(_lookTarget.position + _offset);

            // the item being browsed is what each portrait adds itself; the model's own copy of it stays out of the photos
            var itemInstance = Mode == PortraitMode.Hair ? visEquip.m_hairItemInstance : visEquip.m_beardItemInstance;
            var renderers = itemInstance?.GetComponentsInChildren<Renderer>() ?? _noRenderers;
            foreach (var renderer in renderers)
            {
                renderer.forceRenderingOff = true;
            }

            var count = _characterPortraits.Count;
            var budget = Mathf.Max(1, RendersPerFrame);
            for (var step = 0; step < count && budget > 0; step++)
            {
                var index = (_cursor + step) % count;
                if (!_stale[index])
                    continue;
                _characterPortraits[index].DoRender(visEquip, _camera);
                _stale[index] = false;
                _staleCount--;
                budget--;
                _cursor = (index + 1) % count;
            }

            foreach (var renderer in renderers)
            {
                renderer.forceRenderingOff = false;
            }
        }
    }

    public class CharacterPortrait : MonoBehaviour
    {
        public RawImage Image;
        public Button Button;
        public GameObject Selected;

        private Texture _texture;
        private GameObject _attachedItem;
        private List<Renderer> _renderers;

        public void Setup(PlayerCustomizaton playerCustomizaton, PortraitMode mode, int index)
        {
            var player = playerCustomizaton.GetPlayer();
            var visEquip = player.m_visEquipment;
            var items = mode == PortraitMode.Hair ? playerCustomizaton.m_hairs : playerCustomizaton.m_beards;
            var itemName = items[index].gameObject.name;
            var itemHash = itemName.GetStableHashCode();

            _attachedItem = visEquip.AttachItem(itemHash, 0, visEquip.m_helmet);
            _renderers = _attachedItem != null ? _attachedItem.GetComponentsInChildren<Renderer>().ToList() : new List<Renderer>();
            foreach (var renderer in _renderers)
            {
                renderer.forceRenderingOff = true;   // only visible while its own photo is taken
            }
        }

        public void DoRender(VisEquipment visEquip, Camera camera)
        {
            var motionBlur = PlatformPrefs.GetInt("MotionBlur");
            PlatformPrefs.SetInt("MotionBlur", 0);
            var hairColor = Utils.Vec3ToColor(visEquip.m_nview.GetZDO()?.GetVec3("HairColor", Vector3.one) ?? visEquip.m_hairColor);
            foreach (var renderer in _renderers)
            {
                renderer.forceRenderingOff = false;
                renderer.material.SetColor("_SkinColor", hairColor);
            }

            camera.Render();
            SetTexture(camera.targetTexture);

            foreach (var renderer in _renderers)
            {
                renderer.forceRenderingOff = true;
            }
            PlatformPrefs.SetInt("MotionBlur", motionBlur);
        }

        public void SetTexture(RenderTexture renderTexture)
        {
            if (_texture == null)
            {
                _texture = new Texture2D(renderTexture.width, renderTexture.height, renderTexture.graphicsFormat, renderTexture.mipmapCount, TextureCreationFlags.None);
                Image.texture = _texture;
            }

            Graphics.ConvertTexture(renderTexture, _texture);
        }

        [UsedImplicitly]
        public void OnDestroy()
        {
            _renderers.Clear();
            Destroy(_attachedItem);
            Destroy(_texture);
        }
    }
}
