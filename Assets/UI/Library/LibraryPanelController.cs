using UnityEngine;
using UnityEngine.UIElements;
using UnitySimulationX.Core;
using UnitySimulationX.Editing;
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
        ISceneEditService _edits;
        SceneObjectFactoryRegistry _factoryRegistry;

        public LibraryPanelController(VisualElement root)
        {
            _root = root;
            _content = root?.Q<VisualElement>("primitive-library-content");
        }

        public void Bind()
        {
            if (_root == null || _content == null)
                return;

            _factoryRegistry = ServiceLocator.Resolve<SceneObjectFactoryRegistry>();
            _edits = ServiceLocator.Resolve<ISceneEditService>();
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

            foreach (var factory in _factoryRegistry.GetFactories())
            {
                foreach (var variantId in factory.VariantIds)
                    AddTile(factory, variantId, GetIconName(variantId));
            }
        }

        void AddTile(ISceneObjectFactory factory, string variantId, string iconName)
        {
            var tile = new Button(() => CreateObject(factory, variantId))
            {
                text = $"{iconName}\n{variantId}",
                tooltip = $"Insert {variantId}"
            };

            tile.AddToClassList("library-tile");
            _content.Add(tile);
        }

        void OnToolChanged(ViewportTool tool)
        {
            _root.EnableInClassList("library-panel-hidden", tool != ViewportTool.Insert);
        }

        void CreateObject(ISceneObjectFactory factory, string variantId)
        {
            string parentId = null;

            if (ServiceLocator.TryResolve<ISelectionService>(out var selection) &&
                selection.SelectedObjectIds.Count == 1)
            {
                parentId = selection.SelectedObjectIds[0];
            }

            var draft = factory.Create(variantId, variantId, parentId);
            if (draft == null)
                return;

            var result = _edits.Create(draft);
            if (!result.Succeeded)
                return;

            if (ServiceLocator.TryResolve<ISelectionService>(out var sel))
                sel.Select(draft.Id);
        }

        static string GetIconName(string variantId)
        {
            return variantId switch
            {
                "Cube" => MaterialIconNames.Cube,
                "Cylinder" => MaterialIconNames.Cylinder,
                "Sphere" => MaterialIconNames.Sphere,
                "Capsule" => MaterialIconNames.Capsule,
                "Cone" => MaterialIconNames.Cone,
                "Plane" => MaterialIconNames.Plane,
                _ => "category"
            };
        }
    }
}
