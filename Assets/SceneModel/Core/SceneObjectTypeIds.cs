namespace UnitySimulationX.SceneModel
{
    public static class SceneObjectTypeIds
    {
        public static readonly SceneObjectTypeId Group =
            new("com.unitysimulationx.scene.group");
        public static readonly SceneObjectTypeId Primitive =
            new("com.unitysimulationx.scene.primitive");
        public static readonly SceneObjectTypeId ImportedModel =
            new("com.unitysimulationx.scene.imported-model");
        public static readonly SceneObjectTypeId MissingAsset =
            new("com.unitysimulationx.scene.missing-asset");
    }
}
