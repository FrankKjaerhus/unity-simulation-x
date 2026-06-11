using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnitySimulationX.Import
{
    public sealed class ImporterRegistry
    {
        readonly List<ISceneAssetImporter> _importers = new();

        public void Register(ISceneAssetImporter importer)
        {
            if (importer != null && !_importers.Contains(importer))
                _importers.Add(importer);
        }

        public ISceneAssetImporter Resolve(string path)
        {
            var extension = Path.GetExtension(path)?.ToLowerInvariant();
            return _importers.FirstOrDefault(importer => importer.CanImport(extension));
        }
    }
}
