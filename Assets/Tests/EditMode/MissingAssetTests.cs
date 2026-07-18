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
using UnitySimulationX.SceneModel.Serialization;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class MissingAssetTests
    {
        readonly List<string> _roots = new();
        readonly List<IProjectWorkspace> _workspaces = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var workspace in _workspaces)
                workspace.Dispose();

            _workspaces.Clear();

            foreach (var root in _roots)
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }

            _roots.Clear();
        }

        [Test]
        public async Task Load_MissingCatalogFile_CreatesMetadataPreservingPlaceholder()
        {
            var projectRoot = CreateRoot("missing-asset-project");
            File.WriteAllText(
                ProjectPaths.DocumentPath(projectRoot),
                JsonUtility.ToJson(DocumentWithMissingAsset(), prettyPrint: true));

            var edits = new RecordingSceneEditService();
            var workspace = CreateWorkspace();
            var importers = new ImporterRegistry();
            importers.Register(new ObjSceneAssetImporter());
            importers.Register(new StlSceneAssetImporter());
            importers.Register(new GltfSceneAssetImporter());
            importers.Freeze();

            var service = new ProjectPersistenceService(
                edits,
                workspace,
                new ProjectAssetStore(workspace),
                importers,
                new ImportedAssetProjectionProvider(),
                new MissingAssetFactory());

            var result = await service.LoadAsync(projectRoot, CancellationToken.None);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, edits.ReplaceSceneCalls);
            Assert.AreEqual(SceneObjectTypeIds.MissingAsset, edits.LastSnapshots[0].TypeId);
            Assert.AreEqual("asset-1", edits.LastSnapshots[0].AssetId);
            Assert.AreEqual("Imported Frame", edits.LastSnapshots[0].Name);
            Assert.AreEqual("com.vendor.product.component", edits.LastSnapshots[0].Components[0].TypeId);
        }

        static ProjectViewerDocument DocumentWithMissingAsset()
        {
            var document = new ProjectViewerDocument();
            document.assets.imported.Add(new ProjectAssetDocumentData
            {
                assetId = "asset-1",
                relativePath = "assets/imported/asset-1.obj",
                originalFileName = "frame.obj",
                mediaType = "model/obj",
                contentHash = "abc123",
                importerId = "obj",
                importerVersion = 1,
                importSettingsJson = "{\"unitScale\":1.0,\"generateColliders\":true,\"preserveHierarchy\":true,\"generateMaterials\":true,\"centerOnImport\":false,\"maxSourceBytes\":536870912,\"maxVertices\":5000000,\"maxIndices\":15000000}"
            });

            document.scene.objects.Add(new SceneObjectDocumentData
            {
                id = "imported-1",
                name = "Imported Frame",
                typeId = SceneObjectTypeIds.ImportedModel.Value,
                assetId = "asset-1",
                transform = new TransformData
                {
                    Position = new Vector3(1f, 2f, 3f)
                },
                components = new List<SceneComponentDocumentData>
                {
                    new()
                    {
                        typeId = "com.vendor.product.component",
                        schemaVersion = 1,
                        payloadJson = "{\"retained\":true}"
                    }
                }
            });

            return document;
        }

        IProjectWorkspace CreateWorkspace()
        {
            var workspace = new ProjectWorkspace();
            _workspaces.Add(workspace);
            return workspace;
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

        sealed class RecordingSceneEditService : ISceneEditService
        {
            readonly SceneRegistry _registry = new();

            public int ReplaceSceneCalls { get; private set; }
            public List<SceneObjectModel> LastSnapshots { get; } = new();

            public ISceneRegistryRead Registry => _registry;

            public SceneEditResult Create(SceneObjectDraft draft) => throw new NotSupportedException();
            public SceneEditResult Remove(string objectId) => throw new NotSupportedException();
            public SceneEditResult Rename(string objectId, string name) => throw new NotSupportedException();
            public SceneEditResult SetVisible(string objectId, bool visible) => throw new NotSupportedException();
            public SceneEditResult SetTransform(string objectId, TransformData transform) => throw new NotSupportedException();
            public SceneEditResult SetMaterial(string objectId, MaterialDefinition material) => throw new NotSupportedException();
            public SceneEditResult Reparent(string objectId, string newParentId) => throw new NotSupportedException();
            public SceneEditResult SetComponent(string objectId, SceneComponentData component) => throw new NotSupportedException();

            public SceneEditResult ReplaceScene(IReadOnlyList<SceneObjectModel> snapshots)
            {
                ReplaceSceneCalls++;
                LastSnapshots.Clear();
                foreach (var snapshot in snapshots)
                    LastSnapshots.Add(snapshot.Clone());

                return new SceneEditResult { Succeeded = true };
            }
        }
    }
}
