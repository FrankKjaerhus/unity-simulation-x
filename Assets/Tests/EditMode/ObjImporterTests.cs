using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitySimulationX.Import;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ObjImporterTests
    {
        [Test]
        public async Task ImportAsync_ReadsTriangleMesh()
        {
            var path = Path.Combine(Path.GetTempPath(), "unity-simulation-x-triangle.obj");
            File.WriteAllText(path, "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n");

            try
            {
                var importer = new ObjSceneAssetImporter();
                var result = await importer.ImportAsync(path, new ImportSettings(), CancellationToken.None);

                Assert.IsTrue(result.Succeeded);
                Assert.AreEqual(1, result.Meshes.Count);
                Assert.AreEqual(3, result.Meshes[0].Vertices.Length);
                Assert.AreEqual(3, result.Meshes[0].Triangles.Length);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
