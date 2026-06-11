using NUnit.Framework;
using UnitySimulationX.Viewer.Tools;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ViewportToolServiceTests
    {
        [Test]
        public void SetTool_UpdatesActiveToolAndRaisesEvent()
        {
            var service = new ViewportToolService();
            ViewportTool? raisedTool = null;
            service.ToolChanged += tool => raisedTool = tool;

            service.SetTool(ViewportTool.Measure);

            Assert.AreEqual(ViewportTool.Measure, service.ActiveTool);
            Assert.AreEqual(ViewportTool.Measure, raisedTool);
        }
    }
}
