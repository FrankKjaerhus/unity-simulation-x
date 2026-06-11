using UnityEngine.UIElements;
using UnitySimulationX.Core;
using UnitySimulationX.Viewer;

namespace UnitySimulationX.UI.Shell
{
    public sealed class ShellLayoutController : IShellLayoutService
    {
        const float ExpandedLeftWidth = 300f;
        const float ExpandedRightWidth = 300f;
        const float CollapsedPanelWidth = 28f;
        const float DefaultToolbarHeight = 40f;

        readonly VisualElement _root;
        readonly VisualElement _leftPanel;
        readonly VisualElement _rightPanel;
        readonly VisualElement _toolbar;
        readonly Button _leftCollapse;
        readonly Button _rightCollapse;

        bool _leftCollapsed;
        bool _rightCollapsed;

        public ShellLayoutController(VisualElement root)
        {
            _root = root;
            _leftPanel = root.Q<VisualElement>("left-panel");
            _rightPanel = root.Q<VisualElement>("right-panel");
            _toolbar = root.Q<VisualElement>("toolbar");
            _leftCollapse = root.Q<Button>("left-panel-collapse");
            _rightCollapse = root.Q<Button>("right-panel-collapse");
        }

        public float LeftPanelWidth => _leftCollapsed ? CollapsedPanelWidth : GetResolvedWidth(_leftPanel, ExpandedLeftWidth);
        public float RightPanelWidth => _rightCollapsed ? CollapsedPanelWidth : GetResolvedWidth(_rightPanel, ExpandedRightWidth);
        public float ToolbarHeight => GetResolvedHeight(_toolbar, DefaultToolbarHeight);

        public event System.Action LayoutChanged;

        public void Bind()
        {
            ServiceLocator.Register<IShellLayoutService>(this);

            _leftCollapse?.RegisterCallback<ClickEvent>(_ => ToggleLeftPanel());
            _rightCollapse?.RegisterCallback<ClickEvent>(_ => ToggleRightPanel());
            _root.RegisterCallback<GeometryChangedEvent>(_ => LayoutChanged?.Invoke());

            LayoutChanged?.Invoke();
        }

        public void Unbind()
        {
            _leftCollapse?.UnregisterCallback<ClickEvent>(_ => ToggleLeftPanel());
            _rightCollapse?.UnregisterCallback<ClickEvent>(_ => ToggleRightPanel());
        }

        void ToggleLeftPanel()
        {
            _leftCollapsed = !_leftCollapsed;
            ApplyPanelState(_leftPanel, _leftCollapsed, ExpandedLeftWidth);

            if (_leftCollapse != null)
                _leftCollapse.text = _leftCollapsed ? ">" : "<";

            LayoutChanged?.Invoke();
        }

        void ToggleRightPanel()
        {
            _rightCollapsed = !_rightCollapsed;
            ApplyPanelState(_rightPanel, _rightCollapsed, ExpandedRightWidth);

            if (_rightCollapse != null)
                _rightCollapse.text = _rightCollapsed ? "<" : ">";

            LayoutChanged?.Invoke();
        }

        static void ApplyPanelState(VisualElement panel, bool collapsed, float expandedWidth)
        {
            if (panel == null)
                return;

            panel.EnableInClassList("side-panel-collapsed", collapsed);
            panel.style.width = collapsed ? CollapsedPanelWidth : expandedWidth;
        }

        static float GetResolvedWidth(VisualElement element, float fallback)
        {
            if (element == null)
                return fallback;

            var width = element.resolvedStyle.width;
            return float.IsNaN(width) || width <= 0f ? fallback : width;
        }

        static float GetResolvedHeight(VisualElement element, float fallback)
        {
            if (element == null)
                return fallback;

            var height = element.resolvedStyle.height;
            return float.IsNaN(height) || height <= 0f ? fallback : height;
        }
    }
}
