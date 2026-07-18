using System;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Import
{
    public sealed class PrimitiveFactory : IPrimitiveFactory
    {
        readonly ISceneEditService _edits;

        public PrimitiveFactory(ISceneEditService edits)
        {
            _edits = edits;
        }

        public SceneObjectModel CreatePrimitive(PrimitiveMeshType type, PrimitiveSettings settings)
        {
            settings ??= new PrimitiveSettings();

            var draft = new SceneObjectDraft
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = string.IsNullOrWhiteSpace(settings.Name) ? type.ToString() : settings.Name,
                TypeId = SceneObjectTypeIds.Primitive,
                ParentId = settings.ParentId,
                Transform = new TransformData
                {
                    Position = settings.Position,
                    RotationEuler = settings.RotationEuler,
                    Scale = settings.Scale
                },
                Material = settings.Material ?? new MaterialDefinition(),
                PrimitiveMeshTypeKey = type.ToString()
            };

            var result = _edits.Create(draft);
            if (!result.Succeeded)
                return null;

            return _edits.Registry.Get(draft.Id);
        }
    }
}
