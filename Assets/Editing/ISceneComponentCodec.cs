using System;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Editing
{
    public interface ISceneComponentCodec
    {
        string ComponentTypeId { get; }
        int CurrentSchemaVersion { get; }
        Type ComponentClrType { get; }
        object Decode(SceneComponentData data);
        SceneComponentData Encode(object component);
    }
}
