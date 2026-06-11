using UnityEngine;
using UnitySimulationX.Core;
using UnitySimulationX.Viewer.Settings;

namespace UnitySimulationX.Viewer.Grid
{
    [ExecuteAlways]
    [DefaultExecutionOrder(100)]
    public sealed class FloorGridRenderer : MonoBehaviour, IFloorGridService
    {
        const string GridObjectName = "FloorGrid";
        const string GridShaderName = "UnitySimulationX/FloorGrid";
        const float GridHeightOffset = 0.005f;

        static Mesh _sharedQuadMesh;

        [SerializeField] ViewerPresentationSettings settings;

        GameObject _gridObject;
        MeshRenderer _meshRenderer;
        Material _material;

        public bool Visible
        {
            get => _meshRenderer != null && _meshRenderer.enabled;
            set
            {
                if (_meshRenderer != null)
                    _meshRenderer.enabled = value;
            }
        }

        void OnEnable()
        {
            EnsureGridObject();
            ApplySettings();
        }

        void Awake()
        {
            if (Application.isPlaying)
                ServiceLocator.Register<IFloorGridService>(this);

            EnsureGridObject();
            ApplySettings();
        }

        void OnValidate()
        {
            EnsureGridObject();
            ApplySettings();
        }

        void OnDisable()
        {
            if (Application.isPlaying)
                return;

            DestroyPreview();
        }

        void EnsureGridObject()
        {
            if (_gridObject != null)
                return;

            _gridObject = new GameObject(GridObjectName)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _gridObject.transform.SetPositionAndRotation(new Vector3(0f, GridHeightOffset, 0f), Quaternion.identity);

            var meshFilter = _gridObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetOrCreateQuadMesh();

            _meshRenderer = _gridObject.AddComponent<MeshRenderer>();
            _material = CreateMaterial();
            _meshRenderer.sharedMaterial = _material;
            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
            _meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        void ApplySettings()
        {
            var config = ViewerSettingsUtility.LoadDefault(settings);
            if (config == null || _gridObject == null)
                return;

            var halfLineCount = Mathf.Max(1, config.halfLineCount);
            var spacing = Mathf.Max(0.01f, config.spacing);
            var majorLineEvery = Mathf.Max(1, config.majorLineEvery);
            var lineWidth = Mathf.Max(0.5f, config.lineWidth);
            var extent = halfLineCount * spacing;

            _gridObject.transform.localScale = new Vector3(extent * 2f, 1f, extent * 2f);

            if (_material == null)
                _material = CreateMaterial();

            _material.SetFloat("_Spacing", spacing);
            _material.SetFloat("_MajorEvery", majorLineEvery);
            _material.SetFloat("_Extent", extent);
            _material.SetFloat("_LineWidth", lineWidth);
            _material.SetColor("_MinorColor", config.minorLineColor);
            _material.SetColor("_MajorColor", config.majorLineColor);
            _material.SetColor("_AxisXColor", config.axisXColor);
            _material.SetColor("_AxisYColor", config.axisYColor);
            _material.SetFloat("_FadeStart", config.fadeStart);
            _material.SetFloat("_FadeEnabled", config.distanceFade ? 1f : 0f);
        }

        static Mesh GetOrCreateQuadMesh()
        {
            if (_sharedQuadMesh != null)
                return _sharedQuadMesh;

            _sharedQuadMesh = new Mesh
            {
                name = "FloorGridQuad",
                hideFlags = HideFlags.HideAndDontSave
            };

            _sharedQuadMesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f)
            };

            _sharedQuadMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };

            _sharedQuadMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _sharedQuadMesh.UploadMeshData(true);
            return _sharedQuadMesh;
        }

        Material CreateMaterial()
        {
            var shader = Shader.Find(GridShaderName);
            if (shader == null)
            {
                Debug.LogError($"FloorGridRenderer: Shader '{GridShaderName}' was not found.");
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            return new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        void DestroyPreview()
        {
#if UNITY_EDITOR
            if (_gridObject != null)
            {
                DestroyImmediate(_gridObject);
                _gridObject = null;
                _meshRenderer = null;
            }

            if (_material != null)
            {
                DestroyImmediate(_material);
                _material = null;
            }
#endif
        }

        void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (_gridObject != null)
                    Destroy(_gridObject);

                if (_material != null)
                    Destroy(_material);
            }
            else
            {
                DestroyPreview();
            }
        }
    }
}
