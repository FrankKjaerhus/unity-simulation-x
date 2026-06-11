using System;
using UnityEngine;

namespace UnitySimulationX.SceneModel
{
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error,
        Maintenance,
        ReviewRequired
    }

    /// <summary>
    /// Stub for Sprint 7 diagnostics overlay.
    /// </summary>
    [Serializable]
    public sealed class DiagnosticMarker
    {
        public string Id;
        public string TargetObjectId;
        public DiagnosticSeverity Severity;
        public string Message;
        public Vector3 LocalPosition;
        public bool Visible = true;
    }
}
