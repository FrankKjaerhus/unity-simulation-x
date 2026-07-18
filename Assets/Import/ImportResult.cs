using System.Collections.Generic;
using UnityEngine;
using UnitySimulationX.Editing;

namespace UnitySimulationX.Import
{
    public sealed class ImportResult
    {
        public bool Succeeded { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public SceneObjectDraft RootObject { get; set; }
        public List<ImportedMeshData> Meshes { get; } = new();
        public List<ImportedMaterialData> Materials { get; } = new();
        public List<ImportWarning> Warnings { get; } = new();
    }

    public sealed class ImportedMeshData
    {
        public string Name { get; set; }
        public Vector3[] Vertices { get; set; }
        public int[] Triangles { get; set; }
        public Vector3[] Normals { get; set; }
        public Vector2[] Uvs { get; set; }
    }

    public sealed class ImportedMaterialData
    {
        public string Name { get; set; }
        public Color BaseColor { get; set; } = Color.white;
        public float Metallic { get; set; }
        public float Roughness { get; set; } = 0.5f;
        public string BaseColorTexturePath { get; set; }
    }

    public sealed class ImportWarning
    {
        public string Message { get; set; }
    }
}
