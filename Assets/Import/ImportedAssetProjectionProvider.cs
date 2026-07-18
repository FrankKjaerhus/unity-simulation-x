using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnitySimulationX.Viewer.Projection;

namespace UnitySimulationX.Import
{
    public sealed class ImportedAssetProjectionProvider : IImportedAssetProjectionProvider
    {
        readonly Dictionary<string, ImportResult> _cache = new(StringComparer.Ordinal);

        public bool TryApply(string assetId, GameObject target)
        {
            if (string.IsNullOrWhiteSpace(assetId) || target == null)
                return false;
            if (!_cache.TryGetValue(assetId, out var result))
                return false;
            if (result == null || !result.Succeeded || result.Meshes.Count == 0)
                return false;

            ApplyMesh(target, result.Meshes[0], result.Materials.Count > 0 ? result.Materials[0] : null);
            return true;
        }

        public void Store(string assetId, ImportResult result)
        {
            if (string.IsNullOrWhiteSpace(assetId))
                throw new ArgumentException("Asset id is required.", nameof(assetId));
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            _cache[assetId] = result;
        }

        public IReadOnlyDictionary<string, ImportResult> SnapshotCache() =>
            new Dictionary<string, ImportResult>(_cache, StringComparer.Ordinal);

        public void ReplaceCache(IReadOnlyDictionary<string, ImportResult> cache)
        {
            _cache.Clear();
            if (cache == null)
                return;

            foreach (var pair in cache)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                    continue;

                _cache[pair.Key] = pair.Value;
            }
        }

        public void Remove(string assetId)
        {
            if (!string.IsNullOrWhiteSpace(assetId))
                _cache.Remove(assetId);
        }

        static void ApplyMesh(GameObject target, ImportedMeshData data, ImportedMaterialData materialData)
        {
            if (data?.Vertices == null || data.Vertices.Length == 0 || data.Triangles == null || data.Triangles.Length == 0)
                return;

            var mesh = new Mesh
            {
                name = string.IsNullOrWhiteSpace(data.Name) ? target.name : data.Name,
                indexFormat = data.Vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
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
            material.color = materialData?.BaseColor ?? new Color(0.72f, 0.72f, 0.72f, 1f);
            return material;
        }
    }
}
