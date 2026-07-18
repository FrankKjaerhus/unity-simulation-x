using System.Collections.Generic;

namespace UnitySimulationX.Editing
{
    public sealed class SceneChangeSet
    {
        public SceneChangeKind Kind { get; set; }
        public IReadOnlyList<string> ObjectIds { get; set; }
        public bool HierarchyChanged { get; set; }
        public long Revision { get; set; }
    }
}
