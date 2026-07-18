using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Import
{
    public sealed class ObjSceneAssetImporter : ISceneAssetImporter
    {
        public string ImporterId => "obj";
        public int ImporterVersion => 1;
        public bool CanImport(string fileExtension) => fileExtension == ".obj";

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

                var sourceVertices = new List<Vector3>();
                var sourceNormals = new List<Vector3>();
                var sourceUvs = new List<Vector2>();
                var vertices = new List<Vector3>();
                var normals = new List<Vector3>();
                var uvs = new List<Vector2>();
                var triangles = new List<int>();

                foreach (var rawLine in File.ReadLines(filePath))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#"))
                        continue;

                    var parts = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0)
                        continue;

                    switch (parts[0])
                    {
                        case "v" when parts.Length >= 4:
                            sourceVertices.Add(new Vector3(Parse(parts[1]), Parse(parts[2]), Parse(parts[3])) * settings.UnitScale);
                            break;
                        case "vn" when parts.Length >= 4:
                            sourceNormals.Add(new Vector3(Parse(parts[1]), Parse(parts[2]), Parse(parts[3])).normalized);
                            break;
                        case "vt" when parts.Length >= 3:
                            sourceUvs.Add(new Vector2(Parse(parts[1]), Parse(parts[2])));
                            break;
                        case "f":
                            AddFace(
                                parts,
                                sourceVertices,
                                sourceNormals,
                                sourceUvs,
                                vertices,
                                normals,
                                uvs,
                                triangles,
                                settings);
                            break;
                    }
                }

                var result = new ImportResult
                {
                    Succeeded = true,
                    RootObject = CreateRootObject(filePath)
                };

                result.Meshes.Add(new ImportedMeshData
                {
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    Vertices = vertices.ToArray(),
                    Triangles = triangles.ToArray(),
                    Normals = normals.Count == vertices.Count ? normals.ToArray() : null,
                    Uvs = uvs.Count == vertices.Count ? uvs.ToArray() : null
                });

                if (vertices.Count == 0)
                    result.Warnings.Add(new ImportWarning { Message = "OBJ contained no mesh vertices." });

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

        static void AddFace(
            string[] parts,
            List<Vector3> sourceVertices,
            List<Vector3> sourceNormals,
            List<Vector2> sourceUvs,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles,
            ImportSettings settings)
        {
            var faceIndices = new List<int>();
            for (var i = 1; i < parts.Length; i++)
            {
                faceIndices.Add(AddFaceVertex(parts[i], sourceVertices, sourceNormals, sourceUvs, vertices, normals, uvs));
                EnsureWithinLimits(vertices.Count, triangles.Count, settings);
            }

            for (var i = 1; i < faceIndices.Count - 1; i++)
            {
                triangles.Add(faceIndices[0]);
                triangles.Add(faceIndices[i]);
                triangles.Add(faceIndices[i + 1]);
                EnsureWithinLimits(vertices.Count, triangles.Count, settings);
            }
        }

        static int AddFaceVertex(
            string token,
            List<Vector3> sourceVertices,
            List<Vector3> sourceNormals,
            List<Vector2> sourceUvs,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs)
        {
            var indices = token.Split('/');
            var vertexIndex = ResolveIndex(ParseIndex(indices[0]), sourceVertices.Count);
            vertices.Add(sourceVertices[vertexIndex]);

            if (indices.Length > 1 && !string.IsNullOrEmpty(indices[1]) && sourceUvs.Count > 0)
            {
                var uvIndex = ResolveIndex(ParseIndex(indices[1]), sourceUvs.Count);
                uvs.Add(sourceUvs[uvIndex]);
            }

            if (indices.Length > 2 && !string.IsNullOrEmpty(indices[2]) && sourceNormals.Count > 0)
            {
                var normalIndex = ResolveIndex(ParseIndex(indices[2]), sourceNormals.Count);
                normals.Add(sourceNormals[normalIndex]);
            }

            return vertices.Count - 1;
        }

        static SceneObjectDraft CreateRootObject(string filePath)
        {
            return new SceneObjectDraft
            {
                Id = System.Guid.NewGuid().ToString("N"),
                Name = Path.GetFileNameWithoutExtension(filePath),
                TypeId = SceneObjectTypeIds.ImportedModel
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

        static int ParseIndex(string value) => int.Parse(value, CultureInfo.InvariantCulture);
        static float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);
        static int ResolveIndex(int objIndex, int count) => objIndex < 0 ? count + objIndex : objIndex - 1;

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
