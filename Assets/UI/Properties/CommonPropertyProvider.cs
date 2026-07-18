using System.Collections.Generic;
using UnityEngine;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.UI.Properties
{
    public sealed class CommonPropertyProvider : IPropertyProvider
    {
        public bool Supports(SceneObjectModel obj) => obj != null;

        public IEnumerable<PropertyDescriptor> GetProperties(SceneObjectModel obj)
        {
            yield return new PropertyDescriptor
            {
                Key = "name",
                DisplayName = "Name",
                Category = "Common",
                ValueType = typeof(string),
                Value = obj.Name
            };

            yield return new PropertyDescriptor
            {
                Key = "type",
                DisplayName = "Type",
                Category = "Common",
                ValueType = typeof(string),
                Value = obj.TypeId.Value,
                IsReadOnly = true
            };

            yield return new PropertyDescriptor
            {
                Key = "position",
                DisplayName = "Position",
                Category = "Transform",
                ValueType = typeof(Vector3),
                Value = obj.Transform.Position
            };

            yield return new PropertyDescriptor
            {
                Key = "rotation",
                DisplayName = "Rotation",
                Category = "Transform",
                ValueType = typeof(Vector3),
                Value = obj.Transform.RotationEuler
            };

            yield return new PropertyDescriptor
            {
                Key = "scale",
                DisplayName = "Scale",
                Category = "Transform",
                ValueType = typeof(Vector3),
                Value = obj.Transform.Scale
            };

            yield return new PropertyDescriptor
            {
                Key = "visible",
                DisplayName = "Visibility",
                Category = "Common",
                ValueType = typeof(bool),
                Value = obj.Visible
            };
        }
    }
}
