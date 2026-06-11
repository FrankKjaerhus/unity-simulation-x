using System;

namespace UnitySimulationX.Viewer.Tools
{
    public interface IViewportToolService
    {
        ViewportTool ActiveTool { get; }
        event Action<ViewportTool> ToolChanged;
        void SetTool(ViewportTool tool);
    }
}
