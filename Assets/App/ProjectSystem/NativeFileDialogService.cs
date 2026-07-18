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
        const string LastProjectRootKey = "UnitySimulationX.LastProjectRoot";

        public string OpenProjectFolder()
        {
#if UNITY_EDITOR
            var directory = PlayerPrefs.GetString(LastProjectRootKey, DefaultProjectRoot());
            return EditorUtility.OpenFolderPanel("Load Unity Simulation X Project", directory, string.Empty);
#else
            return PlayerPrefs.GetString(LastProjectRootKey, DefaultProjectRoot());
#endif
        }

        public string SaveProjectFolder(string currentProjectRoot)
        {
#if UNITY_EDITOR
            var directory = string.IsNullOrEmpty(currentProjectRoot)
                ? DefaultProjectRoot()
                : currentProjectRoot;
            return EditorUtility.OpenFolderPanel("Save Unity Simulation X Project", directory, string.Empty);
#else
            var root = string.IsNullOrEmpty(currentProjectRoot)
                ? DefaultProjectRoot()
                : currentProjectRoot;
            PlayerPrefs.SetString(LastProjectRootKey, root);
            return root;
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

        static string DefaultProjectRoot()
        {
            return Path.Combine(Application.persistentDataPath, "Project");
        }
    }
}
