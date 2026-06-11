using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnitySimulationX.UI.Hierarchy;
using UnitySimulationX.UI.Library;
using UnitySimulationX.UI.Properties;
using UnitySimulationX.UI.Shell;
using UnitySimulationX.Viewer;

namespace UnitySimulationX.UI
{
    /// <summary>
    /// Runs before UIDocument OnEnable so PanelSettings has a theme when the panel is created.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class UIDocumentHost : MonoBehaviour
    {
        [SerializeField] VisualTreeAsset shellLayout;

        ShellLayoutController _layout;
        ToolbarController _toolbar;
        ViewportOverlayController _overlay;
        HierarchyPanelController _hierarchy;
        LibraryPanelController _library;
        PropertiesPanelController _properties;
        bool _initialized;
        bool _initializing;

        void Awake()
        {
            var document = GetComponent<UIDocument>();
            if (document == null)
                return;

            var panelSettings = ShellAssets.LoadPanelSettings(document.panelSettings);
            if (panelSettings != null && document.panelSettings != panelSettings)
                document.panelSettings = panelSettings;

            PanelSettingsThemeUtility.EnsureTheme(document.panelSettings);
        }

        void Start()
        {
            StartCoroutine(InitializeWhenReady());
        }

        public void Initialize()
        {
            if (!_initialized)
                StartCoroutine(InitializeWhenReady());
        }

        IEnumerator InitializeWhenReady()
        {
            if (_initialized || _initializing)
                yield break;

            _initializing = true;

            var document = GetComponent<UIDocument>();
            if (document == null)
            {
                _initializing = false;
                yield break;
            }

            shellLayout = ShellAssets.LoadShellLayout(shellLayout);
            if (shellLayout == null)
            {
                Debug.LogError("UIDocumentHost: ViewerShell.uxml VisualTreeAsset is missing. Assign it on UIDocumentHost and UIDocument.");
                _initializing = false;
                yield break;
            }

            for (var attempt = 0; attempt < 30; attempt++)
            {
                if (document.rootVisualElement != null)
                    break;

                yield return null;
            }

            var root = document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError(
                    "UIDocumentHost: UIDocument panel was not created. Assign ViewerPanelSettings with a Theme Style Sheet on the UIDocument component.");
                _initializing = false;
                yield break;
            }

            if (!EnsureShellMounted(document, shellLayout))
            {
                Debug.LogError(
                    $"UIDocumentHost: Failed to mount ViewerShell. layout={shellLayout.name}, rootChildren={root.childCount}, panelSettings={(document.panelSettings != null ? document.panelSettings.name : "null")}");
                _initializing = false;
                yield break;
            }

            var shellRoot = FindShellRoot(root);
            if (shellRoot == null)
            {
                Debug.LogError("UIDocumentHost: hierarchy-panel was not found after mounting ViewerShell.");
                _initializing = false;
                yield break;
            }

            ApplyShellStylesheet(shellRoot);

            _layout = new ShellLayoutController(shellRoot);
            _toolbar = new ToolbarController(shellRoot);
            _overlay = new ViewportOverlayController(shellRoot);
            _hierarchy = new HierarchyPanelController(shellRoot.Q<VisualElement>("hierarchy-panel"));
            _library = new LibraryPanelController(shellRoot.Q<VisualElement>("library-section"));
            _properties = new PropertiesPanelController(shellRoot.Q<VisualElement>("properties-panel"));

            _layout.Bind();
            ViewportInputUtility.ConfigureShellPicking(shellRoot);

            _toolbar.Bind();
            _overlay.Bind();
            _hierarchy?.Bind();
            _library?.Bind();
            _properties?.Bind();

            _initialized = true;
            _initializing = false;
        }

        static bool EnsureShellMounted(UIDocument document, VisualTreeAsset layout)
        {
            var root = document.rootVisualElement;
            if (root == null || layout == null)
                return false;

            if (root.Q<VisualElement>("hierarchy-panel") != null)
                return true;

            root.Clear();

            var instance = layout.Instantiate();
            instance.style.flexGrow = 1f;
            instance.style.width = Length.Percent(100);
            instance.style.height = Length.Percent(100);
            root.Add(instance);

            return root.Q<VisualElement>("hierarchy-panel") != null;
        }

        static VisualElement FindShellRoot(VisualElement root)
        {
            if (root == null)
                return null;

            var shellRoot = root.Q<VisualElement>("shell-root");
            if (shellRoot != null)
                return shellRoot;

            return root.Q<VisualElement>("hierarchy-panel") != null ? root : null;
        }

        static void ApplyShellStylesheet(VisualElement shellRoot)
        {
            var stylesheet = ShellAssets.LoadShellStylesheet();
            if (stylesheet == null || shellRoot == null)
                return;

            var target = shellRoot.Q<VisualElement>("shell-root") ?? shellRoot;
            if (!target.styleSheets.Contains(stylesheet))
                target.styleSheets.Add(stylesheet);

            var documentRoot = shellRoot.parent;
            if (documentRoot != null && !documentRoot.styleSheets.Contains(stylesheet))
                documentRoot.styleSheets.Add(stylesheet);
        }

        void OnDestroy()
        {
            _layout?.Unbind();
            _toolbar?.Unbind();
            _hierarchy?.Unbind();
            _library?.Unbind();
            _properties?.Unbind();
        }
    }
}
