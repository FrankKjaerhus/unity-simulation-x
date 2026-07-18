using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimulationX.Core;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer;
using UnitySimulationX.Viewer.Projection;
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
            public TransformData OriginalTransform;
            public TransformData PreviewTransform;
            public Transform Parent;
        }

        UnityEngine.Camera _camera;
        IEventBus _eventBus;
        ISceneEditService _edits;
        ISceneProjectionService _projection;
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
            ServiceLocator.TryResolve<IEventBus>(out _eventBus);
            ServiceLocator.TryResolve<ISceneEditService>(out _edits);
            ServiceLocator.TryResolve<ISceneProjectionService>(out _projection);
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
                _projection == null ||
                _edits == null)
                return;

            _grabEntries.Clear();

            foreach (var objectId in selection.SelectedObjectIds)
            {
                var model = _edits.Registry.Get(objectId);
                var go = _projection.GetGameObject(objectId);
                if (model == null || go == null)
                    continue;

                _grabEntries.Add(new GrabEntry
                {
                    ObjectId = objectId,
                    WorldStartPosition = go.transform.position,
                    OriginalTransform = model.Transform?.Clone() ?? new TransformData(),
                    PreviewTransform = model.Transform?.Clone() ?? new TransformData(),
                    Parent = go.transform.parent
                });
            }

            if (_grabEntries.Count == 0)
                return;

            var anchor = _projection.GetGameObject(_grabEntries[0].ObjectId).transform.position;
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
            if (_projection == null)
                return;

            var ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            if (!_grabPlane.Raycast(ray, out var enter))
                return;

            var worldPoint = ray.GetPoint(enter);
            var delta = ApplyAxisLock(worldPoint - _grabAnchorWorld, _axisLock);

            foreach (var entry in _grabEntries)
            {
                var go = _projection.GetGameObject(entry.ObjectId);
                if (go == null)
                    continue;

                var newWorld = entry.WorldStartPosition + delta;

                entry.PreviewTransform = entry.Parent != null
                    ? new TransformData
                    {
                        Position = entry.Parent.InverseTransformPoint(newWorld),
                        RotationEuler = entry.OriginalTransform.RotationEuler,
                        Scale = entry.OriginalTransform.Scale
                    }
                    : new TransformData
                    {
                        Position = newWorld,
                        RotationEuler = entry.OriginalTransform.RotationEuler,
                        Scale = entry.OriginalTransform.Scale
                    };

                _projection.PreviewTransform(entry.ObjectId, entry.PreviewTransform);
            }
        }

        void ConfirmGrab()
        {
            if (!_grabbing || _edits == null)
                return;

            foreach (var entry in _grabEntries)
                _edits.SetTransform(entry.ObjectId, entry.PreviewTransform);

            _suppressSelectionClick = true;
            EndGrab();
        }

        void CancelGrab()
        {
            if (!_grabbing)
                return;

            if (_projection != null)
            {
                foreach (var entry in _grabEntries)
                    _projection.PreviewTransform(entry.ObjectId, entry.OriginalTransform);
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

        void PublishGrabModeChanged(bool isGrabbing)
        {
            _eventBus?.Publish(new GrabModeChangedEvent { IsGrabbing = isGrabbing });
        }
    }
}
