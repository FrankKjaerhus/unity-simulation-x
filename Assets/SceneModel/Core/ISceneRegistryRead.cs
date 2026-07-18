using System;
using System.Collections.Generic;

namespace UnitySimulationX.SceneModel
{
    public interface ISceneRegistryRead
    {
        long Revision { get; }
        IReadOnlyList<string> RootIds { get; }
        SceneObjectModel Get(string id);
        IReadOnlyCollection<SceneObjectModel> GetAll();
        IReadOnlyList<string> GetChildrenIds(string parentId);
        bool Contains(string id);
        event Action HierarchyChanged;
    }
}
