using System;

namespace UnitySimulationX.Core
{
    public interface IProjectWorkspace : IDisposable
    {
        string RootPath { get; }
        bool IsTemporary { get; }
        void UsePersistentRoot(string rootPath);
    }
}
