using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnitySimulationX.Viewer.Camera;

namespace UnitySimulationX.Tests.PlayMode
{
    public sealed class NavigationTests
    {
        GameObject _cameraGo;
        Transform _pivot;
        OrbitController _orbit;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _pivot = new GameObject("Pivot").transform;
            _cameraGo = new GameObject("TestCamera");
            var camera = _cameraGo.AddComponent<Camera>();
            _orbit = new OrbitController(_pivot, camera);
            CameraRotationUtility.SetOrbitTransform(_cameraGo.transform, _pivot.position, 0f, 0f, 10f);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_cameraGo != null)
                Object.Destroy(_cameraGo);
            if (_pivot != null)
                Object.Destroy(_pivot.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OrbitController_ChangesCameraOrientation()
        {
            var startRotation = _cameraGo.transform.rotation;
            _orbit.Orbit(new Vector2(30f, 10f));
            yield return null;
            Assert.AreNotEqual(startRotation, _cameraGo.transform.rotation);
        }

        [UnityTest]
        public IEnumerator ZoomController_MovesCameraCloser()
        {
            var zoom = new ZoomController(_orbit);
            var startDistance = _orbit.Distance;
            zoom.Zoom(2f);
            yield return null;
            Assert.Less(_orbit.Distance, startDistance);
        }

        [UnityTest]
        public IEnumerator ViewPresetController_SetsTopView()
        {
            var presets = new ViewPresetController(_pivot, _orbit, _cameraGo.GetComponent<Camera>());
            presets.SetTopView(8f);
            yield return null;
            Assert.IsTrue(_cameraGo.GetComponent<Camera>().orthographic);
            Assert.Greater(_cameraGo.transform.position.y, _pivot.position.y);
        }
    }
}
