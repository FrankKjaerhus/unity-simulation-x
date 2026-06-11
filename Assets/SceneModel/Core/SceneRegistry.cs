using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitySimulationX.SceneModel
{
    public sealed class SceneRegistry
    {
        readonly Dictionary<string, SceneObjectModel> _objects = new();
        readonly List<string> _rootIds = new();

        public event Action HierarchyChanged;

        public IReadOnlyList<string> RootIds => _rootIds;

        public SceneObjectModel Get(string id)
        {
            return _objects.TryGetValue(id, out var model) ? model : null;
        }

        public IReadOnlyCollection<SceneObjectModel> GetAll() => _objects.Values;

        public bool Contains(string id) => _objects.ContainsKey(id);

        public void Add(SceneObjectModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Id))
                throw new ArgumentException("Scene object requires a non-empty Id.");

            if (_objects.ContainsKey(model.Id))
                throw new InvalidOperationException($"Object already registered: {model.Id}");

            _objects[model.Id] = model;
            model.ChildrenIds ??= new List<string>();

            if (string.IsNullOrEmpty(model.ParentId))
            {
                if (!_rootIds.Contains(model.Id))
                    _rootIds.Add(model.Id);
            }
            else
            {
                var parent = Get(model.ParentId);
                if (parent != null && !parent.ChildrenIds.Contains(model.Id))
                    parent.ChildrenIds.Add(model.Id);
            }

            HierarchyChanged?.Invoke();
        }

        public bool Remove(string id)
        {
            if (!_objects.TryGetValue(id, out var model))
                return false;

            var childIds = model.ChildrenIds.ToList();
            foreach (var childId in childIds)
                Remove(childId);

            if (!string.IsNullOrEmpty(model.ParentId))
            {
                var parent = Get(model.ParentId);
                parent?.ChildrenIds.Remove(id);
            }
            else
            {
                _rootIds.Remove(id);
            }

            _objects.Remove(id);
            HierarchyChanged?.Invoke();
            return true;
        }

        public void Reparent(string objectId, string newParentId)
        {
            if (!_objects.TryGetValue(objectId, out var model))
                return;

            if (objectId == newParentId)
                return;

            if (!string.IsNullOrEmpty(newParentId) && IsDescendant(newParentId, objectId))
                return;

            if (!string.IsNullOrEmpty(model.ParentId))
            {
                var oldParent = Get(model.ParentId);
                oldParent?.ChildrenIds.Remove(objectId);
            }
            else
            {
                _rootIds.Remove(objectId);
            }

            model.ParentId = newParentId;

            if (string.IsNullOrEmpty(newParentId))
            {
                if (!_rootIds.Contains(objectId))
                    _rootIds.Add(objectId);
            }
            else
            {
                var newParent = Get(newParentId);
                if (newParent != null && !newParent.ChildrenIds.Contains(objectId))
                    newParent.ChildrenIds.Add(objectId);
            }

            HierarchyChanged?.Invoke();
        }

        public void Update(SceneObjectModel model)
        {
            if (model == null || !_objects.ContainsKey(model.Id))
                return;

            _objects[model.Id] = model;
        }

        bool IsDescendant(string candidateId, string ancestorId)
        {
            var current = Get(candidateId);
            while (current != null && !string.IsNullOrEmpty(current.ParentId))
            {
                if (current.ParentId == ancestorId)
                    return true;
                current = Get(current.ParentId);
            }

            return false;
        }
    }
}
