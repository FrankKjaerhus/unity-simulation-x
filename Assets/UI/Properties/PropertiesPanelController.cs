using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnitySimulationX.Core;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer.Selection;

namespace UnitySimulationX.UI.Properties
{
    public sealed class PropertiesPanelController
    {
        readonly VisualElement _root;
        readonly VisualElement _fieldsContainer;
        readonly List<IPropertyProvider> _providers = new();

        ISceneRegistryRead _registry;
        ISelectionService _selection;
        ISceneEditService _edits;
        IEventBus _eventBus;
        IDisposable _selectionSubscription;
        IDisposable _sceneChangedSubscription;
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

            _registry = ServiceLocator.Resolve<ISceneRegistryRead>();
            _selection = ServiceLocator.Resolve<ISelectionService>();
            _edits = ServiceLocator.Resolve<ISceneEditService>();
            _eventBus = ServiceLocator.Resolve<IEventBus>();

            _selectionSubscription = _eventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
            _sceneChangedSubscription = _eventBus.Subscribe<SceneChangedEvent>(OnSceneChanged);

            Refresh();
        }

        public void Unbind()
        {
            _selectionSubscription?.Dispose();
            _selectionSubscription = null;
            _sceneChangedSubscription?.Dispose();
            _sceneChangedSubscription = null;
        }

        void OnSelectionChanged(SelectionChangedEvent _) => Refresh();

        void OnSceneChanged(SceneChangedEvent evt)
        {
            if (_current != null &&
                evt.ChangeSet?.ObjectIds != null &&
                evt.ChangeSet.ObjectIds.Contains(_current.Id))
            {
                Refresh();
            }
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
                    _edits.Rename(_current.Id, value as string);
                    break;
                case "position":
                {
                    var transform = _current.Transform?.Clone() ?? new TransformData();
                    transform.Position = (Vector3)value;
                    _edits.SetTransform(_current.Id, transform);
                    break;
                }
                case "rotation":
                {
                    var transform = _current.Transform?.Clone() ?? new TransformData();
                    transform.RotationEuler = (Vector3)value;
                    _edits.SetTransform(_current.Id, transform);
                    break;
                }
                case "scale":
                {
                    var transform = _current.Transform?.Clone() ?? new TransformData();
                    transform.Scale = (Vector3)value;
                    _edits.SetTransform(_current.Id, transform);
                    break;
                }
                case "visible":
                    _edits.SetVisible(_current.Id, (bool)value);
                    break;
            }

            _current = _registry.Get(_current.Id);
        }
    }
}
