using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Import
{
    public interface IPrimitiveFactory
    {
        SceneObjectModel CreatePrimitive(PrimitiveMeshType type, PrimitiveSettings settings);
    }
}
