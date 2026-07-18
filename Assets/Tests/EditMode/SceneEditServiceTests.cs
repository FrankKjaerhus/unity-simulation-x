using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnitySimulationX.Core;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class SceneEditServiceTests
    {
        [Test]
        public void Rename_UpdatesRegistryProjectionAndPublishesOnce()
        {
            var projection = new RecordingProjection();
            var bus = new EventBus(_ => { });
            var registry = new SceneRegistry();
            registry.Add(Model("object"));
            var changes = new List<SceneChangeSet>();
            bus.Subscribe<SceneChangedEvent>(evt => changes.Add(evt.ChangeSet));
            var service = new SceneEditService(registry, projection, bus);

            var result = service.Rename("object", "Renamed");

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("Renamed", registry.Get("object").Name);
            Assert.AreEqual(1, projection.UpdateCount);
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(registry.Revision, changes[0].Revision);
        }

        [Test]
        public void Reparent_InvalidCycle_ReturnsFailureWithoutProjectionOrEvent()
        {
            var projection = new RecordingProjection();
            var bus = new EventBus(_ => { });
            var registry = RegistryWithRootAndChild();
            var events = 0;
            bus.Subscribe<SceneChangedEvent>(_ => events++);
            var service = new SceneEditService(registry, projection, bus);

            var result = service.Reparent("root", "child");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("scene.parent.cycle", result.ErrorCode);
            Assert.AreEqual(0, projection.UpdateCount);
            Assert.AreEqual(0, events);
        }

        static SceneObjectModel Model(string id) =>
            new()
            {
                Id = id,
                Name = id,
                TypeId = SceneObjectTypeIds.Primitive
            };

        static SceneRegistry RegistryWithRootAndChild()
        {
            var registry = new SceneRegistry();
            registry.Add(new SceneObjectModel
            {
                Id = "root",
                Name = "Root",
                TypeId = SceneObjectTypeIds.Group
            });
            registry.Add(new SceneObjectModel
            {
                Id = "child",
                Name = "Child",
                TypeId = SceneObjectTypeIds.Primitive,
                ParentId = "root"
            });
            return registry;
        }

        sealed class RecordingProjection : ISceneProjectionWriter
        {
            public int UpdateCount { get; private set; }

            public void CreateProjection(SceneObjectModel snapshot) { }

            public void UpdateProjection(SceneObjectModel snapshot) => UpdateCount++;

            public void RemoveProjection(string objectId) { }

            public void ReplaceAllProjections(IReadOnlyList<SceneObjectModel> snapshots) { }
        }
    }
}
