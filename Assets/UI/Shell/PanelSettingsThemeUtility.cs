using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnitySimulationX.UI.Shell
{
    static class PanelSettingsThemeUtility
    {
        const string ProjectThemePath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss";

        public static void EnsureTheme(PanelSettings settings)
        {
            if (settings == null || settings.themeStyleSheet != null)
                return;

            var theme = LoadDefaultTheme();
            if (theme != null)
                settings.themeStyleSheet = theme;
        }

        public static ThemeStyleSheet LoadDefaultTheme()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ProjectThemePath);
#else
            return Resources.Load<ThemeStyleSheet>("UnityDefaultRuntimeTheme");
#endif
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void FixViewerPanelSettingsOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(ShellAssets.PanelSettingsPath);
                if (settings != null)
                {
                    var theme = LoadDefaultTheme();
                    if (theme != null && settings.themeStyleSheet != theme)
                    {
                        settings.themeStyleSheet = theme;
                        EditorUtility.SetDirty(settings);
                    }
                }

                ValidateShellLayoutImport();
                AssetDatabase.SaveAssets();
            };
        }

        static void ValidateShellLayoutImport()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellAssets.ShellUxmlPath);
            if (layout == null)
            {
                Debug.LogError("ViewerShell.uxml failed to import. Right-click the file and choose Reimport.");
                return;
            }

            if (ShellAssets.IsUsableLayout(layout))
                return;

            AssetDatabase.ImportAsset(ShellAssets.ShellUxmlPath, ImportAssetOptions.ForceUpdate);
            layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellAssets.ShellUxmlPath);
            if (layout != null && ShellAssets.IsUsableLayout(layout))
                return;

            Debug.LogError(
                "ViewerShell.uxml imported but hierarchy-panel is missing after Instantiate. Check UXML for invalid XML.");
        }
#endif
    }
}
