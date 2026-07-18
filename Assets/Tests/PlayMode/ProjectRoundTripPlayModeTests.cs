using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnitySimulationX.App.ProjectSystem;
using UnitySimulationX.Core;
using UnitySimulationX.Editing;
using UnitySimulationX.Import;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer.Projection;
using UnitySimulationX.Viewer.Selection;

namespace UnitySimulationX.Tests.PlayMode
{
    public sealed class ProjectRoundTripPlayModeTests
    {
        readonly List<string> _roots = new();
        readonly List<RoundTripHarness> _harnesses = new();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var harness in _harnesses)
                harness.Dispose();

            _harnesses.Clear();

            yield return null;

            foreach (var root in _roots)
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }

            _roots.Clear();
        }

        [UnityTest]
        public IEnumerator ImportEditSaveLoadAndMissingAssetReload_RoundTripsCompleteEditorStory()
        {
            var externalObjPath = CreateExternalObj("triangle.obj");
            var projectRoot = CreateRoot("playmode-project-round-trip");

            var saver = CreateHarness("ProjectRoundTripSaverRoot");
            var importTask = saver.ImportService.ImportFileAsync(externalObjPath, CancellationToken.None);
            yield return WaitForTask(importTask);

            Assert.IsTrue(importTask.Result.Succeeded, importTask.Result.Message);
            var importedObjectId = importTask.Result.ObjectId;
            var importedBeforeSave = saver.Edits.Registry.Get(importedObjectId);
            Assert.IsNotNull(importedBeforeSave);

            var groupId = CreateGroup(saver, "Imported Assets");
            var targetTransform = new TransformData
            {
                Position = new Vector3(3f, 1.5f, -2f),
                RotationEuler = new Vector3(10f, 25f, 5f),
                Scale = new Vector3(2f, 2f, 2f)
            };
            var targetMaterial = new MaterialDefinition
            {
                BaseColor = new Color(0.2f, 0.4f, 0.8f, 1f),
                Metallic = 0.15f,
                Roughness = 0.65f
            };

            AssertSceneEditSucceeded(saver.Edits.Rename(importedObjectId, "Imported Triangle"));
            AssertSceneEditSucceeded(saver.Edits.SetTransform(importedObjectId, targetTransform));
            AssertSceneEditSucceeded(saver.Edits.SetMaterial(importedObjectId, targetMaterial));
            AssertSceneEditSucceeded(saver.Edits.Reparent(importedObjectId, groupId));

            saver.Selection.Select(importedObjectId);
            saver.Selection.Clear();

            var saveTask = saver.Persistence.SaveAsAsync(projectRoot, CancellationToken.None);
            yield return WaitForTask(saveTask);
            Assert.IsTrue(saveTask.Result.Succeeded);

            yield return null;

            var loader = CreateHarness("ProjectRoundTripLoaderRoot");
            loader.Selection.Clear();
            var loadTask = loader.Persistence.LoadAsync(projectRoot, CancellationToken.None);
            yield return WaitForTask(loadTask);
            Assert.IsTrue(loadTask.Result.Succeeded);

            yield return null;

            var loadedModel = loader.Edits.Registry.Get(importedObjectId);
            Assert.IsNotNull(loadedModel);
            Assert.AreEqual("Imported Triangle", loadedModel.Name);
            Assert.AreEqual(groupId, loadedModel.ParentId);
            AssertVector3Equal(targetTransform.Position, loadedModel.Transform.Position);
            AssertVector3Equal(targetTransform.RotationEuler, loadedModel.Transform.RotationEuler);
            AssertVector3Equal(targetTransform.Scale, loadedModel.Transform.Scale);
            AssertColorEqual(targetMaterial.BaseColor, loadedModel.Material.BaseColor);
            Assert.AreEqual(targetMaterial.Metallic, loadedModel.Material.Metallic, 0.001f);
            Assert.AreEqual(targetMaterial.Roughness, loadedModel.Material.Roughness, 0.001f);

            var groupGameObject = loader.Projection.GetGameObject(groupId);
            var loadedGameObject = loader.Projection.GetGameObject(importedObjectId);
            Assert.IsNotNull(groupGameObject);
            Assert.IsNotNull(loadedGameObject);
            Assert.AreEqual(groupGameObject.transform, loadedGameObject.transform.parent);
            Assert.IsNotNull(loadedGameObject.GetComponent<MeshFilter>());
            Assert.IsNotNull(loadedGameObject.GetComponent<MeshFilter>().sharedMesh);
            Assert.IsNotNull(loadedGameObject.GetComponent<MeshCollider>());
            Assert.IsNotNull(loadedGameObject.GetComponent<SceneObjectIdComponent>());
            Assert.AreEqual(importedObjectId, loader.Projection.GetObjectId(loadedGameObject));

            var entry = loader.AssetStore.Get(loadedModel.AssetId);
            Assert.IsNotNull(entry);
            Assert.IsFalse(Path.IsPathRooted(entry.relativePath));
            var assetPath = ProjectPaths.ResolveInsideRoot(projectRoot, entry.relativePath);
            Assert.IsTrue(File.Exists(assetPath));
            AssertPickable(loadedGameObject);

            loader.Selection.Select(importedObjectId);
            Assert.IsTrue(loader.Selection.IsSelected(importedObjectId));

            File.Delete(assetPath);
            Assert.IsFalse(File.Exists(assetPath));

            loader.Selection.Clear();
            var missingLoadTask = loader.Persistence.LoadAsync(projectRoot, CancellationToken.None);
            yield return WaitForTask(missingLoadTask);
            Assert.IsTrue(missingLoadTask.Result.Succeeded);

            yield return null;

            var missingModel = loader.Edits.Registry.Get(importedObjectId);
            Assert.IsNotNull(missingModel);
            Assert.AreEqual(SceneObjectTypeIds.MissingAsset, missingModel.TypeId);
            Assert.AreEqual(importedObjectId, missingModel.Id);
            Assert.AreEqual("Imported Triangle", missingModel.Name);
            Assert.AreEqual(groupId, missingModel.ParentId);
            Assert.AreEqual(importedBeforeSave.AssetId, missingModel.AssetId);

            var placeholder = loader.Projection.GetGameObject(importedObjectId);
            Assert.IsNotNull(placeholder);
            Assert.IsNotNull(placeholder.GetComponent<MeshFilter>());
            Assert.IsNotNull(placeholder.GetComponent<MeshFilter>().sharedMesh);
            Assert.IsNotNull(placeholder.GetComponent<Collider>());
            Assert.IsNotNull(placeholder.GetComponent<Renderer>());
            Assert.IsTrue(placeholder.GetComponent<Renderer>().enabled);
            AssertPickable(placeholder);

            loader.Selection.Select(importedObjectId);
            Assert.IsTrue(loader.Selection.IsSelected(importedObjectId));
        }

        RoundTripHarness CreateHarness(string sceneRootName)
        {
            var harness = new RoundTripHarness(sceneRootName);
            _harnesses.Add(harness);
            return harness;
        }

        string CreateGroup(RoundTripHarness harness, string name)
        {
            var draft = new SceneObjectDraft
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                TypeId = SceneObjectTypeIds.Group
            };

            var result = harness.Edits.Create(draft);
            AssertSceneEditSucceeded(result);
            return draft.Id;
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
            var root = CreateRoot("playmode-external-obj");
            var path = Path.Combine(root, fileName);
            File.WriteAllText(
                path,
                "# triangle fixture\n" +
                "v 0 0 0\n" +
                "v 1 0 0\n" +
                "v 0 1 0\n" +
                "f 1 2 3\n");
            return path;
        }

        static IEnumerator WaitForTask(Task task)
        {
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                throw task.Exception?.InnerException ?? task.Exception;
        }

        static void AssertSceneEditSucceeded(SceneEditResult result)
        {
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded, result.Message ?? result.ErrorCode);
        }

        static void AssertPickable(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            Assert.IsNotNull(collider);

            var bounds = collider.bounds;
            var rayOrigin = bounds.center + (Vector3.back * 5f);
            var ray = new Ray(rayOrigin, Vector3.forward);
            Assert.IsTrue(collider.Raycast(ray, out _, 20f));
        }

        static void AssertVector3Equal(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f);
            Assert.AreEqual(expected.y, actual.y, 0.001f);
            Assert.AreEqual(expected.z, actual.z, 0.001f);
        }

        static void AssertColorEqual(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.001f);
            Assert.AreEqual(expected.g, actual.g, 0.001f);
            Assert.AreEqual(expected.b, actual.b, 0.001f);
            Assert.AreEqual(expected.a, actual.a, 0.001f);
        }

        sealed class RoundTripHarness : IDisposable
        {
            readonly GameObject _sceneRoot;

            public RoundTripHarness(string sceneRootName)
            {
                Workspace = new ProjectWorkspace();
                AssetStore = new ProjectAssetStore(Workspace);
                ImportedAssetProvider = new ImportedAssetProjectionProvider();
                Registry = new SceneRegistry();
                _sceneRoot = new GameObject(sceneRootName);
                Projection = new SceneProjectionService(_sceneRoot.transform, Registry, ImportedAssetProvider);
                EventBus = new EventBus(_ => { });
                Edits = new SceneEditService(Registry, Projection, EventBus);
                Selection = new SelectionService(Registry, EventBus);

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

            public ProjectWorkspace Workspace { get; }
            public ProjectAssetStore AssetStore { get; }
            public ImportedAssetProjectionProvider ImportedAssetProvider { get; }
            public SceneRegistry Registry { get; }
            public SceneProjectionService Projection { get; }
            public EventBus EventBus { get; }
            public SceneEditService Edits { get; }
            public SelectionService Selection { get; }
            public ImportSceneService ImportService { get; }
            public ProjectPersistenceService Persistence { get; }

            public void Dispose()
            {
                if (_sceneRoot != null)
                    UnityEngine.Object.Destroy(_sceneRoot);

                Workspace.Dispose();
            }
        }
    }
}
