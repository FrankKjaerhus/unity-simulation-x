using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer;
using UnitySimulationX.Viewer.Gizmos;
using UnitySimulationX.Viewer.Projection;
using UnitySimulationX.Viewer.Tools;

namespace UnitySimulationX.Viewer.Selection
{
    [DefaultExecutionOrder(200)]
    public sealed class ViewportSelectionInput : MonoBehaviour
    {
        static readonly RaycastHit[] Hits = new RaycastHit[32];

        UnityEngine.Camera _camera;

        void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
        }

        void Update()
        {
            if (!WasLeftClickThisFrame())
                return;

            if (ViewportInputUtility.IsTextFieldFocused())
                return;

            if (ViewportInputUtility.IsPointerOverBlockingUi())
                return;

            if (ServiceLocator.TryResolve<IViewportToolService>(out var tools) &&
                (tools.ActiveTool == ViewportTool.Measure || tools.ActiveTool == ViewportTool.Insert))
            {
                return;
            }

            if (ServiceLocator.TryResolve<IGrabService>(out var grab))
            {
                if (grab.IsGrabbing || grab.ConsumeSelectionClick())
                    return;
            }

            if (!ServiceLocator.TryResolve<ISelectionService>(out var selection) ||
                !ServiceLocator.TryResolve<ISceneProjectionService>(out _))
                return;

            var pointerPosition = GetPointerPosition();
            if (!ViewportInputUtility.GetViewportScreenRect().Contains(pointerPosition))
                return;

            var ray = _camera.ScreenPointToRay(pointerPosition);
            if (!TryPickSceneObject(ray, out var idComponent))
            {
                var keyboard = Keyboard.current;
                if (keyboard != null &&
                    !keyboard.leftShiftKey.isPressed &&
                    !keyboard.rightShiftKey.isPressed)
                {
                    selection.Clear();
                }

                return;
            }

            var additive = Keyboard.current != null &&
                           (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            selection.Select(idComponent.SceneObjectId, additive);
        }

        static bool WasLeftClickThisFrame()
        {
            var mouse = Mouse.current;
            if (mouse != null)
                return mouse.leftButton.wasPressedThisFrame;

            var pointer = Pointer.current;
            return pointer != null && pointer.press.wasPressedThisFrame;
        }

        static Vector2 GetPointerPosition()
        {
            var pointer = Pointer.current ?? Mouse.current;
            return pointer != null ? pointer.position.ReadValue() : Vector2.zero;
        }

        static bool TryPickSceneObject(Ray ray, out SceneObjectIdComponent idComponent)
        {
            idComponent = null;
            var hitCount = Physics.RaycastNonAlloc(
                ray,
                Hits,
                Mathf.Infinity,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            if (hitCount <= 0)
                return false;

            System.Array.Sort(Hits, 0, hitCount, RaycastHitComparer.Instance);

            for (var i = 0; i < hitCount; i++)
            {
                var hit = Hits[i];
                if (hit.collider == null)
                    continue;

                if (hit.collider.gameObject.name == "SelectionOutline")
                    continue;

                idComponent = hit.collider.GetComponentInParent<SceneObjectIdComponent>();
                if (idComponent != null && !string.IsNullOrEmpty(idComponent.SceneObjectId))
                    return true;
            }

            return false;
        }

        sealed class RaycastHitComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly RaycastHitComparer Instance = new();

            public int Compare(RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance);
        }
    }
}
