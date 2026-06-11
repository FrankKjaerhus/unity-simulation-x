using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnitySimulationX.Core;
using UnitySimulationX.Import;
using UnitySimulationX.Viewer.Tools;

namespace UnitySimulationX.UI.Shell
{
    public sealed class ToolbarController
    {
        readonly VisualElement _root;
        readonly Button _loadButton;
        readonly Button _saveButton;
        readonly Button _importButton;
        readonly Button _selectButton;
        readonly Button _moveButton;
        readonly Button _measureButton;
        readonly Button _insertButton;

        IViewportToolService _toolService;

        public ToolbarController(VisualElement root)
        {
            _root = root;
            _loadButton = root.Q<Button>("toolbar-load");
            _saveButton = root.Q<Button>("toolbar-save");
            _importButton = root.Q<Button>("toolbar-import");
            _selectButton = root.Q<Button>("tool-select");
            _moveButton = root.Q<Button>("tool-move");
            _measureButton = root.Q<Button>("tool-measure");
            _insertButton = root.Q<Button>("tool-insert");
        }

        public void Bind()
        {
            _toolService = ServiceLocator.Resolve<IViewportToolService>();
            _toolService.ToolChanged += OnToolChanged;

            BindToolButton(_selectButton, ViewportTool.Select);
            BindToolButton(_moveButton, ViewportTool.Move);
            BindToolButton(_measureButton, ViewportTool.Measure);
            BindToolButton(_insertButton, ViewportTool.Insert);

            if (_loadButton != null)
                _loadButton.clicked += LoadProject;

            if (_saveButton != null)
                _saveButton.clicked += SaveProject;

            if (_importButton != null)
                _importButton.clicked += ImportAsset;

            OnToolChanged(_toolService.ActiveTool);
        }

        public void Unbind()
        {
            if (_toolService != null)
                _toolService.ToolChanged -= OnToolChanged;
        }

        void BindToolButton(Button button, ViewportTool tool)
        {
            if (button == null)
                return;

            button.clicked += () => _toolService.SetTool(tool);
        }

        void OnToolChanged(ViewportTool tool)
        {
            SetActive(_selectButton, tool == ViewportTool.Select);
            SetActive(_moveButton, tool == ViewportTool.Move);
            SetActive(_measureButton, tool == ViewportTool.Measure);
            SetActive(_insertButton, tool == ViewportTool.Insert);
        }

        static void SetActive(Button button, bool active)
        {
            button?.EnableInClassList("tool-button-active", active);
        }

        void LoadProject()
        {
            try
            {
                if (!ServiceLocator.TryResolve<IFileDialogService>(out var dialogs) ||
                    !ServiceLocator.TryResolve<IProjectPersistenceService>(out var projects))
                    return;

                var path = dialogs.OpenProjectPath();
                if (!string.IsNullOrWhiteSpace(path))
                    projects.Load(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Load project failed: {ex.Message}");
            }
        }

        void SaveProject()
        {
            try
            {
                if (!ServiceLocator.TryResolve<IFileDialogService>(out var dialogs) ||
                    !ServiceLocator.TryResolve<IProjectPersistenceService>(out var projects))
                    return;

                var path = dialogs.SaveProjectPath(projects.CurrentPath);
                if (!string.IsNullOrWhiteSpace(path))
                    projects.Save(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save project failed: {ex.Message}");
            }
        }

        async void ImportAsset()
        {
            try
            {
                if (!ServiceLocator.TryResolve<IFileDialogService>(out var dialogs) ||
                    !ServiceLocator.TryResolve<IImportSceneService>(out var importer))
                    return;

                var path = dialogs.OpenImportPath();
                if (!string.IsNullOrWhiteSpace(path))
                    await importer.ImportFileAsync(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Import failed: {ex.Message}");
            }
        }
    }
}
