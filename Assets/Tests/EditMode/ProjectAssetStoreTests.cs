using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnitySimulationX.App.ProjectSystem;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel.Serialization;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ProjectAssetStoreTests
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
        public async Task ImportExternalFileAsync_CopiesSourceIntoWorkspaceCatalog()
        {
            var workspace = CreateWorkspace();
            var sourcePath = CreateExternalFile("project-asset-store-source.obj", "v 0 0 0\n");
            var store = new ProjectAssetStore(workspace);

            var entry = await store.ImportExternalFileAsync(
                sourcePath,
                "obj",
                1,
                "{\"unitScale\":1}",
                CancellationToken.None);

            Assert.IsNotNull(entry);
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.assetId));
            Assert.IsFalse(Path.IsPathRooted(entry.relativePath));
            Assert.AreEqual(Path.GetFileName(sourcePath), entry.originalFileName);
            Assert.IsTrue(File.Exists(store.Resolve(entry.assetId)));
            Assert.AreEqual(File.ReadAllText(sourcePath), File.ReadAllText(store.Resolve(entry.assetId)));
        }

        [Test]
        public async Task CopyAllToAsync_CopiesCatalogAssetsToTargetProjectRoot()
        {
            var workspace = CreateWorkspace();
            var sourcePath = CreateExternalFile("project-asset-store-copy.stl", "solid sample\nendsolid sample\n");
            var store = new ProjectAssetStore(workspace);

            var entry = await store.ImportExternalFileAsync(
                sourcePath,
                "stl",
                1,
                "{\"unitScale\":1}",
                CancellationToken.None);

            var targetRoot = CreateRoot("project-asset-store-copy-target");
            await store.CopyAllToAsync(targetRoot, CancellationToken.None);

            var copiedPath = ProjectPaths.ResolveInsideRoot(targetRoot, entry.relativePath);
            Assert.IsTrue(File.Exists(copiedPath));
            Assert.AreEqual(File.ReadAllText(store.Resolve(entry.assetId)), File.ReadAllText(copiedPath));
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

        string CreateExternalFile(string fileName, string contents)
        {
            var root = CreateRoot("project-asset-store-external");
            var path = Path.Combine(root, fileName);
            File.WriteAllText(path, contents);
            return path;
        }
    }
}
