using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitySimulationX.Import;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ImportLimitTests
    {
        [Test]
        public async Task ObjImportAsync_SourceExceedsLimit_ReturnsTypedFailure()
        {
            var path = Path.Combine(Path.GetTempPath(), "unity-simulation-x-limit.obj");
            File.WriteAllText(path, "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n");

            try
            {
                var importer = new ObjSceneAssetImporter();
                var result = await importer.ImportAsync(
                    path,
                    new ImportSettings { MaxSourceBytes = 1 },
                    CancellationToken.None);

                Assert.IsFalse(result.Succeeded);
                Assert.AreEqual("import.source.too-large", result.ErrorCode);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public async Task StlImportAsync_VertexLimitExceeded_ReturnsTypedFailure()
        {
            var path = Path.Combine(Path.GetTempPath(), "unity-simulation-x-limit.stl");
            File.WriteAllText(
                path,
                "solid sample\n" +
                "facet normal 0 0 1\n" +
                "outer loop\n" +
                "vertex 0 0 0\n" +
                "vertex 1 0 0\n" +
                "vertex 0 1 0\n" +
                "endloop\n" +
                "endfacet\n" +
                "endsolid sample\n");

            try
            {
                var importer = new StlSceneAssetImporter();
                var result = await importer.ImportAsync(
                    path,
                    new ImportSettings { MaxVertices = 2 },
                    CancellationToken.None);

                Assert.IsFalse(result.Succeeded);
                Assert.AreEqual("import.mesh.vertices.exceeded", result.ErrorCode);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public async Task GltfImportAsync_ReturnsAdapterUnavailableFailure()
        {
            var path = Path.Combine(Path.GetTempPath(), "unity-simulation-x-limit.glb");
            File.WriteAllBytes(path, new byte[] { 0x67, 0x6c, 0x54, 0x46 });

            try
            {
                var importer = new GltfSceneAssetImporter();
                var result = await importer.ImportAsync(
                    path,
                    new ImportSettings(),
                    CancellationToken.None);

                Assert.IsFalse(result.Succeeded);
                Assert.AreEqual("import.glb.adapter-unavailable", result.ErrorCode);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
