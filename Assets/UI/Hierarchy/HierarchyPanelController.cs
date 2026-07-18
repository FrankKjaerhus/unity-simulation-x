using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer.Projection;
using UnitySimulationX.Viewer.Selection;

namespace UnitySimulationX.UI.Hierarchy
{
    public sealed class HierarchyPanelController
    {
        readonly VisualElement _root;
        readonly TextField _searchField;
        readonly ScrollView _treeScroll;
        readonly VisualElement _treeContainer;
        readonly DropdownField _reparentDropdown;
        readonly Button _reparentButton;
        readonly Button _deleteButton;

        SceneRegistry _registry;
        ISelectionService _selection;
        ISceneProjectionService _projection;
        IEventBus _eventBus;
        IDisposable _selectionSubscription;
        IDisposable _hierarchySubscription;
        string _filter = string.Empty;
        string _selectedReparentTarget;
        string _draggedObjectId;
        VisualElement _dragTargetRow;

        public HierarchyPanelController(VisualElement root)
        {
            _root = root;
            if (root == null)
                return;

            _searchField = root.Q<TextField>("hierarchy-search");
            _treeScroll = root.Q<ScrollView>("hierarchy-tree");
            _treeContainer = _treeScroll?.Q<VisualElement>("hierarchy-tree-content") ?? _treeScroll;
            _reparentDropdown = root.Q<DropdownField>("reparent-target");
            _reparentButton = root.Q<Button>("reparent-button");
            _deleteButton = root.Q<Button>("delete-button");
        }

        public void Bind()
        {
            if (_root == null || _treeContainer == null)
                return;

            _registry = ServiceLocator.Resolve<SceneRegistry>();
            _selection = ServiceLocator.Resolve<ISelectionService>();
            _projection = ServiceLocator.Resolve<ISceneProjectionService>();
            _eventBus = ServiceLocator.Resolve<IEventBus>();

            _searchField?.RegisterValueChangedCallback(evt =>
            {
                _filter = evt.newValue ?? string.Empty;
                Rebuild();
            });

            _reparentButton?.RegisterCallback<ClickEvent>(_ => ApplyReparent());
            _deleteButton?.RegisterCallback<ClickEvent>(_ => DeleteSelected());

            _selectionSubscription = _eventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
            _hierarchySubscription = _eventBus.Subscribe<HierarchyChangedEvent>(OnHierarchyChanged);
            _registry.HierarchyChanged += Rebuild;

            Rebuild();
        }

        public void Unbind()
        {
            if (_registry != null)
                _registry.HierarchyChanged -= Rebuild;
            _selectionSubscription?.Dispose();
            _selectionSubscription = null;
            _hierarchySubscription?.Dispose();
            _hierarchySubscription = null;
        }

        void OnSelectionChanged(SelectionChangedEvent _) => Rebuild();
        void OnHierarchyChanged(HierarchyChangedEvent _) => Rebuild();

        void Rebuild()
        {
            if (_treeContainer == null)
                return;

            _treeContainer.Clear();
            UpdateReparentChoices();

            foreach (var rootId in _registry.RootIds)
                BuildNode(rootId, 0);

            if (_treeContainer.childCount == 0)
            {
                _treeContainer.Add(new Label("No scene objects"));
            }
        }

        void BuildNode(string objectId, int depth)
        {
            var model = _registry.Get(objectId);
            if (model == null)
                return;

            if (!MatchesFilter(model))
                return;

            var row = CreateRow(model, depth);
            _treeContainer.Add(row);

            foreach (var childId in _registry.GetChildrenIds(model.Id))
                BuildNode(childId, depth + 1);
        }

        bool MatchesFilter(SceneObjectModel model)
        {
            if (string.IsNullOrWhiteSpace(_filter))
                return true;

            if (model.Name != null && model.Name.Contains(_filter, System.StringComparison.OrdinalIgnoreCase))
                return true;

            return _registry.GetChildrenIds(model.Id).Any(childId =>
            {
                var child = _registry.Get(childId);
                return child != null && MatchesFilter(child);
            });
        }

        VisualElement CreateRow(SceneObjectModel model, int depth)
        {
            var row = new VisualElement();
            row.AddToClassList("hierarchy-row");
            row.style.paddingLeft = depth * 16;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var icon = new Label(GetIcon(model.Type));
            icon.AddToClassList("hierarchy-icon");

            var nameField = new TextField { value = model.Name };
            nameField.AddToClassList("hierarchy-name");
            nameField.RegisterValueChangedCallback(evt =>
            {
                model.Name = evt.newValue;
                _registry.Update(model);
                _projection.UpdateProjection(model);
                _eventBus.Publish(new SceneObjectChangedEvent { ObjectId = model.Id, Model = model });
            });

            var visibilityToggle = new Toggle { value = model.Visible };
            visibilityToggle.RegisterValueChangedCallback(evt =>
            {
                model.Visible = evt.newValue;
                _registry.Update(model);
                _projection.UpdateProjection(model);
                _eventBus.Publish(new SceneObjectChangedEvent { ObjectId = model.Id, Model = model });
            });

            if (_selection.IsSelected(model.Id))
                row.AddToClassList("hierarchy-row-selected");

            row.RegisterCallback<ClickEvent>(evt =>
            {
                if (IsInteractiveTarget(evt.target))
                    return;

                var additive = evt.shiftKey || evt.actionKey;
                _selection.Select(model.Id, additive);
                evt.StopPropagation();
            });

            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || IsInteractiveTarget(evt.target))
                    return;

                _draggedObjectId = model.Id;
            });

            row.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (string.IsNullOrEmpty(_draggedObjectId) || _draggedObjectId == model.Id)
                    return;

                _dragTargetRow?.RemoveFromClassList("hierarchy-row-drag-target");
                _dragTargetRow = row;
                row.AddToClassList("hierarchy-row-drag-target");
            });

            row.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (_dragTargetRow == row)
                {
                    row.RemoveFromClassList("hierarchy-row-drag-target");
                    _dragTargetRow = null;
                }
            });

            row.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button != 0 || string.IsNullOrEmpty(_draggedObjectId) || _draggedObjectId == model.Id)
                {
                    ClearDragState();
                    return;
                }

                ReparentObject(_draggedObjectId, model.Id);
                ClearDragState();
                evt.StopPropagation();
            });

            row.Add(icon);
            row.Add(nameField);
            row.Add(visibilityToggle);
            return row;
        }

        void UpdateReparentChoices()
        {
            if (_reparentDropdown == null)
                return;

            var choices = new List<string> { "(Root)" };
            choices.AddRange(_registry.GetAll().Select(o => $"{o.Name} [{o.Id[..8]}]"));
            _reparentDropdown.choices = choices;
            _reparentDropdown.index = 0;
            _reparentDropdown.RegisterValueChangedCallback(evt => _selectedReparentTarget = evt.newValue);
        }

        void ApplyReparent()
        {
            if (_selection.SelectedObjectIds.Count != 1)
                return;

            var objectId = _selection.SelectedObjectIds[0];
            string newParentId = null;

            if (_reparentDropdown != null && _reparentDropdown.index > 0)
            {
                var all = _registry.GetAll().ToList();
                var target = all[_reparentDropdown.index - 1];
                newParentId = target.Id;
            }

            _registry.Reparent(objectId, newParentId);

            SyncReparentedObject(objectId);
        }

        void ReparentObject(string objectId, string newParentId)
        {
            _registry.Reparent(objectId, newParentId);
            SyncReparentedObject(objectId);
        }

        void SyncReparentedObject(string objectId)
        {
            var model = _registry.Get(objectId);
            if (model != null)
                _projection.UpdateProjection(model);

            _eventBus.Publish(new HierarchyChangedEvent());
            _eventBus.Publish(new SceneObjectChangedEvent { ObjectId = objectId, Model = model });
        }

        void ClearDragState()
        {
            _draggedObjectId = null;
            _dragTargetRow?.RemoveFromClassList("hierarchy-row-drag-target");
            _dragTargetRow = null;
        }

        void DeleteSelected()
        {
            var ids = _selection.SelectedObjectIds.ToList();
            foreach (var id in ids)
            {
                _projection.RemoveProjection(id);
                _registry.Remove(id);
            }

            _selection.Clear();
            _eventBus.Publish(new HierarchyChangedEvent());
            Rebuild();
        }

        static string GetIcon(SceneObjectType type)
        {
            return type switch
            {
                SceneObjectType.Primitive => "PR",
                SceneObjectType.Robot => "RB",
                SceneObjectType.Shuttle => "SH",
                SceneObjectType.Station => "ST",
                SceneObjectType.Sensor => "SE",
                SceneObjectType.SafetyZone => "SZ",
                SceneObjectType.ImportedAsset => "3D",
                _ => "OB"
            };
        }

        static bool IsInteractiveTarget(IEventHandler target)
        {
            return HasAncestor<TextField>(target) || HasAncestor<Toggle>(target);
        }

        static bool HasAncestor<T>(IEventHandler target) where T : VisualElement
        {
            var element = target as VisualElement;
            while (element != null)
            {
                if (element is T)
                    return true;

                element = element.parent;
            }

            return false;
        }
    }
}
