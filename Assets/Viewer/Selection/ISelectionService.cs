using System.Collections.Generic;

namespace UnitySimulationX.Viewer.Selection
{
    public interface ISelectionService
    {
        IReadOnlyList<string> SelectedObjectIds { get; }

        void Select(string objectId, bool additive = false);
        void Deselect(string objectId);
        void Clear();
        bool IsSelected(string objectId);
    }
}
