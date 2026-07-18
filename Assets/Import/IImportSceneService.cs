using System.Threading;
using System.Threading.Tasks;

namespace UnitySimulationX.Import
{
    public interface IImportSceneService
    {
        Task<ImportOperationResult> ImportFileAsync(
            string path,
            CancellationToken cancellationToken);
    }
}
