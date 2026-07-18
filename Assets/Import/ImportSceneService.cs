using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer.Projection;

namespace UnitySimulationX.Import
{
    public sealed class ImportSceneService : IImportSceneService
    {
        readonly ImporterRegistry _importers;
        readonly SceneRegistry _registry;
        readonly ISceneProjectionService _projection;

        public ImportSceneService(ImporterRegistry importers, SceneRegistry registry, ISceneProjectionService projection)
        {
            _importers = importers;
            _registry = registry;
            _projection = projection;
        }

        public async Task ImportFileAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            var importer = _importers.Resolve(path);
            if (importer == null)
            {
                Debug.LogWarning($"No importer registered for: {path}");
                return;
            }

            var result = await importer.ImportAsync(path, new ImportSettings());
            if (result?.RootObject == null)
                return;

            result.RootObject.DomainProperties["SourcePath"] = path;
            _registry.Add(result.RootObject);
            var root = result.ImportedGameObject != null
                ? RegisterImportedGameObject(result.RootObject, result.ImportedGameObject)
                : CreateImportedProjection(result.RootObject);

            if (root != null && result.Meshes.Count > 0)
                ApplyMesh(root, result.Meshes[0], result.Materials.Count > 0 ? result.Materials[0] : null);

            if (root != null)
                EnsurePickColliders(root);

            foreach (var warning in result.Warnings)
                Debug.LogWarning($"Import warning: {warning.Message}");

            EventBus.Publish(new HierarchyChangedEvent());
            EventBus.Publish(new SceneObjectChangedEvent { ObjectId = result.RootObject.Id, Model = result.RootObject });
            Debug.Log($"Imported 3D file: {path}");
        }

        static void ApplyMesh(GameObject target, ImportedMeshData data, ImportedMaterialData materialData)
        {
            if (data?.Vertices == null || data.Vertices.Length == 0 || data.Triangles == null || data.Triangles.Length == 0)
                return;

            var mesh = new Mesh
            {
                name = data.Name,
                indexFormat = data.Vertices.Length > 65535
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };

            mesh.vertices = data.Vertices;
            mesh.triangles = data.Triangles;

            if (data.Normals != null && data.Normals.Length == data.Vertices.Length)
                mesh.normals = data.Normals;
            else
                mesh.RecalculateNormals();

            if (data.Uvs != null && data.Uvs.Length == data.Vertices.Length)
                mesh.uv = data.Uvs;

            mesh.RecalculateBounds();

            var filter = target.GetComponent<MeshFilter>() ?? target.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = target.GetComponent<MeshRenderer>() ?? target.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(materialData);

            var collider = target.GetComponent<MeshCollider>() ?? target.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }

        static Material CreateMaterial(ImportedMaterialData materialData)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));

            if (materialData != null)
                material.color = materialData.BaseColor;
            else
                material.color = new Color(0.72f, 0.72f, 0.72f, 1f);

            return material;
        }

        static void EnsurePickColliders(GameObject root)
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null)
                    continue;

                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }
        }

        GameObject RegisterImportedGameObject(SceneObjectModel model, GameObject target)
        {
            _projection.RegisterExistingTarget(model.Id, target);
            return target;
        }

        GameObject CreateImportedProjection(SceneObjectModel model)
        {
            _projection.CreateProjection(model);
            return _projection.GetGameObject(model.Id);
        }
    }
}
