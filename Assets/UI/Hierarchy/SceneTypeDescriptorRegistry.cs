using System;
using System.Collections.Generic;
using System.Linq;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.UI.Hierarchy
{
    public sealed class SceneTypeDescriptor
    {
        public SceneTypeDescriptor(SceneObjectTypeId typeId, string displayName, string iconText, int order = 0)
        {
            if (string.IsNullOrWhiteSpace(typeId.Value))
                throw new ArgumentException("TypeId is required.", nameof(typeId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("DisplayName is required.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(iconText))
                throw new ArgumentException("IconText is required.", nameof(iconText));

            TypeId = typeId;
            DisplayName = displayName;
            IconText = iconText;
            Order = order;
        }

        public SceneObjectTypeId TypeId { get; }
        public string DisplayName { get; }
        public string IconText { get; }
        public int Order { get; }
    }

    public sealed class SceneTypeDescriptorRegistry
    {
        readonly Dictionary<string, SceneTypeDescriptor> _descriptorsByTypeId =
            new(StringComparer.Ordinal);

        IReadOnlyList<SceneTypeDescriptor> _orderedDescriptors = Array.Empty<SceneTypeDescriptor>();
        bool _isFrozen;

        public void Register(SceneTypeDescriptor descriptor)
        {
            EnsureNotFrozen();

            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            if (_descriptorsByTypeId.ContainsKey(descriptor.TypeId.Value))
                throw new InvalidOperationException(
                    $"A scene type descriptor is already registered for '{descriptor.TypeId.Value}'.");

            _descriptorsByTypeId.Add(descriptor.TypeId.Value, descriptor);
            _orderedDescriptors = null;
        }

        public void Freeze() => _isFrozen = true;

        public IReadOnlyList<SceneTypeDescriptor> GetDescriptors()
        {
            _orderedDescriptors ??= _descriptorsByTypeId.Values
                .OrderBy(descriptor => descriptor.Order)
                .ThenBy(descriptor => descriptor.TypeId.Value, StringComparer.Ordinal)
                .ToArray();

            return _orderedDescriptors;
        }

        public bool TryGet(SceneObjectTypeId typeId, out SceneTypeDescriptor descriptor)
        {
            if (string.IsNullOrWhiteSpace(typeId.Value))
            {
                descriptor = null;
                return false;
            }

            return _descriptorsByTypeId.TryGetValue(typeId.Value, out descriptor);
        }

        void EnsureNotFrozen()
        {
            if (_isFrozen)
                throw new InvalidOperationException("The scene type descriptor registry has been frozen.");
        }
    }
}
