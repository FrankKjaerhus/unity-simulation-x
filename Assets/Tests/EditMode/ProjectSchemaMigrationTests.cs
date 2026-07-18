using NUnit.Framework;
using UnityEngine;
using UnitySimulationX.App.ProjectSystem;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;
using UnitySimulationX.SceneModel.Serialization;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ProjectSchemaMigrationTests
    {
        const string V1FixtureJson = @"{
  ""version"": ""1.0"",
  ""schemaVersion"": 1,
  ""scene"": {
    ""objects"": [
      {
        ""id"": ""root"",
        ""name"": ""Root"",
        ""type"": ""MachineFrame"",
        ""parentId"": null,
        ""childrenIds"": [""child""]
      },
      {
        ""id"": ""child"",
        ""name"": ""Child"",
        ""type"": ""Primitive"",
        ""parentId"": ""root"",
        ""childrenIds"": [],
        ""primitiveMeshTypeKey"": ""Cube""
      }
    ]
  },
  ""viewSettings"": {
    ""activeViewMode"": ""Perspective3D"",
    ""cameraBookmarks"": []
  },
  ""runtime"": { ""bindings"": [] },
  ""diagnostics"": []
}";

        [Test]
        public void V2Json_RoundTripsUnknownComponentPayloadExactly()
        {
            const string payload = "{\"vendorField\":42}";
            var model = new SceneObjectModel
            {
                Id = "object",
                Name = "Object",
                TypeId = new SceneObjectTypeId("com.vendor.product.object"),
                Components =
                {
                    new SceneComponentData("com.vendor.product.component", 3, payload)
                }
            };
            var registry = new SceneRegistry();
            registry.Add(model);

            var document = ProjectSerializer.CreateDocument(registry, System.Array.Empty<ProjectAssetDocumentData>());
            var json = JsonUtility.ToJson(document);
            var decoded = JsonUtility.FromJson<ProjectViewerDocument>(json);
            var restored = ProjectSerializer.CreateSnapshots(decoded);

            Assert.AreEqual(2, decoded.schemaVersion);
            Assert.AreEqual(payload, restored[0].Components[0].PayloadJson);
        }

        [Test]
        public void V1Json_MigratesPrimitiveAndHierarchy()
        {
            var migrated = ProjectSchemaMigrator.DecodeAndMigrate(V1FixtureJson);
            Assert.AreEqual(2, migrated.schemaVersion);
            Assert.AreEqual("com.unitysimulationx.scene.primitive", migrated.scene.objects[1].typeId);
            Assert.AreEqual(migrated.scene.objects[0].id, migrated.scene.objects[1].parentId);
        }
    }
}
