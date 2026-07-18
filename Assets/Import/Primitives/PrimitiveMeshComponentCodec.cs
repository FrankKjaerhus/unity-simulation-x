using System;
using UnityEngine;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Import
{
    public sealed class PrimitiveMeshComponentCodec : ISceneComponentCodec
    {
        public const string PrimitiveMeshComponentTypeId = "com.unitysimulationx.scene.primitive-mesh";

        public string ComponentTypeId => PrimitiveMeshComponentTypeId;
        public int CurrentSchemaVersion => 1;
        public Type ComponentClrType => typeof(PrimitiveMeshComponent);

        public object Decode(SceneComponentData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!string.Equals(data.TypeId, ComponentTypeId, StringComparison.Ordinal))
                throw new ArgumentException($"Unsupported component type id '{data.TypeId}'.", nameof(data));
            if (data.SchemaVersion != CurrentSchemaVersion)
                throw new ArgumentException(
                    $"Unsupported schema version '{data.SchemaVersion}' for '{ComponentTypeId}'.",
                    nameof(data));

            return JsonUtility.FromJson<PrimitiveMeshComponent>(data.PayloadJson) ?? new PrimitiveMeshComponent();
        }

        public SceneComponentData Encode(object component)
        {
            if (component is not PrimitiveMeshComponent primitiveComponent)
                throw new ArgumentException(
                    $"Component must be a {nameof(PrimitiveMeshComponent)}.",
                    nameof(component));

            return new SceneComponentData(
                ComponentTypeId,
                CurrentSchemaVersion,
                JsonUtility.ToJson(primitiveComponent));
        }
    }
}
