# Copilot instructions — Unity Simulation X Viewer

## Product

Standalone Unity 6 engineering viewer for machine scenes: hierarchy, properties, Blender navigation, primitives, later CAD import and runtime binding.

## Code standards

1. **Domain model** lives in `Assets/SceneModel/` — plain C# classes, serializable where possible.
2. **ServiceLocator** (`UnitySimulationX.Core`) registers services at bootstrap; resolve interfaces, not concrete UI types.
3. **EventBus** for selection, hierarchy, and property sync — panels must not call each other directly.
4. **MonoBehaviours** only forward input (camera, picking, gizmos) and hold IDs via `SceneObjectIdComponent`.
5. **Assembly boundaries** — add code to the correct module asmdef; avoid circular references (Core is shared, App bootstraps only).

## Naming

Use plan names: `SceneObjectModel`, `SceneRegistry`, `SceneObjectMapper`, `ViewerCameraController`, `PrimitiveFactory`.

## Sprint scope reminder

- Sprints 1–4 (current): foundation, navigation, UI panels/gizmos, primitives.
- Sprint 5+: import (glTFast), Sprint 6: materials, Sprint 7: diagnostics/mock runtime, Sprint 8: save/load.

## Prompting

Implement one module per task. Reference `COPILOT_IMPLEMENTATION_PLAN.md` for interfaces and acceptance criteria.
