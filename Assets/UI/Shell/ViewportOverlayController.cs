using UnityEngine;
using UnityEngine.UIElements;
using UnitySimulationX.Core;
using UnitySimulationX.Viewer.Camera;
using UnitySimulationX.Viewer.Grid;

namespace UnitySimulationX.UI.Shell
{
    public sealed class ViewportOverlayController
    {
        readonly VisualElement _root;
        readonly Button _gridToggle;
        readonly Button _view2D;
        readonly Button _view3D;

        public ViewportOverlayController(VisualElement root)
        {
            _root = root;
            _gridToggle = root.Q<Button>("grid-toggle");
            _view2D = root.Q<Button>("view-2d");
            _view3D = root.Q<Button>("view-3d");
        }

        public void Bind()
        {
            if (_gridToggle != null)
                _gridToggle.clicked += ToggleGrid;

            if (_view2D != null)
                _view2D.clicked += Set2DView;

            if (_view3D != null)
                _view3D.clicked += Set3DView;
        }

        void ToggleGrid()
        {
            if (!ServiceLocator.TryResolve<IFloorGridService>(out var grid))
                return;

            grid.Visible = !grid.Visible;
            _gridToggle.text = grid.Visible ? "grid_on Grid" : "grid_off Grid";
            _gridToggle.EnableInClassList("overlay-button-active", grid.Visible);
        }

        void Set2DView()
        {
            var controller = Object.FindAnyObjectByType<ViewerCameraController>();
            controller?.ViewPresets.SetTopView();
            _view2D?.EnableInClassList("overlay-button-active", true);
            _view3D?.EnableInClassList("overlay-button-active", false);
        }

        void Set3DView()
        {
            var controller = Object.FindAnyObjectByType<ViewerCameraController>();
            controller?.ViewPresets.SetPerspective();
            _view2D?.EnableInClassList("overlay-button-active", false);
            _view3D?.EnableInClassList("overlay-button-active", true);
        }
    }
}
