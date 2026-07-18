using System;
using System.Collections.Generic;
using System.Linq;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Editing
{
    public sealed class SceneEditService : ISceneEditService
    {
        readonly SceneRegistry _registry;
        readonly ISceneProjectionWriter _projection;
        readonly IEventBus _eventBus;

        public SceneEditService(SceneRegistry registry, ISceneProjectionWriter projection, IEventBus eventBus)
        {
            _registry = registry;
            _projection = projection;
            _eventBus = eventBus;
        }

        public ISceneRegistryRead Registry => _registry;

        public SceneEditResult Create(SceneObjectDraft draft)
        {
            if (draft == null)
                throw new ArgumentNullException(nameof(draft));

            try
            {
                var model = ToModel(draft);
                _registry.Add(model);
                var snapshot = _registry.Get(model.Id);

                if (!draft.SkipProjectionCreate)
                    _projection.CreateProjection(snapshot);

                return PublishSuccess(SceneChangeKind.Created, new[] { model.Id }, hierarchyChanged: true);
            }
            catch (SceneInvariantException ex)
            {
                return Failure(ex);
            }
        }

        public SceneEditResult Remove(string objectId)
        {
            try
            {
                if (!_registry.Contains(objectId))
                    return Missing(objectId);

                var removedIds = CollectSubtreeIds(objectId);
                if (!_registry.Remove(objectId))
                    return Missing(objectId);

                foreach (var id in removedIds)
                    _projection.RemoveProjection(id);

                return PublishSuccess(SceneChangeKind.Removed, removedIds, hierarchyChanged: true);
            }
            catch (SceneInvariantException ex)
            {
                return Failure(ex);
            }
        }

        public SceneEditResult Rename(string objectId, string name) =>
            UpdateObject(objectId, model => model.Name = name);

        public SceneEditResult SetVisible(string objectId, bool visible) =>
            UpdateObject(objectId, model => model.Visible = visible);

        public SceneEditResult SetTransform(string objectId, TransformData transform) =>
            UpdateObject(objectId, model => model.Transform = transform?.Clone() ?? new TransformData());

        public SceneEditResult SetMaterial(string objectId, MaterialDefinition material) =>
            UpdateObject(objectId, model => model.Material = material?.Clone() ?? new MaterialDefinition());

        public SceneEditResult SetComponent(string objectId, SceneComponentData component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            return UpdateObject(objectId, model =>
            {
                model.Components ??= new List<SceneComponentData>();
                var index = model.Components.FindIndex(existing =>
                    string.Equals(existing.TypeId, component.TypeId, StringComparison.Ordinal));

                if (index >= 0)
                    model.Components[index] = component.Clone();
                else
                    model.Components.Add(component.Clone());
            });
        }

        public SceneEditResult Reparent(string objectId, string newParentId)
        {
            try
            {
                _registry.Reparent(objectId, newParentId);
                var snapshot = _registry.Get(objectId);
                _projection.UpdateProjection(snapshot);
                return PublishSuccess(SceneChangeKind.Reparented, new[] { objectId }, hierarchyChanged: true);
            }
            catch (SceneInvariantException ex)
            {
                return Failure(ex);
            }
        }

        public SceneEditResult ReplaceScene(IReadOnlyList<SceneObjectModel> snapshots)
        {
            if (snapshots == null)
                throw new ArgumentNullException(nameof(snapshots));

            try
            {
                var staging = new SceneRegistry();
                staging.ReplaceAll(snapshots);

                var previous = _registry.GetAll().ToList();
                _registry.ReplaceAll(snapshots);

                try
                {
                    _projection.ReplaceAllProjections(_registry.GetAll().ToList());
                }
                catch
                {
                    _registry.ReplaceAll(previous);
                    throw;
                }

                var objectIds = snapshots.Select(snapshot => snapshot.Id).ToList();
                return PublishSuccess(SceneChangeKind.SceneReplaced, objectIds, hierarchyChanged: true);
            }
            catch (SceneInvariantException ex)
            {
                return Failure(ex);
            }
        }

        SceneEditResult UpdateObject(string objectId, Action<SceneObjectModel> mutate)
        {
            try
            {
                var current = _registry.Get(objectId);
                if (current == null)
                    return Missing(objectId);

                mutate(current);
                _registry.Update(current);
                _projection.UpdateProjection(_registry.Get(objectId));
                return PublishSuccess(SceneChangeKind.Updated, new[] { objectId }, hierarchyChanged: false);
            }
            catch (SceneInvariantException ex)
            {
                return Failure(ex);
            }
        }

        SceneEditResult PublishSuccess(SceneChangeKind kind, IReadOnlyList<string> objectIds, bool hierarchyChanged)
        {
            var changeSet = new SceneChangeSet
            {
                Kind = kind,
                ObjectIds = objectIds,
                HierarchyChanged = hierarchyChanged,
                Revision = _registry.Revision
            };

            _eventBus.Publish(new SceneChangedEvent { ChangeSet = changeSet });
            return new SceneEditResult { Succeeded = true, ChangeSet = changeSet };
        }

        static SceneEditResult Failure(SceneInvariantException ex) =>
            new()
            {
                Succeeded = false,
                ErrorCode = ex.Code,
                Message = ex.Message
            };

        static SceneEditResult Missing(string objectId) =>
            new()
            {
                Succeeded = false,
                ErrorCode = "scene.object.missing",
                Message = $"Object not found: {objectId}"
            };

        static SceneObjectModel ToModel(SceneObjectDraft draft)
        {
            var model = new SceneObjectModel
            {
                Id = draft.Id,
                Name = draft.Name,
                TypeId = draft.TypeId,
                ParentId = draft.ParentId,
                Transform = draft.Transform?.Clone() ?? new TransformData(),
                Visible = draft.Visible,
                Material = draft.Material?.Clone() ?? new MaterialDefinition(),
                AssetId = draft.AssetId
            };

            if (draft.Components != null)
            {
                foreach (var component in draft.Components)
                    model.Components.Add(component.Clone());
            }

            return model;
        }

        List<string> CollectSubtreeIds(string rootId)
        {
            var result = new List<string> { rootId };
            foreach (var childId in _registry.GetChildrenIds(rootId))
                result.AddRange(CollectSubtreeIds(childId));

            return result;
        }
    }
}
