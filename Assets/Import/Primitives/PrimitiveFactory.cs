using System;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer.Projection;

namespace UnitySimulationX.Import
{
    public sealed class PrimitiveFactory : IPrimitiveFactory
    {
        readonly SceneRegistry _registry;
        readonly ISceneProjectionService _projection;

        public PrimitiveFactory(SceneRegistry registry, ISceneProjectionService projection)
        {
            _registry = registry;
            _projection = projection;
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
            _projection.CreateProjection(model);

            EventBus.Publish(new HierarchyChangedEvent());
            EventBus.Publish(new SceneObjectChangedEvent { ObjectId = model.Id, Model = model });

            return model;
        }
    }
}
