# Sprints 1–4 Implementation Status

## Summary

Unity 6 project scaffold and Sprints 1–4 features are implemented as code and project assets. Open in Unity 6 LTS to compile packages and run tests.

## Sprint 1 — Foundation

| Item | Status | Location |
|------|--------|----------|
| Unity 6 + URP + Input System + UI Toolkit | Done | `Packages/manifest.json`, `ProjectSettings/`, `Assets/Settings/URP-*` |
| Module folders + asmdef | Done | `App/`, `Core/`, `Viewer/`, `SceneModel/`, `UI/`, `Import/`, `Tests/` |
| Domain model | Done | `Assets/SceneModel/Core/` |
| SceneRegistry | Done | `SceneRegistry.cs` |
| ISceneObjectMapper + mapper + ID component | Done | `SceneObjectMapper.cs`, `SceneObjectIdComponent.cs` |
| ServiceLocator + EventBus | Done | `Assets/Core/Bootstrap/` |
| App bootstrap | Done | `AppBootstrap.cs` |
| UI Toolkit shell | Done | `UI/Shell/ViewerShell.uxml` |
| Basic / full camera | Done | `ViewerCameraController.cs` (+ sub-controllers, expanded in Sprint 2) |
| JSON schema stub | Done | `App/ProjectSystem/project.viewer.json`, `ProjectViewerSchema.cs` |
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
   - Click object in viewport or hierarchy; properties update
   - Edit position in properties or drag in viewport; hold X/Y/Z to constrain axis
   - Toggle visibility in hierarchy
   - Reparent via dropdown + Reparent button
5. **Tests:** `Window > General > Test Runner` → EditMode + PlayMode → Run All.

## Known limitations / follow-ups

- Unity Editor was not run in the implementation environment; first open may require script recompile and URP pipeline assignment confirmation.
- Hierarchy reparent uses dropdown (not drag-and-drop tree reorder).
- Multi-select gizmo drag deferred (single selection move only).
- `Dictionary<string, object>` on `SceneObjectModel` not JSON-round-tripped yet (open point from plan).
- Material editor and visual status are Sprint 6.

## Blockers

None for local development. Requires Unity 6 LTS install on the developer machine.
