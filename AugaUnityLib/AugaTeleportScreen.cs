using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.UI;
using UnityStandardAssets.ImageEffects;

namespace AugaUnity
{
    public class AugaTeleportScreen : MonoBehaviour
    {
        public const string PortalPrefabName = "portal_wood";

        public GameObject StagePrefab;
        public PostProcessingProfile PostProcessingProfile;
        public RawImage DisplayImage;

        private GameObject _stage;
        private GameObject _portal;
        private Transform _cameraTarget;
        private Transform _cameraTargetEnd;
        private Camera _camera;
        private EffectFade _effectFade;
        private MeshRenderer _meshRenderer;
        private Vector3 _camSpeed = Vector3.zero;
        private Vector3 _camRotSpeed = Vector3.zero;

        public void OnEnable()
        {
            if (_stage == null)
            {
                _stage = Instantiate(StagePrefab);
                _stage.transform.position = new Vector3(9000, 9000, 9000);
                _stage.SetActive(false);

                var previousCamera = GameCamera.instance;
                _camera = Instantiate(previousCamera.m_camera, _stage.transform, false);
                _camera.enabled = false;
                _camera.GetComponent<GameCamera>().enabled = false;
                DestroyImmediate(_camera.GetComponent<GameCamera>());
                GameCamera.m_instance = previousCamera;
                _camera.tag = "Untagged";
                _camera.name = "PortalCamera";

                _cameraTarget = _stage.transform.Find("CameraTarget");
                _cameraTargetEnd = _stage.transform.Find("CameraTargetEnd");
                _camera.transform.position = _cameraTarget.position;
                _camera.transform.rotation = _cameraTarget.rotation;
                _camera.targetTexture = new RenderTexture(Screen.width, Screen.height, 24);
                _camera.GetComponent<DepthOfField>().enabled = false;
                _camera.GetComponent<SunShafts>().enabled = false;
                foreach (Transform child in _camera.transform)
                {
                    Destroy(child.gameObject);
                }

                var postProcessing = _camera.GetComponent<PostProcessingBehaviour>();
                postProcessing.profile = PostProcessingProfile;

                DisplayImage.texture = _camera.targetTexture;

                var prefab = ZNetScene.instance.GetPrefab(PortalPrefabName);
                _portal = new GameObject("FakePortal");
                _portal.transform.SetParent(_stage.transform);
                _portal.transform.localPosition = Vector3.zero;
                Instantiate(prefab.transform.Find("_target_found_red").gameObject, _portal.transform, false);
                Instantiate(prefab.transform.Find("New").gameObject, _portal.transform, false);

                _effectFade = _portal.GetComponentInChildren<EffectFade>(true);
                _meshRenderer = _portal.GetComponentInChildren<MeshRenderer>(true);

                Destroy(_portal.GetComponentInChildren<LODGroup>());
                Destroy(_portal.GetComponentInChildren<AudioSource>().gameObject);
            }

            _stage.SetActive(true);

            _camera.enabled = true;
            _camera.GetComponent<SunShafts>().enabled = false;
            _camera.transform.position = _cameraTarget.position;
            _camera.transform.rotation = _cameraTarget.rotation;

            _effectFade.SetActive(true);
            _meshRenderer.material.SetColor("_EmissionColor", new Color(5, 2.38f, 0, 1));
        }

        public void Update()
        {
            var dt = Time.deltaTime;
            _camera.transform.position = Vector3.SmoothDamp(_camera.transform.position, _cameraTargetEnd.position, ref _camSpeed, 7f, 1f, dt);
            var forward = Vector3.SmoothDamp(_camera.transform.forward, _cameraTargetEnd.forward, ref _camRotSpeed, 7f, 1f, dt);
            forward.Normalize();
            _camera.transform.rotation = Quaternion.LookRotation(forward);
        }

        public void OnDisable()
        {
            _stage.gameObject.SetActive(false);

            _camera.transform.position = _cameraTarget.position;
            _camera.transform.rotation = _cameraTarget.rotation;
        }

        public void OnDestroy()
        {
            if (_camera != null && _camera.targetTexture != null)
            {
                _camera.targetTexture.Release();
            }
        }
    }
}
