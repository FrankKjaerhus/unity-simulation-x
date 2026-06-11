using UnityEngine;
using UnityEngine.UIElements;
using UnitySimulationX.Core;
using UnitySimulationX.Import;
using UnitySimulationX.UI.Icons;
using UnitySimulationX.Viewer.Selection;
using UnitySimulationX.Viewer.Tools;

namespace UnitySimulationX.UI.Library
{
    public sealed class LibraryPanelController
    {
        readonly VisualElement _root;
        readonly VisualElement _content;

        IViewportToolService _toolService;

        public LibraryPanelController(VisualElement root)
        {
            _root = root;
            _content = root?.Q<VisualElement>("primitive-library-content");
        }

        public void Bind()
        {
            if (_root == null || _content == null)
                return;

            BuildPrimitiveTiles();

            _toolService = ServiceLocator.Resolve<IViewportToolService>();
            _toolService.ToolChanged += OnToolChanged;
            OnToolChanged(_toolService.ActiveTool);
        }

        public void Unbind()
        {
            if (_toolService != null)
                _toolService.ToolChanged -= OnToolChanged;
        }

        void BuildPrimitiveTiles()
        {
            _content.Clear();
            AddTile(PrimitiveMeshType.Cube, MaterialIconNames.Cube);
            AddTile(PrimitiveMeshType.Cylinder, MaterialIconNames.Cylinder);
            AddTile(PrimitiveMeshType.Sphere, MaterialIconNames.Sphere);
            AddTile(PrimitiveMeshType.Capsule, MaterialIconNames.Capsule);
            AddTile(PrimitiveMeshType.Cone, MaterialIconNames.Cone);
            AddTile(PrimitiveMeshType.Plane, MaterialIconNames.Plane);
        }

        void AddTile(PrimitiveMeshType type, string iconName)
        {
            var tile = new Button(() => CreatePrimitive(type))
            {
                text = $"{iconName}\n{type}",
                tooltip = $"Insert {type}"
            };

            tile.AddToClassList("library-tile");
            _content.Add(tile);
        }

        void OnToolChanged(ViewportTool tool)
        {
            _root.EnableInClassList("library-panel-hidden", tool != ViewportTool.Insert);
        }

        static void CreatePrimitive(PrimitiveMeshType type)
        {
            var factory = ServiceLocator.Resolve<IPrimitiveFactory>();
            string parentId = null;

            if (ServiceLocator.TryResolve<ISelectionService>(out var selection) &&
                selection.SelectedObjectIds.Count == 1)
            {
                parentId = selection.SelectedObjectIds[0];
            }

            var settings = new PrimitiveSettings
            {
                Name = type.ToString(),
                Position = parentId == null
                    ? new Vector3(Random.Range(-2f, 2f), 0.5f, Random.Range(-2f, 2f))
                    : Random.insideUnitSphere * 0.5f,
                ParentId = parentId
            };

            var model = factory.CreatePrimitive(type, settings);

            if (ServiceLocator.TryResolve<ISelectionService>(out var sel))
                sel.Select(model.Id);
        }
    }
}
