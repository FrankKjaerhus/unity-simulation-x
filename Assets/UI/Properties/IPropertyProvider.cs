using System.Collections.Generic;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.UI.Properties
{
    public interface IPropertyProvider
    {
        bool Supports(SceneObjectModel obj);
        IEnumerable<PropertyDescriptor> GetProperties(SceneObjectModel obj);
    }
}
