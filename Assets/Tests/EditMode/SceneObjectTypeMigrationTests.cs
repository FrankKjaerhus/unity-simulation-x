using NUnit.Framework;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class SceneObjectTypeMigrationTests
    {
        [TestCase("Primitive", "com.unitysimulationx.scene.primitive")]
        [TestCase("ImportedAsset", "com.unitysimulationx.scene.imported-model")]
        [TestCase("MachineFrame", "com.unitysimulationx.scene.group")]
        [TestCase("TrackSegment", "com.unitysimulationx.legacy.track-segment")]
        [TestCase("UnknownFutureValue", "com.unitysimulationx.legacy.unknown-future-value")]
        public void FromV1Type_PreservesStableMeaning(string legacy, string expected)
        {
            Assert.AreEqual(expected, LegacySceneObjectTypeMigration.FromV1Type(legacy).Value);
        }

        [Test]
        public void CustomTypeId_RemainsExactOnModelClone()
        {
            var model = new SceneObjectModel
            {
                Id = "custom",
                Name = "Custom",
                TypeId = new SceneObjectTypeId("com.vendor.product.custom-object")
            };
            Assert.AreEqual(model.TypeId, model.Clone().TypeId);
        }
    }
}
