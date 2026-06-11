using UnityEngine;

namespace UnitySimulationX.Viewer.Camera
{
    public sealed class FlyController
    {
        readonly UnityEngine.Camera _camera;
        readonly OrbitController _orbit;
        float _moveSpeed = 5f;
        float _yaw;
        float _pitch;

        public bool IsActive { get; private set; }

        public FlyController(UnityEngine.Camera camera, OrbitController orbit)
        {
            _camera = camera;
            _orbit = orbit;
        }

        public void SetActive(bool active)
        {
            if (active && !IsActive)
                SyncFromCamera();

            if (!active && IsActive)
                _orbit.SyncFromCamera();

            IsActive = active;
        }

        public void SyncFromCamera()
        {
            CameraRotationUtility.ForwardToOrbitAngles(_camera.transform.forward, out _pitch, out _yaw);
        }

        public void Tick(float deltaTime, Vector2 moveInput, Vector2 lookDelta, bool boost)
        {
            if (!IsActive)
                return;

            var speed = _moveSpeed * (boost ? 3f : 1f);
            var rotation = CameraRotationUtility.OrbitRotation(_pitch, _yaw);
            var forward = rotation * Vector3.forward;
            var right = rotation * Vector3.right;
            var motion = (forward * moveInput.y + right * moveInput.x) * speed * deltaTime;
            _camera.transform.position += motion;

            if (lookDelta.sqrMagnitude > 0.0001f)
            {
                _yaw += lookDelta.x * 0.25f;
                _pitch -= lookDelta.y * 0.25f;
                _pitch = Mathf.Clamp(_pitch, -89f, 89f);
                _camera.transform.rotation = CameraRotationUtility.OrbitRotation(_pitch, _yaw);
            }
        }
    }
}
