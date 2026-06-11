using System;
using UnityEngine;

namespace UnitySimulationX.SceneModel
{
    [Serializable]
    public sealed class TransformData
    {
        public Vector3 Position;
        public Vector3 RotationEuler;
        public Vector3 Scale = Vector3.one;

        public TransformData Clone()
        {
            return new TransformData
            {
                Position = Position,
                RotationEuler = RotationEuler,
                Scale = Scale
            };
        }
    }
}
