using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitySimulationX.SceneModel
{
    public sealed class SceneRegistry : ISceneRegistryRead
    {
        readonly Dictionary<string, SceneObjectModel> _objects = new();
        readonly List<string> _rootIds = new();
        readonly Dictionary<string, List<string>> _childrenByParent = new();

        public event Action HierarchyChanged;

        public long Revision { get; private set; }

        public IReadOnlyList<string> RootIds => _rootIds.ToList();

        public SceneObjectModel Get(string id)
        {
            return _objects.TryGetValue(id, out var model) ? model.Clone() : null;
        }

        public IReadOnlyCollection<SceneObjectModel> GetAll() =>
            _objects.Values.Select(model => model.Clone()).ToList();

        public IReadOnlyList<string> GetChildrenIds(string parentId) =>
            _childrenByParent.TryGetValue(parentId, out var children)
                ? children.ToList()
                : Array.Empty<string>();

        public bool Contains(string id) => _objects.ContainsKey(id);

        public void Add(SceneObjectModel model)
        {
            ValidateModelForInsert(model, isNew: true);
            var clone = model.Clone();
            _objects[clone.Id] = clone;
            AttachToHierarchy(clone);
            BumpRevision();
            HierarchyChanged?.Invoke();
        }

        public bool Remove(string id)
        {
            if (!_objects.ContainsKey(id))
                return false;

            var toRemove = CollectDescendantIds(id);
            toRemove.Add(id);

            foreach (var removeId in toRemove)
            {
                if (!_objects.TryGetValue(removeId, out var model))
                    continue;

                DetachFromHierarchy(model);
                _objects.Remove(removeId);
            }

            BumpRevision();
            HierarchyChanged?.Invoke();
            return true;
        }

        public void Reparent(string objectId, string newParentId)
        {
            if (!_objects.TryGetValue(objectId, out var model))
                throw new SceneInvariantException("scene.object.missing", $"Object not found: {objectId}");

            if (objectId == newParentId)
                throw new SceneInvariantException("scene.parent.self", "Object cannot parent itself.");

            if (!string.IsNullOrEmpty(newParentId))
            {
                if (!_objects.ContainsKey(newParentId))
                    throw new SceneInvariantException("scene.parent.missing", $"Parent not found: {newParentId}");

                if (IsDescendant(newParentId, objectId))
                    throw new SceneInvariantException("scene.parent.cycle", "Reparent would create a cycle.");
            }

            DetachFromHierarchy(model);
            model.ParentId = newParentId;
            AttachToHierarchy(model);
            BumpRevision();
            HierarchyChanged?.Invoke();
        }

        public void Replace(string objectId, SceneObjectModel replacement)
        {
            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            if (!_objects.ContainsKey(objectId))
                throw new SceneInvariantException("scene.object.missing", $"Object not found: {objectId}");

            if (replacement.Id != objectId)
                throw new SceneInvariantException("scene.id.required", "Replacement object id must match target id.");

            ValidateModelForInsert(replacement, isNew: false);
            _objects[objectId] = replacement.Clone();
            BumpRevision();
        }

        public void ReplaceAll(IReadOnlyList<SceneObjectModel> models)
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));

            var clones = models.Select(model =>
            {
                ValidateModelForInsert(model, isNew: true);
                return model.Clone();
            }).ToList();

            ValidateHierarchy(clones);

            _objects.Clear();
            _rootIds.Clear();
            _childrenByParent.Clear();

            foreach (var clone in clones)
            {
                _objects[clone.Id] = clone;
                AttachToHierarchy(clone);
            }

            BumpRevision();
            HierarchyChanged?.Invoke();
        }

        public void Update(SceneObjectModel model) => Replace(model.Id, model);

        void ValidateModelForInsert(SceneObjectModel model, bool isNew)
        {
            if (model == null || string.IsNullOrEmpty(model.Id))
                throw new SceneInvariantException("scene.id.required", "Scene object requires a non-empty id.");

            if (!SceneObjectTypeId.IsValid(model.TypeId.Value))
                throw new SceneInvariantException("scene.type.invalid", $"Invalid type id: {model.TypeId.Value}");

            if (isNew && _objects.ContainsKey(model.Id))
                throw new SceneInvariantException("scene.id.duplicate", $"Object already registered: {model.Id}");

            if (!string.IsNullOrEmpty(model.ParentId) && !_objects.ContainsKey(model.ParentId))
                throw new SceneInvariantException("scene.parent.missing", $"Parent not found: {model.ParentId}");

            ValidateComponents(model);
        }

        static void ValidateComponents(SceneObjectModel model)
        {
            if (model.Components == null)
                return;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var component in model.Components)
            {
                if (!seen.Add(component.TypeId))
                    throw new SceneInvariantException("scene.component.duplicate",
                        $"Duplicate component type id: {component.TypeId}");
            }
        }

        void ValidateHierarchy(IReadOnlyList<SceneObjectModel> models)
        {
            var byId = models.ToDictionary(model => model.Id);
            foreach (var model in models)
            {
                if (!string.IsNullOrEmpty(model.ParentId) && !byId.ContainsKey(model.ParentId))
                    throw new SceneInvariantException("scene.parent.missing",
                        $"Parent not found: {model.ParentId}");

                if (ContainsCycle(model.Id, byId))
                    throw new SceneInvariantException("scene.parent.cycle", "Hierarchy contains a cycle.");
            }
        }

        static bool ContainsCycle(string startId, Dictionary<string, SceneObjectModel> byId)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var currentId = startId;
            while (byId.TryGetValue(currentId, out var current) && !string.IsNullOrEmpty(current.ParentId))
            {
                if (!visited.Add(current.ParentId))
                    return true;

                currentId = current.ParentId;
                if (currentId == startId)
                    return true;
            }

            return false;
        }

        void AttachToHierarchy(SceneObjectModel model)
        {
            if (string.IsNullOrEmpty(model.ParentId))
            {
                if (!_rootIds.Contains(model.Id))
                    _rootIds.Add(model.Id);
                return;
            }

            if (!_childrenByParent.TryGetValue(model.ParentId, out var children))
            {
                children = new List<string>();
                _childrenByParent[model.ParentId] = children;
            }

            if (!children.Contains(model.Id))
                children.Add(model.Id);
        }

        void DetachFromHierarchy(SceneObjectModel model)
        {
            if (string.IsNullOrEmpty(model.ParentId))
            {
                _rootIds.Remove(model.Id);
                return;
            }

            if (_childrenByParent.TryGetValue(model.ParentId, out var children))
                children.Remove(model.Id);
        }

        List<string> CollectDescendantIds(string rootId)
        {
            var result = new List<string>();
            if (!_childrenByParent.TryGetValue(rootId, out var children))
                return result;

            foreach (var childId in children.ToList())
            {
                result.Add(childId);
                result.AddRange(CollectDescendantIds(childId));
            }

            return result;
        }

        bool IsDescendant(string candidateId, string ancestorId)
        {
            var currentId = candidateId;
            while (_objects.TryGetValue(currentId, out var current) && !string.IsNullOrEmpty(current.ParentId))
            {
                if (current.ParentId == ancestorId)
                    return true;
                currentId = current.ParentId;
            }

            return false;
        }

        void BumpRevision() => Revision++;
    }
}
