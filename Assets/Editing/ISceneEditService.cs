using System.Collections.Generic;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Editing
{
    public interface ISceneEditService
    {
        ISceneRegistryRead Registry { get; }
        SceneEditResult Create(SceneObjectDraft draft);
        SceneEditResult Remove(string objectId);
        SceneEditResult Rename(string objectId, string name);
        SceneEditResult SetVisible(string objectId, bool visible);
        SceneEditResult SetTransform(string objectId, TransformData transform);
        SceneEditResult SetMaterial(string objectId, MaterialDefinition material);
        SceneEditResult Reparent(string objectId, string newParentId);
        SceneEditResult SetComponent(string objectId, SceneComponentData component);
        SceneEditResult ReplaceScene(IReadOnlyList<SceneObjectModel> snapshots);
    }
}
