using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitySimulationX.Editing
{
    public sealed class SceneComponentCodecRegistry
    {
        readonly Dictionary<string, ISceneComponentCodec> _codecsByTypeId =
            new(StringComparer.Ordinal);

        IReadOnlyList<ISceneComponentCodec> _orderedCodecs = Array.Empty<ISceneComponentCodec>();
        bool _isFrozen;

        public void Register(ISceneComponentCodec codec)
        {
            EnsureNotFrozen();

            if (codec == null)
                throw new ArgumentNullException(nameof(codec));
            if (string.IsNullOrWhiteSpace(codec.ComponentTypeId))
                throw new ArgumentException("ComponentTypeId is required.", nameof(codec));
            if (_codecsByTypeId.ContainsKey(codec.ComponentTypeId))
                throw new InvalidOperationException(
                    $"A scene component codec is already registered for '{codec.ComponentTypeId}'.");

            _codecsByTypeId.Add(codec.ComponentTypeId, codec);
            _orderedCodecs = null;
        }

        public void Freeze() => _isFrozen = true;

        public IReadOnlyList<ISceneComponentCodec> GetCodecs()
        {
            _orderedCodecs ??= _codecsByTypeId.Values
                .OrderBy(codec => codec.ComponentTypeId, StringComparer.Ordinal)
                .ToArray();

            return _orderedCodecs;
        }

        public bool TryGet(string componentTypeId, out ISceneComponentCodec codec)
        {
            if (string.IsNullOrWhiteSpace(componentTypeId))
            {
                codec = null;
                return false;
            }

            return _codecsByTypeId.TryGetValue(componentTypeId, out codec);
        }

        void EnsureNotFrozen()
        {
            if (_isFrozen)
                throw new InvalidOperationException("The scene component codec registry has been frozen.");
        }
    }
}
