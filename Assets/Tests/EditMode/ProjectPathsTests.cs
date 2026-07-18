using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnitySimulationX.App.ProjectSystem;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ProjectPathsTests
    {
        readonly List<string> _roots = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var root in _roots)
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }

            _roots.Clear();
        }

        [Test]
        public void ToAbsolutePath_RejectsParentTraversal()
        {
            var root = CreateRoot();

            Assert.Throws<ArgumentException>(() =>
                ProjectPaths.ResolveInsideRoot(root, "../outside/project.viewer.json"));
        }

        string CreateRoot()
        {
            var root = Path.Combine(
                Application.temporaryCachePath,
                $"project-paths-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            _roots.Add(root);
            return root;
        }
    }
}
