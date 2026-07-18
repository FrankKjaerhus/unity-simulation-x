using System;
using UnitySimulationX.Editing;

namespace UnitySimulationX.UI.Properties
{
    public sealed class PropertyDescriptor
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; }
        public Type ValueType { get; set; }
        public object Value { get; set; }
        public bool IsReadOnly { get; set; }
        public Func<object, SceneEditResult> Apply { get; set; }
    }
}
