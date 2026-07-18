using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitySimulationX.Editing
{
    public sealed class SceneObjectFactoryRegistry
    {
        readonly Dictionary<string, ISceneObjectFactory> _factoriesById =
            new(StringComparer.Ordinal);

        IReadOnlyList<ISceneObjectFactory> _orderedFactories = Array.Empty<ISceneObjectFactory>();
        bool _isFrozen;

        public void Register(ISceneObjectFactory factory)
        {
            EnsureNotFrozen();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (string.IsNullOrWhiteSpace(factory.FactoryId))
                throw new ArgumentException("FactoryId is required.", nameof(factory));
            if (string.IsNullOrWhiteSpace(factory.TypeId.Value))
                throw new ArgumentException("TypeId is required.", nameof(factory));
            if (_factoriesById.ContainsKey(factory.FactoryId))
                throw new InvalidOperationException($"A factory is already registered for '{factory.FactoryId}'.");

            _factoriesById.Add(factory.FactoryId, factory);
            _orderedFactories = null;
        }

        public void Freeze() => _isFrozen = true;

        public IReadOnlyList<ISceneObjectFactory> GetFactories()
        {
            _orderedFactories ??= _factoriesById.Values
                .OrderBy(factory => factory.Order)
                .ThenBy(factory => factory.FactoryId, StringComparer.Ordinal)
                .ToArray();

            return _orderedFactories;
        }

        public bool TryGetFactory(string factoryId, out ISceneObjectFactory factory)
        {
            if (string.IsNullOrWhiteSpace(factoryId))
            {
                factory = null;
                return false;
            }

            return _factoriesById.TryGetValue(factoryId, out factory);
        }

        void EnsureNotFrozen()
        {
            if (_isFrozen)
                throw new InvalidOperationException("The scene object factory registry has been frozen.");
        }
    }
}
