using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel.Serialization;

namespace UnitySimulationX.App.ProjectSystem
{
    public sealed class ProjectAssetStore : IProjectAssetStore
    {
        readonly IProjectWorkspace _workspace;
        readonly Dictionary<string, ProjectAssetDocumentData> _entriesByAssetId =
            new(StringComparer.Ordinal);

        public ProjectAssetStore(IProjectWorkspace workspace)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        }

        public async Task<ProjectAssetDocumentData> ImportExternalFileAsync(
            string sourcePath,
            string importerId,
            int importerVersion,
            string importSettingsJson,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException("Source path is required.", nameof(sourcePath));
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Imported asset source file was not found.", sourcePath);
            if (string.IsNullOrWhiteSpace(importerId))
                throw new ArgumentException("Importer id is required.", nameof(importerId));

            cancellationToken.ThrowIfCancellationRequested();

            var assetId = Guid.NewGuid().ToString("N");
            var extension = Path.GetExtension(sourcePath)?.ToLowerInvariant() ?? string.Empty;
            var relativePath = $"{ProjectPaths.ImportedAssetsDirectory}/{assetId}{extension}";
            var destinationPath = ProjectPaths.ResolveInsideRoot(_workspace.RootPath, relativePath);

            string contentHash = null;
            try
            {
                contentHash = await CopyFileAtomicAndHashAsync(sourcePath, destinationPath, cancellationToken);
            }
            catch
            {
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);

                throw;
            }

            var entry = new ProjectAssetDocumentData
            {
                assetId = assetId,
                relativePath = relativePath,
                originalFileName = Path.GetFileName(sourcePath),
                mediaType = DetectMediaType(extension),
                contentHash = contentHash,
                importerId = importerId,
                importerVersion = importerVersion,
                importSettingsJson = string.IsNullOrWhiteSpace(importSettingsJson) ? "{}" : importSettingsJson
            };

            _entriesByAssetId.Add(assetId, Clone(entry));
            return Clone(entry);
        }

        public ProjectAssetDocumentData Get(string assetId)
        {
            return _entriesByAssetId.TryGetValue(assetId ?? string.Empty, out var entry)
                ? Clone(entry)
                : null;
        }

        public IReadOnlyList<ProjectAssetDocumentData> GetAll() =>
            _entriesByAssetId.Values
                .OrderBy(entry => entry.assetId, StringComparer.Ordinal)
                .Select(Clone)
                .ToList();

        public string Resolve(string assetId)
        {
            if (!_entriesByAssetId.TryGetValue(assetId ?? string.Empty, out var entry))
                throw new InvalidOperationException($"Unknown project asset id '{assetId}'.");

            return ProjectPaths.ResolveInsideRoot(_workspace.RootPath, entry.relativePath);
        }

        public void ReplaceCatalog(IReadOnlyList<ProjectAssetDocumentData> entries)
        {
            _entriesByAssetId.Clear();
            if (entries == null)
                return;

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.assetId))
                    continue;
                if (_entriesByAssetId.ContainsKey(entry.assetId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate project asset id '{entry.assetId}' is not allowed.");
                }

                _entriesByAssetId.Add(entry.assetId, Clone(entry));
            }
        }

        public async Task CopyAllToAsync(string targetProjectRoot, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(targetProjectRoot))
                throw new ArgumentException("Target project root is required.", nameof(targetProjectRoot));

            foreach (var entry in GetAll())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sourcePath = Resolve(entry.assetId);
                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException($"Project asset '{entry.assetId}' is missing.", sourcePath);

                var destinationPath = ProjectPaths.ResolveInsideRoot(targetProjectRoot, entry.relativePath);
                await CopyFileAtomicAsync(sourcePath, destinationPath, cancellationToken);
            }
        }

        public void Remove(string assetId)
        {
            if (!_entriesByAssetId.TryGetValue(assetId ?? string.Empty, out var entry))
                return;

            _entriesByAssetId.Remove(assetId);
            var assetPath = ProjectPaths.ResolveInsideRoot(_workspace.RootPath, entry.relativePath);
            if (File.Exists(assetPath))
                File.Delete(assetPath);
        }

        static async Task<string> CopyFileAtomicAndHashAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? string.Empty);

            var temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.tmp";
            try
            {
                using var source = new FileStream(
                    sourcePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    81920,
                    useAsync: true);
                using var destination = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

                var buffer = new byte[81920];
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    hash.AppendData(buffer, 0, bytesRead);
                    await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                }

                destination.Flush(true);
                ReplaceOrMove(temporaryPath, destinationPath);

                return ToHex(hash.GetHashAndReset());
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        static async Task CopyFileAtomicAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? string.Empty);

            var temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.tmp";
            try
            {
                using var source = new FileStream(
                    sourcePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    81920,
                    useAsync: true);
                using var destination = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true);

                await source.CopyToAsync(destination, 81920, cancellationToken);
                destination.Flush(true);
                ReplaceOrMove(temporaryPath, destinationPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        static void ReplaceOrMove(string sourcePath, string destinationPath)
        {
            if (File.Exists(destinationPath))
                File.Replace(sourcePath, destinationPath, null);
            else
                File.Move(sourcePath, destinationPath);
        }

        static string DetectMediaType(string extension)
        {
            return extension switch
            {
                ".obj" => "model/obj",
                ".stl" => "model/stl",
                ".glb" => "model/gltf-binary",
                ".gltf" => "model/gltf+json",
                _ => "application/octet-stream"
            };
        }

        static string ToHex(byte[] bytes)
        {
            var chars = new char[bytes.Length * 2];
            const string hex = "0123456789abcdef";
            for (var i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = hex[bytes[i] >> 4];
                chars[(i * 2) + 1] = hex[bytes[i] & 0x0f];
            }

            return new string(chars);
        }

        static ProjectAssetDocumentData Clone(ProjectAssetDocumentData entry)
        {
            if (entry == null)
                return null;

            return new ProjectAssetDocumentData
            {
                assetId = entry.assetId,
                relativePath = entry.relativePath,
                originalFileName = entry.originalFileName,
                mediaType = entry.mediaType,
                contentHash = entry.contentHash,
                importerId = entry.importerId,
                importerVersion = entry.importerVersion,
                importSettingsJson = entry.importSettingsJson
            };
        }
    }
}
