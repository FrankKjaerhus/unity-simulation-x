namespace UnitySimulationX.Core
{
    public interface IFileDialogService
    {
        string OpenProjectPath();
        string SaveProjectPath(string currentPath);
        string OpenImportPath();
    }
}
