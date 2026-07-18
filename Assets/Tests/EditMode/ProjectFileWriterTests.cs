using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnitySimulationX.App.ProjectSystem;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ProjectFileWriterTests
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
        public async Task WriteAtomic_ReplacesCompleteDocument()
        {
            var root = CreateRoot();
            var documentPath = Path.Combine(root, ProjectPaths.DocumentFileName);
            File.WriteAllText(documentPath, "{\"schemaVersion\":2,\"scene\":{\"objects\":[]}}");

            const string expected = "{\n  \"schemaVersion\": 2,\n  \"scene\": {\n    \"objects\": [\n      {\n        \"id\": \"root\"\n      }\n    ]\n  }\n}";

            await ProjectFileWriter.WriteAtomicAsync(documentPath, expected, CancellationToken.None);

            Assert.AreEqual(expected, File.ReadAllText(documentPath));
            Assert.IsEmpty(Directory.GetFiles(root, "*.tmp"));
        }

        string CreateRoot()
        {
            var root = Path.Combine(
                Application.temporaryCachePath,
                $"project-file-writer-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            _roots.Add(root);
            return root;
        }
    }
}
