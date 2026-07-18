# Sprints 1–4 Implementation Status

## Summary

Unity 6 project scaffold and Sprints 1–4 feature goals are implemented, and the editor architecture foundation has since been finalized around a domain-first scene model, explicit edit service, projection adapter, and project round-trip persistence.

## Sprint 1 — Foundation

| Item | Status | Location |
|------|--------|----------|
| Unity 6 + URP + Input System + UI Toolkit | Done | `Packages/manifest.json`, `ProjectSettings/`, `Assets/Settings/URP-*` |
| Module folders + asmdef | Done | `App/`, `Core/`, `Viewer/`, `SceneModel/`, `UI/`, `Import/`, `Tests/` |
| Domain model | Done | `Assets/SceneModel/Core/` |
| SceneRegistry | Done | `SceneRegistry.cs` |
| Projection adapter + ID component | Done | `Assets/Viewer/Projection/SceneProjectionService.cs`, `SceneObjectIdComponent.cs` |
| ServiceLocator + EventBus | Done | `Assets/Core/Bootstrap/` |
| App bootstrap | Done | `AppBootstrap.cs` |
| UI Toolkit shell | Done | `UI/Shell/ViewerShell.uxml` |
| Basic / full camera | Done | `ViewerCameraController.cs` (+ sub-controllers, expanded in Sprint 2) |
| Project schema v2 + v1 migration | Done | `Assets/SceneModel/Serialization/`, `Assets/App/ProjectSystem/ProjectSerializer.cs` |
| EditMode tests | Done | `Tests/EditMode/` |

## Sprint 2 — Navigation and Selection

| Item | Status | Location |
|------|--------|----------|
| Orbit / Pan / Zoom / Fly / View presets / Focus | Done | `Viewer/Camera/*Controller.cs` |
| Blender bindings (MMB, Shift+MMB, scroll, F, Home, numpad, Shift+F, RMB+WASD) | Done | `ViewerCameraController.cs` |
| ISelectionService + SelectionService | Done | `Viewer/Selection/` |
| Viewport raycast picking | Done | `ViewportSelectionInput.cs` |
| Selection highlight | Done | `SelectionHighlighter.cs` |
| PlayMode tests | Done | `Tests/PlayMode/NavigationTests.cs`, `SelectionTests.cs` |

## Sprint 3 — UI Panels + Gizmos

| Item | Status | Location |
|------|--------|----------|
| Hierarchy tree (select, rename, visibility, delete, search, icons) | Done | `HierarchyPanelController.cs` |
| Reparent (dropdown + button) | Done | Same |
| Properties panel + IPropertyProvider | Done | `PropertiesPanelController.cs`, `CommonPropertyProvider.cs` |
| Selection sync (hierarchy ↔ viewport ↔ properties) | Done | EventBus + `ISelectionService` |
| Transform gizmo drag + X/Y/Z axis lock | Done | `TransformGizmoController.cs` |

## Sprint 4 — Primitive Creation

| Item | Status | Location |
|------|--------|----------|
| IPrimitiveFactory + PrimitiveFactory | Done | `Import/Primitives/` |
| Cube, Cylinder, Sphere, Capsule, Cone, Plane | Done | `PrimitiveMeshType.cs`, procedural cone |
| Add Object UI | Done | `AddObjectPanelController.cs`, shell UXML buttons |

## How to open and test

1. **Open project:** Unity Hub → Add → `unity-simulation-x` → Unity 6 LTS.
2. **First open:** Allow package restore (URP, Input System). URP assets may need one-time upgrade prompt — accept defaults.
3. **Scene:** `Assets/Scenes/ViewerMain.unity` → Play.
4. **Manual checks:**
   - MMB orbit, Shift+MMB pan, scroll zoom
   - Add primitives from left panel; appear in hierarchy
   - Import OBJ/STL into the project workspace; save and load the project folder
   - Click object in viewport or hierarchy; properties update
   - Edit position in properties or drag in viewport; hold X/Y/Z to constrain axis
   - Toggle visibility in hierarchy
   - Reparent via dropdown + Reparent button
5. **Tests:** `Window > General > Test Runner` → EditMode + PlayMode → Run All.

## Architecture foundation status

- `SceneModel` is projection-free: no MonoBehaviours, `GameObject` fields, runtime bindings, or diagnostic marker stubs remain in the runtime model.
- All committed feature edits route through `ISceneEditService` and publish `SceneChangedEvent`.
- Imported assets are project-owned and saved as relative paths under `assets/imported/`.
- Project save/load uses schema version 2 with explicit schema version 1 migration.
- Contribution registries are explicit and frozen after composition; no runtime plugin discovery was added.
- `.glb` remains a typed adapter-unavailable path; glTFast was intentionally not added.

## Known limitations / follow-ups

- Unity Editor was not run in the cloud implementation environment; first local open may require script recompile and URP pipeline assignment confirmation.
- Hierarchy reparent uses dropdown (not drag-and-drop tree reorder).
- Multi-select gizmo drag remains deferred (single selection move only).
- Domain libraries, runtime binding, and undo/redo are intentionally out of scope for this architecture foundation.

## Blockers

None for local development. Requires Unity 6 LTS install on the developer machine.
