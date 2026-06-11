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
    }
}
