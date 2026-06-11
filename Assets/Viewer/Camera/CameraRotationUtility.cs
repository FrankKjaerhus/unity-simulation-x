using UnityEngine;

namespace UnitySimulationX.Viewer.Camera
{
    public static class CameraRotationUtility
    {
        public static Quaternion OrbitRotation(float pitch, float yaw)
        {
            return Normalize(Quaternion.Euler(pitch, yaw, 0f));
        }

        public static Vector3 OrbitPosition(Vector3 pivot, float pitch, float yaw, float distance)
        {
            var rotation = OrbitRotation(pitch, yaw);
            return pivot + rotation * new Vector3(0f, 0f, -distance);
        }

        public static void SetOrbitTransform(Transform target, Vector3 pivot, float pitch, float yaw, float distance)
        {
            var rotation = OrbitRotation(pitch, yaw);
            target.SetPositionAndRotation(OrbitPosition(pivot, pitch, yaw, distance), rotation);
        }

        public static Quaternion SafeLookAt(Vector3 from, Vector3 to)
        {
            var forward = to - from;
            if (forward.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            return SafeLookRotation(forward.normalized, Vector3.up);
        }

        public static Quaternion SafeLookRotation(Vector3 forward, Vector3 preferredUp)
        {
            if (forward.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            forward.Normalize();

            var up = preferredUp;
            if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.999f)
                up = Mathf.Abs(Vector3.Dot(forward, Vector3.forward)) > 0.999f ? Vector3.right : Vector3.forward;

            return Normalize(Quaternion.LookRotation(forward, up));
        }

        public static Quaternion Normalize(Quaternion rotation)
        {
            var magnitude = Mathf.Sqrt(rotation.x * rotation.x + rotation.y * rotation.y +
                                         rotation.z * rotation.z + rotation.w * rotation.w);
            if (magnitude < 0.0001f)
                return Quaternion.identity;

            return new Quaternion(
                rotation.x / magnitude,
                rotation.y / magnitude,
                rotation.z / magnitude,
                rotation.w / magnitude);
        }

        public static void DirectionToOrbitAngles(Vector3 pivotToCamera, out float pitch, out float yaw)
        {
            if (pivotToCamera.sqrMagnitude < 0.0001f)
            {
                pitch = 20f;
                yaw = 0f;
                return;
            }

            var dir = pivotToCamera.normalized;
            yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
        }

        public static void ForwardToOrbitAngles(Vector3 forward, out float pitch, out float yaw)
        {
            if (forward.sqrMagnitude < 0.0001f)
            {
                pitch = 0f;
                yaw = 0f;
                return;
            }

            forward.Normalize();
            yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            pitch = -Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
        }

        public static Vector3 OrbitRight(float pitch, float yaw)
        {
            return OrbitRotation(pitch, yaw) * Vector3.right;
        }

        public static Vector3 OrbitUp(float pitch, float yaw)
        {
            return OrbitRotation(pitch, yaw) * Vector3.up;
        }
    }
}
