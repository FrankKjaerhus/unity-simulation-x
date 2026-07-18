using System.IO;
using UnityEngine;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;
using UnitySimulationX.SceneModel.Serialization;
using UnitySimulationX.Viewer.Projection;

namespace UnitySimulationX.App.ProjectSystem
{
    public sealed class ProjectPersistenceService : IProjectPersistenceService
    {
        readonly SceneRegistry _registry;
        readonly ISceneProjectionService _projection;

        public ProjectPersistenceService(SceneRegistry registry, ISceneProjectionService projection)
        {
            _registry = registry;
            _projection = projection;
        }

        public string CurrentPath { get; private set; }

        public void Save(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var document = ProjectSerializer.CreateDocument(_registry);
            var json = JsonUtility.ToJson(document, prettyPrint: true);
            File.WriteAllText(path, json);
            CurrentPath = path;
            Debug.Log($"Saved project: {path}");
        }

        public void Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            var json = File.ReadAllText(path);
            var document = JsonUtility.FromJson<ProjectViewerDocument>(json);
            ProjectSerializer.ApplyDocument(document, _registry, _projection);
            CurrentPath = path;

            EventBus.Publish(new HierarchyChangedEvent());
            Debug.Log($"Loaded project: {path}");
        }
    }
}
