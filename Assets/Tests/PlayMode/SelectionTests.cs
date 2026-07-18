using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnitySimulationX.Core;
using UnitySimulationX.Editing;
using UnitySimulationX.Import;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer.Projection;
using UnitySimulationX.Viewer.Selection;

namespace UnitySimulationX.Tests.PlayMode
{
    public sealed class SelectionTests
    {
        static readonly PrimitiveMeshComponentCodec PrimitiveMeshCodec = new();

        SceneRegistry _registry;
        SelectionService _selection;
        SceneProjectionService _projection;
        EventBus _eventBus;
        GameObject _root;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ServiceLocator.Clear();

            _eventBus = new EventBus(_ => { });
            _root = new GameObject("SceneRoot");
            _registry = new SceneRegistry();
            var codecs = new SceneComponentCodecRegistry();
            codecs.Register(PrimitiveMeshCodec);
            codecs.Freeze();
            _projection = new SceneProjectionService(_root.transform, _registry, componentCodecs: codecs);
            _selection = new SelectionService(_registry, _eventBus);

            ServiceLocator.Register<ISceneRegistryRead>(_registry);
            ServiceLocator.Register<ISceneProjectionService>(_projection);
            ServiceLocator.Register<ISelectionService>(_selection);
            ServiceLocator.Register<IEventBus>(_eventBus);
            ServiceLocator.Register<ISceneEditService>(new SceneEditService(_registry, _projection, _eventBus));

            var model = new SceneObjectModel
            {
                Id = "sel1",
                Name = "Selectable",
                TypeId = SceneObjectTypeIds.Primitive
            };
            model.Components.Add(CreatePrimitiveMeshComponent("Cube"));
            _registry.Add(model);
            _projection.CreateProjection(model);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ServiceLocator.Clear();

            if (_root != null)
                Object.Destroy(_root);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Select_SingleObject_UpdatesSelectionList()
        {
            _selection.Select("sel1");

            yield return null;

            Assert.That(_selection.SelectedObjectIds, Does.Contain("sel1"));
            Assert.IsTrue(_selection.IsSelected("sel1"));
        }

        [UnityTest]
        public IEnumerator Select_Additive_KeepsPreviousSelection()
        {
            var model2 = new SceneObjectModel
            {
                Id = "sel2",
                Name = "Second",
                TypeId = SceneObjectTypeIds.Primitive
            };
            model2.Components.Add(CreatePrimitiveMeshComponent("Sphere"));
            _registry.Add(model2);
            _projection.CreateProjection(model2);

            _selection.Select("sel1");
            _selection.Select("sel2", additive: true);

            yield return null;

            Assert.AreEqual(2, _selection.SelectedObjectIds.Count);
            Assert.IsTrue(_selection.IsSelected("sel1"));
            Assert.IsTrue(_selection.IsSelected("sel2"));
        }

        [UnityTest]
        public IEnumerator Clear_RemovesAllSelections()
        {
            _selection.Select("sel1");
            _selection.Clear();

            yield return null;

            Assert.AreEqual(0, _selection.SelectedObjectIds.Count);
        }

        static SceneComponentData CreatePrimitiveMeshComponent(string meshTypeKey)
        {
            return PrimitiveMeshCodec.Encode(new PrimitiveMeshComponent
            {
                MeshTypeKey = meshTypeKey
            });
        }
    }
}
