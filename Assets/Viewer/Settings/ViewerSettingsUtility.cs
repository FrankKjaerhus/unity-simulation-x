using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnitySimulationX.Viewer.Settings
{
    public static class ViewerSettingsUtility
    {
        public const string DefaultSettingsPath = "Assets/Viewer/Settings/ViewerPresentationSettings.asset";

        public static ViewerPresentationSettings LoadDefault(ViewerPresentationSettings assigned)
        {
            if (assigned != null)
                return assigned;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<ViewerPresentationSettings>(DefaultSettingsPath);
#else
            return Resources.Load<ViewerPresentationSettings>("ViewerPresentationSettings");
#endif
        }
    }
}
