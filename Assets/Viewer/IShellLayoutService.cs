using System;

namespace UnitySimulationX.Viewer
{
    public interface IShellLayoutService
    {
        float LeftPanelWidth { get; }
        float RightPanelWidth { get; }
        float ToolbarHeight { get; }
        event Action LayoutChanged;
    }
}
