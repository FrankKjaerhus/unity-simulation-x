using UnityEngine;

namespace UnitySimulationX.Viewer.Camera
{
    public sealed class OrbitController
    {
        readonly Transform _pivot;
        readonly UnityEngine.Camera _camera;
        float _yaw;
        float _pitch = 20f;
        float _distance = 10f;

        public float Pitch => _pitch;
        public float Yaw => _yaw;
        public float Distance => _distance;

        public OrbitController(Transform pivot, UnityEngine.Camera camera)
        {
            _pivot = pivot;
            _camera = camera;
            SyncFromCamera();
        }

        public void SyncFromCamera()
        {
            var offset = _camera.transform.position - _pivot.position;
            _distance = Mathf.Max(offset.magnitude, 0.01f);

            if (offset.sqrMagnitude < 0.0001f)
                return;

            CameraRotationUtility.DirectionToOrbitAngles(offset, out _pitch, out _yaw);
        }

        public void Orbit(Vector2 delta, float sensitivity = 0.25f)
        {
            _yaw += delta.x * sensitivity;
            _pitch -= delta.y * sensitivity;
            _pitch = Mathf.Clamp(_pitch, -89f, 89f);
            Apply();
        }

        public void Apply()
        {
            CameraRotationUtility.SetOrbitTransform(
                _camera.transform,
                _pivot.position,
                _pitch,
                _yaw,
                _distance);
        }

        public void SetDistance(float distance)
        {
            _distance = Mathf.Clamp(distance, 0.5f, 500f);
            Apply();
        }
    }
}
