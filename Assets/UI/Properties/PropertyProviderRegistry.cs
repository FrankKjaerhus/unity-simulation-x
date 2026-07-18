using System;
using System.Collections.Generic;
using System.Linq;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.UI.Properties
{
    public sealed class PropertyProviderRegistry
    {
        readonly Dictionary<string, IPropertyProvider> _providersById =
            new(StringComparer.Ordinal);

        IReadOnlyList<IPropertyProvider> _orderedProviders = Array.Empty<IPropertyProvider>();
        bool _isFrozen;

        public void Register(IPropertyProvider provider)
        {
            EnsureNotFrozen();

            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (string.IsNullOrWhiteSpace(provider.ProviderId))
                throw new ArgumentException("ProviderId is required.", nameof(provider));
            if (_providersById.ContainsKey(provider.ProviderId))
                throw new InvalidOperationException(
                    $"A property provider is already registered for '{provider.ProviderId}'.");

            _providersById.Add(provider.ProviderId, provider);
            _orderedProviders = null;
        }

        public void Freeze() => _isFrozen = true;

        public IReadOnlyList<IPropertyProvider> GetProviders(SceneObjectModel snapshot)
        {
            if (snapshot == null)
                return Array.Empty<IPropertyProvider>();

            _orderedProviders ??= _providersById.Values
                .OrderBy(provider => provider.Order)
                .ThenBy(provider => provider.ProviderId, StringComparer.Ordinal)
                .ToArray();

            return _orderedProviders
                .Where(provider => provider.Supports(snapshot))
                .ToArray();
        }

        void EnsureNotFrozen()
        {
            if (_isFrozen)
                throw new InvalidOperationException("The property provider registry has been frozen.");
        }
    }
}
