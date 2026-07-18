using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnitySimulationX.SceneModel;
using UnitySimulationX.SceneModel.Serialization;

namespace UnitySimulationX.App.ProjectSystem
{
    public static class ProjectSerializer
    {
        public static ProjectViewerDocument CreateDocument(
            ISceneRegistryRead registry,
            IReadOnlyList<ProjectAssetDocumentData> assets)
        {
            var document = new ProjectViewerDocument();
            if (assets != null)
                document.assets.imported.AddRange(assets);

            foreach (var model in registry.GetAll().OrderBy(m => string.IsNullOrEmpty(m.ParentId) ? 0 : 1))
                document.scene.objects.Add(ToDocumentData(model));

            return document;
        }

        public static IReadOnlyList<SceneObjectModel> CreateSnapshots(ProjectViewerDocument document)
        {
            var snapshots = new List<SceneObjectModel>();
            if (document?.scene?.objects == null)
                return snapshots;

            foreach (var data in document.scene.objects)
                snapshots.Add(FromDocumentData(data));

            return snapshots;
        }

        static SceneObjectDocumentData ToDocumentData(SceneObjectModel model)
        {
            var data = new SceneObjectDocumentData
            {
                id = model.Id,
                name = model.Name,
                typeId = model.TypeId.Value,
                parentId = model.ParentId,
                transform = model.Transform?.Clone() ?? new TransformData(),
                visible = model.Visible,
                material = model.Material?.Clone() ?? new MaterialDefinition(),
                assetId = model.AssetId
            };

            if (!string.IsNullOrWhiteSpace(model.PrimitiveMeshTypeKey))
            {
                data.components.Add(new SceneComponentDocumentData
                {
                    typeId = ProjectSchemaMigrator.PrimitiveMeshComponentTypeId,
                    schemaVersion = 1,
                    payloadJson = $"{{\"meshTypeKey\":\"{model.PrimitiveMeshTypeKey}\"}}"
                });
            }

            if (model.Components != null)
            {
                foreach (var component in model.Components)
                {
                    data.components.Add(new SceneComponentDocumentData
                    {
                        typeId = component.TypeId,
                        schemaVersion = component.SchemaVersion,
                        payloadJson = component.PayloadJson
                    });
                }
            }

            return data;
        }

        static SceneObjectModel FromDocumentData(SceneObjectDocumentData data)
        {
            var model = new SceneObjectModel
            {
                Id = string.IsNullOrWhiteSpace(data.id) ? Guid.NewGuid().ToString("N") : data.id,
                Name = string.IsNullOrWhiteSpace(data.name) ? "Object" : data.name,
                TypeId = new SceneObjectTypeId(data.typeId),
                ParentId = data.parentId,
                Transform = data.transform?.Clone() ?? new TransformData(),
                Visible = data.visible,
                Material = data.material?.Clone() ?? new MaterialDefinition(),
                AssetId = data.assetId
            };

            if (data.components == null)
                return model;

            foreach (var component in data.components)
            {
                if (string.Equals(
                        component.typeId,
                        ProjectSchemaMigrator.PrimitiveMeshComponentTypeId,
                        StringComparison.Ordinal))
                {
                    model.PrimitiveMeshTypeKey = ExtractPrimitiveMeshTypeKey(component.payloadJson);
                    continue;
                }

                model.Components.Add(new SceneComponentData(
                    component.typeId,
                    component.schemaVersion,
                    component.payloadJson));
            }

            return model;
        }

        static string ExtractPrimitiveMeshTypeKey(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
                return null;

            const string marker = "\"meshTypeKey\":\"";
            var start = payloadJson.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0)
                return null;

            start += marker.Length;
            var end = payloadJson.IndexOf('"', start);
            return end > start ? payloadJson.Substring(start, end - start) : null;
        }
    }
}
