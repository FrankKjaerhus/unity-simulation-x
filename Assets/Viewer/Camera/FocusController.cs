using System.Collections.Generic;
using UnityEngine;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Viewer.Camera
{
    public sealed class FocusController
    {
        readonly Transform _pivot;
        readonly UnityEngine.Camera _camera;
        readonly OrbitController _orbit;
        readonly ISceneObjectMapper _mapper;

        public FocusController(Transform pivot, UnityEngine.Camera camera, OrbitController orbit, ISceneObjectMapper mapper)
        {
            _pivot = pivot;
            _camera = camera;
            _orbit = orbit;
            _mapper = mapper;
        }

        public void FocusObjects(IEnumerable<string> objectIds, float padding = 1.25f)
        {
            var bounds = CalculateBounds(objectIds);
            if (!bounds.HasValue)
                return;

            FocusBounds(bounds.Value, padding);
        }

        public void FrameAll(SceneRegistry registry, float padding = 2f)
        {
            var ids = new List<string>();
            foreach (var model in registry.GetAll())
                ids.Add(model.Id);

            FocusObjects(ids, padding);
        }

        public void FocusBounds(Bounds bounds, float padding)
        {
            if (bounds.size.sqrMagnitude < 0.0001f)
                bounds = new Bounds(bounds.center, Vector3.one * 0.5f);

            _pivot.position = bounds.center;

            var extent = bounds.extents.magnitude;
            var distance = CalculateFocusDistance(extent, padding);

            var offset = _camera.transform.position - bounds.center;
            if (offset.sqrMagnitude < 0.0001f)
                offset = Quaternion.Euler(20f, 45f, 0f) * Vector3.back * distance;
            else
                offset = offset.normalized * distance;

            CameraRotationUtility.DirectionToOrbitAngles(offset, out var pitch, out var yaw);
            CameraRotationUtility.SetOrbitTransform(_camera.transform, _pivot.position, pitch, yaw, distance);
            _orbit.SyncFromCamera();
        }

        float CalculateFocusDistance(float boundsRadius, float padding)
        {
            if (_camera.orthographic)
            {
                _camera.orthographicSize = Mathf.Max(boundsRadius * padding, 0.5f);
                return Vector3.Distance(_camera.transform.position, _pivot.position);
            }

            var verticalFov = _camera.fieldOfView * Mathf.Deg2Rad;
            var distance = boundsRadius / Mathf.Sin(verticalFov * 0.5f);
            return Mathf.Max(distance * padding, 0.75f);
        }

        Bounds? CalculateBounds(IEnumerable<string> objectIds)
        {
            Bounds? result = null;
            foreach (var id in objectIds)
            {
                var go = _mapper.GetGameObject(id);
                if (go == null)
                    continue;

                foreach (var renderer in go.GetComponentsInChildren<Renderer>())
                {
                    if (!result.HasValue)
                        result = renderer.bounds;
                    else
                    {
                        var b = result.Value;
                        b.Encapsulate(renderer.bounds);
                        result = b;
                    }
                }

                if (result.HasValue)
                    continue;

                foreach (var collider in go.GetComponentsInChildren<Collider>())
                {
                    if (!result.HasValue)
                        result = collider.bounds;
                    else
                    {
                        var b = result.Value;
                        b.Encapsulate(collider.bounds);
                        result = b;
                    }
                }
            }

            return result;
        }
    }
}
