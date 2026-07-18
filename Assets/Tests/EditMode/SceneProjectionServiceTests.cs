using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer.Projection;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class SceneProjectionServiceTests
    {
        Transform _sceneRoot;
        SceneRegistry _registry;
        SceneProjectionService _projection;

        [SetUp]
        public void SetUp()
        {
            var rootGo = new GameObject("TestSceneRoot");
            _sceneRoot = rootGo.transform;
            _registry = new SceneRegistry();
            _projection = new SceneProjectionService(_sceneRoot, _registry);
        }

        [TearDown]
        public void TearDown()
        {
            if (_sceneRoot != null)
                Object.DestroyImmediate(_sceneRoot.gameObject);
        }

        [Test]
        public void CreateProjection_SetsIdComponent()
        {
            var model = new SceneObjectModel
            {
                Id = "obj1",
                Name = "Cube",
                Type = SceneObjectType.Primitive,
                TypeId = SceneObjectTypeIds.Primitive,
                PrimitiveMeshTypeKey = "Cube"
            };
            _registry.Add(model);

            _projection.CreateProjection(model);
            var go = _projection.GetGameObject(model.Id);
            var idComponent = go.GetComponent<SceneObjectIdComponent>();

            Assert.IsNotNull(go);
            Assert.IsNotNull(idComponent);
            Assert.AreEqual("obj1", idComponent.SceneObjectId);
        }

        [Test]
        public void GetObjectId_RoundTripsFromGameObject()
        {
            var model = new SceneObjectModel
            {
                Id = "obj2",
                Name = "Sphere",
                Type = SceneObjectType.Primitive,
                TypeId = SceneObjectTypeIds.Primitive,
                PrimitiveMeshTypeKey = "Sphere"
            };
            _registry.Add(model);
            _projection.CreateProjection(model);
            var go = _projection.GetGameObject(model.Id);

            var objectId = _projection.GetObjectId(go);

            Assert.AreEqual("obj2", objectId);
            Assert.AreEqual("obj2", _registry.Get(objectId).Id);
        }

        [Test]
        public void RegisterExistingTarget_SetsIdAndMapsExistingObject()
        {
            var model = new SceneObjectModel
            {
                Id = "existing",
                Name = "Existing",
                Type = SceneObjectType.ImportedAsset,
                TypeId = SceneObjectTypeIds.ImportedModel
            };
            var existing = new GameObject("Existing");
            _registry.Add(model);

            _projection.RegisterExistingTarget(model.Id, existing);
            var idComponent = existing.GetComponent<SceneObjectIdComponent>();

            Assert.AreSame(existing, _projection.GetGameObject("existing"));
            Assert.IsNotNull(idComponent);
            Assert.AreEqual("existing", idComponent.SceneObjectId);
            Assert.AreEqual("existing", _projection.GetObjectId(existing));
            Assert.AreEqual("existing", _registry.Get(_projection.GetObjectId(existing)).Id);
        }

        [Test]
        public void UpdateProjection_AppliesTransform()
        {
            var model = new SceneObjectModel
            {
                Id = "obj3",
                Name = "Moved",
                Type = SceneObjectType.Primitive,
                TypeId = SceneObjectTypeIds.Primitive,
                PrimitiveMeshTypeKey = "Cube",
                Transform = new TransformData { Position = Vector3.zero }
            };
            _registry.Add(model);
            _projection.CreateProjection(model);
            var go = _projection.GetGameObject(model.Id);

            model.Transform.Position = new Vector3(2f, 1f, 0.5f);
            _projection.UpdateProjection(model);

            Assert.AreEqual(new Vector3(2f, 1f, 0.5f), go.transform.localPosition);
        }

        [Test]
        public void UpdateProjection_DoesNotReplaceExistingMaterial()
        {
            var model = new SceneObjectModel
            {
                Id = "obj5",
                Name = "Colored",
                Type = SceneObjectType.Primitive,
                TypeId = SceneObjectTypeIds.Primitive,
                PrimitiveMeshTypeKey = "Cube",
                Transform = new TransformData { Position = Vector3.zero }
            };
            _registry.Add(model);
            _projection.CreateProjection(model);
            var go = _projection.GetGameObject(model.Id);

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var customMaterial = new Material(shader);
            customMaterial.color = Color.red;
            go.GetComponent<Renderer>().sharedMaterial = customMaterial;

            model.Transform.Position = new Vector3(1f, 0f, 0f);
            _projection.UpdateProjection(model);

            Assert.AreEqual(customMaterial, go.GetComponent<Renderer>().sharedMaterial);
        }

        [Test]
        public void RemoveProjection_RemovesInstance()
        {
            var model = new SceneObjectModel
            {
                Id = "obj4",
                Name = "Temp",
                Type = SceneObjectType.Primitive,
                TypeId = SceneObjectTypeIds.Primitive,
                PrimitiveMeshTypeKey = "Cube"
            };
            _registry.Add(model);
            _projection.CreateProjection(model);

            _projection.RemoveProjection("obj4");

            Assert.IsNull(_projection.GetGameObject("obj4"));
        }

        [Test]
        public void SceneModelAssembly_DoesNotContainProjectionTypes()
        {
            var assembly = typeof(SceneObjectModel).Assembly;
            Assert.IsNull(assembly.GetType("UnitySimulationX.SceneModel.SceneObjectMapper"));
            Assert.IsNull(assembly.GetType("UnitySimulationX.SceneModel.SceneObjectIdComponent"));
        }
    }
}
