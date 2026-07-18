using System.Collections.Generic;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.UI.Properties
{
    public interface IPropertyProvider
    {
        string ProviderId { get; }
        int Order { get; }
        bool Supports(SceneObjectModel snapshot);
        IEnumerable<PropertyDescriptor> GetProperties(SceneObjectModel snapshot, ISceneEditService edits);
    }
}
