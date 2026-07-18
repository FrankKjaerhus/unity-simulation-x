namespace UnitySimulationX.Core
{
    public interface IProjectPersistenceService
    {
        string CurrentProjectRoot { get; }
        System.Threading.Tasks.Task<ProjectOperationResult> SaveAsync(
            System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<ProjectOperationResult> SaveAsAsync(
            string projectRoot,
            System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<ProjectOperationResult> LoadAsync(
            string projectRoot,
            System.Threading.CancellationToken cancellationToken);
    }
}
