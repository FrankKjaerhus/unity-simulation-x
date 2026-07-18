using System.IO;
using UnityEngine;
using System;
using UnitySimulationX.Core;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel.Serialization;

namespace UnitySimulationX.App.ProjectSystem
{
    public sealed class ProjectPersistenceService : IProjectPersistenceService
    {
        readonly ISceneEditService _edits;

        public ProjectPersistenceService(ISceneEditService edits)
        {
            _edits = edits;
        }

        public string CurrentPath { get; private set; }

        public void Save(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var document = ProjectSerializer.CreateDocument(_edits.Registry, Array.Empty<ProjectAssetDocumentData>());
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
            var document = ProjectSchemaMigrator.DecodeAndMigrate(json);
            var snapshots = ProjectSerializer.CreateSnapshots(document);
            _edits.ReplaceScene(snapshots);
            CurrentPath = path;
            Debug.Log($"Loaded project: {path}");
        }
    }
}
