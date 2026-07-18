using System;
using System.IO;
using UnityEngine;
using UnitySimulationX.Core;

namespace UnitySimulationX.App.ProjectSystem
{
    public sealed class ProjectWorkspace : IProjectWorkspace
    {
        readonly string _temporaryRootPath;
        bool _disposed;

        public ProjectWorkspace()
        {
            _temporaryRootPath = Path.Combine(
                Application.temporaryCachePath,
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_temporaryRootPath);

            RootPath = _temporaryRootPath;
            IsTemporary = true;
        }

        public string RootPath { get; private set; }

        public bool IsTemporary { get; private set; }

        public void UsePersistentRoot(string rootPath)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("Project root is required.", nameof(rootPath));

            var fullRootPath = Path.GetFullPath(rootPath);
            Directory.CreateDirectory(fullRootPath);

            RootPath = fullRootPath;
            IsTemporary = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (Directory.Exists(_temporaryRootPath))
                Directory.Delete(_temporaryRootPath, recursive: true);
        }

        void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ProjectWorkspace));
        }
    }
}
