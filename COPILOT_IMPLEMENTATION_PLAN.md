# Copilot Implementation Plan - Unity 3D Viewer

## Purpose

Build a Unity-based 3D Viewer that acts as the shared visual and technical foundation for importing assets, structuring machine scenes, navigating in 2D/3D, inspecting properties, binding runtime values, displaying diagnostics, and generating visual outputs.

The application is the modern Unity-based successor to Scene Viewer and should be designed as a reusable engineering platform for multiple mechatronic systems.

**Standalone viewer:** This project is a standalone CAD/engineering tool. Import from `neexo-simulation-x` or `acopostrak-simulation-contract` is **not planned**. Do not add simulation-contract import adapters or coordinate-transform pipelines for Neexo web configurator export in MVP 1.

---

## Product Scope

The viewer must support:

- 3D file import: GLB, STL, OBJ
- Primitive mesh creation: Cube, Cylinder, Sphere, Capsule, Cone, Plane
- Scene hierarchy for mechatronic components
- Shared properties panel
- Material editor
- Visual status logic
- Blender-like 2D/3D navigation
- Viewport transform gizmos (3D drag, axis-constrained move)
- Runtime binding to OPC UA and future playback data
- Diagnostics overlay in 3D
- Object libraries with reusable behaviours
- Screenshot and report/export shell
- Presentation mode and camera paths

---

## Core Architecture Principle

Do not use Unity GameObjects as the primary data model.

Use a separate domain model as the source of truth. Unity GameObjects are only runtime/rendering representations of the engineering model.

```text
Scene Domain Model
    -> Runtime Mapping Layer
        -> Unity GameObjects / Renderers / Materials / Cameras
```

This makes the platform easier to save, load, validate, export, test, and connect to external engineering systems.

### Service Wiring and Events

Use a **ServiceLocator** plus a lightweight **event bus** for cross-module communication in MVP 1. Do **not** use VContainer or other DI containers in MVP 1.

**ServiceLocator** registers and resolves core services:

- `SceneRegistry`
- `ISceneObjectMapper`
- `ISelectionService`
- `MaterialService`
- `ImporterRegistry`
- `RuntimeBindingService`
- `DiagnosticsOverlaySystem`
- Other module services as they are introduced

**Event bus** publishes domain-level notifications so UI, viewport, and services stay decoupled:

- Selection changed
- Scene object created, updated, or destroyed
- Model property changed
- Hierarchy structure changed
- UI sync requests (hierarchy ↔ properties ↔ viewport highlight)

**Rules:**

- MonoBehaviours forward input and lifecycle events to services; they do **not** hold business logic.
- UI panels subscribe to events; they do not call each other directly.
- Domain model changes flow: edit → registry → mapper → GameObject, with events notifying subscribers.

---

## Recommended Unity Setup

### Unity

Use **Unity 6 LTS** for production.

### UPM Dependencies

Install via Package Manager:

- **Input System** — viewport navigation, selection, gizmo interaction
- **UI Toolkit** — all editor-style panels and shell
- **Universal Render Pipeline (URP)** — engineering visualization rendering
- **glTFast** — GLB/glTF import (decided for Sprint 5)

### Rendering

Use URP.

Reason:

- Good performance for large machine scenes
- Suitable for engineering visualization
- Easier custom overlays and transparent status materials
- Lower complexity than HDRP

### UI

Use UI Toolkit for all editor-style UI:

- Scene hierarchy
- Properties panel
- Material editor
- Runtime binding panel
- Diagnostics panel
- Import/export shell

### Input

Use Unity Input System.

Required input targets:

- Mouse
- Keyboard
- Trackpad-compatible navigation
- Future support for 3D mouse/controller

---

## High-Level Module Layout

```text
Assets/
├── App/
│   ├── Bootstrap/
│   ├── Configuration/
│   └── ProjectSystem/
│
├── Viewer/
│   ├── Camera/
│   ├── Selection/
│   ├── Gizmos/
│   ├── Navigation/
│   ├── Views/
│   └── Screenshots/
│
├── SceneModel/
│   ├── Core/
│   ├── Properties/
│   ├── Materials/
│   ├── Diagnostics/
│   └── Serialization/
│
├── Import/
│   ├── GLB/
│   ├── STL/
│   ├── OBJ/
│   └── Primitives/
│
├── RuntimeBinding/
│   ├── Core/
│   ├── Mock/
│   ├── Playback/
│   └── OpcUa/
│
├── ObjectLibrary/
│   ├── Behaviours/
│   ├── Robots/
│   ├── Shuttles/
│   ├── Tracks/
│   ├── Stations/
│   └── Conveyors/
│
├── UI/
│   ├── Shell/
│   ├── Hierarchy/
│   ├── Properties/
│   ├── MaterialEditor/
│   ├── Diagnostics/
│   └── RuntimeBinding/
│
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

Each top-level module folder should have its own **Assembly Definition** (`.asmdef`) to enforce boundaries and speed up compilation. Sprint 0 establishes this structure.

---

## Main Domain Model

Create the following core model classes.

### SceneObjectModel

```csharp
public sealed class SceneObjectModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public SceneObjectType Type { get; set; }

    public string ParentId { get; set; }
    public List<string> ChildrenIds { get; set; } = new();

    public TransformData Transform { get; set; } = new();
    public bool Visible { get; set; } = true;

    public MaterialDefinition Material { get; set; } = new();
    public VisualStatus VisualStatus { get; set; } = VisualStatus.Normal;

    public Dictionary<string, object> CommonProperties { get; set; } = new();
    public Dictionary<string, object> DomainProperties { get; set; } = new();

    public List<RuntimeBinding> RuntimeBindings { get; set; } = new();
    public List<DiagnosticMarker> Diagnostics { get; set; } = new();
}
```

> **Open point:** `Dictionary<string, object>` JSON serialization strategy is TBD (typed property bags vs. `JsonElement`/custom serializers). Resolve during Sprint 1 JSON-schema stub work.

### SceneObjectType

```csharp
public enum SceneObjectType
{
    MachineFrame,
    TransportSystem,
    TrackSegment,
    Tile6D,
    Shuttle,
    Robot,
    Station,
    Sensor,
    SafetyZone,
    CustomerCadObject,
    DiagnosticOverlay,
    Primitive,
    ImportedAsset
}
```

### VisualStatus

```csharp
public enum VisualStatus
{
    Normal,
    Valid,
    Warning,
    Error,
    RuntimeLive,
    HiddenInactive,
    ReviewRequired
}
```

### TransformData

```csharp
public sealed class TransformData
{
    public Vector3 Position { get; set; }
    public Vector3 RotationEuler { get; set; }
    public Vector3 Scale { get; set; } = Vector3.one;
}
```

---

## Unity Mapping Layer

Create a mapping service that connects domain objects to Unity GameObjects.

### Required Service

```csharp
public interface ISceneObjectMapper
{
    GameObject CreateGameObject(SceneObjectModel model);
    void UpdateGameObject(SceneObjectModel model, GameObject target);
    void DestroyGameObject(string sceneObjectId);
    GameObject GetGameObject(string sceneObjectId);
    SceneObjectModel GetModel(GameObject gameObject);
}
```

### Rules

- Each GameObject must have a component that stores the domain object ID.
- Do not store all engineering data in MonoBehaviours.
- MonoBehaviours should forward events to services via the event bus.
- Domain model changes must update Unity objects through the mapping layer.

---

## Blender-Like Navigation

Implement a dedicated camera controller.

Do not rely on Unity Scene View navigation.

**Intentional deviation:** Navigation follows **Blender conventions**, not B&R Scene Viewer conventions. This is a deliberate product decision.

### Required Navigation

| Action | Input |
|---|---|
| Orbit | Middle mouse drag |
| Pan | Shift + middle mouse drag |
| Zoom | Scroll wheel |
| Focus selected | F |
| Frame all | Home |
| Top view | Numpad 7 |
| Front view | Numpad 1 |
| Side view | Numpad 3 |
| Toggle perspective/orthographic | Numpad 5 |
| Fly mode | Shift + F or right mouse + WASD |
| Select object | Left click |
| Multi-select | Shift + click |

### Camera Module Structure

```text
ViewerCameraController
├── OrbitController
├── PanController
├── ZoomController
├── FlyController
├── ViewPresetController
├── FocusController
└── CameraBookmarkController
```

### View Modes

```csharp
public enum ViewMode
{
    Perspective3D,
    OrthographicTop,
    OrthographicFront,
    OrthographicSide,
    SectionView,
    LayoutEditing,
    PresentationCamera
}
```

### Acceptance Criteria

- User can orbit around selected object.
- User can focus selected object using F.
- User can switch between perspective and orthographic.
- Top/front/side views are stable and predictable.
- Navigation feels close to Blender conventions.

---

## Viewport Transform Gizmos

Implement in **Sprint 3** alongside UI panels.

### Required Behaviour

- 3D drag to move selected object(s) in the viewport
- Axis-constrained drag when X, Y, or Z key is held (lock to world axis)
- Gizmo manipulation updates domain model `TransformData` through the registry and mapper
- Properties panel transform fields stay synchronized via event bus

### Acceptance Criteria

- User can drag a selected object in the viewport to reposition it.
- Holding X, Y, or Z during drag constrains movement to that axis.
- Gizmo edits update the properties panel transform fields.
- Multi-select drag moves all selected objects (optional for MVP 1; document if deferred).

---

## Selection System

Implement object selection as a separate service.

### Required Interface

```csharp
public interface ISelectionService
{
    IReadOnlyList<string> SelectedObjectIds { get; }

    void Select(string objectId, bool additive = false);
    void Deselect(string objectId);
    void Clear();
    bool IsSelected(string objectId);
}
```

### Requirements

- Click object in viewport to select.
- Click object in hierarchy to select.
- Selection must synchronize hierarchy, properties panel, and viewport highlight via event bus.
- Support multi-select.
- Support focus selected.

---

## 3D File Import

Support these formats:

- **GLB** — via **glTFast** (decided)
- **STL** — parser TBD (research in Sprint 5: Assimp vs dedicated STL parser)
- **OBJ** — parser TBD (research in Sprint 5: Assimp vs dedicated OBJ parser)

Do **not** support `.scn` import or ACOPOStrak assembly service import. Supported sources are GLB, OBJ, STL, and procedural primitives only.

### Import Interface

```csharp
public interface ISceneAssetImporter
{
    bool CanImport(string fileExtension);
    Task<ImportResult> ImportAsync(string filePath, ImportSettings settings);
}
```

### ImportResult (Domain-Safe DTOs)

Refactor `ImportResult` to use domain-safe DTOs instead of Unity `Mesh`/`Material` directly. Importers produce intermediate data; the mapping layer converts to Unity assets.

```csharp
public sealed class ImportResult
{
    public SceneObjectModel RootObject { get; set; }
    public List<ImportedMeshData> Meshes { get; set; } = new();
    public List<ImportedMaterialData> Materials { get; set; } = new();
    public BoundsData Bounds { get; set; }
    public List<ImportWarning> Warnings { get; set; } = new();
}

public sealed class ImportedMeshData
{
    public string Name { get; set; }
    public Vector3[] Vertices { get; set; }
    public int[] Triangles { get; set; }
    public Vector3[] Normals { get; set; }
    public Vector2[] Uvs { get; set; }
}

public sealed class ImportedMaterialData
{
    public string Name { get; set; }
    public Color BaseColor { get; set; }
    public float Metallic { get; set; }
    public float Roughness { get; set; }
    public string BaseColorTexturePath { get; set; }
}
```

### ImportSettings

```csharp
public sealed class ImportSettings
{
    public float UnitScale { get; set; } = 1.0f;
    public bool GenerateColliders { get; set; } = true;
    public bool PreserveHierarchy { get; set; } = true;
    public bool GenerateMaterials { get; set; } = true;
    public bool CenterOnImport { get; set; } = false;
}
```

### Requirements

- Import must be async where possible.
- Imported objects must appear in hierarchy.
- Imported objects must be selectable.
- Imported objects must expose transform, visibility, material, and type.
- Importer must calculate bounds.
- Importer must generate basic import statistics.
- GLB importer uses glTFast; OBJ/STL library choice finalized during Sprint 5 research spike.

### Import Statistics

```csharp
public sealed class ImportedAssetMetadata
{
    public string SourceFilePath { get; set; }
    public string SourceFormat { get; set; }
    public DateTime ImportedAt { get; set; }
    public float UnitScale { get; set; }
    public int MeshCount { get; set; }
    public int MaterialCount { get; set; }
    public int TriangleCount { get; set; }
    public Bounds Bounds { get; set; }
}
```

---

## Primitive Mesh Creation

Support procedural primitive creation directly inside the viewer.

### Supported Shapes

- Cube
- Cylinder
- Sphere
- Capsule
- Cone
- Plane

### Interface

```csharp
public interface IPrimitiveFactory
{
    SceneObjectModel CreatePrimitive(PrimitiveMeshType type, PrimitiveSettings settings);
}
```

### PrimitiveMeshType

```csharp
public enum PrimitiveMeshType
{
    Cube,
    Cylinder,
    Sphere,
    Capsule,
    Cone,
    Plane
}
```

### PrimitiveSettings

```csharp
public sealed class PrimitiveSettings
{
    public string Name { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 RotationEuler { get; set; }
    public Vector3 Scale { get; set; } = Vector3.one;
    public MaterialDefinition Material { get; set; }
}
```

### Acceptance Criteria

- User can add each primitive type from UI.
- Primitive appears in scene hierarchy.
- Primitive can be selected and edited.
- Primitive can be assigned a domain type later.

---

## Scene Hierarchy Panel

The hierarchy panel must represent mechatronic components.

### Object Categories

- Machine frames
- Transport systems
- Track segments
- 6D tiles
- Shuttles
- Robots
- Stations
- Sensors
- Safety zones
- Customer CAD objects
- Diagnostic overlays

### Required Behaviour

- Tree view with parent/child structure (UI Toolkit)
- Select object from hierarchy
- Rename object
- Toggle visibility
- Reparent objects (drag-and-drop or explicit reparent action)
- Delete object
- Show icon based on SceneObjectType
- Search/filter objects

### Example Hierarchy

```text
Machine
├── Frame
├── Transport System A
│   ├── Track Segment 001
│   ├── Track Segment 002
│   ├── Shuttle 01
│   └── Shuttle 02
├── Robot Cell 1
│   ├── Robot
│   ├── Station Infeed
│   └── Safety Zone
└── Customer CAD
```

---

## Properties Panel

The properties panel is the shared editing interface for all modules.

### Common Properties

- Name
- Type
- Position
- Rotation
- Scale
- Visibility
- Material

### Domain-Specific Properties

- Track component type
- Shuttle type
- Segment ID
- Station type
- Process time
- Robot TCP
- Reachability status
- Automation Studio variable mapping status

### Property Provider Pattern

```csharp
public interface IPropertyProvider
{
    bool Supports(SceneObjectModel obj);
    IEnumerable<PropertyDescriptor> GetProperties(SceneObjectModel obj);
}
```

### PropertyDescriptor

```csharp
public sealed class PropertyDescriptor
{
    public string Key { get; set; }
    public string DisplayName { get; set; }
    public string Category { get; set; }
    public Type ValueType { get; set; }
    public object Value { get; set; }
    public bool IsReadOnly { get; set; }
}
```

### Rules

- Do not hardcode all domain fields directly into one UI script.
- Use providers to add module-specific properties.
- Properties panel must update when selection changes (via event bus).
- Property edits must update both domain model and Unity GameObject.

---

## Material Editor

The material editor must adjust visual appearance without breaking the engineering model.

### MaterialDefinition

```csharp
public sealed class MaterialDefinition
{
    public Color BaseColor { get; set; } = Color.white;
    public float Alpha { get; set; } = 1.0f;
    public float Metallic { get; set; } = 0.0f;
    public float Roughness { get; set; } = 0.5f;
    public string Preset { get; set; }
    public bool UserOverride { get; set; }
    public bool StatusOverrideEnabled { get; set; } = true;
}
```

### Visual Status Logic

Use material states as an information layer.

| Status | Meaning |
|---|---|
| Green | Valid |
| Yellow | Warning / estimated |
| Red | Error |
| Blue | Runtime/live object |
| Grey | Hidden/inactive |
| Purple/Orange | Review-required area |

Exact colors can be changed later by UI configuration.

### Required Implementation Rule

Do not permanently overwrite imported materials when visual status changes.

Use:

```text
Base Material + Status Overlay
```

or equivalent material state restoration logic.

### Required Presets

- Machine Frame
- Track
- Shuttle
- Robot
- Station
- Sensor
- Safety Zone
- Customer CAD
- Runtime Live
- Warning
- Error
- Review Required

---

## Runtime Binding

Runtime binding connects scene objects to live values from automation targets.

OPC UA is one data source. The binding system must also support mock data and future playback data.

### Data Source Interface

```csharp
public interface IRuntimeDataSource
{
    string Name { get; }
    ConnectionState State { get; }

    Task ConnectAsync(ConnectionSettings settings);
    Task DisconnectAsync();

    void Subscribe(string address, Action<RuntimeValue> callback);
    RuntimeValue GetLatestValue(string address);
}
```

### RuntimeBinding

```csharp
public sealed class RuntimeBinding
{
    public string TargetObjectId { get; set; }
    public string TargetProperty { get; set; }
    public string SourceName { get; set; }
    public string Address { get; set; }
    public BindingMode Mode { get; set; }
    public ValueMapper Mapper { get; set; }
}
```

### BindingMode

```csharp
public enum BindingMode
{
    TransformPosition,
    TransformRotation,
    MaterialColor,
    Visibility,
    TextLabel,
    DiagnosticState,
    RobotJoint,
    ShuttlePosition,
    ConveyorSpeed,
    StationUtilization
}
```

### Data Sources To Implement

1. MockDataSource
2. PlaybackDataSource
3. OpcUaDataSource

### OPC UA and PowerTools Strategy

- **`OpcUaDataSource`** is implemented using **`NeexoConnect`** from the `neexoPowerTools` package (OPC UA stack only).
- Do **not** use **`NeexoRealtimeGatewayClient`** as the primary connectivity path.
- Do **not** reuse PowerTools conveyors, robots, or sensors — implement a new **`ISceneBehaviour`** layer in the viewer for object animation and runtime-driven visuals.
- Verify **Unity 6 + NeexoConnect compatibility** before Sprint 10 (compatibility spike in late MVP 1 if needed).

### Development Order

Start with `MockDataSource`.

Do not block UI, material logic, diagnostics, or scene binding work on real PLC/OPC UA connectivity.

### Example Bindings

| Runtime value | Viewer action |
|---|---|
| Robot.Axis1.ActualPosition | Rotate robot joint 1 |
| Shuttle[3].TrackPosition | Move shuttle along track path |
| Station[2].State | Change station material/status |
| Alarm[17].Active | Show red diagnostic marker |
| Conveyor[1].Speed | Animate conveyor belt surface |

---

## Diagnostics Overlay

Diagnostics must be visible in 3D and listed in UI.

### DiagnosticMarker

```csharp
public sealed class DiagnosticMarker
{
    public string Id { get; set; }
    public string TargetObjectId { get; set; }
    public DiagnosticSeverity Severity { get; set; }
    public string Message { get; set; }
    public Vector3 LocalPosition { get; set; }
    public bool Visible { get; set; } = true;
}
```

### DiagnosticSeverity

```csharp
public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error,
    Maintenance,
    ReviewRequired
}
```

### Overlay System

```text
DiagnosticsOverlaySystem
├── MarkerRegistry
├── IconRenderer
├── LabelRenderer
├── BillboardSystem
├── OcclusionHandling
└── SeverityFilter
```

### Required Diagnostic Types

- Error markers
- Warning icons
- Runtime status labels
- Shuttle IDs
- Robot axis states
- Station utilization
- Route conflicts
- Maintenance warnings
- Review-required markers

### Acceptance Criteria

- Diagnostics appear as 3D markers near objects.
- Diagnostics can be filtered by severity.
- Clicking a marker selects the related object.
- Diagnostics panel and 3D overlay stay synchronized.
- Screenshot export can include diagnostics.

---

## Object Libraries

Create reusable object behaviours that can be attached to scene objects.

Implement behaviours in the viewer's own **`ISceneBehaviour`** layer. Do not depend on PowerTools conveyor/robot/sensor components.

### Required Behaviours

- LinearMover
- Rotator
- PathFollower
- ShuttleFollower
- RobotJointDriver
- ConveyorSurfaceAnimator
- StatusColorDriver
- VisibilityDriver
- LabelDriver

### Behaviour Interface

```csharp
public interface ISceneBehaviour
{
    string BehaviourId { get; }
    void Initialize(SceneObjectModel model, GameObject gameObject);
    void Tick(float deltaTime);
    void ApplyRuntimeValue(RuntimeValue value);
}
```

### Conveyor Example

Initial implementation:

- Runtime speed input
- Direction vector
- UV offset animation on belt material
- Optional visual product flow later

Do not implement physical product simulation in the first version.

---

## Screenshot, Report, and Export Shell

### ScreenshotService

```csharp
public interface IScreenshotService
{
    Task<string> CaptureViewportAsync(ScreenshotSettings settings);
    Task<string> CaptureCameraAsync(Camera camera, ScreenshotSettings settings);
}
```

### Requirements

- Capture current viewport.
- Capture selected camera view.
- Optionally include diagnostics overlay.
- Export PNG.
- Store metadata JSON.

### Future Report Shell

Prepare structure for:

- Cover image
- Scene overview
- Object list
- Diagnostics list
- Runtime binding status
- Screenshots
- Review markers

Do not build a full report designer in MVP 1.

---

## Project Persistence

Use JSON as the first project format.

Introduce a **versioned JSON schema stub from Sprint 1** so persistence (Sprint 8) can extend the schema without rework. Diagnostics and runtime bindings are added to the schema in Sprint 7 before full save/load in Sprint 8.

### Project Folder Structure

```text
ProjectFolder/
├── project.viewer.json
├── assets/
│   ├── imported/
│   └── thumbnails/
├── bindings/
│   └── runtime-bindings.json
├── materials/
│   └── material-presets.json
├── diagnostics/
│   └── diagnostics.json
└── reports/
```

### project.viewer.json

```json
{
  "version": "1.0",
  "schemaVersion": 1,
  "scene": {
    "rootObjects": []
  },
  "viewSettings": {
    "activeViewMode": "Perspective3D",
    "cameraBookmarks": []
  },
  "runtime": {
    "bindings": []
  },
  "diagnostics": []
}
```

### Requirements

- Save project.
- Load project.
- Restore hierarchy.
- Restore transforms.
- Restore materials.
- Restore runtime bindings.
- Restore diagnostics.

---

## Performance Requirements

Large CAD files are a key risk.

Implement performance instrumentation early.

### Import Stats

Track:

- Mesh count
- Triangle count
- Material count
- Texture memory
- Bounds
- Import time
- Generated collider count

### Required Optimizations

- Bounds calculation
- Material deduplication
- Optional collider generation
- Async import where possible
- Object picking acceleration
- Hierarchy UI virtualization
- Static batching where safe
- GPU instancing for repeated objects
- Culling
- Lazy expansion of imported CAD hierarchy

### Rule

Do not create a full editable property UI for every tiny imported CAD submesh unless the user expands or selects it.

---

## Test Strategy

### EditMode Tests (from Sprint 1)

- `SceneRegistry` — add, remove, lookup, hierarchy
- `SceneObjectMapper` — create, update, destroy, ID round-trip
- JSON serialization — schema stub, round-trip of core model types

### PlayMode Tests (from Sprint 2)

- Navigation — orbit, pan, zoom, view presets
- Selection — viewport click select, additive select, clear

### Integration Verification

- **Manual demo** for MVP 1 end-to-end workflow (see First Internal Demo Target)
- Automated PlayMode/EditMode tests cover isolated modules; full integration validated manually until MVP 1.5

---

## Scene Viewer Parity Roadmap

This viewer is a **modern Unity successor** to B&R Scene Viewer with **intentional differences**. Use this section to set stakeholder expectations.

### Never In Scope

| Feature | Decision |
|---|---|
| PVI / ParID mask | **Never** — runtime binding uses OPC UA only |
| `.scn` import / ACOPOStrak assembly service | **Never** — GLB, OBJ, STL, and primitives only |
| `neexo-simulation-x` / simulation-contract import | **Not planned** — standalone CAD/engineering tool |

### In MVP 1 (This Plan)

| Feature | Sprint |
|---|---|
| Blender-like navigation | Sprint 2 |
| Viewport 3D drag + axis lock (X/Y/Z) | Sprint 3 |
| Hierarchy, properties, materials | Sprints 3–6 |
| GLB/OBJ/STL import | Sprint 5 |
| Diagnostics + mock runtime | Sprint 7 |
| Save/load + screenshots | Sprint 8 |

### Post-MVP (Without PVI)

| Feature | Target |
|---|---|
| Axes panel with sliders (numeric transform editing UI) | MVP 2 / 1.5 |
| Kinematic chains specification (child in parent coordinate system) | MVP 2 / 1.5 |
| Command-line startup args (`-connect`, `-fullscreen`, panel-hide) | MVP 2+ |
| ACOPOStrak assembly service integration | MVP 3+ |
| Presentation mode and guided camera paths | MVP 3 |
| Section views and measurement tools | MVP 3 |

### Intentional Deviations From Scene Viewer

- **Navigation:** Blender conventions (orbit, pan, fly mode) — not B&R Scene Viewer camera behaviour.
- **Data sources:** OPC UA via NeexoConnect — no PVI/ParID mask layer.
- **Import:** Industry CAD formats (GLB/OBJ/STL) — no proprietary `.scn` pipeline.

---

## MVP 1 - Solid 3D Engineering Viewer

### Goal

A user can import or create objects, structure the scene, navigate like Blender, manipulate objects in the viewport, inspect/edit properties, change materials, save/load the project, show visual statuses, and export screenshots.

### Required Features

- Unity 6 project foundation (Sprint 0–1)
- Scene domain model
- ServiceLocator + event bus
- GameObject mapping layer
- Blender-like navigation
- Viewport transform gizmos
- Object selection
- Hierarchy panel with reparent and search/filter
- Properties panel
- Primitive creation
- GLB import (glTFast)
- OBJ import
- STL import
- Material editor
- Visual status logic
- Save/load JSON project
- Screenshot export
- Simple diagnostics markers
- Mock runtime binding

### Explicitly Out Of Scope For MVP 1

- PVI / ParID mask (OPC UA only; never planned)
- `.scn` import (GLB/OBJ/STL + primitives only)
- `neexo-simulation-x` / `acopostrak-simulation-contract` import
- Full OPC UA production integration (prototype in Sprint 10 only)
- Advanced robot kinematics
- Automatic CAD simplification
- Full report designer
- Full simulation playback
- Multi-user collaboration
- Real mesh section cutting
- Advanced route conflict solving
- VContainer or other DI framework

### Timeline Estimate

Full MVP 1 as scoped above: **approximately 4–6 months** for a solo developer using Copilot/AI assistance (11 sprints including Sprint 0).

---

## MVP 2 - Runtime-Connected Viewer

### Add

- OPC UA connector (production-hardened NeexoConnect integration)
- Runtime binding UI
- Live transform binding
- Live material/status binding
- Alarm and warning overlay
- Shuttle position preview
- Robot joint value preview
- Conveyor material animation
- Runtime label system
- Playback data source
- Axes panel with sliders
- Kinematic chains specification

---

## MVP 3 - Shared Engineering Platform

### Add

- Object libraries for machine modules
- B&R visual presets
- Guided camera paths
- Presentation mode
- Report/export shell
- Section views
- Measurement tools
- Review markers
- Domain-specific validation
- Automation Studio variable mapping status
- Command-line startup arguments
- ACOPOStrak assembly service (if ever required — evaluate separately)

---

## Sprint Plan

**Team model:** Solo developer + Copilot/AI. One module at a time; use scoped Copilot prompts below.

**Sprint estimate:** 11 sprints (0–10); full MVP 1 ≈ 4–6 months solo + Copilot.

---

## Sprint 0 - Repo and AI Baseline

### Implement

- `git init` and initial commit structure
- README with project purpose, Unity 6 LTS requirement, and module overview
- `.github/copilot-instructions.md` — project conventions, domain-first architecture, service patterns
- `AGENTS.md` — agent/Copilot workflow guidance
- Scoped instruction files per module (optional but recommended)
- Assembly definition (`.asmdef`) per top-level module folder
- Solo developer + Copilot/AI workflow note in README

### Acceptance Criteria

- Repository initializes cleanly with documented setup steps.
- Copilot/AI agents have clear instructions for architecture and coding standards.
- Each top-level module has an asmdef boundary defined (may be empty stubs).

---

## Sprint 1 - Foundation

### Implement

- Unity 6 LTS project setup
- URP setup
- UI Toolkit shell
- ServiceLocator bootstrap
- Event bus stub
- SceneObjectModel
- SceneObjectType
- VisualStatus
- TransformData
- Scene registry
- GameObject mapping service
- Basic viewport camera
- **Versioned JSON schema stub** (`project.viewer.json` structure with `schemaVersion`)
- EditMode tests for SceneRegistry and Mapper

### Acceptance Criteria

- Empty project starts without errors.
- A root scene model exists.
- A SceneObjectModel can be created and mapped to a GameObject.
- Basic camera movement works.
- JSON schema stub file exists with version field.
- EditMode tests pass for registry and mapper basics.

---

## Sprint 2 - Navigation and Selection

### Implement

- Orbit
- Pan
- Zoom
- Focus selected
- Frame all
- Top/front/side views
- Perspective/orthographic toggle
- Fly mode
- Selection service
- Viewport picking
- Selection highlight
- Event bus integration for selection changes
- PlayMode tests for navigation and selection

### Acceptance Criteria

- Navigation matches Blender-style expectations.
- User can select objects in viewport.
- Selected object can be focused with F.
- Selection state is stored centrally and published via event bus.
- PlayMode tests pass for core navigation and selection flows.

---

## Sprint 3 - UI Panels and Viewport Gizmos

### Implement

- Hierarchy panel (UI Toolkit tree)
- Properties panel
- Common property editor
- Rename
- Visibility toggle
- Transform editing (properties panel)
- Scene object search/filter
- **Reparent objects** in hierarchy
- **Viewport transform gizmos** — 3D drag to move selected objects
- **Axis-constrained drag** — X/Y/Z key locks movement axis

### Acceptance Criteria

- Hierarchy reflects scene model.
- Selecting hierarchy object selects viewport object.
- Editing transform updates GameObject.
- Visibility toggle works.
- User can reparent objects in hierarchy.
- User can drag selected object in viewport; X/Y/Z keys constrain axis.
- Gizmo and properties panel transforms stay synchronized.

---

## Sprint 4 - Primitive Creation

### Implement

- Primitive factory
- Cube
- Cylinder
- Sphere
- Capsule
- Cone
- Plane
- Add Object UI
- Primitive property editing

### Acceptance Criteria

- User can create all primitive types.
- Created primitives appear in hierarchy.
- Created primitives can be selected and edited.

---

## Sprint 5 - Import Pipeline

### Implement

- Importer registry
- ImportSettings
- ImportResult with **domain-safe DTOs** (not raw Unity Mesh/Material)
- **GLB importer via glTFast**
- OBJ importer (library TBD — research spike: Assimp vs dedicated parser)
- STL importer (library TBD — research spike: Assimp vs dedicated parser)
- Bounds calculation
- Import warnings
- Import statistics
- Mapper conversion from import DTOs to Unity assets

### Acceptance Criteria

- User can import GLB (glTFast), OBJ, and STL.
- Imported object appears in hierarchy.
- Imported geometry is selectable.
- Import stats are visible or logged.
- Object focus works after import.
- ImportResult contains no direct Unity Mesh/Material references in the domain layer.

---

## Sprint 6 - Materials and Visual Status

### Implement

- MaterialDefinition
- Material editor UI
- Base material application
- Visual status overlay logic
- Category show/hide
- Preset registry
- B&R visual preset placeholders

### Acceptance Criteria

- User can change color, alpha, metallic, and roughness.
- Visual status can change object appearance.
- Original material state can be restored.
- Hidden/inactive state works.

---

## Sprint 7 - Diagnostics and Mock Runtime

### Implement

- DiagnosticMarker
- Diagnostics overlay system
- Diagnostics panel
- Billboard labels
- Severity filter
- RuntimeBinding model
- MockDataSource
- Basic transform binding
- Basic material/status binding
- Extend JSON schema stub with diagnostics and runtime binding sections

### Acceptance Criteria

- Diagnostics appear in 3D.
- Diagnostics are visible in panel.
- Clicking diagnostic selects target object.
- Mock runtime values can move or recolor objects.
- JSON schema includes diagnostics and runtime binding fields.

---

## Sprint 8 - Persistence and Screenshots

### Implement

- Project save
- Project load
- JSON serialization (including diagnostics and runtime bindings)
- EditMode serialization tests
- Screenshot service
- Camera bookmarks
- PNG export
- Screenshot metadata

### Acceptance Criteria

- User can save and reload a project.
- Object hierarchy, transforms, visibility, materials, diagnostics, and runtime bindings are restored.
- User can export screenshot from current view.
- EditMode serialization round-trip tests pass.

---

## Sprint 9 - Runtime Binding UI

### Implement

- Runtime binding panel
- Add/edit/remove binding
- Binding mode selector
- Address input
- Value preview
- Connection state display
- Mock source controls

### Acceptance Criteria

- User can create binding from UI.
- Binding updates object transform or material.
- Runtime value is visible in properties panel.

---

## Sprint 10 - OPC UA Prototype (NeexoConnect)

### Prerequisites

- Verify **Unity 6 LTS + NeexoConnect** compatibility before starting this sprint.

### Implement

- OpcUaDataSource via **NeexoConnect** (from neexoPowerTools — OPC layer only)
- Connection settings
- Subscribe/read values
- Error handling
- Runtime reconnection handling
- Simple variable browser if feasible

### Acceptance Criteria

- Viewer can connect to an OPC UA endpoint via NeexoConnect.
- Viewer can subscribe to at least one value.
- Value can drive object status or transform.
- Connection loss is shown clearly.
- NeexoRealtimeGatewayClient is not used as the primary path.

---

## Copilot Work Instructions

When using GitHub Copilot or Copilot Chat, implement one module at a time.

Do not ask Copilot to build the whole application in one prompt.

Use prompts like the following.

---

## Copilot Prompt 1 - Scene Domain Model

```text
Create the core domain model for a Unity engineering 3D viewer.

Implement these C# types:
- SceneObjectModel
- SceneObjectType
- VisualStatus
- TransformData
- MaterialDefinition
- RuntimeBinding
- DiagnosticMarker

Requirements:
- Use plain serializable C# classes where possible.
- Do not derive domain model classes from MonoBehaviour.
- Include stable string IDs.
- Include common properties and domain-specific properties dictionaries.
- Include comments explaining that Unity GameObjects are only runtime representations.
```

---

## Copilot Prompt 2 - Scene Registry and Mapper

```text
Create a SceneRegistry and SceneObjectMapper for a Unity 3D viewer.

Requirements:
- SceneRegistry stores SceneObjectModel instances by ID.
- SceneObjectMapper creates and updates Unity GameObjects from SceneObjectModel.
- Add a SceneObjectIdComponent MonoBehaviour that stores the model ID on each GameObject.
- Support create, update transform, update visibility, destroy, and lookup.
- Keep the domain model independent from Unity MonoBehaviours.
- Register both services with ServiceLocator during bootstrap.
```

---

## Copilot Prompt 3 - Blender-Like Camera Controller

```text
Create a Unity camera controller with Blender-like navigation.

Requirements:
- Middle mouse orbit.
- Shift + middle mouse pan.
- Scroll wheel zoom.
- F focuses selected object bounds.
- Home frames all visible objects.
- Numpad 7 top view.
- Numpad 1 front view.
- Numpad 3 side view.
- Numpad 5 toggles perspective and orthographic.
- Right mouse + WASD fly mode.
- Use the Unity Input System if available, but keep the logic testable.
- Split orbit, pan, zoom, focus, view presets, and fly mode into clear methods.
```

---

## Copilot Prompt 4 - Selection Service

```text
Create a selection system for a Unity 3D viewer.

Requirements:
- Central ISelectionService interface.
- Support single select, additive select, deselect, and clear.
- Store selected scene object IDs.
- Raycast from viewport mouse click to select GameObjects with SceneObjectIdComponent.
- Publish selection-changed events on the event bus.
- Notify UI and viewport highlight when selection changes.
- Keep selection state independent of UI implementation.
```

---

## Copilot Prompt 5 - Primitive Factory

```text
Create a primitive mesh factory for a Unity 3D viewer.

Requirements:
- Support Cube, Cylinder, Sphere, Capsule, Cone, and Plane.
- Return SceneObjectModel instances and create corresponding Unity GameObjects through the mapper.
- Include editable PrimitiveSettings with name, position, rotation, scale, and material.
- Cone can be generated procedurally if Unity has no built-in primitive for it.
- Created primitives must appear in the scene registry and be selectable.
```

---

## Copilot Prompt 6 - Import Pipeline

```text
Create a file import pipeline for a Unity 3D viewer.

Requirements:
- Define ISceneAssetImporter.
- Define ImportSettings, ImportResult (domain-safe DTOs: ImportedMeshData, ImportedMaterialData — NOT Unity Mesh/Material), ImportWarning, and ImportedAssetMetadata.
- Create an ImporterRegistry that selects importer by file extension.
- GLB importer uses glTFast.
- Add placeholder or research stubs for OBJ and STL (library TBD in Sprint 5).
- Each importer must create a SceneObjectModel root object.
- Each import must calculate bounds and basic statistics.
- The import pipeline must be async-compatible.
- A separate mapper step converts import DTOs to Unity Mesh/Material assets.
```

---

## Copilot Prompt 7 - Material System

```text
Create a material system for a Unity engineering viewer.

Requirements:
- Implement MaterialDefinition.
- Implement VisualStatus material logic.
- Support base color, alpha, metallic, roughness, and presets.
- Visual status must not permanently overwrite imported base materials.
- Implement status states: Normal, Valid, Warning, Error, RuntimeLive, HiddenInactive, ReviewRequired.
- Add a MaterialService that applies material definitions to Renderers.
- Register MaterialService with ServiceLocator.
```

---

## Copilot Prompt 8 - Properties Panel With Providers

```text
Create a UI Toolkit based properties panel for a Unity engineering viewer.

Requirements:
- Use a property provider pattern.
- Implement IPropertyProvider and PropertyDescriptor.
- Show common properties: Name, Type, Position, Rotation, Scale, Visibility, Material.
- Allow providers to add domain-specific properties.
- Subscribe to selection-changed events from the event bus.
- Update the selected SceneObjectModel when a property changes.
- Notify the SceneObjectMapper after model edits.
```

---

## Copilot Prompt 9 - Diagnostics Overlay

```text
Create a diagnostics overlay system for a Unity 3D viewer.

Requirements:
- Support DiagnosticMarker and DiagnosticSeverity.
- Render world-space icons and labels.
- Labels must billboard toward the active camera.
- Support severity filtering.
- Clicking a marker selects the target scene object.
- Diagnostics must also be available for a UI list panel.
```

---

## Copilot Prompt 10 - Runtime Binding System

```text
Create a generic runtime binding system for a Unity 3D viewer.

Requirements:
- Implement IRuntimeDataSource.
- Implement RuntimeBinding and BindingMode.
- Implement MockDataSource that emits test values.
- Implement RuntimeBindingService that applies runtime values to objects.
- Support transform position, transform rotation, material color, visibility, diagnostic state, robot joint, shuttle position, conveyor speed, and station utilization modes.
- Do not make OPC UA logic mandatory for the binding system.
- Use ISceneBehaviour for viewer-side animation; do not depend on PowerTools conveyor/robot components.
```

---

## Copilot Prompt 11 - Screenshot Service

```text
Create a screenshot service for a Unity 3D viewer.

Requirements:
- Capture current viewport.
- Capture a specific camera.
- Export PNG.
- Optionally include diagnostics overlay.
- Write metadata JSON with timestamp, camera position, selected object IDs, and active view mode.
```

---

## Copilot Prompt 12 - Project Save and Load

```text
Create a JSON project persistence system for a Unity 3D viewer.

Requirements:
- Save scene objects, hierarchy, transforms, visibility, materials, runtime bindings, diagnostics, camera bookmarks, and active view mode.
- Load the project and recreate scene model and GameObjects.
- Use a project.viewer.json root file with schemaVersion field.
- Keep imported assets in an assets/imported folder.
- Keep bindings, diagnostics, materials, and reports in separate folders.
- Include EditMode tests for serialization round-trip.
```

---

## Copilot Prompt 13 - App Bootstrap + ServiceLocator + Event Bus

```text
Create the application bootstrap for a Unity 3D engineering viewer.

Requirements:
- AppBootstrap MonoBehaviour or entry point that runs on play/start.
- ServiceLocator with register, resolve, and optional try-resolve.
- Register core services: SceneRegistry, ISceneObjectMapper, ISelectionService, MaterialService.
- Lightweight event bus with subscribe, unsubscribe, and publish for typed events.
- Define events: SelectionChangedEvent, SceneObjectChangedEvent, HierarchyChangedEvent.
- MonoBehaviours only forward input/lifecycle; no business logic in MonoBehaviours.
- Do not use VContainer or other DI containers.
- Document service registration order in comments.
```

---

## Copilot Prompt 14 - Hierarchy Panel

```text
Create a UI Toolkit hierarchy panel for a Unity 3D engineering viewer.

Requirements:
- TreeView showing SceneObjectModel parent/child structure from SceneRegistry.
- Click row to select object (publish via ISelectionService / event bus).
- Rename inline or via context action.
- Toggle visibility per object.
- Reparent via drag-and-drop or explicit reparent UI.
- Delete object with confirmation.
- Show icon per SceneObjectType.
- Search/filter box to filter visible tree nodes.
- Subscribe to hierarchy-changed and selection-changed events for sync.
- Do not hardcode direct references to properties panel; use event bus.
```

---

## Copilot Prompt 15 - Viewport Transform Gizmos

```text
Create viewport transform gizmos for a Unity 3D engineering viewer.

Requirements:
- When object(s) selected, show move gizmo in viewport.
- Left-drag on gizmo or selected object moves along camera-relative or world axes.
- Hold X, Y, or Z key during drag to constrain movement to that world axis.
- Updates TransformData in SceneObjectModel via SceneRegistry.
- SceneObjectMapper updates GameObject transform after model change.
- Publish SceneObjectChangedEvent so properties panel syncs.
- Use Input System for mouse and keyboard axis-lock keys.
- Keep gizmo logic in a dedicated service/class, not in UI scripts.
```

---

## Definition Of Done

A feature is done only when:

- It compiles without errors.
- It has no hardcoded test-only scene dependencies.
- It works from UI or a documented service call.
- It updates the domain model and Unity representation correctly.
- It is testable in isolation where practical.
- It handles invalid input without crashing.
- It has clear logs for failure cases.
- It does not break project save/load.

---

## Coding Standards

### General

- Prefer small focused services.
- Avoid large MonoBehaviour classes.
- Keep domain models separate from Unity runtime components.
- Use ServiceLocator for service access in MVP 1; event bus for cross-module notifications.
- Use interfaces for importers, data sources, selection, materials, screenshots, and persistence.
- Avoid hardcoded colors and presets in UI scripts.
- Avoid direct cross-panel dependencies.

### Naming

Use explicit engineering names:

- `SceneObjectModel`
- `SceneRegistry`
- `SceneObjectMapper`
- `RuntimeBindingService`
- `DiagnosticsOverlaySystem`
- `MaterialStatusService`
- `ViewerCameraController`
- `PrimitiveFactory`
- `ImporterRegistry`
- `ServiceLocator`
- `EventBus`

### Error Handling

- Import failures must show useful messages.
- Runtime connection loss must not crash the viewer.
- Missing bindings must be marked clearly.
- Invalid property values must be rejected or clamped.

---

## Technical Risks

### Large CAD Files

Risk:
Performance problems from many meshes and materials.

Mitigation:

- Import stats
- Lazy hierarchy expansion
- Optional collider generation
- Material deduplication
- Culling and batching

### Runtime Binding Coupling

Risk:
OPC UA code becomes hardcoded into scene objects.

Mitigation:

- Keep `IRuntimeDataSource` abstraction.
- Build with `MockDataSource` first.
- Use NeexoConnect only inside `OpcUaDataSource`; do not leak OPC types into scene model.

### Unity 6 + NeexoConnect Compatibility

Risk:
NeexoConnect from neexoPowerTools may not be verified on Unity 6 LTS; Sprint 10 OPC UA prototype could be blocked.

Mitigation:

- Run compatibility verification before Sprint 10 (spike in Sprint 8–9 if needed).
- Keep MockDataSource as fallback demo path.
- Document minimum supported NeexoConnect version once verified.

### Properties Panel Complexity

Risk:
All domain-specific properties end up in one large UI class.

Mitigation:

- Use `IPropertyProvider` registry.

### Material State Corruption

Risk:
Visual status permanently overwrites imported material values.

Mitigation:

- Keep base material and status overlay separate.

### Unity Scene As Project Format

Risk:
Project becomes difficult to version, validate, and export.

Mitigation:

- Use JSON domain model as source of truth.
- Versioned schema from Sprint 1; extend incrementally.

### Dictionary Serialization

Risk:
`Dictionary<string, object>` does not serialize cleanly to JSON in Unity.

Mitigation:

- Resolve typed property bag or `JsonElement` strategy in Sprint 1 (open point).

---

## Open Points

| Topic | Status |
|---|---|
| STL/OBJ parser libraries | TBD — Assimp vs dedicated parsers; research in Sprint 5 |
| `Dictionary<string, object>` serialization | TBD — typed property bags vs `JsonElement`/custom serializers |
| Sprint timeline | Full MVP 1 solo + Copilot: **~4–6 months** (11 sprints) |
| Command-line args / presentation mode | Confirm priority when approaching MVP 2 |
| Unity 6 + NeexoConnect compatibility | Verify before Sprint 10 |

---

## Plan Assessment Summary

| Dimension | Status | Notes |
|---|---|---|
| Architecture | Strong | Domain-first + ServiceLocator + event bus |
| MVP scope | Full MVP 1 | Accept longer timeline (4–6 months solo + Copilot) |
| Sprint plan | Updated | Sprints 0–10; diagnostics before persistence; gizmos in Sprint 3 |
| Scene Viewer parity | Documented | Parity roadmap; PVI/.scn never; Blender navigation intentional |
| Neexo integration | Scoped | NeexoConnect OPC only; no simulation-contract import |
| Copilot readiness | Strong | 15 modular prompts including bootstrap, hierarchy, gizmos |
| Implementability | High | Ready to execute after Sprint 0 repo baseline |

---

## First Internal Demo Target

The first demo should show this workflow:

1. Open empty viewer.
2. Import a GLB, OBJ, or STL file.
3. Add a primitive cube and cylinder.
4. Select objects in viewport and hierarchy.
5. Navigate using Blender-like controls.
6. Drag selected object in viewport; use X/Y/Z axis lock.
7. Edit transform and material properties.
8. Apply visual status: valid, warning, error, runtime live.
9. Add a diagnostic marker.
10. Simulate mock runtime data moving or recoloring an object.
11. Save project.
12. Reload project.
13. Export screenshot.

---

## Final Guidance For Copilot

Build this as an extensible engineering platform, not as a demo scene.

This is a **standalone CAD/engineering viewer** — do not add neexo-simulation-x import, `.scn` import, or PVI/ParID mask layers.

Prioritize:

1. Sprint 0 repo and AI baseline (asmdef, copilot-instructions, AGENTS.md)
2. Stable data model + versioned JSON schema stub
3. ServiceLocator + event bus (no VContainer in MVP 1)
4. Reliable Blender-like navigation
5. Viewport transform gizmos (Sprint 3)
6. Clean service interfaces
7. Import pipeline (GLB via glTFast; OBJ/STL TBD)
8. Properties and hierarchy synchronization
9. Material/status logic
10. Diagnostics + mock runtime binding (before persistence)
11. Save/load + screenshots
12. OPC UA via NeexoConnect (Sprint 10, after compatibility check)

Do not start with OPC UA.
Do not use NeexoRealtimeGatewayClient as the primary OPC path.
Do not reuse PowerTools conveyors/robots/sensors — build viewer `ISceneBehaviour` layer.
Do not hardcode machine-specific assumptions into core viewer systems.
Do not let UI scripts become the application architecture.
