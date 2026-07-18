using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnitySimulationX.Import
{
    public sealed class ImporterRegistry
    {
        readonly Dictionary<string, ISceneAssetImporter> _importersById =
            new(System.StringComparer.Ordinal);

        IReadOnlyList<ISceneAssetImporter> _orderedImporters = System.Array.Empty<ISceneAssetImporter>();
        bool _isFrozen;

        public void Register(ISceneAssetImporter importer)
        {
            EnsureNotFrozen();

            if (importer == null)
                throw new System.ArgumentNullException(nameof(importer));
            if (string.IsNullOrWhiteSpace(importer.ImporterId))
                throw new System.ArgumentException("ImporterId is required.", nameof(importer));
            if (_importersById.ContainsKey(importer.ImporterId))
            {
                throw new System.InvalidOperationException(
                    $"An importer is already registered for '{importer.ImporterId}'.");
            }

            _importersById.Add(importer.ImporterId, importer);
            _orderedImporters = null;
        }

        public void Freeze() => _isFrozen = true;

        public ISceneAssetImporter Resolve(string path)
        {
            var extension = Path.GetExtension(path)?.ToLowerInvariant();
            _orderedImporters ??= _importersById.Values
                .OrderBy(importer => importer.ImporterId, System.StringComparer.Ordinal)
                .ToArray();

            return _orderedImporters.FirstOrDefault(importer => importer.CanImport(extension));
        }

        void EnsureNotFrozen()
        {
            if (_isFrozen)
                throw new System.InvalidOperationException("The importer registry has been frozen.");
        }
    }
}
