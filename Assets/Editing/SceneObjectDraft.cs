using System.Collections.Generic;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Editing
{
    public sealed class SceneObjectDraft
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public SceneObjectTypeId TypeId { get; set; } = SceneObjectTypeIds.Group;
        public SceneObjectType Type { get; set; }
        public string ParentId { get; set; }
        public TransformData Transform { get; set; } = new();
        public bool Visible { get; set; } = true;
        public MaterialDefinition Material { get; set; } = new();
        public string AssetId { get; set; }
        public string PrimitiveMeshTypeKey { get; set; }
        public List<SceneComponentData> Components { get; set; } = new();
        public bool SkipProjectionCreate { get; set; }
    }
}
