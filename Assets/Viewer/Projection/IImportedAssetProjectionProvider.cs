using UnityEngine;

namespace UnitySimulationX.Viewer.Projection
{
    public interface IImportedAssetProjectionProvider
    {
        bool TryApply(string assetId, GameObject target);
    }
}
