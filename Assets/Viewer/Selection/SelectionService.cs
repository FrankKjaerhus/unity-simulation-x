using System.Collections.Generic;
using System.Linq;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Viewer.Selection
{
    public sealed class SelectionService : ISelectionService
    {
        readonly SceneRegistry _registry;
        readonly IEventBus _eventBus;
        readonly List<string> _selected = new();

        public SelectionService(SceneRegistry registry, IEventBus eventBus)
        {
            _registry = registry;
            _eventBus = eventBus;
        }

        public IReadOnlyList<string> SelectedObjectIds => _selected;

        public void Select(string objectId, bool additive = false)
        {
            if (string.IsNullOrEmpty(objectId) || _registry.Get(objectId) == null)
                return;

            if (!additive)
                _selected.Clear();

            if (!_selected.Contains(objectId))
                _selected.Add(objectId);

            Publish();
        }

        public void Deselect(string objectId)
        {
            if (_selected.Remove(objectId))
                Publish();
        }

        public void Clear()
        {
            if (_selected.Count == 0)
                return;

            _selected.Clear();
            Publish();
        }

        public bool IsSelected(string objectId) => _selected.Contains(objectId);

        void Publish()
        {
            _eventBus.Publish(new SelectionChangedEvent
            {
                SelectedObjectIds = _selected.ToList()
            });
        }
    }
}
