using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Import
{
    public sealed class GltfSceneAssetImporter : ISceneAssetImporter
    {
        public bool CanImport(string fileExtension)
        {
            return fileExtension == ".glb" || fileExtension == ".gltf";
        }

        public async Task<ImportResult> ImportAsync(string filePath, ImportSettings settings)
        {
            var gltfImportType = ResolveGltfImportType();
            if (gltfImportType == null)
                throw new NotSupportedException("GLB/GLTF import requires Unity glTFast. Install package 'com.unity.cloud.gltfast' from Package Manager.");

            var gltf = Activator.CreateInstance(gltfImportType);
            var loadMethod = gltfImportType.GetMethods()
                .FirstOrDefault(method => method.Name == "Load" &&
                                          method.GetParameters().Length >= 1 &&
                                          method.GetParameters()[0].ParameterType == typeof(string) &&
                                          method.GetParameters().Skip(1).All(parameter => parameter.IsOptional));

            if (loadMethod == null)
                throw new MissingMethodException("GLTFast.GltfImport.Load(string) was not found.");

            var success = await InvokeBoolTask(loadMethod.Invoke(gltf, BuildArguments(loadMethod, filePath)));
            if (!success)
                throw new InvalidOperationException($"glTFast failed to load: {filePath}");

            var root = new GameObject(Path.GetFileNameWithoutExtension(filePath));
            var instantiateMethod = gltfImportType.GetMethods()
                .FirstOrDefault(method => method.Name == "InstantiateMainSceneAsync" &&
                                          method.GetParameters().Length == 1 &&
                                          method.GetParameters()[0].ParameterType == typeof(Transform));

            if (instantiateMethod == null)
                throw new MissingMethodException("GLTFast.GltfImport.InstantiateMainSceneAsync(Transform) was not found.");

            await InvokeTask(instantiateMethod.Invoke(gltf, new object[] { root.transform }));

            return new ImportResult
            {
                RootObject = new SceneObjectModel
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = root.name,
                    TypeId = SceneObjectTypeIds.ImportedModel
                },
                ImportedGameObject = root,
                Bounds = CalculateBounds(root)
            };
        }

        static Type ResolveGltfImportType()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("GLTFast.GltfImport"))
                .FirstOrDefault(type => type != null);
        }

        static async Task<bool> InvokeBoolTask(object value)
        {
            if (value is Task<bool> boolTask)
                return await boolTask;

            if (value is Task task)
            {
                await task;
                return true;
            }

            return value is bool result && result;
        }

        static async Task InvokeTask(object value)
        {
            if (value is Task task)
                await task;
        }

        static object[] BuildArguments(MethodInfo method, object firstArgument)
        {
            var parameters = method.GetParameters();
            var arguments = new object[parameters.Length];
            arguments[0] = firstArgument;

            for (var i = 1; i < arguments.Length; i++)
                arguments[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : Type.Missing;

            return arguments;
        }

        static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.zero);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }
    }
}
