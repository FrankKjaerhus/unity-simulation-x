using System.Threading.Tasks;

namespace UnitySimulationX.Import
{
    public interface ISceneAssetImporter
    {
        string ImporterId { get; }
        bool CanImport(string fileExtension);
        Task<ImportResult> ImportAsync(string filePath, ImportSettings settings);
    }
}
