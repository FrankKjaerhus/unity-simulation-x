using UnityEngine;
using UnitySimulationX.Viewer.Navigation;

namespace UnitySimulationX.Viewer.Camera
{
    public sealed class ViewPresetController
    {
        readonly Transform _pivot;
        readonly OrbitController _orbit;
        readonly UnityEngine.Camera _camera;
        ViewMode _viewMode = ViewMode.Perspective3D;

        public ViewMode CurrentViewMode => _viewMode;

        public ViewPresetController(Transform pivot, OrbitController orbit, UnityEngine.Camera camera)
        {
            _pivot = pivot;
            _orbit = orbit;
            _camera = camera;
        }

        public void SetTopView(float distance = 10f)
        {
            _viewMode = ViewMode.OrthographicTop;
            PositionCamera(Vector3.up, distance, true);
        }

        public void SetFrontView(float distance = 10f)
        {
            _viewMode = ViewMode.OrthographicFront;
            PositionCamera(Vector3.forward, distance, true);
        }

        public void SetSideView(float distance = 10f)
        {
            _viewMode = ViewMode.OrthographicSide;
            PositionCamera(Vector3.right, distance, true);
        }

        public void TogglePerspectiveOrthographic()
        {
            if (_camera.orthographic)
            {
                _camera.orthographic = false;
                _viewMode = ViewMode.Perspective3D;
            }
            else
            {
                _camera.orthographic = true;
                if (_viewMode == ViewMode.Perspective3D)
                    _viewMode = ViewMode.OrthographicTop;
            }
        }

        public void SetPerspective()
        {
            _camera.orthographic = false;
            _viewMode = ViewMode.Perspective3D;
        }

        void PositionCamera(Vector3 direction, float distance, bool orthographic)
        {
            _camera.orthographic = orthographic;
            CameraRotationUtility.DirectionToOrbitAngles(direction.normalized * distance, out var pitch, out var yaw);
            CameraRotationUtility.SetOrbitTransform(_camera.transform, _pivot.position, pitch, yaw, distance);
            _orbit.SyncFromCamera();
        }
    }
}
