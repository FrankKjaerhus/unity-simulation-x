using UnityEngine;
using UnityEngine.UIElements;
using UnitySimulationX.Core;
using UnitySimulationX.Import;
using UnitySimulationX.Viewer.Selection;

namespace UnitySimulationX.UI.Shell
{
    public sealed class AddObjectPanelController
    {
        readonly VisualElement _root;

        public AddObjectPanelController(VisualElement root)
        {
            _root = root;
        }

        public void Bind()
        {
            BindButton("add-cube", PrimitiveMeshType.Cube);
            BindButton("add-cylinder", PrimitiveMeshType.Cylinder);
            BindButton("add-sphere", PrimitiveMeshType.Sphere);
            BindButton("add-capsule", PrimitiveMeshType.Capsule);
            BindButton("add-cone", PrimitiveMeshType.Cone);
            BindButton("add-plane", PrimitiveMeshType.Plane);
        }

        void BindButton(string name, PrimitiveMeshType type)
        {
            var button = _root.Q<Button>(name);
            if (button == null)
                return;

            button.clicked += () => CreatePrimitive(type);
        }

        void CreatePrimitive(PrimitiveMeshType type)
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
