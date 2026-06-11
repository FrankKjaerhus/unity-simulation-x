using UnityEngine;

namespace UnitySimulationX.Viewer.Camera
{
    public sealed class ZoomController
    {
        const float ZoomStepPerNotch = 1.18f;

        readonly OrbitController _orbit;

        public ZoomController(OrbitController orbit)
        {
            _orbit = orbit;
        }

        public void Zoom(float scrollNotches, float sensitivity = 1f)
        {
            if (Mathf.Approximately(scrollNotches, 0f))
                return;

            var scale = Mathf.Pow(ZoomStepPerNotch, -scrollNotches * sensitivity);
            var newDistance = Mathf.Clamp(_orbit.Distance * scale, 0.5f, 500f);
            _orbit.SetDistance(newDistance);
        }
    }
}
