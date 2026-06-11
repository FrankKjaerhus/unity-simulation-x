using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer;
using UnitySimulationX.Viewer.Selection;

namespace UnitySimulationX.Viewer.Camera
{
    public sealed class ViewerCameraController : MonoBehaviour
    {
        [SerializeField] Transform pivot;
        [SerializeField] float orbitSensitivity = 0.25f;
        [SerializeField] float panSensitivity = 0.002f;
        [SerializeField] float zoomSensitivity = 2.5f;

        UnityEngine.Camera _camera;
        OrbitController _orbit;
        PanController _pan;
        ZoomController _zoom;
        FlyController _fly;
        ViewPresetController _viewPresets;
        FocusController _focus;

        bool _shiftHeld;

        void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            if (pivot == null)
            {
                var pivotGo = new GameObject("CameraPivot");
                pivot = pivotGo.transform;
                pivot.position = Vector3.zero;
            }

            _orbit = new OrbitController(pivot, _camera);
            _pan = new PanController(pivot, _orbit);
            _zoom = new ZoomController(_orbit);
            _fly = new FlyController(_camera, _orbit);
            _viewPresets = new ViewPresetController(pivot, _orbit, _camera);
        }

        void Start()
        {
            if (ServiceLocator.TryResolve<ISceneObjectMapper>(out var mapper))
                _focus = new FocusController(pivot, _camera, _orbit, mapper);

            if (_camera.transform.position == Vector3.zero)
            {
                CameraRotationUtility.SetOrbitTransform(
                    _camera.transform,
                    pivot.position,
                    20f,
                    45f,
                    10f);
            }

            _orbit.SyncFromCamera();
        }

        void LateUpdate()
        {
            if (_camera == null)
                return;

            _camera.transform.rotation = CameraRotationUtility.Normalize(_camera.transform.rotation);
        }

        void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || keyboard == null)
                return;

            _shiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

            HandleFlyToggle(keyboard);
            HandleViewPresets(keyboard);
            HandleFocus(keyboard);

            if (_fly.IsActive)
            {
                HandleFlyMode(mouse, keyboard);
                return;
            }

            HandleOrbitPanZoom(mouse);
        }

        void HandleFlyToggle(Keyboard keyboard)
        {
            if (_shiftHeld && keyboard.fKey.wasPressedThisFrame)
                _fly.SetActive(true);

            if (keyboard.escapeKey.wasPressedThisFrame)
                _fly.SetActive(false);
        }

        void HandleViewPresets(Keyboard keyboard)
        {
            if (keyboard.numpad7Key.wasPressedThisFrame || keyboard[Key.Digit7].wasPressedThisFrame)
                _viewPresets.SetTopView();

            if (keyboard.numpad1Key.wasPressedThisFrame || keyboard[Key.Digit1].wasPressedThisFrame)
                _viewPresets.SetFrontView();

            if (keyboard.numpad3Key.wasPressedThisFrame || keyboard[Key.Digit3].wasPressedThisFrame)
                _viewPresets.SetSideView();

            if (keyboard.numpad5Key.wasPressedThisFrame || keyboard[Key.Digit5].wasPressedThisFrame)
                _viewPresets.TogglePerspectiveOrthographic();
        }

        void HandleFocus(Keyboard keyboard)
        {
            if (_focus == null)
                return;

            if (keyboard.fKey.wasPressedThisFrame && !_shiftHeld && !ViewportInputUtility.IsTextFieldFocused())
            {
                if (ServiceLocator.TryResolve<ISelectionService>(out var selection) &&
                    selection.SelectedObjectIds.Count > 0)
                {
                    _focus.FocusObjects(selection.SelectedObjectIds);
                }
            }

            if (keyboard.homeKey.wasPressedThisFrame && !ViewportInputUtility.IsTextFieldFocused())
            {
                if (ServiceLocator.TryResolve<SceneRegistry>(out var registry))
                    _focus.FrameAll(registry);
            }
        }

        void HandleOrbitPanZoom(Mouse mouse)
        {
            var delta = mouse.delta.ReadValue();

            if (mouse.middleButton.isPressed)
            {
                if (_shiftHeld)
                    _pan.Pan(delta, panSensitivity);
                else
                    _orbit.Orbit(delta, orbitSensitivity);
            }

            var scroll = mouse.scroll.ReadValue().y;
            if (!Mathf.Approximately(scroll, 0f))
                _zoom.Zoom(scroll / 120f, zoomSensitivity);
        }

        void HandleFlyMode(Mouse mouse, Keyboard keyboard)
        {
            var move = Vector2.zero;
            if (keyboard.wKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed) move.y -= 1f;
            if (keyboard.aKey.isPressed) move.x -= 1f;
            if (keyboard.dKey.isPressed) move.x += 1f;

            _fly.Tick(Time.deltaTime, move, mouse.delta.ReadValue(), keyboard.leftShiftKey.isPressed);

            if (keyboard.escapeKey.wasPressedThisFrame)
                _fly.SetActive(false);
        }

        public Transform Pivot => pivot;
        public OrbitController Orbit => _orbit;
        public PanController Pan => _pan;
        public ZoomController Zoom => _zoom;
        public FlyController Fly => _fly;
        public ViewPresetController ViewPresets => _viewPresets;
        public FocusController Focus => _focus;
    }
}
