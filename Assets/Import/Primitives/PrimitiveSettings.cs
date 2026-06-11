using UnityEngine;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Import
{
    public sealed class PrimitiveSettings
    {
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 RotationEuler { get; set; }
        public Vector3 Scale { get; set; } = Vector3.one;
        public MaterialDefinition Material { get; set; }
        public string ParentId { get; set; }
    }
}
