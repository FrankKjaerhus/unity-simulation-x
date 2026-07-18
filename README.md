# Unity Simulation X Viewer

Unity 6 LTS engineering 3D editor with a domain-first scene model, Blender-like navigation, UI Toolkit panels, project-owned imported assets, and validated project round-trip persistence.

## Requirements

- **Unity 6 LTS** (tested target: `6000.4.5f1`)
- Universal Render Pipeline (URP)
- Input System package
- UI Toolkit

## Open the project

1. Install Unity 6 LTS via Unity Hub.
2. Add project from `unity-simulation-x` folder.
3. Open `Assets/Scenes/ViewerMain.unity`.
4. Press Play.

## Module layout

| Module | Purpose |
|--------|---------|
| `Core` | ServiceLocator bridge, EventBus, shared project interfaces |
| `SceneModel` | Authoritative scene snapshots, invariants, type IDs, schema DTOs |
| `Editing` | `ISceneEditService`, change sets, component codecs, factory registries |
| `Viewer` | Projection, camera navigation, selection, gizmos |
| `UI` | Hierarchy, properties, library, shell panels |
| `Import` | OBJ/STL import, primitive factory, imported-asset projection cache |
| `App` | Bootstrap, project workspace, validation, atomic save/load |
| `Tests` | EditMode and PlayMode tests |

## Architecture

- **Domain model is source of truth** — `SceneObjectModel` is authoritative, not MonoBehaviours.
- **Mutation boundary is explicit** — feature code commits edits through `ISceneEditService`.
- **Projection is adapter-only** — `SceneProjectionService` mirrors scene snapshots into runtime `GameObject`s.
- **ServiceLocator + EventBus** compose the app (no VContainer in MVP 1).
- MonoBehaviours forward input/lifecycle only.

### Assembly dependency graph

```text
SceneModel
   ↓
Editing
   ↓
Viewer / UI / Import
   ↓
App
```

`Core` provides shared contracts used by the higher-level modules. `SceneModel` does not reference `Viewer`, `Import`, or `App`.

### Responsibilities

- `SceneModel` owns IDs, transforms, materials, component envelopes, registry invariants, and project schema migration.
- `Editing` owns committed scene mutations, change notifications, and extension registries.
- `Viewer`, `UI`, and `Import` adapt the editor story to Unity runtime behavior without becoming sources of truth.
- `App` wires services, project workspaces, document validation, and persistence.

### Project folder structure

Saved projects use a folder root containing:

```text
<project-root>/
├── project.viewer.json
└── assets/
    └── imported/
        └── <asset-id>.<ext>
```

Imported asset paths stored in the document are always project-relative.

### Project schema

- Current document format is **schema version 2**.
- **Schema version 1** still loads through an explicit in-memory migration.
- Legacy `primitiveMeshTypeKey` values are migrated into the component envelope `com.unitysimulationx.scene.primitive-mesh`.
- Unknown component `payloadJson` is preserved exactly through save/load.

### Deliberate non-goals in this foundation

- No VContainer or other heavy DI framework
- No runtime binding system
- No plugin discovery or reflection-based module loading
- No glTFast dependency
- No undo stack

`.glb` remains explicitly registered and currently returns the typed error `import.glb.adapter-unavailable` until the planned adapter package sprint lands.

## Tests

Unity Test Runner (`Window > General > Test Runner`):

- **EditMode:** registry invariants, component codecs, schema migration, persistence validation, architecture boundaries
- **PlayMode:** navigation controllers, selection service, import → edit → save → load round-trip

## Solo + Copilot workflow

Implement one module at a time. See `COPILOT_IMPLEMENTATION_PLAN.md` and `.github/copilot-instructions.md`.
