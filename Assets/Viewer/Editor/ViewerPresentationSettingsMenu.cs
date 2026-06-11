#if UNITY_EDITOR
using UnityEditor;
using UnitySimulationX.Viewer.Settings;

namespace UnitySimulationX.Viewer.Editor
{
    static class ViewerPresentationSettingsMenu
    {
        [MenuItem("Tools/Unity Simulation X/Viewer Presentation Settings")]
        static void SelectSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ViewerPresentationSettings>(
                ViewerSettingsUtility.DefaultSettingsPath);

            if (settings == null)
            {
                EditorUtility.DisplayDialog(
                    "Viewer Presentation Settings",
                    $"Could not find settings asset at:\n{ViewerSettingsUtility.DefaultSettingsPath}",
                    "OK");
                return;
            }

            UnityEditor.Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}
#endif
