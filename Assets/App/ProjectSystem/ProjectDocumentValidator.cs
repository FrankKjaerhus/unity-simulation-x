using System;
using System.Collections.Generic;
using System.IO;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;
using UnitySimulationX.SceneModel.Serialization;

namespace UnitySimulationX.App.ProjectSystem
{
    public sealed class ProjectDocumentValidator
    {
        public IReadOnlyList<ProjectIssue> Validate(ProjectViewerDocument document, string projectRoot)
        {
            var issues = new List<ProjectIssue>();

            if (document == null)
            {
                issues.Add(Error("project.document.missing", "Project document is missing."));
                return issues;
            }

            if (document.schemaVersion != 2)
                issues.Add(Error("project.schema.unsupported", $"Unsupported schema version: {document.schemaVersion}."));

            var importedAssets = document.assets?.imported ?? new List<ProjectAssetDocumentData>();
            var sceneObjects = document.scene?.objects ?? new List<SceneObjectDocumentData>();
            var objectIds = new Dictionary<string, SceneObjectDocumentData>(StringComparer.Ordinal);
            var assetIds = new Dictionary<string, ProjectAssetDocumentData>(StringComparer.Ordinal);

            ValidateAssets(importedAssets, projectRoot, assetIds, issues);
            ValidateObjects(sceneObjects, objectIds, assetIds, issues);
            ValidateHierarchy(objectIds, issues);

            return issues;
        }

        static void ValidateAssets(
            IReadOnlyList<ProjectAssetDocumentData> assets,
            string projectRoot,
            IDictionary<string, ProjectAssetDocumentData> assetIds,
            ICollection<ProjectIssue> issues)
        {
            foreach (var asset in assets)
            {
                if (asset == null)
                    continue;

                if (string.IsNullOrWhiteSpace(asset.assetId))
                {
                    issues.Add(Error("project.assets.asset-id.required", "Imported asset id is required."));
                    continue;
                }

                if (assetIds.ContainsKey(asset.assetId))
                {
                    issues.Add(Error(
                        "project.assets.asset-id.duplicate",
                        $"Duplicate imported asset id '{asset.assetId}'."));
                }
                else
                {
                    assetIds.Add(asset.assetId, asset);
                }

                if (string.IsNullOrWhiteSpace(asset.relativePath))
                {
                    issues.Add(Error(
                        "project.assets.path.required",
                        $"Imported asset '{asset.assetId}' requires a relative path."));
                    continue;
                }

                try
                {
                    var absolutePath = ProjectPaths.ResolveInsideRoot(projectRoot, asset.relativePath);
                    if (!File.Exists(absolutePath))
                    {
                        issues.Add(Warning(
                            "project.assets.file.missing",
                            $"Imported asset file '{asset.relativePath}' is missing."));
                    }
                }
                catch (ArgumentException)
                {
                    issues.Add(Error(
                        "project.assets.path.invalid",
                        $"Imported asset '{asset.assetId}' has an invalid relative path."));
                }
            }
        }

        static void ValidateObjects(
            IReadOnlyList<SceneObjectDocumentData> objects,
            IDictionary<string, SceneObjectDocumentData> objectIds,
            IReadOnlyDictionary<string, ProjectAssetDocumentData> assetIds,
            ICollection<ProjectIssue> issues)
        {
            foreach (var sceneObject in objects)
            {
                if (sceneObject == null)
                    continue;

                if (string.IsNullOrWhiteSpace(sceneObject.id))
                {
                    issues.Add(Error("project.scene.object-id.required", "Scene object id is required."));
                    continue;
                }

                if (objectIds.ContainsKey(sceneObject.id))
                {
                    issues.Add(Error(
                        "project.scene.object-id.duplicate",
                        $"Duplicate scene object id '{sceneObject.id}'."));
                    continue;
                }

                objectIds.Add(sceneObject.id, sceneObject);

                if (!SceneObjectTypeId.IsValid(sceneObject.typeId))
                {
                    issues.Add(Error(
                        "project.scene.type-id.invalid",
                        $"Scene object '{sceneObject.id}' has an invalid type id '{sceneObject.typeId}'."));
                }

                if (!string.IsNullOrWhiteSpace(sceneObject.assetId) && !assetIds.ContainsKey(sceneObject.assetId))
                {
                    issues.Add(Error(
                        "project.assets.reference.missing",
                        $"Scene object '{sceneObject.id}' references missing asset id '{sceneObject.assetId}'."));
                }

                ValidateComponents(sceneObject, issues);
            }
        }

        static void ValidateComponents(SceneObjectDocumentData sceneObject, ICollection<ProjectIssue> issues)
        {
            var components = sceneObject.components ?? new List<SceneComponentDocumentData>();
            var componentIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var component in components)
            {
                if (component == null)
                    continue;

                if (!componentIds.Add(component.typeId ?? string.Empty))
                {
                    issues.Add(Error(
                        "project.scene.component-type-id.duplicate",
                        $"Scene object '{sceneObject.id}' has duplicate component type id '{component.typeId}'."));
                }

                if (component.schemaVersion < 1)
                {
                    issues.Add(Error(
                        "project.scene.component-schema.invalid",
                        $"Scene object '{sceneObject.id}' component '{component.typeId}' has invalid schema version '{component.schemaVersion}'."));
                }
            }
        }

        static void ValidateHierarchy(
            IReadOnlyDictionary<string, SceneObjectDocumentData> objectIds,
            ICollection<ProjectIssue> issues)
        {
            foreach (var pair in objectIds)
            {
                var sceneObject = pair.Value;

                if (!string.IsNullOrWhiteSpace(sceneObject.parentId) &&
                    !objectIds.ContainsKey(sceneObject.parentId))
                {
                    issues.Add(Error(
                        "project.scene.parent.missing",
                        $"Scene object '{sceneObject.id}' references missing parent '{sceneObject.parentId}'."));
                }

                if (ContainsHierarchyCycle(sceneObject.id, objectIds))
                {
                    issues.Add(Error(
                        "project.scene.parent.cycle",
                        $"Scene object '{sceneObject.id}' participates in a parent cycle."));
                }
            }
        }

        static bool ContainsHierarchyCycle(
            string startId,
            IReadOnlyDictionary<string, SceneObjectDocumentData> objectIds)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var currentId = startId;

            while (objectIds.TryGetValue(currentId, out var sceneObject) &&
                   !string.IsNullOrWhiteSpace(sceneObject.parentId))
            {
                if (!visited.Add(sceneObject.parentId))
                    return true;

                currentId = sceneObject.parentId;
                if (string.Equals(currentId, startId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        static ProjectIssue Error(string code, string message) =>
            new()
            {
                Code = code,
                Message = message,
                IsError = true
            };

        static ProjectIssue Warning(string code, string message) =>
            new()
            {
                Code = code,
                Message = message,
                IsError = false
            };
    }
}
