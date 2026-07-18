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
using UnitySimulationX.SceneModel;
using UnitySimulationX.SceneModel.Serialization;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ProjectPersistenceServiceTests
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
        public async Task Load_InvalidDocument_DoesNotCallReplaceScene()
        {
            var projectRoot = CreateRoot("project-persistence-load-invalid");
            var documentPath = Path.Combine(projectRoot, ProjectPaths.DocumentFileName);
            File.WriteAllText(documentPath, CreateInvalidDuplicateIdJson());

            var edits = new RecordingSceneEditService();
            var workspace = CreateWorkspace();
            var service = new ProjectPersistenceService(edits, workspace);

            var result = await service.LoadAsync(projectRoot, CancellationToken.None);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(0, edits.ReplaceSceneCalls);
            Assert.AreEqual(workspace.RootPath, service.CurrentProjectRoot);
        }

        [Test]
        public async Task Save_Failure_DoesNotChangeCurrentProjectRoot()
        {
            var workspace = CreateWorkspace();
            var edits = new RecordingSceneEditService();
            edits.RegistryData.Add(new SceneObjectModel
            {
                Id = "root",
                Name = "Root",
                TypeId = SceneObjectTypeIds.Group
            });

            var service = new ProjectPersistenceService(edits, workspace);
            var initialRoot = service.CurrentProjectRoot;
            var failingRoot = CreateRoot("project-persistence-save-failure");
            Directory.CreateDirectory(Path.Combine(failingRoot, ProjectPaths.DocumentFileName));

            var result = await service.SaveAsAsync(failingRoot, CancellationToken.None);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(initialRoot, service.CurrentProjectRoot);
        }

        static string CreateInvalidDuplicateIdJson()
        {
            var document = new ProjectViewerDocument();
            document.scene.objects.Add(new SceneObjectDocumentData
            {
                id = "duplicate",
                name = "Root",
                typeId = "com.unitysimulationx.scene.group"
            });
            document.scene.objects.Add(new SceneObjectDocumentData
            {
                id = "duplicate",
                name = "Other",
                typeId = "com.unitysimulationx.scene.group"
            });

            return JsonUtility.ToJson(document, prettyPrint: true);
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

            public List<SceneObjectModel> RegistryData { get; } = new();

            public int ReplaceSceneCalls { get; private set; }

            public ISceneRegistryRead Registry
            {
                get
                {
                    _registry.ReplaceAll(RegistryData);
                    return _registry;
                }
            }

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
                return new SceneEditResult { Succeeded = true };
            }
        }
    }
}
