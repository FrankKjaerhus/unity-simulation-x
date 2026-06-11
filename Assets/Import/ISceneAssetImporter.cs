using System.Threading.Tasks;

namespace UnitySimulationX.Import
{
    public interface ISceneAssetImporter
    {
        bool CanImport(string fileExtension);
        Task<ImportResult> ImportAsync(string filePath, ImportSettings settings);
    }
}
