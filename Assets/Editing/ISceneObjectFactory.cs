using System.Collections.Generic;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Editing
{
    public interface ISceneObjectFactory
    {
        string FactoryId { get; }
        SceneObjectTypeId TypeId { get; }
        string DisplayName { get; }
        int Order { get; }
        IReadOnlyList<string> VariantIds { get; }
        SceneObjectDraft Create(string variantId, string name, string parentId);
    }
}
