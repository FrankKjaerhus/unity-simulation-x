using System;
using UnityEngine;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.SceneModel.Serialization
{
    public static class ProjectSchemaMigrator
    {
        public const string PrimitiveMeshComponentTypeId = "com.unitysimulationx.scene.primitive-mesh";

        [Serializable]
        sealed class SchemaVersionProbe
        {
            public int schemaVersion;
        }

        public static ProjectViewerDocument DecodeAndMigrate(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ProjectFormatException("Project document is empty.");

            var probe = JsonUtility.FromJson<SchemaVersionProbe>(json);
            return probe.schemaVersion switch
            {
                1 => MigrateFromV1(json),
                2 => JsonUtility.FromJson<ProjectViewerDocument>(json),
                < 1 => throw new ProjectFormatException($"Unsupported schema version: {probe.schemaVersion}."),
                > 2 => throw new ProjectFormatException($"Unsupported schema version: {probe.schemaVersion}."),
                _ => throw new ProjectFormatException("Unsupported schema version.")
            };
        }

        static ProjectViewerDocument MigrateFromV1(string json)
        {
            var source = JsonUtility.FromJson<ProjectSchemaV1Document>(json);
            var document = new ProjectViewerDocument
            {
                schemaVersion = 2,
                viewSettings = source.viewSettings ?? new ViewSettingsData()
            };

            if (source.scene?.objects == null)
                return document;

            foreach (var sourceObject in source.scene.objects)
            {
                var target = new SceneObjectDocumentData
                {
                    id = sourceObject.id,
                    name = sourceObject.name,
                    typeId = LegacySceneObjectTypeMigration.FromV1Type(sourceObject.type).Value,
                    parentId = sourceObject.parentId,
                    transform = sourceObject.transform ?? new TransformData(),
                    visible = sourceObject.visible,
                    material = new MaterialDefinition { BaseColor = sourceObject.baseColor }
                };

                if (!string.IsNullOrWhiteSpace(sourceObject.primitiveMeshTypeKey))
                {
                    target.components.Add(new SceneComponentDocumentData
                    {
                        typeId = PrimitiveMeshComponentTypeId,
                        schemaVersion = 1,
                        payloadJson = $"{{\"meshTypeKey\":\"{sourceObject.primitiveMeshTypeKey}\"}}"
                    });
                }

                document.scene.objects.Add(target);
            }

            return document;
        }
    }
}
