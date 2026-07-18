using NUnit.Framework;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class SceneRegistryInvariantTests
    {
        SceneRegistry _registry;

        [SetUp]
        public void SetUp() => _registry = new SceneRegistry();

        [Test]
        public void Add_WithMissingParent_ThrowsAndDoesNotMutate()
        {
            var child = Model("child", "missing");
            Assert.Throws<SceneInvariantException>(() => _registry.Add(child));
            Assert.IsFalse(_registry.Contains("child"));
        }

        [Test]
        public void Reparent_ToDescendant_ThrowsAndPreservesHierarchy()
        {
            _registry.Add(Model("root"));
            _registry.Add(Model("child", "root"));

            Assert.Throws<SceneInvariantException>(() => _registry.Reparent("root", "child"));
            Assert.IsNull(_registry.Get("root").ParentId);
            Assert.AreEqual("root", _registry.Get("child").ParentId);
        }

        [Test]
        public void Get_ReturnsSnapshotThatCannotMutateRegistry()
        {
            _registry.Add(Model("root"));
            var snapshot = _registry.Get("root");
            snapshot.Name = "Changed outside registry";
            Assert.AreEqual("root", _registry.Get("root").Name);
        }

        static SceneObjectModel Model(string id, string parentId = null) =>
            new()
            {
                Id = id,
                Name = id,
                ParentId = parentId,
                TypeId = SceneObjectTypeIds.Group
            };
    }
}
