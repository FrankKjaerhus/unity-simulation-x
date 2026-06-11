# Unity Simulation X Viewer

Unity 6 LTS engineering 3D viewer — domain-first scene model, Blender-like navigation, UI Toolkit panels, and procedural primitives.

## Requirements

- **Unity 6 LTS** (tested target: `6000.0.38f1`)
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
| `Core` | ServiceLocator, EventBus, shared events |
| `SceneModel` | Domain model, registry, GameObject mapper |
| `Viewer` | Camera navigation, selection, gizmos |
| `UI` | Hierarchy, properties, add-object panels |
| `Import` | Primitive factory (GLB/OBJ/STL in Sprint 5+) |
| `App` | Bootstrap and project JSON stub |
| `Tests` | EditMode and PlayMode tests |

## Architecture

- **Domain model is source of truth** — not MonoBehaviours.
- **ServiceLocator + EventBus** for wiring (no VContainer in MVP 1).
- MonoBehaviours forward input/lifecycle only.

## Tests

Unity Test Runner (`Window > General > Test Runner`):

- **EditMode:** `SceneRegistry`, `SceneObjectMapper`, JSON schema stub
- **PlayMode:** navigation controllers, selection service

## Solo + Copilot workflow

Implement one module at a time. See `COPILOT_IMPLEMENTATION_PLAN.md` and `.github/copilot-instructions.md`.
