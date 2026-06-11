using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer;
using UnitySimulationX.Viewer.Selection;

namespace UnitySimulationX.Viewer.Gizmos
{
    /// <summary>
    /// Blender-style grab (G): move selection on a view plane, confirm with LMB, cancel with Esc.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class TransformGizmoController : MonoBehaviour, IGrabService
    {
        sealed class GrabEntry
        {
            public string ObjectId;
            public Vector3 WorldStartPosition;
            public Transform Parent;
        }

        UnityEngine.Camera _camera;
        bool _grabbing;
        bool _suppressSelectionClick;
        Plane _grabPlane;
        Vector3 _grabAnchorWorld;
        readonly List<GrabEntry> _grabEntries = new();
        int _axisLock = -1;

        public bool IsGrabbing => _grabbing;

        public bool ConsumeSelectionClick()
        {
            if (!_suppressSelectionClick)
                return false;

            _suppressSelectionClick = false;
            return true;
        }

        void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            ServiceLocator.Register<IGrabService>(this);
        }

        void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || keyboard == null)
                return;

            if (ViewportInputUtility.IsPointerOverBlockingUi())
                return;

            _axisLock = GetAxisLock(keyboard);

            if (!_grabbing &&
                keyboard.gKey.wasPressedThisFrame &&
                !ViewportInputUtility.IsTextFieldFocused())
            {
                TryBeginGrab(mouse);
            }

            if (!_grabbing)
                return;

            ContinueGrab(mouse);

            if (mouse.leftButton.wasPressedThisFrame)
                ConfirmGrab();

            if (keyboard.escapeKey.wasPressedThisFrame)
                CancelGrab();
        }

        int GetAxisLock(Keyboard keyboard)
        {
            if (keyboard.xKey.isPressed) return 0;
            if (keyboard.yKey.isPressed) return 1;
            if (keyboard.zKey.isPressed) return 2;
            return -1;
        }

        void TryBeginGrab(Mouse mouse)
        {
            if (!ServiceLocator.TryResolve<ISelectionService>(out var selection) ||
                selection.SelectedObjectIds.Count == 0 ||
                !ServiceLocator.TryResolve<ISceneObjectMapper>(out var mapper))
                return;

            _grabEntries.Clear();

            foreach (var objectId in selection.SelectedObjectIds)
            {
                var go = mapper.GetGameObject(objectId);
                if (go == null)
                    continue;

                _grabEntries.Add(new GrabEntry
                {
                    ObjectId = objectId,
                    WorldStartPosition = go.transform.position,
                    Parent = go.transform.parent
                });
            }

            if (_grabEntries.Count == 0)
                return;

            var anchor = _grabEntries[0].WorldStartPosition;
            _grabPlane = new Plane(_camera.transform.forward, anchor);

            var ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            _grabAnchorWorld = _grabPlane.Raycast(ray, out var enter)
                ? ray.GetPoint(enter)
                : anchor;

            _grabbing = true;
            PublishGrabModeChanged(true);
        }

        void ContinueGrab(Mouse mouse)
        {
            if (!ServiceLocator.TryResolve<SceneRegistry>(out var registry) ||
                !ServiceLocator.TryResolve<ISceneObjectMapper>(out var mapper))
                return;

            var ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            if (!_grabPlane.Raycast(ray, out var enter))
                return;

            var worldPoint = ray.GetPoint(enter);
            var delta = ApplyAxisLock(worldPoint - _grabAnchorWorld, _axisLock);

            foreach (var entry in _grabEntries)
            {
                var model = registry.Get(entry.ObjectId);
                var go = mapper.GetGameObject(entry.ObjectId);
                if (model == null || go == null)
                    continue;

                var newWorld = entry.WorldStartPosition + delta;
                model.Transform.Position = entry.Parent != null
                    ? entry.Parent.InverseTransformPoint(newWorld)
                    : newWorld;

                mapper.UpdateGameObject(model, go);
            }
        }

        void ConfirmGrab()
        {
            if (!_grabbing)
                return;

            if (ServiceLocator.TryResolve<SceneRegistry>(out var registry))
            {
                foreach (var entry in _grabEntries)
                {
                    var model = registry.Get(entry.ObjectId);
                    if (model == null)
                        continue;

                    EventBus.Publish(new SceneObjectChangedEvent
                    {
                        ObjectId = model.Id,
                        Model = model
                    });
                }
            }

            _suppressSelectionClick = true;
            EndGrab();
        }

        void CancelGrab()
        {
            if (!_grabbing)
                return;

            if (!ServiceLocator.TryResolve<SceneRegistry>(out var registry) ||
                !ServiceLocator.TryResolve<ISceneObjectMapper>(out var mapper))
            {
                EndGrab();
                return;
            }

            foreach (var entry in _grabEntries)
            {
                var model = registry.Get(entry.ObjectId);
                var go = mapper.GetGameObject(entry.ObjectId);
                if (model == null || go == null)
                    continue;

                model.Transform.Position = entry.Parent != null
                    ? entry.Parent.InverseTransformPoint(entry.WorldStartPosition)
                    : entry.WorldStartPosition;

                mapper.UpdateGameObject(model, go);
            }

            EndGrab();
        }

        void EndGrab()
        {
            _grabbing = false;
            _grabEntries.Clear();
            PublishGrabModeChanged(false);
        }

        static Vector3 ApplyAxisLock(Vector3 delta, int axisLock)
        {
            return axisLock switch
            {
                0 => new Vector3(delta.x, 0f, 0f),
                1 => new Vector3(0f, delta.y, 0f),
                2 => new Vector3(0f, 0f, delta.z),
                _ => delta
            };
        }

        static void PublishGrabModeChanged(bool isGrabbing)
        {
            EventBus.Publish(new GrabModeChangedEvent { IsGrabbing = isGrabbing });
        }
    }
}
