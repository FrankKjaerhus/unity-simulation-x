using NUnit.Framework;
using UnityEngine;
using UnitySimulationX.App.ProjectSystem;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer.Projection;

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
                TypeId = SceneObjectTypeIds.Group
            };
            var child = new SceneObjectModel
            {
                Id = "child",
                Name = "Child",
                TypeId = SceneObjectTypeIds.Primitive,
                ParentId = root.Id,
                Transform = new TransformData { Position = new Vector3(1f, 2f, 3f), Scale = Vector3.one }
            };

            sourceRegistry.Add(root);
            sourceRegistry.Add(child);

            var document = ProjectSerializer.CreateDocument(sourceRegistry);
            var sceneRoot = new GameObject("SerializerTestRoot");
            var targetRegistry = new SceneRegistry();
            var projection = new SceneProjectionService(sceneRoot.transform, targetRegistry);

            try
            {
                ProjectSerializer.ApplyDocument(document, targetRegistry, projection);

                var loadedChild = targetRegistry.Get("child");
                Assert.IsNotNull(loadedChild);
                Assert.AreEqual("root", loadedChild.ParentId);
                Assert.AreEqual(new Vector3(1f, 2f, 3f), loadedChild.Transform.Position);
                Assert.IsNotNull(projection.GetGameObject("child"));
            }
            finally
            {
                Object.DestroyImmediate(sceneRoot);
            }
        }
    }
}
