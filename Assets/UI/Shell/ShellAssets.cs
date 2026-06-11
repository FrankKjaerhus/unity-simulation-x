using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnitySimulationX.UI.Shell
{
    public static class ShellAssets
    {
        public const string ShellUxmlPath = "Assets/UI/Shell/ViewerShell.uxml";
        public const string ShellUssPath = "Assets/UI/Shell/ViewerShell.uss";
        public const string PanelSettingsPath = "Assets/UI/Shell/ViewerPanelSettings.asset";

        public static VisualTreeAsset LoadShellLayout(VisualTreeAsset assigned)
        {
#if UNITY_EDITOR
            var fromPath = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
            if (IsUsableLayout(fromPath))
                return fromPath;
#endif

            if (IsUsableLayout(assigned))
                return assigned;

#if !UNITY_EDITOR
            return Resources.Load<VisualTreeAsset>("ViewerShell");
#else
            return null;
#endif
        }

        public static StyleSheet LoadShellStylesheet()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(ShellUssPath);
#else
            return Resources.Load<StyleSheet>("ViewerShell");
#endif
        }

        public static PanelSettings LoadPanelSettings(PanelSettings assigned)
        {
#if UNITY_EDITOR
            var fromPath = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (fromPath != null)
                return fromPath;
#endif

            return assigned;
        }

        public static bool IsUsableLayout(VisualTreeAsset layout)
        {
            if (layout == null)
                return false;

            var instance = layout.Instantiate();
            return instance != null && instance.Q("hierarchy-panel") != null;
        }
    }
}
