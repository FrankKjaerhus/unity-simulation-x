using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnitySimulationX.App.ProjectSystem;
using UnitySimulationX.Core;
using UnitySimulationX.Editing;
using UnitySimulationX.Import;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer.Projection;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ImportedAssetRoundTripTests
    {
        readonly List<string> _roots = new();
        readonly List<ImportHarness> _harnesses = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var harness in _harnesses)
                harness.Dispose();

            _harnesses.Clear();

            foreach (var root in _roots)
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }

            _roots.Clear();
        }

        [Test]
        public async Task ImportObj_CopiesSourceAndStoresOnlyRelativeReference()
        {
            var harness = CreateHarness();
            var externalObjPath = CreateExternalObj("import-relative-reference.obj");

            var imported = await harness.ImportService.ImportFileAsync(externalObjPath, CancellationToken.None);

            Assert.IsTrue(imported.Succeeded);
            var model = harness.Edits.Registry.Get(imported.ObjectId);
            var entry = harness.AssetStore.Get(model.AssetId);
            Assert.IsFalse(Path.IsPathRooted(entry.relativePath));
            Assert.IsTrue(File.Exists(ProjectPaths.ResolveInsideRoot(harness.Workspace.RootPath, entry.relativePath)));
            Assert.AreNotEqual(externalObjPath, entry.relativePath);
        }

        [Test]
        public async Task SaveAndLoad_ImportedObj_RebuildsProjectionMeshFromCatalog()
        {
            var saver = CreateHarness();
            var externalObjPath = CreateExternalObj("import-round-trip.obj");
            var imported = await saver.ImportService.ImportFileAsync(externalObjPath, CancellationToken.None);
            var projectRoot = CreateRoot("import-round-trip-project");

            var saveResult = await saver.Persistence.SaveAsAsync(projectRoot, CancellationToken.None);

            Assert.IsTrue(imported.Succeeded);
            Assert.IsTrue(saveResult.Succeeded);

            var loader = CreateHarness();
            var loadResult = await loader.Persistence.LoadAsync(projectRoot, CancellationToken.None);

            Assert.IsTrue(loadResult.Succeeded);
            var loaded = loader.Edits.Registry.Get(imported.ObjectId);
            Assert.IsNotNull(loaded);
            Assert.IsFalse(string.IsNullOrWhiteSpace(loaded.AssetId));

            var entry = loader.AssetStore.Get(loaded.AssetId);
            Assert.IsNotNull(entry);
            Assert.IsFalse(Path.IsPathRooted(entry.relativePath));

            var gameObject = loader.Projection.GetGameObject(imported.ObjectId);
            Assert.IsNotNull(gameObject);
            Assert.IsNotNull(gameObject.GetComponent<MeshFilter>());
            Assert.IsNotNull(gameObject.GetComponent<MeshFilter>().sharedMesh);
            Assert.IsNotNull(gameObject.GetComponent<MeshCollider>());
        }

        ImportHarness CreateHarness()
        {
            var harness = new ImportHarness();
            _harnesses.Add(harness);
            return harness;
        }

        string CreateRoot(string prefix)
        {
            var root = Path.Combine(
                Application.temporaryCachePath,
                $"{prefix}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            _roots.Add(root);
            return root;
        }

        string CreateExternalObj(string fileName)
        {
            var root = CreateRoot("import-external-obj");
            var path = Path.Combine(root, fileName);
            File.WriteAllText(path, "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n");
            return path;
        }

        sealed class ImportHarness : IDisposable
        {
            readonly GameObject _sceneRoot;
            readonly EventBus _eventBus;

            public ImportHarness()
            {
                Workspace = new ProjectWorkspace();
                AssetStore = new ProjectAssetStore(Workspace);
                ImportedAssetProvider = new ImportedAssetProjectionProvider();
                Registry = new SceneRegistry();
                _sceneRoot = new GameObject("ImportedAssetRoundTripRoot");
                Projection = new SceneProjectionService(_sceneRoot.transform, Registry, ImportedAssetProvider);
                _eventBus = new EventBus(_ => { });
                Edits = new SceneEditService(Registry, Projection, _eventBus);

                var importers = new ImporterRegistry();
                importers.Register(new ObjSceneAssetImporter());
                importers.Register(new StlSceneAssetImporter());
                importers.Register(new GltfSceneAssetImporter());
                importers.Freeze();

                ImportService = new ImportSceneService(importers, Edits, AssetStore, ImportedAssetProvider);
                Persistence = new ProjectPersistenceService(
                    Edits,
                    Workspace,
                    AssetStore,
                    importers,
                    ImportedAssetProvider,
                    new MissingAssetFactory());
            }

            public IProjectWorkspace Workspace { get; }
            public ProjectAssetStore AssetStore { get; }
            public ImportedAssetProjectionProvider ImportedAssetProvider { get; }
            public SceneRegistry Registry { get; }
            public SceneProjectionService Projection { get; }
            public SceneEditService Edits { get; }
            public ImportSceneService ImportService { get; }
            public ProjectPersistenceService Persistence { get; }

            public void Dispose()
            {
                if (_sceneRoot != null)
                    UnityEngine.Object.DestroyImmediate(_sceneRoot);

                Workspace.Dispose();
            }
        }
    }
}
