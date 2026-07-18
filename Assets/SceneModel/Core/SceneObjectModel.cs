using System;
using System.Collections.Generic;

namespace UnitySimulationX.SceneModel
{
    /// <summary>
    /// Engineering scene object. Unity GameObjects are runtime representations only.
    /// </summary>
    [Serializable]
    public sealed class SceneObjectModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public SceneObjectType Type { get; set; }
        public SceneObjectTypeId TypeId { get; set; } = SceneObjectTypeIds.Group;
        public string AssetId { get; set; }

        public string ParentId { get; set; }
        public List<string> ChildrenIds { get; set; } = new();

        public TransformData Transform { get; set; } = new();
        public bool Visible { get; set; } = true;

        public MaterialDefinition Material { get; set; } = new();
        public VisualStatus VisualStatus { get; set; } = VisualStatus.Normal;

        public Dictionary<string, object> CommonProperties { get; set; } = new();
        public Dictionary<string, object> DomainProperties { get; set; } = new();

        public List<RuntimeBinding> RuntimeBindings { get; set; } = new();
        public List<DiagnosticMarker> Diagnostics { get; set; } = new();

        public string PrimitiveMeshTypeKey { get; set; }
        public List<SceneComponentData> Components { get; set; } = new();

        public SceneObjectModel Clone()
        {
            var clone = new SceneObjectModel
            {
                Id = Id,
                Name = Name,
                Type = Type,
                TypeId = TypeId,
                AssetId = AssetId,
                ParentId = ParentId,
                ChildrenIds = ChildrenIds != null ? new List<string>(ChildrenIds) : new List<string>(),
                Transform = Transform?.Clone() ?? new TransformData(),
                Visible = Visible,
                Material = Material?.Clone() ?? new MaterialDefinition(),
                VisualStatus = VisualStatus,
                PrimitiveMeshTypeKey = PrimitiveMeshTypeKey
            };

            foreach (var kvp in CommonProperties)
                clone.CommonProperties[kvp.Key] = kvp.Value;

            foreach (var kvp in DomainProperties)
                clone.DomainProperties[kvp.Key] = kvp.Value;

            foreach (var binding in RuntimeBindings)
                clone.RuntimeBindings.Add(binding);

            foreach (var marker in Diagnostics)
                clone.Diagnostics.Add(marker);

            foreach (var component in Components)
                clone.Components.Add(component.Clone());

            return clone;
        }
    }
}
