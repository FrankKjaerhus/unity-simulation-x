using System.Collections.Generic;
using UnityEngine;

namespace UnitySimulationX.SceneModel
{
    public sealed class SceneObjectMapper : ISceneObjectMapper
    {
        readonly Dictionary<string, GameObject> _gameObjects = new();
        readonly Transform _sceneRoot;

        public SceneObjectMapper(Transform sceneRoot)
        {
            _sceneRoot = sceneRoot;
        }

        public Transform SceneRoot => _sceneRoot;

        public GameObject CreateGameObject(SceneObjectModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Id))
                return null;

            if (_gameObjects.ContainsKey(model.Id))
                DestroyGameObject(model.Id);

            var go = new GameObject(model.Name ?? model.Id);
            var idComponent = go.AddComponent<SceneObjectIdComponent>();
            idComponent.SceneObjectId = model.Id;

            if (!string.IsNullOrEmpty(model.PrimitiveMeshTypeKey))
            {
                ApplyPrimitiveMesh(go, model.PrimitiveMeshTypeKey);
            }

            ApplyTransform(go.transform, model);
            ApplyVisibility(go, model.Visible);
            ApplyMaterial(go, model.Material);
            ParentGameObject(go, model.ParentId);

            _gameObjects[model.Id] = go;
            return go;
        }

        public GameObject RegisterExistingGameObject(SceneObjectModel model, GameObject target)
        {
            if (model == null || string.IsNullOrEmpty(model.Id) || target == null)
                return null;

            if (_gameObjects.TryGetValue(model.Id, out var existing) && existing != target)
                DestroyGameObject(model.Id);

            var idComponent = target.GetComponent<SceneObjectIdComponent>();
            if (idComponent == null)
                idComponent = target.AddComponent<SceneObjectIdComponent>();

            idComponent.SceneObjectId = model.Id;
            ParentGameObject(target, model.ParentId, worldPositionStays: true);
            _gameObjects[model.Id] = target;
            return target;
        }

        public void UpdateGameObject(SceneObjectModel model, GameObject target)
        {
            if (model == null || target == null)
                return;

            target.name = model.Name ?? model.Id;
            ApplyTransform(target.transform, model);
            ApplyVisibility(target, model.Visible);
            ParentGameObject(target, model.ParentId);

            var idComponent = target.GetComponent<SceneObjectIdComponent>();
            if (idComponent != null)
                idComponent.SceneObjectId = model.Id;
        }

        public void DestroyGameObject(string sceneObjectId)
        {
            if (!_gameObjects.TryGetValue(sceneObjectId, out var go))
                return;

            _gameObjects.Remove(sceneObjectId);
            if (go != null)
                Object.Destroy(go);
        }

        public GameObject GetGameObject(string sceneObjectId)
        {
            return _gameObjects.TryGetValue(sceneObjectId, out var go) ? go : null;
        }

        public SceneObjectModel GetModel(GameObject gameObject)
        {
            if (gameObject == null)
                return null;

            var idComponent = gameObject.GetComponent<SceneObjectIdComponent>();
            if (idComponent == null || string.IsNullOrEmpty(idComponent.SceneObjectId))
                return null;

            return ServiceLocatorBridge.TryGetRegistry()?.Get(idComponent.SceneObjectId);
        }

        void ParentGameObject(GameObject go, string parentId)
        {
            if (!string.IsNullOrEmpty(parentId))
            {
                var parentGo = GetGameObject(parentId);
                if (parentGo != null)
                {
                    go.transform.SetParent(parentGo.transform, false);
                    return;
                }
            }

            go.transform.SetParent(_sceneRoot, false);
        }

        void ParentGameObject(GameObject go, string parentId, bool worldPositionStays)
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

        static void ApplyTransform(Transform transform, SceneObjectModel model)
        {
            var data = model.Transform ?? new TransformData();
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
                mesh = ConeMeshGenerator.Create();
            }
            else
            {
                return;
            }

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            AddPickCollider(go, meshTypeKey, mesh);
        }

        static void AddPickCollider(GameObject go, string meshTypeKey, Mesh mesh)
        {
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
    }

    /// <summary>
    /// Avoids circular asmdef reference from SceneModel to App.
    /// </summary>
    public static class ServiceLocatorBridge
    {
        static System.Func<SceneRegistry> _registryResolver;

        public static void SetRegistryResolver(System.Func<SceneRegistry> resolver)
        {
            _registryResolver = resolver;
        }

        public static SceneRegistry TryGetRegistry() => _registryResolver?.Invoke();
    }
}
