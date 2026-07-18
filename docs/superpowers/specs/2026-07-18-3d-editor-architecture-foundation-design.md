# 3D Editor Architecture Foundation Design

**Status:** Approved direction, refined by critical review  
**Date:** 2026-07-18

## Purpose

Build a durable offline Unity 3D editor foundation. Users can compose a scene, import 3D assets, edit hierarchy and properties, and save/reopen the complete project without data loss.

ACOPOStrak, ACOPOS 6D, Robotics, live binding, OPC UA, and automation control are not part of this design. Future domain libraries must be addable without changing the editor's fundamental scene, persistence, or UI architecture.

## Product Decisions

- Unity 3D editor functionality is the product foundation.
- Current scope is editor core plus narrow future extension seams.
- Future domain libraries begin in the same repository and may later move into Unity packages.
- Future library objects use open, namespaced type IDs and typed domain components.
- No runtime binding architecture is introduced now.
- No dynamic plugin loading, reflection discovery, or general plugin framework is introduced now.
- Unity GameObjects are projections of the scene model, never the source of truth.

## Critical Review Outcome

The existing direction is sound, but four issues must be corrected before feature growth:

1. Imported GLB/STL/OBJ data does not currently survive project save/load.
2. The closed `SceneObjectType` enum and `Dictionary<string, object>` property bags conflict with future library extensibility.
3. UI, gizmos, import, and persistence mutate `SceneRegistry` and projections independently.
4. `SceneModel` currently owns Unity projection types such as `GameObject`, `MonoBehaviour`, meshes, and materials.

“Plugin-ready” therefore means stable persisted data, open IDs, opaque unknown-component preservation, project-owned assets, and narrow contribution registries. It does not mean building a plugin runtime.

## Target Module Boundaries

```text
SceneModel
  scene data, IDs, component envelopes, hierarchy invariants
       ↑
Editing
  mutation use cases, transactions, change sets, projection contracts
       ↑
Viewer             Import              UI
  Unity projection   parser adapters     UI Toolkit panels/providers
       ↑                ↑                   ↑
       └────────────────┴───────────────────┘
                         App
             explicit composition root and persistence
```

### `SceneModel`

- Owns `SceneObjectModel`, `SceneObjectTypeId`, `SceneComponentData`, `SceneRegistry`, and serializable project DTOs.
- May use Unity value types such as `Vector3`, `Color`, and `Bounds`.
- Must not contain `GameObject`, `Transform`, `MonoBehaviour`, renderer, mesh-building, shader, or ServiceLocator bridge logic.
- Owns and enforces hierarchy invariants.

### `Editing`

- New application-layer module.
- Exposes `ISceneEditService`, edit requests/results, `SceneChangeSet`, and `ISceneProjectionService`.
- Implements the only supported mutation flow:

```text
validate → mutate model → update projection → publish one change set
```

- Provides scene replacement as one transaction for project loading.
- Establishes the future undo seam without implementing an undo stack.

### `Viewer`

- Owns Unity projection, camera, selection, gizmos, grid, and viewport tools.
- Maps scene data to GameObjects.
- Gizmo drag may preview a transform directly on the projection, but commits exactly one model edit through `ISceneEditService`.

### `Import`

- Resolves file formats through importer adapters.
- Parsers return import DTOs and warnings, not persisted GameObject references.
- The application imports from project-owned asset paths.
- Format-specific details remain behind `ISceneAssetImporter`.

### `UI`

- Owns UI Toolkit views, presenters/controllers, property descriptors, and provider registries.
- Reads scene state and submits edits through `ISceneEditService`.
- Does not mutate models or registry collections directly.
- Does not hardcode future domain types.

### `App`

- Remains the composition root.
- Creates concrete services and injects explicit dependencies.
- `ServiceLocator` is limited to attaching scene MonoBehaviours and UI controllers to already-composed services.
- Contains project workspace and persistence adapters.

## Canonical Scene Model

Core editor concepts remain direct fields; they are not needlessly converted into components:

```text
SceneObjectModel
├── Id                 stable document-unique GUID
├── TypeId             open namespaced SceneObjectTypeId
├── Name
├── ParentId           hierarchy source of truth
├── Transform
├── Visible
├── Material
├── AssetId?           project asset catalog reference
└── Components[]       future domain component envelopes
```

`ChildrenIds` is derived and registry-owned. Consumers receive read-only views.

### IDs

- Object IDs are stable GUID strings.
- Type, component, importer, factory, and future library item IDs use reverse-DNS namespacing.
- Examples:
  - `com.unitysimulationx.scene.root`
  - `com.unitysimulationx.scene.primitive`
  - `com.unitysimulationx.scene.imported-model`
  - `com.br-automation.acopostrak.track-segment` (future only)
- IDs are case-sensitive, never derived from display names, and never reused with different meaning.

### Domain Component Persistence

```json
{
  "typeId": "com.vendor.product.component",
  "schemaVersion": 1,
  "payloadJson": "{}"
}
```

Known future libraries may materialize an envelope as a typed C# component through a registered codec. If its codec is unavailable or its schema version is newer:

- the envelope remains attached to the object;
- it is displayed as unknown/read-only;
- it is saved again byte-for-byte without data loss.

No concrete ACOPOS, robotics, diagnostics, or runtime-binding components are created in this foundation.

## Scene Registry Invariants

`SceneRegistry` is the aggregate boundary and must guarantee:

- non-empty, unique object IDs;
- valid namespaced type IDs;
- every non-root parent exists;
- no self-parenting or cycles;
- `ParentId`, roots, and derived children always agree;
- component type IDs are unique per object;
- an object ID cannot change after registration;
- cascade removal has deterministic ordering;
- failed edits leave registry state unchanged.

`SceneRegistry` throws `SceneInvariantException` for invariant violations. `ISceneEditService` catches that exception at the application boundary and returns a typed `SceneEditResult`; public editor operations must not silently no-op.

## Editing and Change Notification

`ISceneEditService` provides explicit operations for create, remove, rename, visibility, transform, material, reparent, component replacement, and whole-scene replacement.

Each committed operation produces one immutable `SceneChangeSet` containing:

- operation kind;
- affected object IDs;
- whether hierarchy changed;
- the new registry revision.

UI and viewer subscribers consume change sets. Feature services do not manually publish duplicate hierarchy/object events.

Full undo/redo is deferred. Edit operations and immutable change sets form its future boundary.

## Project Workspace and Assets

Every editing session has an `IProjectWorkspace`:

- a temporary workspace for an unsaved project;
- a persistent project root after Save As or Load.

External imports are copied into the workspace before parsing:

```text
MyProject/
├── project.viewer.json
├── assets/
│   └── imported/
│       └── {assetId}.{extension}
└── cache/                 optional and always regenerable
```

The asset catalog stores:

```text
assetId
relativePath
originalFileName
mediaType
contentHash
importerId
importerVersion
importSettings
```

Saved documents never contain absolute source paths. Relative paths are normalized and cannot escape the project root.

Save As copies required immutable assets first and atomically replaces `project.viewer.json` last. Old referenced assets are not deleted during save, ensuring the previous document remains valid if saving fails.

## Project Format and Lifecycle

Schema version 2 contains:

```text
ProjectViewerDocument
├── schemaVersion
├── assets[]
├── scene.objects[]
└── viewSettings
```

Scene objects store core fields plus component envelopes. Schema version 1 is migrated in memory and never mutated on disk during load.

### Save

1. Snapshot the current registry and workspace catalog.
2. Validate the snapshot.
3. Encode the complete document.
4. Write a temporary file in the target directory.
5. Flush and atomically replace `project.viewer.json`.
6. Update current project state only after success.

### Load

1. Read and decode without touching the active scene.
2. Migrate supported older schema versions.
3. Validate IDs, hierarchy, type IDs, asset references, and paths.
4. Resolve and parse all required assets into staged import results.
5. Construct a candidate scene snapshot.
6. Replace the active model and projection as one edit transaction.
7. Restore the previous snapshot if projection activation fails.

A catalog reference to a missing file creates a visible placeholder and warning while preserving object and asset metadata. A missing catalog entry, invalid hierarchy, duplicate ID, path traversal, or unsupported core schema aborts load and leaves the active project unchanged.

## Import Pipeline

```text
external file
  → copy into project workspace
  → select importer by extension
  → parse into import DTO
  → create scene object through ISceneEditService
  → project DTO/model into Unity GameObjects
```

OBJ and STL remain DTO-based. GLB remains behind the same adapter contract, but adding or replacing glTFast is outside this foundation plan in accordance with repository scope rules.

Importers return typed results with warnings/errors and accept cancellation. Default configurable limits are 512 MiB source size, 5,000,000 vertices, and 15,000,000 indices; exceeding a limit fails before scene mutation. Performance caches are optional and never canonical.

## Future Extension Seams

The foundation introduces narrow registries only where the editor already needs them:

- scene type descriptors;
- component codecs;
- object factories;
- property providers;
- importers.

Registration is explicit and deterministic in `AppBootstrap`. Duplicate IDs fail startup. Registries freeze after composition.

`IDomainModule`, package discovery, dependency resolution, unload, hot reload, marketplace installation, and arbitrary plugin UI are deferred until the first real domain library provides concrete requirements.

## Error Handling

Public application operations return typed results containing errors and warnings. Expected user/data errors are not represented solely by logs.

- Invalid edits do not mutate model or projection.
- Invalid project documents do not replace the active scene.
- Failed imports do not leave partial registry objects or copied catalog entries.
- Subscriber failures are isolated so one UI listener cannot block others.
- Errors include stable codes suitable for future UI presentation and tests.

## Testing Strategy

### EditMode

- ID validation and open custom type IDs.
- Registry hierarchy invariants and failed-edit atomicity.
- One projection update and one change set per edit.
- Known and unknown component envelope round-trip.
- Version 1 to version 2 migration.
- Atomic save behavior.
- Path traversal rejection.
- Imported asset copy and relative reference persistence.
- Invalid load leaves existing registry unchanged.
- Missing asset creates placeholder without losing metadata.
- Property/importer registry duplicate handling.

### PlayMode

- Model edits update GameObject projections.
- Gizmo preview commits once.
- Import → save → load restores a selectable object.
- Missing imported asset produces a visible selectable placeholder.

EditMode and PlayMode suites run in Unity Test Runner after each implementation task.

## Explicitly Out of Scope

- ACOPOStrak, ACOPOS 6D, and Robotics models or behavior.
- Runtime binding, OPC UA, PLC communication, or control commands.
- Full undo/redo history and UI.
- Dynamic plugin or DLL loading.
- Reflection-based module discovery.
- Runtime package installation, plugin unload, or hot reload.
- Arbitrary plugin-owned UI panels.
- Addressables for user-imported project files.
- glTFast package integration.
- Generated mesh cache optimization.

## Acceptance Criteria

1. `SceneModel` contains no GameObject projection or ServiceLocator bridge.
2. All editor mutations pass through `ISceneEditService`.
3. Registry invariants cannot be bypassed by UI, viewer, import, or persistence.
4. Open namespaced scene type IDs are preserved exactly.
5. Unknown component envelopes round-trip without modification.
6. Imported OBJ/STL files are project-owned and referenced only by relative paths.
7. Save is atomic at the canonical document boundary.
8. Invalid loads leave the active project unchanged.
9. Missing imported assets remain represented by selectable placeholders.
10. Future contribution registries exist without introducing a general plugin framework.
