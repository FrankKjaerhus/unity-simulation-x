using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnitySimulationX.Core;

namespace UnitySimulationX.Viewer
{
    /// <summary>
    /// Blocks viewport input only over side panels and toolbar — not the central 3D region.
    /// </summary>
    public static class ViewportInputUtility
    {
        const string ViewportPlaceholderName = "viewport-placeholder";
        static readonly string[] BlockingPanelNames = { "toolbar", "left-panel", "right-panel", "viewport-overlay-controls" };

        const float DefaultSidePanelWidth = 300f;
        const float DefaultToolbarHeight = 40f;

        public static bool IsPointerOverBlockingUi()
        {
            var pointer = Pointer.current ?? Mouse.current;
            if (pointer == null)
                return false;

            var screenPosition = pointer.position.ReadValue();

            foreach (var document in Object.FindObjectsByType<UIDocument>())
            {
                var root = document.rootVisualElement;
                if (root == null)
                    continue;

                foreach (var panelName in BlockingPanelNames)
                {
                    if (IsScreenPointInsideElement(root.Q(panelName), screenPosition))
                        return true;
                }
            }

            return IsScreenPointInsideSidePanelMargins(screenPosition);
        }

        public static bool IsTextFieldFocused()
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>())
            {
                var focused = document.rootVisualElement?.panel?.focusController?.focusedElement;
                if (focused is TextField)
                    return true;
            }

            return false;
        }

        public static Rect GetViewportScreenRect()
        {
            var left = DefaultSidePanelWidth;
            var right = DefaultSidePanelWidth;
            var toolbar = DefaultToolbarHeight;

            if (ServiceLocator.TryResolve<IShellLayoutService>(out var layout))
            {
                left = layout.LeftPanelWidth;
                right = layout.RightPanelWidth;
                toolbar = layout.ToolbarHeight;
            }

            left = Mathf.Min(left, Screen.width * 0.4f);
            right = Mathf.Min(right, Screen.width * 0.4f);
            toolbar = Mathf.Min(toolbar, Screen.height * 0.15f);

            var width = Mathf.Max(Screen.width - left - right, 1f);
            var height = Mathf.Max(Screen.height - toolbar, 1f);

            // Screen-space origin is bottom-left; toolbar occupies the top band.
            return new Rect(left, 0f, width, height);
        }

        public static bool IsPointerInsideViewport()
        {
            var pointer = Pointer.current ?? Mouse.current;
            if (pointer == null)
                return false;

            return GetViewportScreenRect().Contains(pointer.position.ReadValue());
        }

        static bool IsScreenPointInsideSidePanelMargins(Vector2 screenPosition)
        {
            var viewport = GetViewportScreenRect();
            return !viewport.Contains(screenPosition);
        }

        static bool IsScreenPointInsideElement(VisualElement element, Vector2 screenPosition)
        {
            if (element == null)
                return false;

            if (element.resolvedStyle.display == DisplayStyle.None)
                return false;

            var bounds = element.worldBound;
            if (bounds.width <= 0f || bounds.height <= 0f)
                return false;

            var panel = element.panel;
            if (panel == null)
                return false;

            var panelPoint = ScreenToPanelPoint(panel, screenPosition);
            return bounds.Contains(panelPoint);
        }

        static Vector2 ScreenToPanelPoint(IPanel panel, Vector2 screenPosition)
        {
            var panelHeight = panel.visualTree.worldBound.height;
            if (panelHeight <= 0f)
                panelHeight = Screen.height;

            return new Vector2(screenPosition.x, panelHeight - screenPosition.y);
        }

        public static void ConfigureShellPicking(VisualElement shellRoot)
        {
            if (shellRoot == null)
                return;

            var documentRoot = shellRoot.parent;
            if (documentRoot != null)
                documentRoot.style.backgroundColor = Color.clear;

            shellRoot.style.backgroundColor = Color.clear;

            var mainContent = shellRoot.Q("main-content");
            if (mainContent != null)
            {
                mainContent.pickingMode = PickingMode.Ignore;
                mainContent.style.backgroundColor = Color.clear;
            }

            var viewportRoot = shellRoot.Q("viewport-root");
            if (viewportRoot != null)
            {
                viewportRoot.pickingMode = PickingMode.Ignore;
                viewportRoot.style.backgroundColor = Color.clear;
            }

            var placeholder = shellRoot.Q(ViewportPlaceholderName);
            if (placeholder != null)
            {
                placeholder.pickingMode = PickingMode.Ignore;
                placeholder.style.display = DisplayStyle.None;
            }
        }
    }
}
