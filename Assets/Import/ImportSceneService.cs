using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnitySimulationX.Core;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;
using UnitySimulationX.SceneModel.Serialization;
using UnityEngine;

namespace UnitySimulationX.Import
{
    public sealed class ImportSceneService : IImportSceneService
    {
        readonly ImporterRegistry _importers;
        readonly ISceneEditService _edits;
        readonly IProjectAssetStore _assetStore;
        readonly ImportedAssetProjectionProvider _projectionProvider;
        readonly ImportSettings _settings;

        public ImportSceneService(
            ImporterRegistry importers,
            ISceneEditService edits,
            IProjectAssetStore assetStore,
            ImportedAssetProjectionProvider projectionProvider,
            ImportSettings settings = null)
        {
            _importers = importers ?? throw new ArgumentNullException(nameof(importers));
            _edits = edits ?? throw new ArgumentNullException(nameof(edits));
            _assetStore = assetStore ?? throw new ArgumentNullException(nameof(assetStore));
            _projectionProvider = projectionProvider ?? throw new ArgumentNullException(nameof(projectionProvider));
            _settings = settings ?? new ImportSettings();
        }

        public async Task<ImportOperationResult> ImportFileAsync(
            string path,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return Failure(
                    "import.source.missing",
                    "Import source file does not exist.");
            }

            var importer = _importers.Resolve(path);
            if (importer == null)
            {
                return Failure(
                    "import.importer.missing",
                    $"No importer registered for '{path}'.");
            }

            var settings = CloneSettings(_settings);
            var importSettingsJson = SerializeSettings(settings);
            ProjectAssetDocumentData entry = null;

            try
            {
                entry = await _assetStore.ImportExternalFileAsync(
                    path,
                    importer.ImporterId,
                    importer.ImporterVersion,
                    importSettingsJson,
                    cancellationToken);

                var projectOwnedPath = _assetStore.Resolve(entry.assetId);
                var importResult = await importer.ImportAsync(projectOwnedPath, settings, cancellationToken);
                if (importResult == null)
                {
                    CleanupFailedImport(entry.assetId);
                    return Failure(
                        "import.failed",
                        $"Importer '{importer.ImporterId}' returned no result.");
                }

                if (!importResult.Succeeded || importResult.RootObject == null)
                {
                    CleanupFailedImport(entry.assetId);
                    return new ImportOperationResult
                    {
                        Succeeded = false,
                        ErrorCode = importResult.ErrorCode ?? "import.failed",
                        Message = importResult.Message ?? "Import failed.",
                        Warnings = importResult.Warnings
                    };
                }

                importResult.RootObject.AssetId = entry.assetId;
                ApplyImportedMaterial(importResult.RootObject, importResult.Materials);
                _projectionProvider.Store(entry.assetId, importResult);

                var createResult = _edits.Create(importResult.RootObject);
                if (!createResult.Succeeded)
                {
                    CleanupFailedImport(entry.assetId);
                    return Failure(
                        createResult.ErrorCode ?? "import.scene-create.failed",
                        createResult.Message ?? "Imported scene object could not be created.",
                        importResult.Warnings);
                }

                return new ImportOperationResult
                {
                    Succeeded = true,
                    ObjectId = importResult.RootObject.Id,
                    Warnings = importResult.Warnings
                };
            }
            catch (OperationCanceledException)
            {
                if (entry != null)
                    CleanupFailedImport(entry.assetId);

                return Failure("import.cancelled", "Import cancelled.");
            }
            catch (Exception ex)
            {
                if (entry != null)
                    CleanupFailedImport(entry.assetId);

                return Failure("import.failed", ex.Message);
            }
        }

        void CleanupFailedImport(string assetId)
        {
            _projectionProvider.Remove(assetId);
            _assetStore.Remove(assetId);
        }

        static void ApplyImportedMaterial(SceneObjectDraft draft, System.Collections.Generic.IReadOnlyList<ImportedMaterialData> materials)
        {
            if (draft == null || materials == null || materials.Count == 0 || materials[0] == null)
                return;

            draft.Material ??= new MaterialDefinition();
            draft.Material.BaseColor = materials[0].BaseColor;
            draft.Material.Metallic = materials[0].Metallic;
            draft.Material.Roughness = materials[0].Roughness;
        }

        static ImportSettings CloneSettings(ImportSettings settings)
        {
            return new ImportSettings
            {
                UnitScale = settings.UnitScale,
                GenerateColliders = settings.GenerateColliders,
                PreserveHierarchy = settings.PreserveHierarchy,
                GenerateMaterials = settings.GenerateMaterials,
                CenterOnImport = settings.CenterOnImport,
                MaxSourceBytes = settings.MaxSourceBytes,
                MaxVertices = settings.MaxVertices,
                MaxIndices = settings.MaxIndices
            };
        }

        static string SerializeSettings(ImportSettings settings)
        {
            return JsonUtility.ToJson(new ImportSettingsData
            {
                unitScale = settings.UnitScale,
                generateColliders = settings.GenerateColliders,
                preserveHierarchy = settings.PreserveHierarchy,
                generateMaterials = settings.GenerateMaterials,
                centerOnImport = settings.CenterOnImport,
                maxSourceBytes = settings.MaxSourceBytes,
                maxVertices = settings.MaxVertices,
                maxIndices = settings.MaxIndices
            });
        }

        static ImportOperationResult Failure(
            string code,
            string message,
            System.Collections.Generic.IReadOnlyList<ImportWarning> warnings = null)
        {
            return new ImportOperationResult
            {
                Succeeded = false,
                ErrorCode = code,
                Message = message,
                Warnings = warnings
            };
        }

        [Serializable]
        sealed class ImportSettingsData
        {
            public float unitScale = 1f;
            public bool generateColliders = true;
            public bool preserveHierarchy = true;
            public bool generateMaterials = true;
            public bool centerOnImport;
            public long maxSourceBytes = 512L * 1024L * 1024L;
            public int maxVertices = 5_000_000;
            public int maxIndices = 15_000_000;
        }
    }
}
