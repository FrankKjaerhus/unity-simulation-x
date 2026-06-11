using NUnit.Framework;
using UnityEngine;
using UnitySimulationX.App.ProjectSystem;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ProjectSerializerTests
    {
        [Test]
        public void ApplyDocument_RebuildsHierarchyAndTransforms()
        {
            var sourceRegistry = new SceneRegistry();
            var root = new SceneObjectModel
            {
                Id = "root",
                Name = "Root",
                Type = SceneObjectType.MachineFrame
            };
            var child = new SceneObjectModel
            {
                Id = "child",
                Name = "Child",
                Type = SceneObjectType.Primitive,
                ParentId = root.Id,
                Transform = new TransformData { Position = new Vector3(1f, 2f, 3f), Scale = Vector3.one }
            };

            sourceRegistry.Add(root);
            sourceRegistry.Add(child);

            var document = ProjectSerializer.CreateDocument(sourceRegistry);
            var sceneRoot = new GameObject("SerializerTestRoot");
            var targetRegistry = new SceneRegistry();
            var mapper = new SceneObjectMapper(sceneRoot.transform);

            try
            {
                ProjectSerializer.ApplyDocument(document, targetRegistry, mapper);

                var loadedChild = targetRegistry.Get("child");
                Assert.IsNotNull(loadedChild);
                Assert.AreEqual("root", loadedChild.ParentId);
                Assert.AreEqual(new Vector3(1f, 2f, 3f), loadedChild.Transform.Position);
                Assert.IsNotNull(mapper.GetGameObject("child"));
            }
            finally
            {
                Object.DestroyImmediate(sceneRoot);
            }
        }
    }
}
