using System;

namespace UnitySimulationX.SceneModel
{
    /// <summary>
    /// Stub for Sprint 7 runtime binding work.
    /// </summary>
    [Serializable]
    public sealed class RuntimeBinding
    {
        public string TargetObjectId;
        public string TargetProperty;
        public string SourceName;
        public string Address;
        public string Mode;
    }
}
