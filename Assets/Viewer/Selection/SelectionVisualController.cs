using System.Collections.Generic;
using UnityEngine;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer.Settings;

namespace UnitySimulationX.Viewer.Selection
{
    /// <summary>
    /// Keeps viewport selection visuals in sync with the central selection service.
    /// </summary>
    public sealed class SelectionVisualController : MonoBehaviour
    {
        [SerializeField] ViewerPresentationSettings settings;

        bool _isGrabbing;
        IReadOnlyList<string> _selectedIds = System.Array.Empty<string>();

        void Awake()
        {
            settings = ViewerSettingsUtility.LoadDefault(settings);
        }

        void OnEnable()
        {
            settings = ViewerSettingsUtility.LoadDefault(settings);
            EventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
            EventBus.Subscribe<GrabModeChangedEvent>(OnGrabModeChanged);
        }

        void Start()
        {
            SyncCurrentSelection();
        }

        void SyncCurrentSelection()
        {
            if (ServiceLocator.TryResolve<ISelectionService>(out var selection))
                _selectedIds = selection.SelectedObjectIds;

            RefreshOutline();
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<SelectionChangedEvent>(OnSelectionChanged);
            EventBus.Unsubscribe<GrabModeChangedEvent>(OnGrabModeChanged);
            SelectionOutlineRenderer.Clear();
        }

        void OnSelectionChanged(SelectionChangedEvent evt)
        {
            _selectedIds = evt.SelectedObjectIds;
            RefreshOutline();
        }

        void OnGrabModeChanged(GrabModeChangedEvent evt)
        {
            _isGrabbing = evt.IsGrabbing;
            RefreshOutline();
        }

        void RefreshOutline()
        {
            if (!ServiceLocator.TryResolve<ISceneObjectMapper>(out var mapper))
                return;

            if (_selectedIds == null || _selectedIds.Count == 0)
            {
                SelectionOutlineRenderer.Clear();
                return;
            }

            var style = _isGrabbing
                ? SelectionOutlineRenderer.OutlineStyle.Grabbing
                : SelectionOutlineRenderer.OutlineStyle.Selected;

            SelectionOutlineRenderer.ApplyOutline(mapper, _selectedIds, settings, style);
        }
    }
}
