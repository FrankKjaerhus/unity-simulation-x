using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Import
{
    public sealed class StlSceneAssetImporter : ISceneAssetImporter
    {
        public string ImporterId => "stl";
        public int ImporterVersion => 1;
        public bool CanImport(string fileExtension) => fileExtension == ".stl";

        public Task<ImportResult> ImportAsync(
            string filePath,
            ImportSettings settings,
            CancellationToken cancellationToken)
        {
            settings ??= new ImportSettings();
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                ValidateSourceSize(filePath, settings);

                var mesh = LooksLikeAsciiStl(filePath)
                    ? ReadAscii(filePath, settings, cancellationToken)
                    : ReadBinary(filePath, settings, cancellationToken);

                var result = new ImportResult
                {
                    Succeeded = true,
                    RootObject = new SceneObjectDraft
                    {
                        Id = System.Guid.NewGuid().ToString("N"),
                        Name = Path.GetFileNameWithoutExtension(filePath),
                        TypeId = SceneObjectTypeIds.ImportedModel
                    }
                };

                result.Meshes.Add(mesh);
                if (mesh.Vertices == null || mesh.Vertices.Length == 0)
                    result.Warnings.Add(new ImportWarning { Message = "STL contained no mesh vertices." });

                return Task.FromResult(result);
            }
            catch (ImportFailureException ex)
            {
                return Task.FromResult(new ImportResult
                {
                    Succeeded = false,
                    ErrorCode = ex.Code,
                    Message = ex.Message
                });
            }
        }

        static bool LooksLikeAsciiStl(string filePath)
        {
            var buffer = new byte[Mathf.Min(256, (int)new FileInfo(filePath).Length)];
            using (var stream = File.OpenRead(filePath))
                stream.Read(buffer, 0, buffer.Length);

            var header = Encoding.ASCII.GetString(buffer).TrimStart();
            return header.StartsWith("solid") && header.Contains("facet");
        }

        static ImportedMeshData ReadAscii(
            string filePath,
            ImportSettings settings,
            CancellationToken cancellationToken)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            var currentNormal = Vector3.up;

            foreach (var rawLine in File.ReadLines(filePath))
            {
                cancellationToken.ThrowIfCancellationRequested();

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

                vertices.Add(new Vector3(Parse(parts[1]), Parse(parts[2]), Parse(parts[3])) * settings.UnitScale);
                normals.Add(currentNormal);
                triangles.Add(vertices.Count - 1);
                EnsureWithinLimits(vertices.Count, triangles.Count, settings);
            }

            return new ImportedMeshData
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                Vertices = vertices.ToArray(),
                Triangles = triangles.ToArray(),
                Normals = normals.ToArray()
            };
        }

        static ImportedMeshData ReadBinary(
            string filePath,
            ImportSettings settings,
            CancellationToken cancellationToken)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            using var reader = new BinaryReader(File.OpenRead(filePath));
            reader.ReadBytes(80);
            var triangleCount = reader.ReadUInt32();

            for (var i = 0; i < triangleCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()).normalized;
                for (var v = 0; v < 3; v++)
                {
                    vertices.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()) * settings.UnitScale);
                    normals.Add(normal);
                    triangles.Add(vertices.Count - 1);
                    EnsureWithinLimits(vertices.Count, triangles.Count, settings);
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

        static void ValidateSourceSize(string filePath, ImportSettings settings)
        {
            if (new FileInfo(filePath).Length > settings.MaxSourceBytes)
            {
                throw new ImportFailureException(
                    "import.source.too-large",
                    $"Source file exceeds the import size limit of {settings.MaxSourceBytes} bytes.");
            }
        }

        static float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);

        static void EnsureWithinLimits(int vertexCount, int indexCount, ImportSettings settings)
        {
            if (vertexCount > settings.MaxVertices)
            {
                throw new ImportFailureException(
                    "import.mesh.vertices.exceeded",
                    $"Imported mesh exceeded the vertex limit of {settings.MaxVertices}.");
            }

            if (indexCount > settings.MaxIndices)
            {
                throw new ImportFailureException(
                    "import.mesh.indices.exceeded",
                    $"Imported mesh exceeded the index limit of {settings.MaxIndices}.");
            }
        }

        sealed class ImportFailureException : System.Exception
        {
            public ImportFailureException(string code, string message) : base(message)
            {
                Code = code;
            }

            public string Code { get; }
        }
    }
}
