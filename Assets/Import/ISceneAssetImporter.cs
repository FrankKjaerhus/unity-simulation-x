using System.Threading;
using System.Threading.Tasks;

namespace UnitySimulationX.Import
{
    public interface ISceneAssetImporter
    {
        string ImporterId { get; }
        int ImporterVersion { get; }
        bool CanImport(string fileExtension);
        Task<ImportResult> ImportAsync(
            string filePath,
            ImportSettings settings,
            CancellationToken cancellationToken);
    }
}
