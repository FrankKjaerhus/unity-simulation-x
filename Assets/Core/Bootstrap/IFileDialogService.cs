namespace UnitySimulationX.Core
{
    public interface IFileDialogService
    {
        string OpenProjectFolder();
        string SaveProjectFolder(string currentProjectRoot);
        string OpenImportPath();
    }
}
