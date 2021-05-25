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

    public class CharacterPortraitsController : MonoBehaviour
    {
        public CharacterPortrait PortraitPrefab;
        public RectTransform PortraitList;
        public RenderTexture RenderTexture;
        public PortraitMode Mode;
        public PostProcessingProfile Profile;

        private Camera _camera;
        private Transform _lookTarget;
        private const float FOV = 11;
        private Vector3 _offset = new Vector3(0, -0.05f, 0);
        private PlayerCustomizaton _playerCustomizaton;
        private readonly List<CharacterPortrait> _characterPortraits = new List<CharacterPortrait>();
        private readonly Renderer[] _noRenderers = new Renderer[0];

        [UsedImplicitly]
        public void Awake()
        {
            _playerCustomizaton = FejdStartup.instance.m_newCharacterPanel.GetComponent<PlayerCustomizaton>();

            _camera = GetCamera(RenderTexture, Profile);
            _camera.name = "AugaCamera NewCharPortraits";
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

            var count = Mode == PortraitMode.Hair ? _playerCustomizaton.m_hairs.Count : _playerCustomizaton.m_beards.Count;
            for (var i = 0; i < count; i++)
            {
                var characterPortrait = Instantiate(PortraitPrefab, PortraitList);
                var index = i;
                characterPortrait.Button.onClick.AddListener(() => OnPortraitClick(index));
                characterPortrait.Setup(_playerCustomizaton, Mode, index);
                _characterPortraits.Add(characterPortrait);
            }
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

        public void Update()
        {
            var newLookTarget = Utils.FindChild(FejdStartup.instance.m_playerInstance.transform, "Head");
            if (_characterPortraits.Count == 0 || _lookTarget != newLookTarget)
            {
                InitializeChraracterPortraits();
            }

            _lookTarget = newLookTarget;
            _camera.transform.LookAt(_lookTarget.position + _offset);

            var player = _playerCustomizaton.GetPlayer();
            var visEquip = player.m_visEquipment;
            var itemInstance = Mode == PortraitMode.Hair ? visEquip.m_hairItemInstance : visEquip.m_beardItemInstance;
            var renderers = itemInstance?.GetComponentsInChildren<Renderer>() ?? _noRenderers;
            foreach (var renderer in renderers)
            {
                renderer.forceRenderingOff = true;
            }

            var currentIndex = Mode == PortraitMode.Hair ? _playerCustomizaton.GetHairIndex() : _playerCustomizaton.GetBeardIndex();
            for (var index = 0; index < _characterPortraits.Count; index++)
            {
                var characterPortrait = _characterPortraits[index];
                characterPortrait.Selected.SetActive(index == currentIndex);
                characterPortrait.DoRender(visEquip, _camera);
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
        }

        public void DoRender(VisEquipment visEquip, Camera camera)
        {
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
            _renderers.Clear();
            Destroy(_attachedItem);
            Destroy(_texture);
        }
    }
}
