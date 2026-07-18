using System;
using UnityEngine;

namespace UnitySimulationX.Import
{
    [Serializable]
    public sealed class PrimitiveMeshComponent
    {
        [SerializeField] string meshTypeKey;

        public string MeshTypeKey
        {
            get => meshTypeKey;
            set => meshTypeKey = value;
        }
    }
}
