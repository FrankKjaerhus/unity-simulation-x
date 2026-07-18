using System;

namespace UnitySimulationX.SceneModel.Serialization
{
    public sealed class ProjectFormatException : Exception
    {
        public ProjectFormatException(string message) : base(message) { }
    }
}
