using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnitySimulationX.Core;
using UnitySimulationX.Editing;
using UnitySimulationX.Import;
using UnitySimulationX.SceneModel;
using UnitySimulationX.SceneModel.Serialization;
using UnityEngine;

namespace UnitySimulationX.App.ProjectSystem
{
    public sealed class ProjectPersistenceService : IProjectPersistenceService
    {
        readonly ISceneEditService _edits;
        readonly IProjectWorkspace _workspace;
        readonly IProjectAssetStore _assetStore;
        readonly ImporterRegistry _importers;
        readonly ImportedAssetProjectionProvider _importedAssetProjectionProvider;
        readonly MissingAssetFactory _missingAssetFactory;
        readonly ProjectDocumentValidator _validator;

        public ProjectPersistenceService(
            ISceneEditService edits,
            IProjectWorkspace workspace,
            ProjectDocumentValidator validator = null)
            : this(
                edits,
                workspace,
                new ProjectAssetStore(workspace),
                CreateDefaultImporters(),
                new ImportedAssetProjectionProvider(),
                new MissingAssetFactory(),
                validator)
        {
        }

        public ProjectPersistenceService(
            ISceneEditService edits,
            IProjectWorkspace workspace,
            IProjectAssetStore assetStore,
            ImporterRegistry importers,
            ImportedAssetProjectionProvider importedAssetProjectionProvider,
            MissingAssetFactory missingAssetFactory,
            ProjectDocumentValidator validator = null)
        {
            _edits = edits ?? throw new ArgumentNullException(nameof(edits));
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            _assetStore = assetStore ?? throw new ArgumentNullException(nameof(assetStore));
            _importers = importers ?? throw new ArgumentNullException(nameof(importers));
            _importedAssetProjectionProvider = importedAssetProjectionProvider ?? throw new ArgumentNullException(nameof(importedAssetProjectionProvider));
            _missingAssetFactory = missingAssetFactory ?? throw new ArgumentNullException(nameof(missingAssetFactory));
            _validator = validator ?? new ProjectDocumentValidator();
        }

        public string CurrentProjectRoot => _workspace.RootPath;

        public Task<ProjectOperationResult> SaveAsync(CancellationToken cancellationToken)
        {
            return SaveInternalAsync(_workspace.RootPath, updateWorkspaceRoot: false, cancellationToken);
        }

        public Task<ProjectOperationResult> SaveAsAsync(
            string projectRoot,
            CancellationToken cancellationToken)
        {
            return SaveInternalAsync(projectRoot, updateWorkspaceRoot: true, cancellationToken);
        }

        public async Task<ProjectOperationResult> LoadAsync(
            string projectRoot,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                return Failure("project.load.root.required", "Project root is required.");

            try
            {
                var documentPath = ProjectPaths.DocumentPath(projectRoot);
                if (!File.Exists(documentPath))
                {
                    return Failure(
                        "project.load.document.missing",
                        $"Project document not found at '{documentPath}'.");
                }

                var json = await ReadAllTextAsync(documentPath, cancellationToken);
                var document = ProjectSchemaMigrator.DecodeAndMigrate(json);
                var issues = _validator.Validate(document, projectRoot);
                if (HasErrors(issues))
                    return ResultFromIssues(issues);

                var loadPreparation = await BuildLoadPreparationAsync(document, projectRoot, cancellationToken);
                var previousCache = _importedAssetProjectionProvider.SnapshotCache();

                _importedAssetProjectionProvider.ReplaceCache(loadPreparation.CandidateCache);
                try
                {
                    var replaceResult = _edits.ReplaceScene(loadPreparation.Snapshots);
                    if (!replaceResult.Succeeded)
                    {
                        _importedAssetProjectionProvider.ReplaceCache(previousCache);
                        return Failure(
                            replaceResult.ErrorCode ?? "project.load.scene-replace.failed",
                            replaceResult.Message ?? "Scene replacement failed.");
                    }
                }
                catch
                {
                    _importedAssetProjectionProvider.ReplaceCache(previousCache);
                    throw;
                }

                _workspace.UsePersistentRoot(projectRoot);
                _assetStore.ReplaceCatalog(document.assets?.imported ?? Array.Empty<ProjectAssetDocumentData>());
                return ResultFromIssues(issues);
            }
            catch (ProjectFormatException ex)
            {
                return Failure("project.load.format.invalid", ex.Message);
            }
            catch (Exception ex)
            {
                return Failure("project.load.failed", ex.Message);
            }
        }

        async Task<ProjectOperationResult> SaveInternalAsync(
            string projectRoot,
            bool updateWorkspaceRoot,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                return Failure("project.save.root.required", "Project root is required.");

            try
            {
                var document = ProjectSerializer.CreateDocument(
                    _edits.Registry,
                    _assetStore.GetAll());
                var issues = _validator.Validate(document, projectRoot);
                if (HasErrors(issues))
                    return ResultFromIssues(issues);

                if (updateWorkspaceRoot)
                {
                    await _assetStore.CopyAllToAsync(projectRoot, cancellationToken);
                }

                var json = JsonUtility.ToJson(document, prettyPrint: true);
                await ProjectFileWriter.WriteAtomicAsync(
                    ProjectPaths.DocumentPath(projectRoot),
                    json,
                    cancellationToken);

                if (updateWorkspaceRoot)
                    _workspace.UsePersistentRoot(projectRoot);

                return ResultFromIssues(issues);
            }
            catch (Exception ex)
            {
                return Failure("project.save.failed", ex.Message);
            }
        }

        async Task<LoadPreparation> BuildLoadPreparationAsync(
            ProjectViewerDocument document,
            string projectRoot,
            CancellationToken cancellationToken)
        {
            var candidateCache = new Dictionary<string, ImportResult>(StringComparer.Ordinal);
            var missingAssetIds = new HashSet<string>(StringComparer.Ordinal);
            var assets = document.assets?.imported ?? new List<ProjectAssetDocumentData>();

            foreach (var asset in assets)
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.assetId))
                    continue;

                cancellationToken.ThrowIfCancellationRequested();

                var absolutePath = ProjectPaths.ResolveInsideRoot(projectRoot, asset.relativePath);
                if (!File.Exists(absolutePath))
                {
                    missingAssetIds.Add(asset.assetId);
                    continue;
                }

                var importer = _importers.ResolveById(asset.importerId) ?? _importers.Resolve(absolutePath);
                if (importer == null)
                {
                    throw new InvalidOperationException(
                        $"No importer registered for project asset '{asset.assetId}'.");
                }

                var importResult = await importer.ImportAsync(
                    absolutePath,
                    DecodeImportSettings(asset.importSettingsJson),
                    cancellationToken);
                if (importResult == null || !importResult.Succeeded || importResult.RootObject == null)
                {
                    throw new InvalidOperationException(
                        importResult?.Message ??
                        $"Failed to rebuild imported asset '{asset.assetId}'.");
                }

                importResult.RootObject.AssetId = asset.assetId;
                candidateCache[asset.assetId] = importResult;
            }

            var snapshots = ProjectSerializer.CreateSnapshots(document);
            var resolvedSnapshots = new List<SceneObjectModel>(snapshots.Count);
            foreach (var snapshot in snapshots)
            {
                if (!string.IsNullOrWhiteSpace(snapshot.AssetId) &&
                    missingAssetIds.Contains(snapshot.AssetId))
                {
                    resolvedSnapshots.Add(_missingAssetFactory.Create(snapshot));
                }
                else
                {
                    resolvedSnapshots.Add(snapshot);
                }
            }

            return new LoadPreparation(resolvedSnapshots, candidateCache);
        }

        static ImportSettings DecodeImportSettings(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new ImportSettings();
            if (string.Equals(json.Trim(), "{}", StringComparison.Ordinal))
                return new ImportSettings();

            var data = JsonUtility.FromJson<ImportSettingsData>(json);
            if (data == null)
                return new ImportSettings();

            return new ImportSettings
            {
                UnitScale = data.unitScale,
                GenerateColliders = data.generateColliders,
                PreserveHierarchy = data.preserveHierarchy,
                GenerateMaterials = data.generateMaterials,
                CenterOnImport = data.centerOnImport,
                MaxSourceBytes = data.maxSourceBytes,
                MaxVertices = data.maxVertices,
                MaxIndices = data.maxIndices
            };
        }

        static ImporterRegistry CreateDefaultImporters()
        {
            var registry = new ImporterRegistry();
            registry.Register(new ObjSceneAssetImporter());
            registry.Register(new StlSceneAssetImporter());
            registry.Register(new GltfSceneAssetImporter());
            registry.Freeze();
            return registry;
        }

        static async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                useAsync: true);
            using var reader = new StreamReader(stream);

            cancellationToken.ThrowIfCancellationRequested();
            return await reader.ReadToEndAsync();
        }

        static bool HasErrors(IReadOnlyList<ProjectIssue> issues)
        {
            if (issues == null)
                return false;

            foreach (var issue in issues)
            {
                if (issue.IsError)
                    return true;
            }

            return false;
        }

        static ProjectOperationResult ResultFromIssues(IReadOnlyList<ProjectIssue> issues)
        {
            var issueList = issues ?? Array.Empty<ProjectIssue>();
            return new ProjectOperationResult
            {
                Succeeded = !HasErrors(issueList),
                Issues = issueList
            };
        }

        static ProjectOperationResult Failure(string code, string message) =>
            new()
            {
                Succeeded = false,
                Issues = new List<ProjectIssue>
                {
                    new()
                    {
                        Code = code,
                        Message = message,
                        IsError = true
                    }
                }
            };

        sealed class LoadPreparation
        {
            public LoadPreparation(
                IReadOnlyList<SceneObjectModel> snapshots,
                IReadOnlyDictionary<string, ImportResult> candidateCache)
            {
                Snapshots = snapshots;
                CandidateCache = candidateCache;
            }

            public IReadOnlyList<SceneObjectModel> Snapshots { get; }
            public IReadOnlyDictionary<string, ImportResult> CandidateCache { get; }
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
