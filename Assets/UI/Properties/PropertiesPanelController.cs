using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer.Selection;

namespace UnitySimulationX.UI.Properties
{
    public sealed class PropertiesPanelController
    {
        readonly VisualElement _root;
        readonly VisualElement _fieldsContainer;
        readonly List<IPropertyProvider> _providers = new();

        SceneRegistry _registry;
        ISelectionService _selection;
        ISceneObjectMapper _mapper;
        SceneObjectModel _current;

        public PropertiesPanelController(VisualElement root)
        {
            _root = root;
            if (root == null)
                return;

            _fieldsContainer = root.Q<VisualElement>("properties-fields") ?? root;
            _providers.Add(new CommonPropertyProvider());
        }

        public void Bind()
        {
            if (_root == null || _fieldsContainer == null)
                return;

            _registry = ServiceLocator.Resolve<SceneRegistry>();
            _selection = ServiceLocator.Resolve<ISelectionService>();
            _mapper = ServiceLocator.Resolve<ISceneObjectMapper>();

            EventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
            EventBus.Subscribe<SceneObjectChangedEvent>(OnSceneObjectChanged);

            Refresh();
        }

        public void Unbind()
        {
            EventBus.Unsubscribe<SelectionChangedEvent>(OnSelectionChanged);
            EventBus.Unsubscribe<SceneObjectChangedEvent>(OnSceneObjectChanged);
        }

        void OnSelectionChanged(SelectionChangedEvent _) => Refresh();

        void OnSceneObjectChanged(SceneObjectChangedEvent evt)
        {
            if (_current != null && evt.ObjectId == _current.Id)
                Refresh();
        }

        void Refresh()
        {
            _fieldsContainer.Clear();

            if (_selection.SelectedObjectIds.Count != 1)
            {
                _fieldsContainer.Add(new Label("Select a single object"));
                _current = null;
                return;
            }

            _current = _registry.Get(_selection.SelectedObjectIds[0]);
            if (_current == null)
            {
                _fieldsContainer.Add(new Label("Object not found"));
                return;
            }

            var provider = _providers.FirstOrDefault(p => p.Supports(_current));
            if (provider == null)
            {
                _fieldsContainer.Add(new Label("No property provider"));
                return;
            }

            foreach (var descriptor in provider.GetProperties(_current))
                _fieldsContainer.Add(CreateField(descriptor));
        }

        VisualElement CreateField(PropertyDescriptor descriptor)
        {
            var row = new VisualElement();
            row.AddToClassList("property-row");
            row.style.flexDirection = FlexDirection.Column;
            row.style.marginBottom = 4;

            var label = new Label(descriptor.DisplayName);
            label.AddToClassList("property-label");
            row.Add(label);

            if (descriptor.IsReadOnly)
            {
                var readOnly = new Label(descriptor.Value?.ToString() ?? string.Empty);
                readOnly.AddToClassList("property-value");
                row.Add(readOnly);
                return row;
            }

            if (descriptor.ValueType == typeof(string))
            {
                var field = new TextField { value = descriptor.Value as string };
                field.AddToClassList("editor-field");
                field.RegisterValueChangedCallback(evt => ApplyChange(descriptor.Key, evt.newValue));
                row.Add(field);
            }
            else if (descriptor.ValueType == typeof(bool))
            {
                var toggle = new Toggle { value = descriptor.Value is bool b && b };
                toggle.AddToClassList("editor-field");
                toggle.RegisterValueChangedCallback(evt => ApplyChange(descriptor.Key, evt.newValue));
                row.Add(toggle);
            }
            else if (descriptor.ValueType == typeof(Vector3))
            {
                var vector = descriptor.Value is Vector3 v ? v : Vector3.zero;
                var vectorField = new Vector3Field { value = vector };
                vectorField.AddToClassList("editor-field");
                vectorField.RegisterValueChangedCallback(evt => ApplyChange(descriptor.Key, evt.newValue));
                row.Add(vectorField);
            }
            else
            {
                var fallback = new Label(descriptor.Value?.ToString() ?? string.Empty);
                fallback.AddToClassList("property-value");
                row.Add(fallback);
            }

            return row;
        }

        void ApplyChange(string key, object value)
        {
            if (_current == null)
                return;

            switch (key)
            {
                case "name":
                    _current.Name = value as string;
                    break;
                case "position":
                    _current.Transform.Position = (Vector3)value;
                    break;
                case "rotation":
                    _current.Transform.RotationEuler = (Vector3)value;
                    break;
                case "scale":
                    _current.Transform.Scale = (Vector3)value;
                    break;
                case "visible":
                    _current.Visible = (bool)value;
                    break;
            }

            _registry.Update(_current);
            var go = _mapper.GetGameObject(_current.Id);
            if (go != null)
                _mapper.UpdateGameObject(_current, go);

            EventBus.Publish(new SceneObjectChangedEvent { ObjectId = _current.Id, Model = _current });
        }
    }
}
