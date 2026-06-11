using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer.Selection;

namespace UnitySimulationX.Tests.PlayMode
{
    public sealed class SelectionTests
    {
        SceneRegistry _registry;
        SelectionService _selection;
        SceneObjectMapper _mapper;
        GameObject _root;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ServiceLocator.Clear();
            EventBus.Clear();

            _root = new GameObject("SceneRoot");
            _registry = new SceneRegistry();
            _mapper = new SceneObjectMapper(_root.transform);
            _selection = new SelectionService(_registry);

            ServiceLocator.Register(_registry);
            ServiceLocator.Register<ISceneObjectMapper>(_mapper);
            ServiceLocator.Register<ISelectionService>(_selection);
            ServiceLocatorBridge.SetRegistryResolver(() => _registry);

            var model = new SceneObjectModel
            {
                Id = "sel1",
                Name = "Selectable",
                Type = SceneObjectType.Primitive,
                PrimitiveMeshTypeKey = "Cube"
            };
            _registry.Add(model);
            _mapper.CreateGameObject(model);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ServiceLocator.Clear();
            EventBus.Clear();
            ServiceLocatorBridge.SetRegistryResolver(null);

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
                Type = SceneObjectType.Primitive,
                PrimitiveMeshTypeKey = "Sphere"
            };
            _registry.Add(model2);
            _mapper.CreateGameObject(model2);

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
    }
}
