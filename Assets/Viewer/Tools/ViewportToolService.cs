using System;

namespace UnitySimulationX.Viewer.Tools
{
    public sealed class ViewportToolService : IViewportToolService
    {
        public ViewportTool ActiveTool { get; private set; } = ViewportTool.Select;

        public event Action<ViewportTool> ToolChanged;

        public void SetTool(ViewportTool tool)
        {
            if (ActiveTool == tool)
                return;

            ActiveTool = tool;
            ToolChanged?.Invoke(tool);
        }
    }
}
