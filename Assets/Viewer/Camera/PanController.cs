using UnityEngine;

namespace UnitySimulationX.Viewer.Camera
{
    public sealed class PanController
    {
        readonly Transform _pivot;
        readonly OrbitController _orbit;

        public PanController(Transform pivot, OrbitController orbit)
        {
            _pivot = pivot;
            _orbit = orbit;
        }

        public void Pan(Vector2 delta, float sensitivity = 0.002f)
        {
            var scale = _orbit.Distance * sensitivity;
            var right = CameraRotationUtility.OrbitRight(_orbit.Pitch, _orbit.Yaw);
            var up = CameraRotationUtility.OrbitUp(_orbit.Pitch, _orbit.Yaw);
            var move = (-right * delta.x + -up * delta.y) * scale;

            _pivot.position += move;
            _orbit.Apply();
        }
    }
}
