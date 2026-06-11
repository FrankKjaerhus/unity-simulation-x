# Agent / Copilot guidance

## Architecture

- Domain-first: `SceneObjectModel` in `SceneModel`, not MonoBehaviours.
- Register services in `AppBootstrap` via `ServiceLocator`.
- Cross-module notifications via `EventBus` (`SelectionChangedEvent`, `SceneObjectChangedEvent`, `HierarchyChangedEvent`).
- Update flow: edit model → `SceneRegistry` → `ISceneObjectMapper` → GameObject.

## Conventions

- One `.asmdef` per top-level module under `Assets/`.
- Small focused services with interfaces (`ISelectionService`, `IPrimitiveFactory`, `IPropertyProvider`).
- Blender-like camera in `Viewer/Camera/` — do not use Scene View defaults.
- UI Toolkit only for editor-style panels.

## Do not

- Add VContainer or heavy DI frameworks in MVP 1.
- Put business logic in MonoBehaviours.
- Add OPC UA, glTFast import, or full materials until the planned sprints.

## Verification

Run EditMode and PlayMode tests in Unity Test Runner after changes.

## Unity MCP (Cursor super user)

For Play mode debugging, console logs, and Test Runner from Cursor Agent mode:

1. Open the project in **Unity 6** with `ViewerMain.unity`.
2. Ensure **Tools → MCP Unity → Server Window** shows the WebSocket server running (port **8090**). Auto-start is enabled via `ProjectSettings/McpUnitySettings.json`.
3. In Cursor, use **Agent mode** (not Ask/Plan). Project MCP config: [`.cursor/mcp.json`](.cursor/mcp.json) → `mcp-unity` Node server.
4. After changing MCP config, **Reload Window** in Cursor (`Ctrl+Shift+P` → Developer: Reload Window).

If the Node server is missing, run **Force Install Server** in the MCP Unity Server Window, or from the repo root:

```bash
cd Packages/com.gamelovers.mcp-unity/Server~
npm install
npm run build
```

Example Agent prompts: fetch Unity console logs, start Play mode, run EditMode tests, inspect scene hierarchy.

## Unity Skills (Cursor)

18 skills from [JulianKerignard/Unity-Skills](https://github.com/JulianKerignard/Unity-Skills) live in [`.cursor/skills/`](.cursor/skills/). Reload Cursor after install so Agent discovers them.

Most relevant for this repo:

| Skill | Use when |
|-------|----------|
| `unity` | General Unity 6 / C# / URP / architecture |
| `unity-ui-toolkit` | UXML, USS, UIDocument panels |
| `unity-test` | EditMode / PlayMode NUnit tests |
| `unity-debug` | Play mode bugs, NullRef, input/lifecycle |
| `unity-code-gen` | New C# types + tests |
| `unity-refactor` | Safe incremental refactors |

**This `AGENTS.md` overrides generic skill defaults** (e.g. domain-first model, no VContainer, thin MonoBehaviours).
