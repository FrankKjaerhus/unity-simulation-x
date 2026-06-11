using NUnit.Framework;
using UnityEngine;
using UnitySimulationX.SceneModel;
using UnitySimulationX.SceneModel.Serialization;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ProjectSchemaTests
    {
        [Test]
        public void ProjectViewerDocument_HasVersionFields()
        {
            var doc = new ProjectViewerDocument();
            Assert.AreEqual("1.0", doc.version);
            Assert.AreEqual(1, doc.schemaVersion);
            Assert.IsNotNull(doc.scene);
            Assert.IsNotNull(doc.viewSettings);
            Assert.IsNotNull(doc.runtime);
        }

        [Test]
        public void ProjectViewerDocument_RoundTripsThroughJsonUtility()
        {
            var doc = new ProjectViewerDocument();
            doc.scene.objects.Add(new SceneObjectDocumentData
            {
                id = "test",
                name = "Test",
                type = SceneObjectType.Primitive.ToString()
            });

            var json = JsonUtility.ToJson(doc, prettyPrint: true);
            var loaded = JsonUtility.FromJson<ProjectViewerDocument>(json);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded.schemaVersion);
            Assert.AreEqual(1, loaded.scene.objects.Count);
            Assert.AreEqual("test", loaded.scene.objects[0].id);
        }
    }
}
