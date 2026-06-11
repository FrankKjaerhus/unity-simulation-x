namespace UnitySimulationX.Core
{
    public interface IProjectPersistenceService
    {
        string CurrentPath { get; }
        void Save(string path);
        void Load(string path);
    }
}
