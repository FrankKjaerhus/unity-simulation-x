using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnitySimulationX.SceneModel.Serialization;

namespace UnitySimulationX.Core
{
    public interface IProjectAssetStore
    {
        Task<ProjectAssetDocumentData> ImportExternalFileAsync(
            string sourcePath,
            string importerId,
            int importerVersion,
            string importSettingsJson,
            CancellationToken cancellationToken);

        ProjectAssetDocumentData Get(string assetId);
        IReadOnlyList<ProjectAssetDocumentData> GetAll();
        string Resolve(string assetId);
        void ReplaceCatalog(IReadOnlyList<ProjectAssetDocumentData> entries);
        Task CopyAllToAsync(string targetProjectRoot, CancellationToken cancellationToken);
        void Remove(string assetId);
    }
}
