using NUnit.Framework;
using UnityEngine.UIElements;
using UnitySimulationX.UI.Shell;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ViewerShellMountTests
    {
        [Test]
        public void ViewerShell_Instantiate_ContainsHierarchyPanel()
        {
#if UNITY_EDITOR
            var layout = ShellAssets.LoadShellLayout(null);
            Assert.IsNotNull(layout, "ViewerShell.uxml VisualTreeAsset should import.");
            Assert.IsTrue(ShellAssets.IsUsableLayout(layout), "ViewerShell.uxml should instantiate hierarchy-panel.");

            var host = new VisualElement();
            host.Add(layout.Instantiate());
            Assert.IsNotNull(host.Q<VisualElement>("hierarchy-panel"), "hierarchy-panel should exist after Instantiate.");
#else
            Assert.Inconclusive("Editor-only asset import test.");
#endif
        }
    }
}
