using System.Collections.Generic;

namespace UnitySimulationX.Core
{
    public sealed class SelectionChangedEvent
    {
        public IReadOnlyList<string> SelectedObjectIds { get; set; }
    }

    public sealed class GrabModeChangedEvent
    {
        public bool IsGrabbing { get; set; }
    }
}
