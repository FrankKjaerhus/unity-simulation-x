using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimulationX.Core;
using UnitySimulationX.Viewer.Tools;

namespace UnitySimulationX.Viewer.Measure
{
    [DefaultExecutionOrder(210)]
    public sealed class MeasureToolController : MonoBehaviour, IMeasureToolService
    {
        UnityEngine.Camera _camera;
        LineRenderer _line;
        TextMesh _label;
        bool _hasFirstPoint;
        Vector3 _firstPoint;

        void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            ServiceLocator.Register<IMeasureToolService>(this);
            EnsureVisuals();
        }

        void Update()
        {
            if (!IsMeasureActive())
                return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Clear();
                return;
            }

            if (!WasLeftClickThisFrame() || ViewportInputUtility.IsPointerOverBlockingUi())
                return;

            var point = PickPoint();
            if (!point.HasValue)
                return;

            if (!_hasFirstPoint)
            {
                _firstPoint = point.Value;
                _hasFirstPoint = true;
                SetLine(_firstPoint, _firstPoint);
                return;
            }

            SetLine(_firstPoint, point.Value);
            SetLabel(_firstPoint, point.Value);
            _hasFirstPoint = false;
        }

        public void Clear()
        {
            _hasFirstPoint = false;
            if (_line != null)
                _line.enabled = false;
            if (_label != null)
                _label.gameObject.SetActive(false);
        }

        bool IsMeasureActive()
        {
            return ServiceLocator.TryResolve<IViewportToolService>(out var tools) &&
                   tools.ActiveTool == ViewportTool.Measure;
        }

        Vector3? PickPoint()
        {
            var pointer = Pointer.current ?? Mouse.current;
            if (pointer == null || _camera == null)
                return null;

            var screen = pointer.position.ReadValue();
            if (!ViewportInputUtility.GetViewportScreenRect().Contains(screen))
                return null;

            var ray = _camera.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return hit.point;

            var ground = new Plane(Vector3.up, Vector3.zero);
            return ground.Raycast(ray, out var distance) ? ray.GetPoint(distance) : null;
        }

        void EnsureVisuals()
        {
            if (_line == null)
            {
                var lineGo = new GameObject("MeasureLine");
                _line = lineGo.AddComponent<LineRenderer>();
                _line.positionCount = 2;
                _line.startWidth = 0.025f;
                _line.endWidth = 0.025f;
                _line.useWorldSpace = true;
                _line.sharedMaterial = CreateLineMaterial();
                _line.enabled = false;
            }

            if (_label == null)
            {
                var labelGo = new GameObject("MeasureLabel");
                _label = labelGo.AddComponent<TextMesh>();
                _label.anchor = TextAnchor.MiddleCenter;
                _label.alignment = TextAlignment.Center;
                _label.characterSize = 0.16f;
                _label.color = Color.white;
                _label.gameObject.SetActive(false);
            }
        }

        void SetLine(Vector3 start, Vector3 end)
        {
            EnsureVisuals();
            _line.enabled = true;
            _line.SetPosition(0, start);
            _line.SetPosition(1, end);
        }

        void SetLabel(Vector3 start, Vector3 end)
        {
            EnsureVisuals();
            var distance = Vector3.Distance(start, end);
            _label.text = $"{distance:0.###} m";
            _label.transform.position = (start + end) * 0.5f + Vector3.up * 0.1f;
            _label.transform.rotation = _camera.transform.rotation;
            _label.gameObject.SetActive(true);
        }

        static bool WasLeftClickThisFrame()
        {
            var mouse = Mouse.current;
            if (mouse != null)
                return mouse.leftButton.wasPressedThisFrame;

            var pointer = Pointer.current;
            return pointer != null && pointer.press.wasPressedThisFrame;
        }

        static Material CreateLineMaterial()
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            material.color = new Color(1f, 0.72f, 0.22f, 1f);
            return material;
        }
    }
}
