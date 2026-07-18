using System;
using System.Collections.Generic;

namespace UnitySimulationX.Core
{
    [Serializable]
    public sealed class ProjectOperationResult
    {
        public bool Succeeded { get; set; }
        public IReadOnlyList<ProjectIssue> Issues { get; set; } = Array.Empty<ProjectIssue>();
    }

    [Serializable]
    public sealed class ProjectIssue
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public bool IsError { get; set; }
    }
}
