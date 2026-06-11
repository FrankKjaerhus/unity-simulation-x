using System.Collections.Generic;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Core
{
    public sealed class SelectionChangedEvent
    {
        public IReadOnlyList<string> SelectedObjectIds { get; set; }
    }

    public sealed class SceneObjectChangedEvent
    {
        public string ObjectId { get; set; }
        public SceneObjectModel Model { get; set; }
    }

    public sealed class HierarchyChangedEvent
    {
    }

    public sealed class GrabModeChangedEvent
    {
        public bool IsGrabbing { get; set; }
    }
}
