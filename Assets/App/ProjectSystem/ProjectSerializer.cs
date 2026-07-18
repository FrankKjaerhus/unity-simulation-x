using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnitySimulationX.SceneModel;
using UnitySimulationX.SceneModel.Serialization;
using UnitySimulationX.Viewer.Projection;

namespace UnitySimulationX.App.ProjectSystem
{
    public static class ProjectSerializer
    {
        public static ProjectViewerDocument CreateDocument(SceneRegistry registry)
        {
            var document = new ProjectViewerDocument();
            foreach (var model in registry.GetAll().OrderBy(m => string.IsNullOrEmpty(m.ParentId) ? 0 : 1))
                document.scene.objects.Add(ToDocumentData(model, registry));

            return document;
        }

        public static void ApplyDocument(ProjectViewerDocument document, SceneRegistry registry, ISceneProjectionService projection)
        {
            if (document?.scene?.objects == null)
                return;

            var existingIds = registry.GetAll().Select(model => model.Id).ToList();
            foreach (var id in existingIds)
                projection.RemoveProjection(id);

            foreach (var rootId in registry.RootIds.ToList())
                registry.Remove(rootId);

            var remaining = document.scene.objects.ToList();
            var guard = 0;

            while (remaining.Count > 0 && guard++ < document.scene.objects.Count + 1)
            {
                var addedAny = false;
                for (var i = remaining.Count - 1; i >= 0; i--)
                {
                    var data = remaining[i];
                    if (!string.IsNullOrEmpty(data.parentId) && !registry.Contains(data.parentId))
                        continue;

                    var model = FromDocumentData(data);
                    registry.Add(model);
                    projection.CreateProjection(model);
                    remaining.RemoveAt(i);
                    addedAny = true;
                }

                if (!addedAny)
                    break;
            }

            foreach (var data in remaining)
            {
                data.parentId = null;
                var model = FromDocumentData(data);
                registry.Add(model);
                projection.CreateProjection(model);
            }
        }

        static SceneObjectDocumentData ToDocumentData(SceneObjectModel model, SceneRegistry registry)
        {
            return new SceneObjectDocumentData
            {
                id = model.Id,
                name = model.Name,
                type = model.Type.ToString(),
                parentId = model.ParentId,
                childrenIds = registry.GetChildrenIds(model.Id).ToList(),
                transform = model.Transform?.Clone() ?? new TransformData(),
                visible = model.Visible,
                primitiveMeshTypeKey = model.PrimitiveMeshTypeKey,
                baseColor = model.Material?.BaseColor ?? Color.white
            };
        }

        static SceneObjectModel FromDocumentData(SceneObjectDocumentData data)
        {
            var model = new SceneObjectModel
            {
                Id = string.IsNullOrWhiteSpace(data.id) ? Guid.NewGuid().ToString("N") : data.id,
                Name = string.IsNullOrWhiteSpace(data.name) ? "Object" : data.name,
                Type = ParseType(data.type),
                ParentId = data.parentId,
                Transform = data.transform ?? new TransformData(),
                Visible = data.visible,
                PrimitiveMeshTypeKey = data.primitiveMeshTypeKey,
                Material = new MaterialDefinition { BaseColor = data.baseColor }
            };

            return model;
        }

        static SceneObjectType ParseType(string value)
        {
            return Enum.TryParse<SceneObjectType>(value, out var type) ? type : SceneObjectType.ImportedAsset;
        }
    }
}
