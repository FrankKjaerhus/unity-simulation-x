using System;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.App.ProjectSystem
{
    public sealed class MissingAssetFactory
    {
        public SceneObjectModel Create(SceneObjectModel snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            var placeholder = snapshot.Clone();
            placeholder.TypeId = SceneObjectTypeIds.MissingAsset;
            placeholder.PrimitiveMeshTypeKey = null;
            return placeholder;
        }
    }
}
