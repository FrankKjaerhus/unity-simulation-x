using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Import
{
    public sealed class StlSceneAssetImporter : ISceneAssetImporter
    {
        public string ImporterId => "stl";
        public bool CanImport(string fileExtension) => fileExtension == ".stl";

        public Task<ImportResult> ImportAsync(string filePath, ImportSettings settings)
        {
            var mesh = LooksLikeAsciiStl(filePath)
                ? ReadAscii(filePath, settings.UnitScale)
                : ReadBinary(filePath, settings.UnitScale);

            var result = new ImportResult
            {
                RootObject = new SceneObjectModel
                {
                    Id = System.Guid.NewGuid().ToString("N"),
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    TypeId = SceneObjectTypeIds.ImportedModel
                },
                Bounds = CalculateBounds(mesh.Vertices)
            };

            result.Meshes.Add(mesh);
            if (mesh.Vertices == null || mesh.Vertices.Length == 0)
                result.Warnings.Add(new ImportWarning { Message = "STL contained no mesh vertices." });

            return Task.FromResult(result);
        }

        static bool LooksLikeAsciiStl(string filePath)
        {
            var buffer = new byte[Mathf.Min(256, (int)new FileInfo(filePath).Length)];
            using (var stream = File.OpenRead(filePath))
                stream.Read(buffer, 0, buffer.Length);

            var header = Encoding.ASCII.GetString(buffer).TrimStart();
            return header.StartsWith("solid") && header.Contains("facet");
        }

        static ImportedMeshData ReadAscii(string filePath, float unitScale)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            var currentNormal = Vector3.up;

            foreach (var rawLine in File.ReadLines(filePath))
            {
                var line = rawLine.Trim();
                var parts = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                if (parts[0] == "facet" && parts.Length >= 5)
                {
                    currentNormal = new Vector3(Parse(parts[2]), Parse(parts[3]), Parse(parts[4])).normalized;
                    continue;
                }

                if (parts[0] != "vertex" || parts.Length < 4)
                    continue;

                vertices.Add(new Vector3(Parse(parts[1]), Parse(parts[2]), Parse(parts[3])) * unitScale);
                normals.Add(currentNormal);
                triangles.Add(vertices.Count - 1);
            }

            return new ImportedMeshData
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                Vertices = vertices.ToArray(),
                Triangles = triangles.ToArray(),
                Normals = normals.ToArray()
            };
        }

        static ImportedMeshData ReadBinary(string filePath, float unitScale)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            using var reader = new BinaryReader(File.OpenRead(filePath));
            reader.ReadBytes(80);
            var triangleCount = reader.ReadUInt32();

            for (var i = 0; i < triangleCount; i++)
            {
                var normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()).normalized;
                for (var v = 0; v < 3; v++)
                {
                    vertices.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()) * unitScale);
                    normals.Add(normal);
                    triangles.Add(vertices.Count - 1);
                }

                reader.ReadUInt16();
            }

            return new ImportedMeshData
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                Vertices = vertices.ToArray(),
                Triangles = triangles.ToArray(),
                Normals = normals.ToArray()
            };
        }

        static Bounds CalculateBounds(Vector3[] vertices)
        {
            if (vertices == null || vertices.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            var bounds = new Bounds(vertices[0], Vector3.zero);
            for (var i = 1; i < vertices.Length; i++)
                bounds.Encapsulate(vertices[i]);

            return bounds;
        }

        static float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    }
}
