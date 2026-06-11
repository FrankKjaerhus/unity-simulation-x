using System.Linq;
using NUnit.Framework;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class SceneRegistryTests
    {
        SceneRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new SceneRegistry();
        }

        [Test]
        public void Add_RootObject_AppearsInRootIds()
        {
            var model = CreateModel("root");
            _registry.Add(model);

            Assert.That(_registry.RootIds, Does.Contain("root"));
            Assert.AreSame(model, _registry.Get("root"));
        }

        [Test]
        public void Add_Child_UpdatesParentChildren()
        {
            var parent = CreateModel("parent");
            var child = CreateModel("child");
            child.ParentId = "parent";

            _registry.Add(parent);
            _registry.Add(child);

            Assert.Contains("child", _registry.Get("parent").ChildrenIds);
            Assert.IsFalse(_registry.RootIds.Contains("child"));
        }

        [Test]
        public void Remove_DeletesDescendants()
        {
            var parent = CreateModel("parent");
            var child = CreateModel("child");
            child.ParentId = "parent";

            _registry.Add(parent);
            _registry.Add(child);

            Assert.IsTrue(_registry.Remove("parent"));
            Assert.IsNull(_registry.Get("parent"));
            Assert.IsNull(_registry.Get("child"));
        }

        [Test]
        public void Reparent_MovesBetweenParents()
        {
            var a = CreateModel("a");
            var b = CreateModel("b");
            var c = CreateModel("c");
            c.ParentId = "a";

            _registry.Add(a);
            _registry.Add(b);
            _registry.Add(c);

            _registry.Reparent("c", "b");

            Assert.IsFalse(_registry.Get("a").ChildrenIds.Contains("c"));
            Assert.Contains("c", _registry.Get("b").ChildrenIds);
            Assert.AreEqual("b", _registry.Get("c").ParentId);
        }

        [Test]
        public void Reparent_ToRoot_RemovesFromParent()
        {
            var parent = CreateModel("parent");
            var child = CreateModel("child");
            child.ParentId = "parent";

            _registry.Add(parent);
            _registry.Add(child);

            _registry.Reparent("child", null);

            Assert.That(_registry.RootIds, Does.Contain("child"));
            Assert.IsEmpty(_registry.Get("parent").ChildrenIds);
        }

        static SceneObjectModel CreateModel(string id)
        {
            return new SceneObjectModel
            {
                Id = id,
                Name = id,
                Type = SceneObjectType.Primitive
            };
        }
    }
}
