# 3D Editor Architecture Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the current Unity MVP into a reliable offline 3D editor whose scene model, edits, imported assets, and project files round-trip without data loss and expose narrow seams for future domain libraries.

**Architecture:** Keep core editor fields directly on `SceneObjectModel`, replace the closed enum with open namespaced IDs, and preserve future domain data as versioned opaque component envelopes. Route every mutation through a new `Editing` application module, move GameObject projection into `Viewer`, and persist project-owned imported assets through a staged, validated, atomic project pipeline.

**Tech Stack:** Unity 6000.4.5f1, C#/.NET Standard 2.1, URP 17.4.0, UI Toolkit, Unity Input System 1.19.0, NUnit/Unity Test Framework 1.6.0, `JsonUtility`, existing ServiceLocator composition.

## Global Constraints

- Follow `AGENTS.md`: domain-first model, one `.asmdef` per top-level module, services registered in `AppBootstrap`, UI Toolkit only, and no business logic in MonoBehaviours.
- Unity GameObjects remain projections; `SceneObjectModel` is authoritative.
- New stable IDs use lowercase reverse-DNS namespacing and are never derived from display names.
- `SceneModel` may use Unity value types but must not own GameObject, MonoBehaviour, mesh, shader, renderer, or ServiceLocator bridge logic after Task 3.
- Do not add VContainer, Addressables, Newtonsoft.Json, glTFast, runtime binding, OPC UA, ACOPOStrak, ACOPOS 6D, Robotics, dynamic plugin loading, reflection discovery, or an undo stack.
- Keep schema-version-1 project loading through an explicit in-memory migration.
- Preserve unknown component `payloadJson` exactly.
- Import limits default to 512 MiB, 5,000,000 vertices, and 15,000,000 indices.
- Use explicit constructor/bind injection between services. ServiceLocator is only the composition/attachment bridge.
- Run focused EditMode tests after each task and the complete EditMode and PlayMode suites at the end.
- Set `UNITY_EDITOR` to the Unity 6000.4.5f1 executable before using the commands below.
- Run `mkdir -p Artifacts` once before the first test command.

## Target File Structure

```text
Assets/
├── SceneModel/
│   ├── Core/
│   │   ├── SceneObjectModel.cs
│   │   ├── SceneObjectTypeId.cs
│   │   ├── SceneObjectTypeIds.cs
│   │   ├── SceneComponentData.cs
│   │   ├── SceneInvariantException.cs
│   │   ├── ISceneRegistryRead.cs
│   │   └── SceneRegistry.cs
│   └── Serialization/
│       ├── ProjectViewerSchema.cs
│       ├── ProjectSchemaV1.cs
│       └── ProjectSchemaMigrator.cs
├── Editing/
│   ├── UnitySimulationX.Editing.asmdef
│   ├── ISceneProjectionWriter.cs
│   ├── ISceneEditService.cs
│   ├── SceneEditService.cs
│   ├── SceneEditResult.cs
│   ├── SceneChangeSet.cs
│   └── SceneObjectDraft.cs
├── Viewer/
│   └── Projection/
│       ├── ISceneProjectionService.cs
│       ├── SceneProjectionService.cs
│       ├── SceneObjectIdComponent.cs
│       └── PrimitiveMeshBuilder.cs
├── App/
│   └── ProjectSystem/
│       ├── ProjectWorkspace.cs
│       ├── ProjectPaths.cs
│       ├── ProjectFileWriter.cs
│       ├── ProjectDocumentValidator.cs
│       ├── ProjectAssetStore.cs
│       └── ProjectPersistenceService.cs
├── Core/
│   └── Projects/
│       ├── IProjectWorkspace.cs
│       ├── IProjectAssetStore.cs
│       └── ProjectOperationResult.cs
└── UI/
    └── Properties/
        ├── PropertyProviderRegistry.cs
        └── SceneTypeDescriptorRegistry.cs
```

Generated Unity `.meta` files for all new assets and folders are committed with the task that creates them.

---

### Task 1: Open Scene Type IDs and Opaque Component Envelopes

**Files:**
- Create: `Assets/SceneModel/Core/SceneObjectTypeId.cs`
- Create: `Assets/SceneModel/Core/SceneObjectTypeIds.cs`
- Create: `Assets/SceneModel/Core/SceneComponentData.cs`
- Modify: `Assets/SceneModel/Core/SceneObjectModel.cs:10-31`
- Modify: `Assets/SceneModel/Materials/MaterialDefinition.cs:10-19`
- Create: `Assets/Tests/EditMode/SceneExtensibilityDataTests.cs`

**Interfaces:**
- Produces: `SceneObjectTypeId`, `SceneObjectTypeIds`, and `SceneComponentData`.
- Compatibility: retains `SceneObjectType Type` temporarily; Task 6 removes it after all consumers use `TypeId`.

- [ ] **Step 1: Write failing type-ID and opaque-data tests**

```csharp
using NUnit.Framework;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class SceneExtensibilityDataTests
    {
        [Test]
        public void SceneObjectTypeId_AcceptsLowercaseReverseDnsId()
        {
            var id = new SceneObjectTypeId("com.vendor.product.track-segment");
            Assert.AreEqual("com.vendor.product.track-segment", id.Value);
        }

        [TestCase("")]
        [TestCase("Primitive")]
        [TestCase("com.vendor.BadType")]
        [TestCase("com..vendor.type")]
        public void SceneObjectTypeId_RejectsInvalidId(string value)
        {
            Assert.Throws<System.ArgumentException>(() => new SceneObjectTypeId(value));
        }

        [Test]
        public void SceneComponentData_ClonePreservesOpaquePayloadExactly()
        {
            const string payload = "{\"future\":true,\"order\":[3,2,1]}";
            var source = new SceneComponentData(
                "com.vendor.product.component",
                7,
                payload);

            var clone = source.Clone();

            Assert.AreEqual(source.TypeId, clone.TypeId);
            Assert.AreEqual(source.SchemaVersion, clone.SchemaVersion);
            Assert.AreEqual(payload, clone.PayloadJson);
        }
    }
}
```

- [ ] **Step 2: Run the focused tests and confirm RED**

Run:

```bash
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -runTests \
  -testPlatform editmode \
  -testFilter UnitySimulationX.Tests.EditMode.SceneExtensibilityDataTests \
  -testResults Artifacts/task-01-editmode.xml \
  -logFile Artifacts/task-01-editmode.log
```

Expected: non-zero exit because the three new types do not exist.

- [ ] **Step 3: Add the exact ID value object and core IDs**

```csharp
// Assets/SceneModel/Core/SceneObjectTypeId.cs
using System;
using System.Text.RegularExpressions;

namespace UnitySimulationX.SceneModel
{
    [Serializable]
    public readonly struct SceneObjectTypeId : IEquatable<SceneObjectTypeId>
    {
        static readonly Regex Pattern = new(
            "^[a-z0-9]+(?:[.-][a-z0-9]+)*$",
            RegexOptions.CultureInvariant);

        public SceneObjectTypeId(string value)
        {
            if (!IsValid(value))
                throw new ArgumentException($"Invalid scene object type id: '{value}'.", nameof(value));
            Value = value;
        }

        public string Value { get; }

        public static bool IsValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Split('.').Length >= 3 &&
                   Pattern.IsMatch(value);
        }

        public bool Equals(SceneObjectTypeId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is SceneObjectTypeId other && Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }
}
```

```csharp
// Assets/SceneModel/Core/SceneObjectTypeIds.cs
namespace UnitySimulationX.SceneModel
{
    public static class SceneObjectTypeIds
    {
        public static readonly SceneObjectTypeId Group =
            new("com.unitysimulationx.scene.group");
        public static readonly SceneObjectTypeId Primitive =
            new("com.unitysimulationx.scene.primitive");
        public static readonly SceneObjectTypeId ImportedModel =
            new("com.unitysimulationx.scene.imported-model");
        public static readonly SceneObjectTypeId MissingAsset =
            new("com.unitysimulationx.scene.missing-asset");
    }
}
```

- [ ] **Step 4: Add the serializable opaque envelope and model fields**

```csharp
// Assets/SceneModel/Core/SceneComponentData.cs
using System;

namespace UnitySimulationX.SceneModel
{
    [Serializable]
    public sealed class SceneComponentData
    {
        public SceneComponentData(string typeId, int schemaVersion, string payloadJson)
        {
            if (!SceneObjectTypeId.IsValid(typeId))
                throw new ArgumentException($"Invalid component type id: '{typeId}'.", nameof(typeId));
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));

            TypeId = typeId;
            SchemaVersion = schemaVersion;
            PayloadJson = payloadJson ?? "{}";
        }

        public string TypeId { get; }
        public int SchemaVersion { get; }
        public string PayloadJson { get; }

        public SceneComponentData Clone() =>
            new(TypeId, SchemaVersion, PayloadJson);
    }
}
```

Add these properties to `SceneObjectModel` while keeping the legacy enum property until Task 6:

```csharp
public SceneObjectTypeId TypeId { get; set; } = SceneObjectTypeIds.Group;
public string AssetId { get; set; }
public List<SceneComponentData> Components { get; set; } = new();
```

Add a `Clone()` method that copies every scalar, calls `Transform.Clone()` and `Material.Clone()`, clones each `SceneComponentData`, and copies the temporary legacy collections. Add this exact material clone:

```csharp
public MaterialDefinition Clone()
{
    return new MaterialDefinition
    {
        BaseColor = BaseColor,
        Alpha = Alpha,
        Metallic = Metallic,
        Roughness = Roughness,
        Preset = Preset,
        UserOverride = UserOverride,
        StatusOverrideEnabled = StatusOverrideEnabled
    };
}
```

- [ ] **Step 5: Run focused and existing schema tests**

Run:

```bash
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -runTests \
  -testPlatform editmode \
  -testFilter "UnitySimulationX.Tests.EditMode.SceneExtensibilityDataTests;UnitySimulationX.Tests.EditMode.ProjectSchemaTests" \
  -testResults Artifacts/task-01-green.xml \
  -logFile Artifacts/task-01-green.log
```

Expected: all selected tests pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/SceneModel Assets/Tests/EditMode
git commit -m "feat(scene): add open type ids and component envelopes"
```

---

### Task 2: Make SceneRegistry the Hierarchy Invariant Owner

**Files:**
- Create: `Assets/SceneModel/Core/SceneInvariantException.cs`
- Create: `Assets/SceneModel/Core/ISceneRegistryRead.cs`
- Modify: `Assets/SceneModel/Core/SceneObjectModel.cs`
- Modify: `Assets/SceneModel/Core/SceneRegistry.cs:7-133`
- Modify: `Assets/App/ProjectSystem/ProjectSerializer.cs:65-94`
- Modify: `Assets/UI/Hierarchy/HierarchyPanelController.cs:95-121`
- Modify: `Assets/Tests/EditMode/SceneRegistryTests.cs`
- Create: `Assets/Tests/EditMode/SceneRegistryInvariantTests.cs`

**Interfaces:**
- Produces: read-only `ISceneRegistryRead` and strict registry operations.
- Invariant: `ParentId` is authoritative; children and roots are registry-derived.
- Snapshot rule: `Get` and `GetAll` return clones, so external mutation cannot alter registry state.

- [ ] **Step 1: Write failing invariant and isolation tests**

```csharp
using NUnit.Framework;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class SceneRegistryInvariantTests
    {
        SceneRegistry _registry;

        [SetUp]
        public void SetUp() => _registry = new SceneRegistry();

        [Test]
        public void Add_WithMissingParent_ThrowsAndDoesNotMutate()
        {
            var child = Model("child", "missing");
            Assert.Throws<SceneInvariantException>(() => _registry.Add(child));
            Assert.IsFalse(_registry.Contains("child"));
        }

        [Test]
        public void Reparent_ToDescendant_ThrowsAndPreservesHierarchy()
        {
            _registry.Add(Model("root"));
            _registry.Add(Model("child", "root"));

            Assert.Throws<SceneInvariantException>(() => _registry.Reparent("root", "child"));
            Assert.IsNull(_registry.Get("root").ParentId);
            Assert.AreEqual("root", _registry.Get("child").ParentId);
        }

        [Test]
        public void Get_ReturnsSnapshotThatCannotMutateRegistry()
        {
            _registry.Add(Model("root"));
            var snapshot = _registry.Get("root");
            snapshot.Name = "Changed outside registry";
            Assert.AreEqual("root", _registry.Get("root").Name);
        }

        static SceneObjectModel Model(string id, string parentId = null) =>
            new()
            {
                Id = id,
                Name = id,
                ParentId = parentId,
                TypeId = SceneObjectTypeIds.Group
            };
    }
}
```

- [ ] **Step 2: Run focused tests and confirm RED**

Run the Unity EditMode command from Task 1 with:

```text
-testFilter UnitySimulationX.Tests.EditMode.SceneRegistryInvariantTests
```

Expected: failures for missing-parent acceptance and leaked mutable model references.

- [ ] **Step 3: Add the read contract and typed exception**

```csharp
// Assets/SceneModel/Core/ISceneRegistryRead.cs
using System;
using System.Collections.Generic;

namespace UnitySimulationX.SceneModel
{
    public interface ISceneRegistryRead
    {
        long Revision { get; }
        IReadOnlyList<string> RootIds { get; }
        SceneObjectModel Get(string id);
        IReadOnlyCollection<SceneObjectModel> GetAll();
        IReadOnlyList<string> GetChildrenIds(string parentId);
        bool Contains(string id);
        event Action HierarchyChanged;
    }
}
```

```csharp
// Assets/SceneModel/Core/SceneInvariantException.cs
namespace UnitySimulationX.SceneModel
{
    public sealed class SceneInvariantException : System.InvalidOperationException
    {
        public SceneInvariantException(string code, string message) : base(message)
        {
            Code = code;
        }

        public string Code { get; }
    }
}
```

- [ ] **Step 4: Rewrite registry storage rules**

Implement `SceneRegistry : ISceneRegistryRead` with these exact public mutation signatures:

```csharp
public void Add(SceneObjectModel model);
public bool Remove(string id);
public void Reparent(string objectId, string newParentId);
public void Replace(string objectId, SceneObjectModel replacement);
public void ReplaceAll(IReadOnlyList<SceneObjectModel> models);
```

Every input is cloned before storage. Every returned object and collection is cloned or copied. `Add`, `Reparent`, `Replace`, and `ReplaceAll` validate before mutation and throw these codes:

```text
scene.id.required
scene.id.duplicate
scene.type.invalid
scene.parent.missing
scene.parent.self
scene.parent.cycle
scene.object.missing
scene.component.duplicate
```

Increment `Revision` exactly once per successful top-level operation. Cascade removal increments once, not once per descendant. Fire `HierarchyChanged` exactly once when roots or parent relationships change.

- [ ] **Step 5: Remove writable children from the model and consumers**

Replace `SceneObjectModel.ChildrenIds` with no model field. Change hierarchy traversal to:

```csharp
foreach (var childId in _registry.GetChildrenIds(model.Id))
    AddObjectRecursive(childId, depth + 1);
```

Stop assigning `ChildrenIds` in `ProjectSerializer.FromDocumentData`; schema-version-1 `childrenIds` remains read-only migration input and is not trusted.

- [ ] **Step 6: Run registry and serializer tests**

Run:

```bash
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -runTests \
  -testPlatform editmode \
  -testFilter "UnitySimulationX.Tests.EditMode.SceneRegistryTests;UnitySimulationX.Tests.EditMode.SceneRegistryInvariantTests;UnitySimulationX.Tests.EditMode.ProjectSerializerTests" \
  -testResults Artifacts/task-02-green.xml \
  -logFile Artifacts/task-02-green.log
```

Expected: all selected tests pass; each failed invariant leaves `Revision` and object count unchanged.

- [ ] **Step 7: Commit**

```bash
git add Assets/SceneModel Assets/App/ProjectSystem/ProjectSerializer.cs Assets/UI/Hierarchy Assets/Tests/EditMode
git commit -m "refactor(scene): enforce registry hierarchy invariants"
```

---

### Task 3: Move Unity Projection from SceneModel to Viewer

**Files:**
- Create: `Assets/Editing/UnitySimulationX.Editing.asmdef`
- Create: `Assets/Editing/ISceneProjectionWriter.cs`
- Create: `Assets/Viewer/Projection/ISceneProjectionService.cs`
- Rename: `Assets/SceneModel/Core/SceneObjectMapper.cs` → `Assets/Viewer/Projection/SceneProjectionService.cs`
- Rename: `Assets/SceneModel/Core/SceneObjectIdComponent.cs` → `Assets/Viewer/Projection/SceneObjectIdComponent.cs`
- Rename: `Assets/SceneModel/Core/ConeMeshGenerator.cs` → `Assets/Viewer/Projection/PrimitiveMeshBuilder.cs`
- Delete: `Assets/SceneModel/Core/ISceneObjectMapper.cs`
- Modify: `Assets/Viewer/UnitySimulationX.Viewer.asmdef`
- Modify: `Assets/UI/UnitySimulationX.UI.asmdef`
- Modify: `Assets/Import/UnitySimulationX.Import.asmdef`
- Modify: `Assets/App/UnitySimulationX.App.asmdef`
- Modify: all current `ISceneObjectMapper` consumers listed by `rg "ISceneObjectMapper|SceneObjectMapper|SceneObjectIdComponent|ServiceLocatorBridge" Assets`
- Rename: `Assets/Tests/EditMode/SceneObjectMapperTests.cs` → `Assets/Tests/EditMode/SceneProjectionServiceTests.cs`

**Interfaces:**
- Produces: pure edit-side `ISceneProjectionWriter` and Unity-facing `ISceneProjectionService`.
- Removes: `ServiceLocatorBridge` and reverse GameObject-to-model lookup.

- [ ] **Step 1: Port mapper tests to the target names and behavior**

Use the existing seven mapper test bodies, rename the fixture to `SceneProjectionServiceTests`, instantiate `SceneProjectionService`, and replace reverse lookup assertions with:

```csharp
var id = _projection.GetObjectId(gameObject);
Assert.AreEqual(model.Id, id);
Assert.AreEqual(model.Id, _registry.Get(id).Id);
```

Add:

```csharp
[Test]
public void SceneModelAssembly_DoesNotContainProjectionTypes()
{
    var assembly = typeof(SceneObjectModel).Assembly;
    Assert.IsNull(assembly.GetType("UnitySimulationX.SceneModel.SceneObjectMapper"));
    Assert.IsNull(assembly.GetType("UnitySimulationX.SceneModel.SceneObjectIdComponent"));
}
```

- [ ] **Step 2: Run the renamed fixture and confirm RED**

Run the Unity EditMode command with:

```text
-testFilter UnitySimulationX.Tests.EditMode.SceneProjectionServiceTests
```

Expected: compile failure until projection types and asmdef references move.

- [ ] **Step 3: Add the Editing assembly and projection contracts**

```json
{
  "name": "UnitySimulationX.Editing",
  "rootNamespace": "UnitySimulationX.Editing",
  "references": [
    "UnitySimulationX.Core",
    "UnitySimulationX.SceneModel"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

```csharp
// Assets/Editing/ISceneProjectionWriter.cs
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Editing
{
    public interface ISceneProjectionWriter
    {
        void CreateProjection(SceneObjectModel snapshot);
        void UpdateProjection(SceneObjectModel snapshot);
        void RemoveProjection(string objectId);
        void ReplaceAllProjections(System.Collections.Generic.IReadOnlyList<SceneObjectModel> snapshots);
    }
}
```

```csharp
// Assets/Viewer/Projection/ISceneProjectionService.cs
using UnityEngine;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Viewer.Projection
{
    public interface ISceneProjectionService : ISceneProjectionWriter
    {
        Transform SceneRoot { get; }
        GameObject GetGameObject(string objectId);
        string GetObjectId(GameObject gameObject);
        void RegisterExistingTarget(string objectId, GameObject target);
        void PreviewTransform(string objectId, TransformData transform);
    }
}
```

- [ ] **Step 4: Move implementation and remove the bridge**

Move mesh/material/projection behavior into `UnitySimulationX.Viewer.Projection.SceneProjectionService`. Inject `ISceneRegistryRead` into its constructor. `GetObjectId` only reads `SceneObjectIdComponent`; callers retrieve snapshots from `ISceneRegistryRead`.

Rename `ConeMeshGenerator` to `PrimitiveMeshBuilder` and keep all current primitive output unchanged. Remove `ServiceLocatorBridge` completely.

- [ ] **Step 5: Migrate every consumer and asmdef**

Apply these replacements:

```text
ISceneObjectMapper                → ISceneProjectionService
SceneObjectMapper                 → SceneProjectionService
CreateGameObject(model)           → CreateProjection(model)
UpdateGameObject(model, gameObject) → UpdateProjection(model)
DestroyGameObject(id)             → RemoveProjection(id)
GetModel(gameObject)              → registry.Get(projection.GetObjectId(gameObject))
```

Add `UnitySimulationX.Editing` references to Viewer, UI, Import, App, EditMode tests, and PlayMode tests. Do not add a SceneModel reference back to Viewer.

- [ ] **Step 6: Run projection, selection, and navigation tests**

Run focused EditMode `SceneProjectionServiceTests`, then focused PlayMode:

```bash
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -runTests \
  -testPlatform playmode \
  -testFilter "UnitySimulationX.Tests.PlayMode.SelectionTests;UnitySimulationX.Tests.PlayMode.NavigationTests" \
  -testResults Artifacts/task-03-playmode.xml \
  -logFile Artifacts/task-03-playmode.log
```

Expected: all selected tests pass and `SceneModel` no longer defines projection classes.

- [ ] **Step 7: Commit**

```bash
git add Assets/Editing Assets/Viewer Assets/SceneModel Assets/UI Assets/Import Assets/App Assets/Tests
git commit -m "refactor(viewer): move scene projection out of domain"
```

---

### Task 4: Replace the Static Event Bus with an Injected Instance

**Files:**
- Create: `Assets/Core/Bootstrap/IEventBus.cs`
- Modify: `Assets/Core/Bootstrap/EventBus.cs`
- Modify: `Assets/App/Bootstrap/AppBootstrap.cs:37-78`
- Modify: all files returned by `rg "EventBus\\." Assets --glob "*.cs"`
- Create: `Assets/Tests/EditMode/EventBusTests.cs`

**Interfaces:**
- Produces: `IEventBus.Subscribe<T>()`, `IEventBus.Publish<T>()`.
- Subscription ownership: every subscription returns `IDisposable`.
- Error isolation: one subscriber exception is reported and does not block later subscribers.

- [ ] **Step 1: Write failing lifecycle and isolation tests**

```csharp
using System;
using NUnit.Framework;
using UnitySimulationX.Core;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class EventBusTests
    {
        [Test]
        public void Dispose_UnsubscribesHandler()
        {
            var calls = 0;
            var bus = new EventBus(_ => { });
            var subscription = bus.Subscribe<HierarchyChangedEvent>(_ => calls++);
            subscription.Dispose();
            bus.Publish(new HierarchyChangedEvent());
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Publish_IsolatesSubscriberException()
        {
            Exception reported = null;
            var secondCalled = false;
            var bus = new EventBus(ex => reported = ex);
            bus.Subscribe<HierarchyChangedEvent>(_ => throw new InvalidOperationException("boom"));
            bus.Subscribe<HierarchyChangedEvent>(_ => secondCalled = true);

            bus.Publish(new HierarchyChangedEvent());

            Assert.IsInstanceOf<InvalidOperationException>(reported);
            Assert.IsTrue(secondCalled);
        }
    }
}
```

- [ ] **Step 2: Run the fixture and confirm RED**

Expected: compile failure because `EventBus` is currently static.

- [ ] **Step 3: Implement the exact event-bus contract**

```csharp
public interface IEventBus
{
    System.IDisposable Subscribe<T>(System.Action<T> handler);
    void Publish<T>(T message);
}
```

Implement `EventBus` as a sealed instance class with:

```csharp
public EventBus(System.Action<System.Exception> reportSubscriberError);
public System.IDisposable Subscribe<T>(System.Action<T> handler);
public void Publish<T>(T message);
```

Snapshot the subscriber list before publishing. Catch each subscriber exception separately and call the reporter. `Dispose` is idempotent.

- [ ] **Step 4: Migrate publishers and subscribers**

Construct once:

```csharp
var eventBus = new EventBus(Debug.LogException);
ServiceLocator.Register<IEventBus>(eventBus);
```

Services receive `IEventBus` through constructors. MonoBehaviour/controllers resolve it once during bind/awake and store each returned `IDisposable`; dispose subscriptions during unbind/destroy. Remove `Clear`, static `Subscribe`, static `Unsubscribe`, and static `Publish`.

- [ ] **Step 5: Run EventBus plus selection tests**

Expected: `EventBusTests` and PlayMode `SelectionTests` pass with no static cleanup in test teardown.

- [ ] **Step 6: Commit**

```bash
git add Assets/Core Assets/App Assets/UI Assets/Viewer Assets/Import Assets/Tests
git commit -m "refactor(core): inject event bus with owned subscriptions"
```

---

### Task 5: Introduce the Single Scene Editing Boundary

**Files:**
- Create: `Assets/Editing/SceneObjectDraft.cs`
- Create: `Assets/Editing/SceneEditResult.cs`
- Create: `Assets/Editing/SceneChangeSet.cs`
- Create: `Assets/Editing/SceneChangedEvent.cs`
- Create: `Assets/Editing/ISceneEditService.cs`
- Create: `Assets/Editing/SceneEditService.cs`
- Modify: `Assets/Import/Primitives/PrimitiveFactory.cs`
- Modify: `Assets/UI/Properties/PropertiesPanelController.cs`
- Modify: `Assets/UI/Hierarchy/HierarchyPanelController.cs`
- Modify: `Assets/Viewer/Gizmos/TransformGizmoController.cs`
- Modify: `Assets/App/Bootstrap/AppBootstrap.cs`
- Create: `Assets/Tests/EditMode/SceneEditServiceTests.cs`

**Interfaces:**
- Consumes: strict `SceneRegistry`, `ISceneProjectionWriter`, `IEventBus`.
- Produces: the only feature-facing mutation API.

- [ ] **Step 1: Write failing edit orchestration tests**

```csharp
[Test]
public void Rename_UpdatesRegistryProjectionAndPublishesOnce()
{
    var projection = new RecordingProjection();
    var bus = new EventBus(_ => { });
    var registry = new SceneRegistry();
    registry.Add(Model("object"));
    var changes = new System.Collections.Generic.List<SceneChangeSet>();
    bus.Subscribe<SceneChangedEvent>(evt => changes.Add(evt.ChangeSet));
    var service = new SceneEditService(registry, projection, bus);

    var result = service.Rename("object", "Renamed");

    Assert.IsTrue(result.Succeeded);
    Assert.AreEqual("Renamed", registry.Get("object").Name);
    Assert.AreEqual(1, projection.UpdateCount);
    Assert.AreEqual(1, changes.Count);
    Assert.AreEqual(registry.Revision, changes[0].Revision);
}

[Test]
public void Reparent_InvalidCycle_ReturnsFailureWithoutProjectionOrEvent()
{
    var projection = new RecordingProjection();
    var bus = new EventBus(_ => { });
    var registry = RegistryWithRootAndChild();
    var events = 0;
    bus.Subscribe<SceneChangedEvent>(_ => events++);
    var service = new SceneEditService(registry, projection, bus);

    var result = service.Reparent("root", "child");

    Assert.IsFalse(result.Succeeded);
    Assert.AreEqual("scene.parent.cycle", result.ErrorCode);
    Assert.AreEqual(0, projection.UpdateCount);
    Assert.AreEqual(0, events);
}
```

The test file includes local `RecordingProjection`, `Model`, and `RegistryWithRootAndChild` helpers implementing the exact interfaces from Tasks 2–3.

- [ ] **Step 2: Run tests and confirm RED**

Expected: compile failure until editing contracts and implementation exist.

- [ ] **Step 3: Add exact edit contracts**

```csharp
public enum SceneChangeKind
{
    Created,
    Updated,
    Removed,
    Reparented,
    SceneReplaced
}

public sealed class SceneChangeSet
{
    public SceneChangeKind Kind { get; set; }
    public System.Collections.Generic.IReadOnlyList<string> ObjectIds { get; set; }
    public bool HierarchyChanged { get; set; }
    public long Revision { get; set; }
}

public sealed class SceneEditResult
{
    public bool Succeeded { get; set; }
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public SceneChangeSet ChangeSet { get; set; }
}

public interface ISceneEditService
{
    ISceneRegistryRead Registry { get; }
    SceneEditResult Create(SceneObjectDraft draft);
    SceneEditResult Remove(string objectId);
    SceneEditResult Rename(string objectId, string name);
    SceneEditResult SetVisible(string objectId, bool visible);
    SceneEditResult SetTransform(string objectId, TransformData transform);
    SceneEditResult SetMaterial(string objectId, MaterialDefinition material);
    SceneEditResult Reparent(string objectId, string newParentId);
    SceneEditResult SetComponent(string objectId, SceneComponentData component);
    SceneEditResult ReplaceScene(System.Collections.Generic.IReadOnlyList<SceneObjectModel> snapshots);
}
```

`SceneObjectDraft` contains `Id`, `Name`, `TypeId`, `ParentId`, `Transform`, `Visible`, `Material`, `AssetId`, and copied `Components`.

- [ ] **Step 4: Implement validate → mutate → project → publish**

For each operation:

1. Read the before snapshot.
2. Construct a replacement snapshot.
3. Call the strict registry operation.
4. Call the projection writer once.
5. Publish one `SceneChangedEvent`.
6. Catch `SceneInvariantException` and return a failed `SceneEditResult` without projection or event work.

`ReplaceScene` validates all snapshots through a temporary `SceneRegistry`, captures the active snapshots, replaces registry and projections, and restores both active snapshots if projection replacement throws.

- [ ] **Step 5: Migrate current editor mutations**

Replace direct registry/projection/event triples in:

- `PrimitiveFactory`
- `PropertiesPanelController`
- `HierarchyPanelController`
- `TransformGizmoController`
- `AppBootstrap.CreateDefaultRoot`
- `AppBootstrap.AdoptAuthoredSceneObjects`

Gizmo behavior becomes:

```text
pointer down  → capture original model transform
pointer drag  → projection.PreviewTransform only
confirm       → edits.SetTransform exactly once
cancel        → projection.PreviewTransform(original), no model edit
```

Define `SceneChangedEvent` in the `Editing` assembly so `Core` does not reference `Editing`. Remove manual `HierarchyChangedEvent` and `SceneObjectChangedEvent` publishing from migrated files. Replace both with `SceneChangedEvent { ChangeSet = result.ChangeSet }` emitted only inside `SceneEditService`.

`AppBootstrap` stops registering concrete `SceneRegistry` in ServiceLocator. Register `ISceneRegistryRead` and `ISceneEditService`; migrate selection, projection, UI, import, and persistence constructors to the read interface/edit service so feature modules cannot obtain the concrete mutation API.

Remove `SceneRegistry.HierarchyChanged` and `ISceneRegistryRead.HierarchyChanged` after migration; `SceneChangedEvent` is the sole committed-scene notification. For authored GameObjects, call `projection.RegisterExistingTarget(draft.Id, candidate)` before `edits.Create(draft)` and remove the staged target when creation fails.

- [ ] **Step 6: Run editing, hierarchy, projection, and gizmo-related tests**

Expected: selected tests pass; event-count assertions prove one publication per committed operation.

- [ ] **Step 7: Commit**

```bash
git add Assets/Editing Assets/Import/Primitives Assets/UI Assets/Viewer/Gizmos Assets/App/Bootstrap Assets/Tests
git commit -m "feat(editing): centralize scene mutations"
```

---

### Task 6: Complete the Open Type-ID Migration

**Files:**
- Create: `Assets/SceneModel/Core/LegacySceneObjectTypeMigration.cs`
- Modify: every file returned by `rg "SceneObjectType|\\.Type\\b" Assets --glob "*.cs"`
- Delete: `Assets/SceneModel/Core/SceneObjectType.cs`
- Modify: `Assets/SceneModel/Core/SceneObjectModel.cs`
- Create: `Assets/Tests/EditMode/SceneObjectTypeMigrationTests.cs`

**Interfaces:**
- Produces: `TypeId` as the sole scene-type identity.
- Migration: schema-version-1 enum strings map deterministically without claiming future domain semantics.

- [ ] **Step 1: Write failing migration coverage**

```csharp
[TestCase("Primitive", "com.unitysimulationx.scene.primitive")]
[TestCase("ImportedAsset", "com.unitysimulationx.scene.imported-model")]
[TestCase("MachineFrame", "com.unitysimulationx.scene.group")]
[TestCase("TrackSegment", "com.unitysimulationx.legacy.track-segment")]
[TestCase("UnknownFutureValue", "com.unitysimulationx.legacy.unknown-future-value")]
public void FromV1Type_PreservesStableMeaning(string legacy, string expected)
{
    Assert.AreEqual(expected, LegacySceneObjectTypeMigration.FromV1Type(legacy).Value);
}

[Test]
public void CustomTypeId_RemainsExactOnModelClone()
{
    var model = new SceneObjectModel
    {
        Id = "custom",
        Name = "Custom",
        TypeId = new SceneObjectTypeId("com.vendor.product.custom-object")
    };
    Assert.AreEqual(model.TypeId, model.Clone().TypeId);
}
```

- [ ] **Step 2: Implement deterministic legacy mapping**

Special-case `Primitive`, `ImportedAsset`, and `MachineFrame` as asserted above. Convert every other input to lowercase kebab case under `com.unitysimulationx.legacy.`. Empty input maps to `com.unitysimulationx.legacy.unknown`.

- [ ] **Step 3: Migrate all producers and consumers**

Use:

```text
SceneObjectType.Primitive     → SceneObjectTypeIds.Primitive
SceneObjectType.ImportedAsset → SceneObjectTypeIds.ImportedModel
SceneObjectType.MachineFrame  → SceneObjectTypeIds.Group
model.Type                    → model.TypeId
```

`HierarchyPanelController` receives icons from Task 10's descriptor registry eventually; until then map only the four core IDs and use `"OB"` for unknown IDs. `CommonPropertyProvider` displays `TypeId.Value`.

- [ ] **Step 4: Delete the enum and compatibility property**

Delete `SceneObjectType.cs`, remove `SceneObjectModel.Type`, and verify:

```bash
rg "SceneObjectType|model\\.Type|\\.Type = SceneObjectType" Assets --glob "*.cs"
```

Expected: only `LegacySceneObjectTypeMigration` references the old serialized string concept; no enum references remain.

- [ ] **Step 5: Run all EditMode tests**

Expected: all EditMode tests pass with custom namespaced type IDs accepted by registry and projection.

- [ ] **Step 6: Commit**

```bash
git add Assets
git commit -m "refactor(scene): replace closed object type enum"
```

---

### Task 7: Introduce Project Schema Version 2 and Lossless Migration

**Files:**
- Modify: `Assets/SceneModel/Serialization/ProjectViewerSchema.cs`
- Create: `Assets/SceneModel/Serialization/ProjectSchemaV1.cs`
- Create: `Assets/SceneModel/Serialization/ProjectSchemaMigrator.cs`
- Modify: `Assets/App/ProjectSystem/ProjectSerializer.cs`
- Modify: `Assets/App/ProjectSystem/project.viewer.json`
- Modify: `Assets/Tests/EditMode/ProjectSchemaTests.cs`
- Modify: `Assets/Tests/EditMode/ProjectSerializerTests.cs`
- Create: `Assets/Tests/EditMode/ProjectSchemaMigrationTests.cs`

**Interfaces:**
- Consumes: open IDs and opaque components.
- Produces: v2 document with asset catalog, full core fields, and component envelopes.

- [ ] **Step 1: Write failing v2 and migration tests**

```csharp
[Test]
public void V2Json_RoundTripsUnknownComponentPayloadExactly()
{
    const string payload = "{\"vendorField\":42}";
    var model = new SceneObjectModel
    {
        Id = "object",
        Name = "Object",
        TypeId = new SceneObjectTypeId("com.vendor.product.object"),
        Components = new()
        {
            new SceneComponentData("com.vendor.product.component", 3, payload)
        }
    };
    var registry = new SceneRegistry();
    registry.Add(model);

    var document = ProjectSerializer.CreateDocument(registry, System.Array.Empty<ProjectAssetDocumentData>());
    var json = UnityEngine.JsonUtility.ToJson(document);
    var decoded = UnityEngine.JsonUtility.FromJson<ProjectViewerDocument>(json);
    var restored = ProjectSerializer.CreateSnapshots(decoded);

    Assert.AreEqual(2, decoded.schemaVersion);
    Assert.AreEqual(payload, restored[0].Components[0].PayloadJson);
}

[Test]
public void V1Json_MigratesPrimitiveAndHierarchy()
{
    var migrated = ProjectSchemaMigrator.DecodeAndMigrate(V1FixtureJson);
    Assert.AreEqual(2, migrated.schemaVersion);
    Assert.AreEqual("com.unitysimulationx.scene.primitive", migrated.scene.objects[1].typeId);
    Assert.AreEqual(migrated.scene.objects[0].id, migrated.scene.objects[1].parentId);
}
```

- [ ] **Step 2: Define the exact v2 DTO shape**

```csharp
[System.Serializable]
public sealed class ProjectViewerDocument
{
    public int schemaVersion = 2;
    public ProjectAssetsDocumentData assets = new();
    public SceneDocumentData scene = new();
    public ViewSettingsData viewSettings = new();
}

[System.Serializable]
public sealed class ProjectAssetsDocumentData
{
    public System.Collections.Generic.List<ProjectAssetDocumentData> imported = new();
}

[System.Serializable]
public sealed class ProjectAssetDocumentData
{
    public string assetId;
    public string relativePath;
    public string originalFileName;
    public string mediaType;
    public string contentHash;
    public string importerId;
    public int importerVersion;
    public string importSettingsJson;
}

[System.Serializable]
public sealed class SceneObjectDocumentData
{
    public string id;
    public string name;
    public string typeId;
    public string parentId;
    public TransformData transform = new();
    public bool visible = true;
    public MaterialDefinition material = new();
    public string assetId;
    public System.Collections.Generic.List<SceneComponentDocumentData> components = new();
}

[System.Serializable]
public sealed class SceneComponentDocumentData
{
    public string typeId;
    public int schemaVersion;
    public string payloadJson;
}
```

- [ ] **Step 3: Implement separate v1 DTOs and migration**

`ProjectSchemaV1.cs` mirrors the current schema exactly, including `type`, `childrenIds`, `primitiveMeshTypeKey`, and `baseColor`. `ProjectSchemaMigrator.DecodeAndMigrate` first reads only `schemaVersion`, then:

- decodes v1 into v1 DTOs and maps each object to v2;
- maps legacy types through `LegacySceneObjectTypeMigration`;
- moves `primitiveMeshTypeKey` into component type `com.unitysimulationx.scene.primitive-mesh`;
- copies full transform, visibility, and base color;
- ignores v1 `childrenIds` and rebuilds hierarchy from `parentId`;
- rejects schema versions less than 1 or greater than 2 with `ProjectFormatException`.

- [ ] **Step 4: Make serializer mapping complete and non-mutating**

Use these signatures:

```csharp
public static ProjectViewerDocument CreateDocument(
    ISceneRegistryRead registry,
    System.Collections.Generic.IReadOnlyList<ProjectAssetDocumentData> assets);

public static System.Collections.Generic.IReadOnlyList<SceneObjectModel> CreateSnapshots(
    ProjectViewerDocument document);
```

`ProjectSerializer` no longer clears registry or creates projections. It only maps snapshots to/from documents. Copy every component `payloadJson` without parsing it.

- [ ] **Step 5: Run schema, migration, serializer, and opaque-data tests**

Expected: selected tests pass; the exact unknown payload string survives encode/decode and v1 remains readable.

- [ ] **Step 6: Commit**

```bash
git add Assets/SceneModel/Serialization Assets/App/ProjectSystem/ProjectSerializer.cs Assets/App/ProjectSystem/project.viewer.json Assets/Tests/EditMode
git commit -m "feat(project): add lossless schema version two"
```

---

### Task 8: Add Temporary/Persistent Workspaces and Atomic Validated Persistence

**Files:**
- Create: `Assets/Core/Projects/IProjectWorkspace.cs`
- Create: `Assets/Core/Projects/ProjectOperationResult.cs`
- Create: `Assets/App/ProjectSystem/ProjectWorkspace.cs`
- Create: `Assets/App/ProjectSystem/ProjectPaths.cs`
- Create: `Assets/App/ProjectSystem/ProjectFileWriter.cs`
- Create: `Assets/App/ProjectSystem/ProjectDocumentValidator.cs`
- Modify: `Assets/Core/Bootstrap/IProjectPersistenceService.cs`
- Modify: `Assets/App/ProjectSystem/ProjectPersistenceService.cs`
- Modify: `Assets/Core/Bootstrap/IFileDialogService.cs`
- Modify: `Assets/App/ProjectSystem/NativeFileDialogService.cs`
- Modify: `Assets/UI/Shell/ToolbarController.cs`
- Modify: `Assets/App/Bootstrap/AppBootstrap.cs`
- Create: `Assets/Tests/EditMode/ProjectPathsTests.cs`
- Create: `Assets/Tests/EditMode/ProjectFileWriterTests.cs`
- Create: `Assets/Tests/EditMode/ProjectDocumentValidatorTests.cs`
- Create: `Assets/Tests/EditMode/ProjectPersistenceServiceTests.cs`

**Interfaces:**
- Consumes: pure serializer snapshots and `ISceneEditService.ReplaceScene`.
- Produces: project-folder lifecycle with typed results and no partial active-scene mutation.

- [ ] **Step 1: Write failing path, validation, and atomicity tests**

Cover these exact cases:

```csharp
[Test] public void ToAbsolutePath_RejectsParentTraversal();
[Test] public void Validate_RejectsDuplicateObjectIds();
[Test] public void Validate_RejectsMissingParent();
[Test] public void Validate_RejectsUnknownAssetCatalogId();
[Test] public async System.Threading.Tasks.Task Load_InvalidDocument_DoesNotCallReplaceScene();
[Test] public async System.Threading.Tasks.Task Save_Failure_DoesNotChangeCurrentProjectRoot();
[Test] public async System.Threading.Tasks.Task WriteAtomic_ReplacesCompleteDocument();
```

Each test uses a unique directory under `Application.temporaryCachePath` and removes it in teardown.

- [ ] **Step 2: Add workspace and safe-path contracts**

```csharp
public interface IProjectWorkspace : System.IDisposable
{
    string RootPath { get; }
    bool IsTemporary { get; }
    void UsePersistentRoot(string rootPath);
}

public static class ProjectPaths
{
    public const string DocumentFileName = "project.viewer.json";
    public const string ImportedAssetsDirectory = "assets/imported";

    public static string DocumentPath(string rootPath);
    public static string ImportedPath(string rootPath);
    public static string ResolveInsideRoot(string rootPath, string relativePath);
    public static string MakeRelative(string rootPath, string absolutePath);
}
```

`ProjectWorkspace` creates a GUID-named temporary directory at startup and deletes only that temporary root on dispose. `UsePersistentRoot` validates/creates the target but never deletes user-owned content.

- [ ] **Step 3: Implement atomic document writing**

`ProjectFileWriter.WriteAtomicAsync(path, content, cancellationToken)`:

1. creates the parent directory;
2. writes UTF-8 without BOM to `{path}.{guid}.tmp`;
3. calls `Flush(true)` on the file stream;
4. uses `File.Replace` when the destination exists and `File.Move` otherwise;
5. deletes the temporary file in `finally`.

- [ ] **Step 4: Implement exact validation policy**

`ProjectDocumentValidator.Validate(document, projectRoot)` returns errors for:

- unsupported core schema;
- missing/duplicate object IDs;
- invalid type IDs;
- missing parents or hierarchy cycles;
- duplicate asset IDs;
- object references to absent catalog IDs;
- rooted asset paths, `..` traversal, or resolved paths outside root;
- duplicate component type IDs on one object;
- component schema versions below 1.

A catalog entry whose safe relative path resolves inside the root but whose file is absent is a warning, not an error.

- [ ] **Step 5: Replace persistence service API**

```csharp
public sealed class ProjectOperationResult
{
    public bool Succeeded { get; set; }
    public System.Collections.Generic.IReadOnlyList<ProjectIssue> Issues { get; set; }
}

public sealed class ProjectIssue
{
    public string Code { get; set; }
    public string Message { get; set; }
    public bool IsError { get; set; }
}

public interface IProjectPersistenceService
{
    string CurrentProjectRoot { get; }
    System.Threading.Tasks.Task<ProjectOperationResult> SaveAsync(
        System.Threading.CancellationToken cancellationToken);
    System.Threading.Tasks.Task<ProjectOperationResult> SaveAsAsync(
        string projectRoot,
        System.Threading.CancellationToken cancellationToken);
    System.Threading.Tasks.Task<ProjectOperationResult> LoadAsync(
        string projectRoot,
        System.Threading.CancellationToken cancellationToken);
}
```

`LoadAsync` performs decode → migrate → validate → create snapshots before calling `ISceneEditService.ReplaceScene`. `SaveAsAsync` copies workspace assets in Task 9, encodes the document, writes atomically, and updates `CurrentProjectRoot` only after success.

Change dialogs to folder methods:

```csharp
string OpenProjectFolder();
string SaveProjectFolder(string currentProjectRoot);
string OpenImportPath();
```

Update toolbar methods to be `async void` UI event handlers, await the persistence operation with `CancellationToken.None`, inspect `ProjectOperationResult`, and log each typed error once.

- [ ] **Step 6: Run all project-system tests**

Expected: invalid load leaves the recording edit service untouched; failed save preserves current root and prior document.

- [ ] **Step 7: Commit**

```bash
git add Assets/App/ProjectSystem Assets/Core/Projects Assets/Core/Bootstrap Assets/UI/Shell/ToolbarController.cs Assets/App/Bootstrap Assets/Tests/EditMode
git commit -m "feat(project): add atomic validated project lifecycle"
```

---

### Task 9: Make Imported OBJ/STL Assets Project-Owned and Reloadable

**Files:**
- Create: `Assets/Core/Projects/IProjectAssetStore.cs`
- Create: `Assets/App/ProjectSystem/ProjectAssetStore.cs`
- Create: `Assets/App/ProjectSystem/MissingAssetFactory.cs`
- Create: `Assets/Viewer/Projection/IImportedAssetProjectionProvider.cs`
- Create: `Assets/Import/ImportedAssetProjectionProvider.cs`
- Create: `Assets/Import/ImportOperationResult.cs`
- Modify: `Assets/Import/ISceneAssetImporter.cs`
- Modify: `Assets/Import/IImportSceneService.cs`
- Modify: `Assets/Import/ImporterRegistry.cs`
- Modify: `Assets/Import/ImportSettings.cs`
- Modify: `Assets/Import/ImportResult.cs`
- Modify: `Assets/Import/ImportSceneService.cs`
- Modify: `Assets/Import/ObjSceneAssetImporter.cs`
- Modify: `Assets/Import/StlSceneAssetImporter.cs`
- Modify: `Assets/Import/GltfSceneAssetImporter.cs`
- Modify: `Assets/App/ProjectSystem/ProjectPersistenceService.cs`
- Modify: `Assets/App/Bootstrap/AppBootstrap.cs`
- Create: `Assets/Tests/EditMode/ProjectAssetStoreTests.cs`
- Create: `Assets/Tests/EditMode/ImportLimitTests.cs`
- Create: `Assets/Tests/EditMode/ImportedAssetRoundTripTests.cs`
- Create: `Assets/Tests/EditMode/MissingAssetTests.cs`

**Interfaces:**
- Consumes: workspace, asset catalog DTO, importers, edit service.
- Produces: source-copy → DTO parse → scene edit; no persisted GameObject or absolute source path.

- [ ] **Step 1: Write failing asset ownership and round-trip tests**

```csharp
[Test]
public async System.Threading.Tasks.Task ImportObj_CopiesSourceAndStoresOnlyRelativeReference()
{
    var imported = await _service.ImportFileAsync(_externalObjPath, default);
    Assert.IsTrue(imported.Succeeded);
    var model = _edits.Registry.Get(imported.ObjectId);
    var entry = _assetStore.Get(model.AssetId);
    Assert.IsFalse(System.IO.Path.IsPathRooted(entry.relativePath));
    Assert.IsTrue(System.IO.File.Exists(
        ProjectPaths.ResolveInsideRoot(_workspace.RootPath, entry.relativePath)));
    Assert.AreNotEqual(_externalObjPath, entry.relativePath);
}

[Test]
public void Load_MissingCatalogFile_CreatesMetadataPreservingPlaceholder()
{
    var snapshots = _loader.BuildSnapshots(DocumentWithMissingAsset(), _workspace.RootPath);
    Assert.AreEqual(SceneObjectTypeIds.MissingAsset, snapshots[0].TypeId);
    Assert.AreEqual("asset-1", snapshots[0].AssetId);
    Assert.AreEqual("Imported Frame", snapshots[0].Name);
}
```

- [ ] **Step 2: Add importer identity, cancellation, and limits**

```csharp
public interface ISceneAssetImporter
{
    string ImporterId { get; }
    int ImporterVersion { get; }
    bool CanImport(string fileExtension);
    System.Threading.Tasks.Task<ImportResult> ImportAsync(
        string filePath,
        ImportSettings settings,
        System.Threading.CancellationToken cancellationToken);
}
```

`ImportSettings` adds:

```csharp
public long MaxSourceBytes { get; set; } = 512L * 1024L * 1024L;
public int MaxVertices { get; set; } = 5_000_000;
public int MaxIndices { get; set; } = 15_000_000;
```

OBJ/STL parsers check source size before parsing, check cancellation during record loops, and return a typed failure before producing scene data when limits are exceeded.

Change the orchestration contract to:

```csharp
public sealed class ImportOperationResult
{
    public bool Succeeded { get; set; }
    public string ObjectId { get; set; }
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public System.Collections.Generic.IReadOnlyList<ImportWarning> Warnings { get; set; }
}

public interface IImportSceneService
{
    System.Threading.Tasks.Task<ImportOperationResult> ImportFileAsync(
        string path,
        System.Threading.CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Remove the GameObject leak from ImportResult**

Delete `ImportResult.ImportedGameObject`. `ImportResult` contains:

```csharp
public bool Succeeded { get; set; }
public string ErrorCode { get; set; }
public string Message { get; set; }
public SceneObjectDraft RootObject { get; set; }
public System.Collections.Generic.List<ImportedMeshData> Meshes { get; } = new();
public System.Collections.Generic.List<ImportedMaterialData> Materials { get; } = new();
public System.Collections.Generic.List<ImportWarning> Warnings { get; } = new();
```

Until a supported GLB DTO adapter is added, `GltfSceneAssetImporter.ImportAsync` returns:

```text
Succeeded = false
ErrorCode = "import.glb.adapter-unavailable"
Message = "GLB import requires the planned GLB adapter package."
```

Keep `.glb` registration explicit so UI receives the typed error. Do not add glTFast.

- [ ] **Step 4: Implement project asset storage**

```csharp
public interface IProjectAssetStore
{
    System.Threading.Tasks.Task<ProjectAssetDocumentData> ImportExternalFileAsync(
        string sourcePath,
        string importerId,
        int importerVersion,
        string importSettingsJson,
        System.Threading.CancellationToken cancellationToken);
    ProjectAssetDocumentData Get(string assetId);
    System.Collections.Generic.IReadOnlyList<ProjectAssetDocumentData> GetAll();
    string Resolve(string assetId);
    void ReplaceCatalog(System.Collections.Generic.IReadOnlyList<ProjectAssetDocumentData> entries);
    System.Threading.Tasks.Task CopyAllToAsync(
        string targetProjectRoot,
        System.Threading.CancellationToken cancellationToken);
    void Remove(string assetId);
}
```

Copy to `assets/imported/{assetId}{lowercase-extension}`. Compute SHA-256 while copying. If parser/edit creation fails, remove the new catalog entry and copied file. Never overwrite a catalog file in place.

- [ ] **Step 5: Stage imported geometry for Unity projection**

Define in Viewer:

```csharp
public interface IImportedAssetProjectionProvider
{
    bool TryApply(string assetId, UnityEngine.GameObject target);
}
```

Implement `ImportedAssetProjectionProvider` in Import. It owns a runtime-only dictionary from asset ID to successful `ImportResult`, applies mesh/material DTOs to target GameObjects, and exposes `ReplaceCache` plus `Remove` for transactional staging. Add a Viewer reference to `UnitySimulationX.Import.asmdef`; Viewer must not reference Import.

Inject `IImportedAssetProjectionProvider` into `SceneProjectionService`. When a snapshot has `AssetId`, call the provider while creating/updating its projection. The project document and scene model store only `AssetId`; runtime mesh DTOs remain regenerable cache.

- [ ] **Step 6: Route external import and project load**

External import:

```text
copy asset → resolve project path → parse DTO → create model through edit service
→ apply mesh DTO through projection adapter → commit catalog entry
```

Project load resolves and reparses every catalog entry into a local candidate cache before replacing the scene. Swap the candidate cache into `ImportedAssetProjectionProvider`, call `ISceneEditService.ReplaceScene`, and restore the prior cache if scene replacement fails. Missing files produce `SceneObjectTypeIds.MissingAsset` snapshots retaining object ID, name, transform, material, `AssetId`, and components. Projection renders a pickable warning cube for that type.

`SaveAsAsync` awaits `ProjectAssetStore.CopyAllToAsync(targetRoot, cancellationToken)` before atomically replacing the target document. It does not delete target assets during save, so the previous target document remains valid if copying or document encoding fails.

- [ ] **Step 7: Run importer, asset-store, round-trip, and missing-asset tests**

Expected: OBJ/STL imports store no absolute path; import → serialize → load restores mesh data; missing file creates a selectable placeholder snapshot.

- [ ] **Step 8: Commit**

```bash
git add Assets/Core/Projects Assets/App/ProjectSystem Assets/Import Assets/Viewer/Projection Assets/App/Bootstrap Assets/Tests/EditMode
git commit -m "feat(import): persist project-owned 3d assets"
```

---

### Task 10: Add Narrow Contribution Registries and Provider-Owned Edits

**Files:**
- Create: `Assets/Editing/SceneObjectFactoryRegistry.cs`
- Create: `Assets/Editing/ISceneObjectFactory.cs`
- Create: `Assets/Editing/SceneComponentCodecRegistry.cs`
- Create: `Assets/Editing/ISceneComponentCodec.cs`
- Create: `Assets/Import/Primitives/PrimitiveMeshComponent.cs`
- Create: `Assets/Import/Primitives/PrimitiveMeshComponentCodec.cs`
- Create: `Assets/UI/Properties/PropertyProviderRegistry.cs`
- Create: `Assets/UI/Hierarchy/SceneTypeDescriptorRegistry.cs`
- Modify: `Assets/Import/ImporterRegistry.cs`
- Modify: `Assets/UI/Properties/IPropertyProvider.cs`
- Modify: `Assets/UI/Properties/PropertyDescriptor.cs`
- Modify: `Assets/UI/Properties/CommonPropertyProvider.cs`
- Modify: `Assets/UI/Properties/PropertiesPanelController.cs`
- Modify: `Assets/UI/Hierarchy/HierarchyPanelController.cs`
- Modify: `Assets/UI/Library/LibraryPanelController.cs`
- Modify: `Assets/Import/Primitives/PrimitiveFactory.cs`
- Modify: `Assets/App/Bootstrap/AppBootstrap.cs`
- Create: `Assets/Tests/EditMode/ContributionRegistryTests.cs`

**Interfaces:**
- Produces: deterministic explicit extension seams.
- Constraint: registries expose registration only during composition and reject registration after `Freeze()`.
- Constraint: no `IDomainModule`, discovery, lifecycle hooks, or service resolution from registries.

- [ ] **Step 1: Write failing duplicate/freeze/provider-composition tests**

```csharp
[Test]
public void PropertyProviderRegistry_ReturnsAllSupportingProvidersInOrder()
{
    var registry = new PropertyProviderRegistry();
    registry.Register(new FakeProvider("common", 0));
    registry.Register(new FakeProvider("vendor", 10));
    registry.Freeze();
    CollectionAssert.AreEqual(
        new[] { "common", "vendor" },
        registry.GetProviders(Model()).Select(provider => provider.ProviderId));
}

[Test]
public void SceneTypeDescriptorRegistry_RejectsDuplicateTypeId()
{
    var registry = new SceneTypeDescriptorRegistry();
    registry.Register(new SceneTypeDescriptor(SceneObjectTypeIds.Group, "Group", "GR"));
    Assert.Throws<System.InvalidOperationException>(() =>
        registry.Register(new SceneTypeDescriptor(SceneObjectTypeIds.Group, "Duplicate", "XX")));
}

[Test]
public void FrozenRegistry_RejectsLateRegistration()
{
    var registry = new SceneComponentCodecRegistry();
    registry.Freeze();
    Assert.Throws<System.InvalidOperationException>(() =>
        registry.Register(new FakeComponentCodec()));
}
```

- [ ] **Step 2: Implement shared registry behavior**

Each registry:

- keys entries with `StringComparer.Ordinal`;
- rejects null and duplicate IDs;
- returns entries in deterministic `(Order, Id)` order;
- throws `InvalidOperationException` after `Freeze`;
- exposes no ServiceLocator or event bus.

Use these identities:

```csharp
IPropertyProvider.ProviderId
IPropertyProvider.Order
ISceneObjectFactory.FactoryId
ISceneObjectFactory.TypeId
ISceneComponentCodec.ComponentTypeId
ISceneAssetImporter.ImporterId
```

- [ ] **Step 3: Define the narrow factory and codec contracts**

```csharp
public interface ISceneObjectFactory
{
    string FactoryId { get; }
    SceneObjectTypeId TypeId { get; }
    string DisplayName { get; }
    int Order { get; }
    System.Collections.Generic.IReadOnlyList<string> VariantIds { get; }
    SceneObjectDraft Create(string variantId, string name, string parentId);
}

public interface ISceneComponentCodec
{
    string ComponentTypeId { get; }
    int CurrentSchemaVersion { get; }
    System.Type ComponentClrType { get; }
    object Decode(SceneComponentData data);
    SceneComponentData Encode(object component);
}
```

`PrimitiveMeshComponent` has one `string MeshTypeKey` property. `PrimitiveMeshComponentCodec` uses component ID `com.unitysimulationx.scene.primitive-mesh`, schema version 1, and `JsonUtility` to encode/decode that typed component. It rejects a mismatched type ID, schema version, or CLR type with `ArgumentException`.

- [ ] **Step 4: Move property writes into descriptors**

Extend `PropertyDescriptor`:

```csharp
public System.Func<object, SceneEditResult> Apply { get; set; }
```

Change provider contract:

```csharp
public interface IPropertyProvider
{
    string ProviderId { get; }
    int Order { get; }
    bool Supports(SceneObjectModel snapshot);
    System.Collections.Generic.IEnumerable<PropertyDescriptor> GetProperties(
        SceneObjectModel snapshot,
        ISceneEditService edits);
}
```

`CommonPropertyProvider` supplies callbacks to `Rename`, `SetTransform`, and `SetVisible`. `PropertiesPanelController` invokes `descriptor.Apply(value)` and removes its hardcoded property-key switch. Render every supporting provider, not only the first.

For every `SceneComponentData` without a registered codec, append a read-only “Unknown component” row showing its exact type ID and schema version. Never parse or rewrite its `PayloadJson`.

- [ ] **Step 5: Replace hardcoded hierarchy icons and library primitives**

Register core descriptors explicitly:

```text
com.unitysimulationx.scene.group          → "GR"
com.unitysimulationx.scene.primitive      → "PR"
com.unitysimulationx.scene.imported-model → "3D"
com.unitysimulationx.scene.missing-asset  → "!!"
```

Unknown IDs use `"OB"` without changing their identity.

Register primitive creation through `SceneObjectFactoryRegistry`; `LibraryPanelController` enumerates factory descriptors instead of hardcoding primitive buttons.

- [ ] **Step 6: Compose and freeze in AppBootstrap**

Register core importers, type descriptors, codecs, factories, and property providers explicitly. Freeze all registries after registration, then pass read access to consumers. Do not scan assemblies.

- [ ] **Step 7: Run registry, properties, hierarchy, library, and shell tests**

Expected: duplicate registrations fail deterministically; multiple property providers render; provider-owned callbacks produce edit-service changes.

- [ ] **Step 8: Commit**

```bash
git add Assets/Editing Assets/UI Assets/Import Assets/App/Bootstrap Assets/Tests/EditMode
git commit -m "feat(editor): add deterministic contribution registries"
```

---

### Task 11: Remove Legacy Mutation Paths and Verify the Complete Editor Story

**Files:**
- Delete: obsolete `SceneObjectModel.CommonProperties`, `DomainProperties`, `RuntimeBindings`, `Diagnostics`, and `PrimitiveMeshTypeKey` fields after migration to core fields/component envelopes.
- Delete: `Assets/SceneModel/Core/RuntimeBinding.cs`
- Delete: `Assets/SceneModel/Diagnostics/DiagnosticMarker.cs`
- Delete: unused runtime/diagnostic schema stubs from `Assets/SceneModel/Serialization/ProjectViewerSchema.cs`.
- Delete: legacy `SceneObjectChangedEvent` and `HierarchyChangedEvent` from `Assets/Core/Bootstrap/Events.cs` after every subscriber uses `SceneChangedEvent`.
- Modify: `Assets/App/Bootstrap/AppBootstrap.cs`
- Modify: `README.md`
- Modify: `SPRINTS_1-4_STATUS.md`
- Create: `Assets/Tests/EditMode/ArchitectureBoundaryTests.cs`
- Create: `Assets/Tests/PlayMode/ProjectRoundTripPlayModeTests.cs`
- Modify: `Assets/Tests/PlayMode/SelectionTests.cs`

**Interfaces:**
- Verifies: no feature bypasses edit service, SceneModel has no projection types, project round-trip is complete, and documented scope matches implementation.

- [ ] **Step 1: Write architecture guard tests**

```csharp
[Test]
public void SceneModelAssembly_ContainsNoMonoBehaviours()
{
    var offenders = typeof(SceneObjectModel).Assembly.GetTypes()
        .Where(type => typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(type))
        .Select(type => type.FullName)
        .ToArray();
    CollectionAssert.IsEmpty(offenders);
}

[Test]
public void SceneModelAssembly_ContainsNoGameObjectFieldsOrProperties()
{
    var forbidden = new[]
    {
        typeof(UnityEngine.GameObject),
        typeof(UnityEngine.Transform),
        typeof(UnityEngine.Mesh),
        typeof(UnityEngine.Material),
        typeof(UnityEngine.Renderer),
        typeof(UnityEngine.Shader)
    };
    var offenders = typeof(SceneObjectModel).Assembly.GetTypes()
        .Where(type => type.GetFields().Any(field => forbidden.Contains(field.FieldType)) ||
                       type.GetProperties().Any(property => forbidden.Contains(property.PropertyType)))
        .Select(type => type.FullName)
        .ToArray();
    CollectionAssert.IsEmpty(offenders);
}
```

- [ ] **Step 2: Write complete PlayMode round-trip tests**

Add tests that:

1. create a temporary workspace;
2. import the existing OBJ triangle fixture;
3. rename, transform, and reparent through `ISceneEditService`;
4. save to a persistent test project folder;
5. clear selection and load;
6. assert hierarchy, transform, material, relative asset path, mesh/collider, and pickability;
7. delete the imported asset and reload;
8. assert a visible selectable missing-asset placeholder retains the original object ID/name.

- [ ] **Step 3: Delete compatibility fields and direct mutation paths**

Run:

```bash
rg "_registry\\.(Add|Replace|Remove|Reparent)|registry\\.(Add|Replace|Remove|Reparent)" \
  Assets/UI Assets/Viewer Assets/Import Assets/App --glob "*.cs"
```

Expected: only `SceneEditService` and project staging helpers mutate concrete registry state.

Run:

```bash
rg "CommonProperties|DomainProperties|RuntimeBindings|Diagnostics|PrimitiveMeshTypeKey|ServiceLocatorBridge|ISceneObjectMapper|SceneObjectType" \
  Assets --glob "*.cs"
```

Expected: no production references. Schema-version-1 DTOs may retain lower-cased serialized field names required for migration.

- [ ] **Step 4: Run complete EditMode suite**

```bash
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -runTests \
  -testPlatform editmode \
  -testResults Artifacts/final-editmode.xml \
  -logFile Artifacts/final-editmode.log
```

Expected: zero failed EditMode tests.

- [ ] **Step 5: Run complete PlayMode suite**

```bash
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -runTests \
  -testPlatform playmode \
  -testResults Artifacts/final-playmode.xml \
  -logFile Artifacts/final-playmode.log
```

Expected: zero failed PlayMode tests, including import → save → load → select and missing-asset placeholder.

- [ ] **Step 6: Update project documentation**

Document:

- the final asmdef dependency graph;
- `SceneModel → Editing → adapters → App` responsibilities;
- project folder structure;
- schema version 2 and version 1 migration;
- the deliberate absence of domain libraries/runtime binding/plugin discovery;
- GLB's typed “adapter unavailable” behavior until its planned package sprint.

- [ ] **Step 7: Commit**

```bash
git add Assets README.md SPRINTS_1-4_STATUS.md
git commit -m "docs: finalize editor architecture foundation"
```

## Final Acceptance Checklist

- [ ] Open custom type IDs survive model and project round-trip exactly.
- [ ] Unknown component envelopes preserve `payloadJson` exactly.
- [ ] Registry rejects missing parents, cycles, duplicate IDs, and duplicate component IDs atomically.
- [ ] SceneModel assembly owns no GameObject projection.
- [ ] All feature edits pass through `ISceneEditService`.
- [ ] Each committed edit updates projection once and publishes one immutable change set.
- [ ] Unsaved sessions use a disposable temporary workspace.
- [ ] Saved projects contain only project-relative imported-asset paths.
- [ ] OBJ/STL import → save → load restores hierarchy, transform, mesh, collider, and selection.
- [ ] Invalid project documents leave the active project untouched.
- [ ] Missing asset files create metadata-preserving selectable placeholders.
- [ ] Schema version 1 loads through an explicit migration.
- [ ] Contribution registries reject duplicate and late registrations.
- [ ] No plugin runtime, domain library, runtime binding, glTFast dependency, or undo stack was added.
- [ ] Complete EditMode and PlayMode suites report zero failures.
