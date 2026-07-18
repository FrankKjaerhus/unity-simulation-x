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
