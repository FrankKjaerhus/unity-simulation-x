using NUnit.Framework;
using UnityEngine;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class SceneObjectMapperTests
    {
        Transform _sceneRoot;
        SceneRegistry _registry;
        SceneObjectMapper _mapper;

        [SetUp]
        public void SetUp()
        {
            var rootGo = new GameObject("TestSceneRoot");
            _sceneRoot = rootGo.transform;
            _registry = new SceneRegistry();
            _mapper = new SceneObjectMapper(_sceneRoot);
            ServiceLocatorBridge.SetRegistryResolver(() => _registry);
        }

        [TearDown]
        public void TearDown()
        {
            if (_sceneRoot != null)
                Object.DestroyImmediate(_sceneRoot.gameObject);
            ServiceLocatorBridge.SetRegistryResolver(null);
        }

        [Test]
        public void CreateGameObject_SetsIdComponent()
        {
            var model = new SceneObjectModel
            {
                Id = "obj1",
                Name = "Cube",
                Type = SceneObjectType.Primitive,
                PrimitiveMeshTypeKey = "Cube"
            };
            _registry.Add(model);

            var go = _mapper.CreateGameObject(model);
            var idComponent = go.GetComponent<SceneObjectIdComponent>();

            Assert.IsNotNull(go);
            Assert.IsNotNull(idComponent);
            Assert.AreEqual("obj1", idComponent.SceneObjectId);
        }

        [Test]
        public void GetModel_RoundTripsFromGameObject()
        {
            var model = new SceneObjectModel
            {
                Id = "obj2",
                Name = "Sphere",
                Type = SceneObjectType.Primitive,
                PrimitiveMeshTypeKey = "Sphere"
            };
            _registry.Add(model);
            var go = _mapper.CreateGameObject(model);

            var resolved = _mapper.GetModel(go);

            Assert.IsNotNull(resolved);
            Assert.AreEqual("obj2", resolved.Id);
        }

        [Test]
        public void RegisterExistingGameObject_SetsIdAndMapsExistingObject()
        {
            var model = new SceneObjectModel
            {
                Id = "existing",
                Name = "Existing",
                Type = SceneObjectType.ImportedAsset
            };
            var existing = new GameObject("Existing");
            _registry.Add(model);

            var registered = _mapper.RegisterExistingGameObject(model, existing);
            var idComponent = existing.GetComponent<SceneObjectIdComponent>();

            Assert.AreSame(existing, registered);
            Assert.IsNotNull(idComponent);
            Assert.AreEqual("existing", idComponent.SceneObjectId);
            Assert.AreSame(existing, _mapper.GetGameObject("existing"));
            Assert.AreEqual(model, _mapper.GetModel(existing));
        }

        [Test]
        public void UpdateGameObject_AppliesTransform()
        {
            var model = new SceneObjectModel
            {
                Id = "obj3",
                Name = "Moved",
                Type = SceneObjectType.Primitive,
                PrimitiveMeshTypeKey = "Cube",
                Transform = new TransformData { Position = Vector3.zero }
            };
            _registry.Add(model);
            var go = _mapper.CreateGameObject(model);

            model.Transform.Position = new Vector3(2f, 1f, 0.5f);
            _mapper.UpdateGameObject(model, go);

            Assert.AreEqual(new Vector3(2f, 1f, 0.5f), go.transform.localPosition);
        }

        [Test]
        public void UpdateGameObject_DoesNotReplaceExistingMaterial()
        {
            var model = new SceneObjectModel
            {
                Id = "obj5",
                Name = "Colored",
                Type = SceneObjectType.Primitive,
                PrimitiveMeshTypeKey = "Cube",
                Transform = new TransformData { Position = Vector3.zero }
            };
            _registry.Add(model);
            var go = _mapper.CreateGameObject(model);

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var customMaterial = new Material(shader);
            customMaterial.color = Color.red;
            go.GetComponent<Renderer>().sharedMaterial = customMaterial;

            model.Transform.Position = new Vector3(1f, 0f, 0f);
            _mapper.UpdateGameObject(model, go);

            Assert.AreEqual(customMaterial, go.GetComponent<Renderer>().sharedMaterial);
        }

        [Test]
        public void DestroyGameObject_RemovesInstance()
        {
            var model = new SceneObjectModel
            {
                Id = "obj4",
                Name = "Temp",
                Type = SceneObjectType.Primitive,
                PrimitiveMeshTypeKey = "Cube"
            };
            _registry.Add(model);
            _mapper.CreateGameObject(model);

            _mapper.DestroyGameObject("obj4");

            Assert.IsNull(_mapper.GetGameObject("obj4"));
        }
    }
}
