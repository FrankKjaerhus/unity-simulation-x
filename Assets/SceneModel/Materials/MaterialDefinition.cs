using System;
using UnityEngine;

namespace UnitySimulationX.SceneModel
{
    /// <summary>
    /// Domain material description. Full editor UI arrives in Sprint 6.
    /// </summary>
    [Serializable]
    public sealed class MaterialDefinition
    {
        public Color BaseColor = Color.white;
        public float Alpha = 1.0f;
        public float Metallic;
        public float Roughness = 0.5f;
        public string Preset;
        public bool UserOverride;
        public bool StatusOverrideEnabled = true;
    }
}
