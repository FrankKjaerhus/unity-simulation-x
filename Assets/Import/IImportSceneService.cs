using System.Threading.Tasks;

namespace UnitySimulationX.Import
{
    public interface IImportSceneService
    {
        Task ImportFileAsync(string path);
    }
}
