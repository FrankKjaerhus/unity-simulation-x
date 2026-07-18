using NUnit.Framework;
using UnitySimulationX.SceneModel.Serialization;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ProjectSchemaTests
    {
        [Test]
        public void ProjectViewerDocument_HasVersionFields()
        {
            var doc = new ProjectViewerDocument();
            Assert.AreEqual(2, doc.schemaVersion);
            Assert.IsNotNull(doc.scene);
            Assert.IsNotNull(doc.viewSettings);
            Assert.IsNotNull(doc.assets);
        }

        [Test]
        public void ProjectViewerDocument_RoundTripsThroughJsonUtility()
        {
            var doc = new ProjectViewerDocument();
            doc.scene.objects.Add(new SceneObjectDocumentData
            {
                id = "test",
                name = "Test",
                typeId = "com.unitysimulationx.scene.primitive"
            });

            var json = UnityEngine.JsonUtility.ToJson(doc, prettyPrint: true);
            var loaded = UnityEngine.JsonUtility.FromJson<ProjectViewerDocument>(json);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(2, loaded.schemaVersion);
            Assert.AreEqual(1, loaded.scene.objects.Count);
            Assert.AreEqual("test", loaded.scene.objects[0].id);
        }
    }
}
