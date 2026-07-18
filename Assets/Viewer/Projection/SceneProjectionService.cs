using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Viewer.Projection
{
    public sealed class SceneProjectionService : ISceneProjectionService
    {
        readonly Dictionary<string, GameObject> _gameObjects = new();
        readonly Transform _sceneRoot;
        readonly ISceneRegistryRead _registry;
        readonly IImportedAssetProjectionProvider _importedAssetProjectionProvider;

        public SceneProjectionService(
            Transform sceneRoot,
            ISceneRegistryRead registry,
            IImportedAssetProjectionProvider importedAssetProjectionProvider = null)
        {
            _sceneRoot = sceneRoot;
            _registry = registry;
            _importedAssetProjectionProvider = importedAssetProjectionProvider;
        }

        public Transform SceneRoot => _sceneRoot;

        public void CreateProjection(SceneObjectModel snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.Id))
                return;

            if (_gameObjects.ContainsKey(snapshot.Id))
                RemoveProjection(snapshot.Id);

            var go = new GameObject(snapshot.Name ?? snapshot.Id);
            var idComponent = go.AddComponent<SceneObjectIdComponent>();
            idComponent.SceneObjectId = snapshot.Id;

            if (snapshot.TypeId.Equals(SceneObjectTypeIds.MissingAsset))
            {
                ApplyMissingAssetPlaceholder(go);
            }
            else if (!string.IsNullOrWhiteSpace(snapshot.AssetId))
            {
                _importedAssetProjectionProvider?.TryApply(snapshot.AssetId, go);
            }
            else if (!string.IsNullOrEmpty(snapshot.PrimitiveMeshTypeKey))
            {
                ApplyPrimitiveMesh(go, snapshot.PrimitiveMeshTypeKey);
            }

            ApplyTransform(go.transform, snapshot.Transform);
            ApplyVisibility(go, snapshot.Visible);
            if (!snapshot.TypeId.Equals(SceneObjectTypeIds.MissingAsset))
                ApplyMaterial(go, snapshot.Material);
            ParentGameObject(go, snapshot.ParentId);

            _gameObjects[snapshot.Id] = go;
        }

        public void RegisterExistingTarget(string objectId, GameObject target)
        {
            if (string.IsNullOrEmpty(objectId) || target == null)
                return;

            if (_gameObjects.TryGetValue(objectId, out var existing) && existing != target)
                RemoveProjection(objectId);

            var idComponent = target.GetComponent<SceneObjectIdComponent>();
            if (idComponent == null)
                idComponent = target.AddComponent<SceneObjectIdComponent>();

            idComponent.SceneObjectId = objectId;

            var snapshot = _registry.Get(objectId);
            if (snapshot != null)
                ParentGameObject(target, snapshot.ParentId, worldPositionStays: true);

            _gameObjects[objectId] = target;
        }

        public void UpdateProjection(SceneObjectModel snapshot)
        {
            if (snapshot == null || !_gameObjects.TryGetValue(snapshot.Id, out var target) || target == null)
                return;

            target.name = snapshot.Name ?? snapshot.Id;
            if (snapshot.TypeId.Equals(SceneObjectTypeIds.MissingAsset))
                ApplyMissingAssetPlaceholder(target);
            else if (!string.IsNullOrWhiteSpace(snapshot.AssetId))
                _importedAssetProjectionProvider?.TryApply(snapshot.AssetId, target);
            ApplyTransform(target.transform, snapshot.Transform);
            ApplyVisibility(target, snapshot.Visible);
            ParentGameObject(target, snapshot.ParentId);

            var idComponent = target.GetComponent<SceneObjectIdComponent>();
            if (idComponent != null)
                idComponent.SceneObjectId = snapshot.Id;
        }

        public void PreviewTransform(string objectId, TransformData transform)
        {
            if (!_gameObjects.TryGetValue(objectId, out var go) || go == null)
                return;

            ApplyTransform(go.transform, transform);
        }

        public void RemoveProjection(string objectId)
        {
            if (!_gameObjects.TryGetValue(objectId, out var go))
                return;

            _gameObjects.Remove(objectId);
            if (go != null)
                Object.Destroy(go);
        }

        public void ReplaceAllProjections(IReadOnlyList<SceneObjectModel> snapshots)
        {
            foreach (var id in _gameObjects.Keys.ToList())
                RemoveProjection(id);

            if (snapshots == null)
                return;

            foreach (var snapshot in snapshots)
                CreateProjection(snapshot);
        }

        public GameObject GetGameObject(string objectId) =>
            _gameObjects.TryGetValue(objectId, out var go) ? go : null;

        public string GetObjectId(GameObject gameObject)
        {
            if (gameObject == null)
                return null;

            var idComponent = gameObject.GetComponent<SceneObjectIdComponent>();
            return idComponent != null && !string.IsNullOrEmpty(idComponent.SceneObjectId)
                ? idComponent.SceneObjectId
                : null;
        }

        void ParentGameObject(GameObject go, string parentId, bool worldPositionStays = false)
        {
            if (!string.IsNullOrEmpty(parentId))
            {
                var parentGo = GetGameObject(parentId);
                if (parentGo != null)
                {
                    go.transform.SetParent(parentGo.transform, worldPositionStays);
                    return;
                }
            }

            go.transform.SetParent(_sceneRoot, worldPositionStays);
        }

        static void ApplyTransform(Transform transform, TransformData data)
        {
            data ??= new TransformData();
            transform.localPosition = data.Position;
            transform.localRotation = Quaternion.Euler(data.RotationEuler);
            transform.localScale = data.Scale;
        }

        static void ApplyVisibility(GameObject go, bool visible)
        {
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject.name == "SelectionOutline")
                    continue;

                renderer.enabled = visible;
            }
        }

        static void ApplyMaterial(GameObject go, MaterialDefinition definition)
        {
            if (definition == null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                return;

            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject.name == "SelectionOutline")
                    continue;

                var material = new Material(shader);
                material.color = definition.BaseColor;
                renderer.sharedMaterial = material;
            }
        }

        static void ApplyPrimitiveMesh(GameObject go, string meshTypeKey)
        {
            PrimitiveType? primitiveType = meshTypeKey switch
            {
                "Cube" => PrimitiveType.Cube,
                "Sphere" => PrimitiveType.Sphere,
                "Cylinder" => PrimitiveType.Cylinder,
                "Capsule" => PrimitiveType.Capsule,
                "Plane" => PrimitiveType.Plane,
                _ => null
            };

            Mesh mesh;
            if (primitiveType.HasValue)
            {
                var temp = GameObject.CreatePrimitive(primitiveType.Value);
                mesh = temp.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(temp);
            }
            else if (meshTypeKey == "Cone")
            {
                mesh = PrimitiveMeshBuilder.CreateCone();
            }
            else
            {
                return;
            }

            var meshFilter = go.GetComponent<MeshFilter>() ?? go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            if (go.GetComponent<MeshRenderer>() == null)
                go.AddComponent<MeshRenderer>();
            AddPickCollider(go, meshTypeKey, mesh);
        }

        static void AddPickCollider(GameObject go, string meshTypeKey, Mesh mesh)
        {
            if (go.GetComponent<Collider>() != null)
                return;

            switch (meshTypeKey)
            {
                case "Cube":
                    go.AddComponent<BoxCollider>();
                    break;
                case "Sphere":
                    go.AddComponent<SphereCollider>();
                    break;
                case "Capsule":
                    go.AddComponent<CapsuleCollider>();
                    break;
                case "Cylinder":
                    var cylinder = go.AddComponent<CapsuleCollider>();
                    cylinder.direction = 1;
                    cylinder.height = 2f;
                    cylinder.radius = 0.5f;
                    break;
                case "Plane":
                    var plane = go.AddComponent<BoxCollider>();
                    plane.size = new Vector3(10f, 0.01f, 10f);
                    plane.center = Vector3.zero;
                    break;
                default:
                    var meshCollider = go.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = mesh;
                    meshCollider.convex = true;
                    break;
            }
        }

        static void ApplyMissingAssetPlaceholder(GameObject go)
        {
            ApplyPrimitiveMesh(go, "Cube");

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                return;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;

            var material = new Material(shader);
            material.color = new Color(1f, 0.55f, 0.1f, 1f);
            renderer.sharedMaterial = material;
        }
    }
}
