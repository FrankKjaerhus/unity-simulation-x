using System;
using System.Collections.Generic;
using UnityEngine;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Import
{
    public sealed class PrimitiveFactory : IPrimitiveFactory, ISceneObjectFactory
    {
        readonly ISceneEditService _edits;
        readonly PrimitiveMeshComponentCodec _primitiveMeshCodec;

        static readonly IReadOnlyList<string> SupportedVariantIds = new[]
        {
            nameof(PrimitiveMeshType.Cube),
            nameof(PrimitiveMeshType.Cylinder),
            nameof(PrimitiveMeshType.Sphere),
            nameof(PrimitiveMeshType.Capsule),
            nameof(PrimitiveMeshType.Cone),
            nameof(PrimitiveMeshType.Plane)
        };

        public PrimitiveFactory(ISceneEditService edits, PrimitiveMeshComponentCodec primitiveMeshCodec)
        {
            _edits = edits;
            _primitiveMeshCodec = primitiveMeshCodec;
        }

        public string FactoryId => "primitive";
        public SceneObjectTypeId TypeId => SceneObjectTypeIds.Primitive;
        public string DisplayName => "Primitive";
        public int Order => 0;
        public IReadOnlyList<string> VariantIds => SupportedVariantIds;

        public SceneObjectModel CreatePrimitive(PrimitiveMeshType type, PrimitiveSettings settings)
        {
            var draft = CreateDraft(type, settings ?? new PrimitiveSettings());
            var result = _edits.Create(draft);
            if (!result.Succeeded)
                return null;

            return _edits.Registry.Get(draft.Id);
        }

        public SceneObjectDraft Create(string variantId, string name, string parentId)
        {
            if (!Enum.TryParse(variantId, ignoreCase: false, out PrimitiveMeshType type))
                throw new ArgumentException($"Unsupported primitive variant '{variantId}'.", nameof(variantId));

            var settings = new PrimitiveSettings
            {
                Name = string.IsNullOrWhiteSpace(name) ? type.ToString() : name,
                Position = parentId == null
                    ? new Vector3(UnityEngine.Random.Range(-2f, 2f), 0.5f, UnityEngine.Random.Range(-2f, 2f))
                    : UnityEngine.Random.insideUnitSphere * 0.5f,
                ParentId = parentId
            };

            return CreateDraft(type, settings);
        }

        SceneObjectDraft CreateDraft(PrimitiveMeshType type, PrimitiveSettings settings)
        {
            settings ??= new PrimitiveSettings();

            var meshTypeKey = type.ToString();
            var draft = new SceneObjectDraft
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = string.IsNullOrWhiteSpace(settings.Name) ? meshTypeKey : settings.Name,
                TypeId = SceneObjectTypeIds.Primitive,
                ParentId = settings.ParentId,
                Transform = new TransformData
                {
                    Position = settings.Position,
                    RotationEuler = settings.RotationEuler,
                    Scale = settings.Scale
                },
                Material = settings.Material ?? new MaterialDefinition()
            };

            draft.Components.Add(_primitiveMeshCodec.Encode(new PrimitiveMeshComponent
            {
                MeshTypeKey = meshTypeKey
            }));

            return draft;
        }
    }
}
