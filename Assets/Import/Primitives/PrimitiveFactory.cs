using System;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Import
{
    public sealed class PrimitiveFactory : IPrimitiveFactory
    {
        readonly SceneRegistry _registry;
        readonly ISceneObjectMapper _mapper;

        public PrimitiveFactory(SceneRegistry registry, ISceneObjectMapper mapper)
        {
            _registry = registry;
            _mapper = mapper;
        }

        public SceneObjectModel CreatePrimitive(PrimitiveMeshType type, PrimitiveSettings settings)
        {
            settings ??= new PrimitiveSettings();

            var model = new SceneObjectModel
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = string.IsNullOrWhiteSpace(settings.Name) ? type.ToString() : settings.Name,
                Type = SceneObjectType.Primitive,
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

            _registry.Add(model);
            _mapper.CreateGameObject(model);

            EventBus.Publish(new HierarchyChangedEvent());
            EventBus.Publish(new SceneObjectChangedEvent { ObjectId = model.Id, Model = model });

            return model;
        }
    }
}
