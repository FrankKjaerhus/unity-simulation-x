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
        public SceneObjectTypeId TypeId { get; set; } = SceneObjectTypeIds.Group;
        public string AssetId { get; set; }

        public string ParentId { get; set; }

        public TransformData Transform { get; set; } = new();
        public bool Visible { get; set; } = true;

        public MaterialDefinition Material { get; set; } = new();
        public VisualStatus VisualStatus { get; set; } = VisualStatus.Normal;
        public List<SceneComponentData> Components { get; set; } = new();

        public SceneObjectModel Clone()
        {
            var clone = new SceneObjectModel
            {
                Id = Id,
                Name = Name,
                TypeId = TypeId,
                AssetId = AssetId,
                ParentId = ParentId,
                Transform = Transform?.Clone() ?? new TransformData(),
                Visible = Visible,
                Material = Material?.Clone() ?? new MaterialDefinition(),
                VisualStatus = VisualStatus
            };

            foreach (var component in Components)
                clone.Components.Add(component.Clone());

            return clone;
        }
    }
}
