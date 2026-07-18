using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnitySimulationX.App.ProjectSystem;
using UnitySimulationX.Core;
using UnitySimulationX.Editing;
using UnitySimulationX.Import;
using UnitySimulationX.SceneModel;
using UnitySimulationX.UI;
using UnitySimulationX.UI.Hierarchy;
using UnitySimulationX.UI.Properties;
using UnitySimulationX.Viewer.Camera;
using UnitySimulationX.Viewer.Gizmos;
using UnitySimulationX.Viewer.Grid;
using UnitySimulationX.Viewer.Measure;
using UnitySimulationX.Viewer;
using UnitySimulationX.Viewer.Projection;
using UnitySimulationX.Viewer.Selection;
using UnitySimulationX.Viewer.Tools;

namespace UnitySimulationX.App
{
    /// <summary>
    /// Application entry point. Registration order: scene model → editing → adapters → UI.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class AppBootstrap : MonoBehaviour
    {
        [SerializeField] Transform sceneRoot;
        [SerializeField] Camera viewportCamera;
        [SerializeField] UIDocumentHost uiDocumentHost;

        SceneRegistry _registry;
        SceneProjectionService _projection;
        SceneEditService _edits;
        IProjectWorkspace _projectWorkspace;
        IProjectAssetStore _projectAssetStore;
        IShellLayoutService _layoutService;
        string _sceneRootModelId;

        void Awake()
        {
            ServiceLocator.Clear();

            var eventBus = new EventBus(Debug.LogException);
            ServiceLocator.Register<IEventBus>(eventBus);

            _registry = new SceneRegistry();
            ServiceLocator.Register<ISceneRegistryRead>(_registry);

            var root = sceneRoot != null ? sceneRoot : new GameObject("SceneRoot").transform;
            if (sceneRoot == null)
                sceneRoot = root;

            var importedAssetProjectionProvider = new ImportedAssetProjectionProvider();
            ServiceLocator.Register<IImportedAssetProjectionProvider>(importedAssetProjectionProvider);

            var primitiveMeshCodec = new PrimitiveMeshComponentCodec();
            var componentCodecRegistry = new SceneComponentCodecRegistry();
            componentCodecRegistry.Register(primitiveMeshCodec);
            componentCodecRegistry.Freeze();
            ServiceLocator.Register(componentCodecRegistry);

            _projection = new SceneProjectionService(
                root,
                _registry,
                importedAssetProjectionProvider,
                componentCodecRegistry);
            ServiceLocator.Register<ISceneProjectionService>(_projection);

            _edits = new SceneEditService(_registry, _projection, eventBus);
            ServiceLocator.Register<ISceneEditService>(_edits);

            var selection = new SelectionService(_registry, eventBus);
            ServiceLocator.Register<ISelectionService>(selection);

            var propertyProviderRegistry = new PropertyProviderRegistry();
            propertyProviderRegistry.Register(new CommonPropertyProvider());
            propertyProviderRegistry.Freeze();
            ServiceLocator.Register(propertyProviderRegistry);

            var typeDescriptorRegistry = new SceneTypeDescriptorRegistry();
            typeDescriptorRegistry.Register(new SceneTypeDescriptor(SceneObjectTypeIds.Group, "Group", "GR"));
            typeDescriptorRegistry.Register(new SceneTypeDescriptor(SceneObjectTypeIds.Primitive, "Primitive", "PR"));
            typeDescriptorRegistry.Register(new SceneTypeDescriptor(SceneObjectTypeIds.ImportedModel, "Imported Model", "3D"));
            typeDescriptorRegistry.Register(new SceneTypeDescriptor(SceneObjectTypeIds.MissingAsset, "Missing Asset", "!!"));
            typeDescriptorRegistry.Freeze();
            ServiceLocator.Register(typeDescriptorRegistry);

            var primitiveFactory = new PrimitiveFactory(_edits, primitiveMeshCodec);
            var sceneObjectFactoryRegistry = new SceneObjectFactoryRegistry();
            sceneObjectFactoryRegistry.Register(primitiveFactory);
            sceneObjectFactoryRegistry.Freeze();
            ServiceLocator.Register(sceneObjectFactoryRegistry);
            ServiceLocator.Register<IPrimitiveFactory>(primitiveFactory);

            var importerRegistry = new ImporterRegistry();
            importerRegistry.Register(new ObjSceneAssetImporter());
            importerRegistry.Register(new StlSceneAssetImporter());
            importerRegistry.Register(new GltfSceneAssetImporter());
            importerRegistry.Freeze();
            ServiceLocator.Register(importerRegistry);
            _projectWorkspace = new ProjectWorkspace();
            ServiceLocator.Register<IProjectWorkspace>(_projectWorkspace);
            _projectAssetStore = new ProjectAssetStore(_projectWorkspace);
            ServiceLocator.Register<IProjectAssetStore>(_projectAssetStore);
            ServiceLocator.Register<IImportSceneService>(
                new ImportSceneService(
                    importerRegistry,
                    _edits,
                    _projectAssetStore,
                    importedAssetProjectionProvider));
            ServiceLocator.Register<IProjectPersistenceService>(
                new ProjectPersistenceService(
                    _edits,
                    _projectWorkspace,
                    _projectAssetStore,
                    importerRegistry,
                    importedAssetProjectionProvider,
                    new MissingAssetFactory()));
            ServiceLocator.Register<IFileDialogService>(new NativeFileDialogService());
            ServiceLocator.Register<IViewportToolService>(new ViewportToolService());

            EnsureEventSystem();
            EnsureCamera();
            EnsureUI();
            EnsureInputHandlers();

            CreateDefaultRoot();
            AdoptAuthoredSceneObjects();
        }

        void Start()
        {
            SubscribeToShellLayout();
            ConfigureCameraViewport();
        }

        void Update()
        {
            if (_layoutService == null)
                SubscribeToShellLayout();

            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
                ConfigureCameraViewport();
        }

        int _lastScreenWidth;
        int _lastScreenHeight;

        void ConfigureCameraViewport()
        {
            if (viewportCamera == null)
                return;

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            var viewport = ViewportInputUtility.GetViewportScreenRect();
            viewportCamera.rect = new Rect(
                viewport.x / Screen.width,
                viewport.y / Screen.height,
                viewport.width / Screen.width,
                viewport.height / Screen.height);
        }

        void SubscribeToShellLayout()
        {
            if (ServiceLocator.TryResolve<IShellLayoutService>(out _layoutService))
                _layoutService.LayoutChanged += ConfigureCameraViewport;
        }

        void EnsureEventSystem()
        {
#if ENABLE_INPUT_SYSTEM
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
            }

            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule != null && inputModule.actionsAsset == null)
                inputModule.AssignDefaultActions();
#endif
        }

        void EnsureCamera()
        {
            if (viewportCamera == null)
                viewportCamera = Camera.main;

            if (viewportCamera == null)
                return;

            if (viewportCamera.GetComponent<ViewerCameraController>() == null)
                viewportCamera.gameObject.AddComponent<ViewerCameraController>();
        }

        void EnsureUI()
        {
            if (uiDocumentHost == null)
                uiDocumentHost = FindAnyObjectByType<UIDocumentHost>();

            if (uiDocumentHost == null)
            {
                var uiGo = new GameObject("UI");
                uiDocumentHost = uiGo.AddComponent<UIDocumentHost>();
            }

        }

        void EnsureInputHandlers()
        {
            if (viewportCamera == null)
                return;

            if (viewportCamera.GetComponent<ViewportSelectionInput>() == null)
                viewportCamera.gameObject.AddComponent<ViewportSelectionInput>();

            if (viewportCamera.GetComponent<SelectionVisualController>() == null)
                viewportCamera.gameObject.AddComponent<SelectionVisualController>();

            if (viewportCamera.GetComponent<TransformGizmoController>() == null)
                viewportCamera.gameObject.AddComponent<TransformGizmoController>();

            if (viewportCamera.GetComponent<FloorGridRenderer>() == null)
                viewportCamera.gameObject.AddComponent<FloorGridRenderer>();

            if (viewportCamera.GetComponent<MeasureToolController>() == null)
                viewportCamera.gameObject.AddComponent<MeasureToolController>();
        }

        void OnDestroy()
        {
            if (_layoutService != null)
                _layoutService.LayoutChanged -= ConfigureCameraViewport;

            _projectWorkspace?.Dispose();
        }

        void CreateDefaultRoot()
        {
            if (_registry.GetAll().Count > 0)
                return;

            var rootModel = new SceneObjectDraft
            {
                Id = System.Guid.NewGuid().ToString("N"),
                Name = "Scene",
                TypeId = SceneObjectTypeIds.Group
            };

            var result = _edits.Create(rootModel);
            if (result.Succeeded)
                _sceneRootModelId = rootModel.Id;
        }

        void AdoptAuthoredSceneObjects()
        {
            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var candidates = new HashSet<GameObject>();

            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.gameObject.name == "SelectionOutline")
                    continue;

                var candidate = GetAuthoredObjectRoot(renderer.gameObject);
                if (candidate == null || !candidates.Add(candidate))
                    continue;

                if (candidate.GetComponentInParent<SceneObjectIdComponent>() != null)
                    continue;

                var draft = new SceneObjectDraft
                {
                    Id = System.Guid.NewGuid().ToString("N"),
                    Name = candidate.name,
                    TypeId = SceneObjectTypeIds.ImportedModel,
                    ParentId = _sceneRootModelId,
                    Transform = new TransformData
                    {
                        Position = candidate.transform.localPosition,
                        RotationEuler = candidate.transform.localEulerAngles,
                        Scale = candidate.transform.localScale
                    },
                    Visible = renderer.enabled,
                    SkipProjectionCreate = true
                };

                _projection.RegisterExistingTarget(draft.Id, candidate);
                var createResult = _edits.Create(draft);
                if (!createResult.Succeeded)
                    _projection.RemoveProjection(draft.Id);
            }
        }

        GameObject GetAuthoredObjectRoot(GameObject source)
        {
            var current = source.transform;
            while (current.parent != null &&
                   current.parent != sceneRoot &&
                   current.parent.GetComponent<AppBootstrap>() == null)
            {
                current = current.parent;
            }

            if (current == sceneRoot || current.GetComponent<Camera>() != null)
                return null;

            return current.gameObject;
        }
    }
}
