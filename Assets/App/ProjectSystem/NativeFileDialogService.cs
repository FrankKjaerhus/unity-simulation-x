using System.IO;
using UnityEngine;
using UnitySimulationX.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnitySimulationX.App.ProjectSystem
{
    public sealed class NativeFileDialogService : IFileDialogService
    {
        const string ProjectExtension = "viewer.json";

        public string OpenProjectPath()
        {
#if UNITY_EDITOR
            return EditorUtility.OpenFilePanel("Load Unity Simulation X Project", Application.dataPath, "json");
#else
            return PlayerPrefs.GetString("UnitySimulationX.LastProjectPath", DefaultProjectPath());
#endif
        }

        public string SaveProjectPath(string currentPath)
        {
#if UNITY_EDITOR
            var directory = string.IsNullOrEmpty(currentPath) ? Application.dataPath : Path.GetDirectoryName(currentPath);
            var fileName = string.IsNullOrEmpty(currentPath) ? "project.viewer.json" : Path.GetFileName(currentPath);
            return EditorUtility.SaveFilePanel("Save Unity Simulation X Project", directory, fileName, "json");
#else
            var path = string.IsNullOrEmpty(currentPath) ? DefaultProjectPath() : currentPath;
            PlayerPrefs.SetString("UnitySimulationX.LastProjectPath", path);
            return path;
#endif
        }

        public string OpenImportPath()
        {
#if UNITY_EDITOR
            return EditorUtility.OpenFilePanel("Import 3D File", Application.dataPath, "glb,gltf,obj,stl");
#else
            return string.Empty;
#endif
        }

        static string DefaultProjectPath()
        {
            return Path.Combine(Application.persistentDataPath, ProjectExtension);
        }
    }
}
