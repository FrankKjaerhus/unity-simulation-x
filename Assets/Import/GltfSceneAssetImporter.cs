using System.Threading;
using System.Threading.Tasks;

namespace UnitySimulationX.Import
{
    public sealed class GltfSceneAssetImporter : ISceneAssetImporter
    {
        public string ImporterId => "gltf";
        public int ImporterVersion => 1;

        public bool CanImport(string fileExtension)
        {
            return fileExtension == ".glb" || fileExtension == ".gltf";
        }

        public Task<ImportResult> ImportAsync(
            string filePath,
            ImportSettings settings,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ImportResult
            {
                Succeeded = false,
                ErrorCode = "import.glb.adapter-unavailable",
                Message = "GLB import requires the planned GLB adapter package."
            });
        }
    }
}
