using NUnit.Framework;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class SceneExtensibilityDataTests
    {
        [Test]
        public void SceneObjectTypeId_AcceptsLowercaseReverseDnsId()
        {
            var id = new SceneObjectTypeId("com.vendor.product.track-segment");
            Assert.AreEqual("com.vendor.product.track-segment", id.Value);
        }

        [TestCase("")]
        [TestCase("Primitive")]
        [TestCase("com.vendor.BadType")]
        [TestCase("com..vendor.type")]
        public void SceneObjectTypeId_RejectsInvalidId(string value)
        {
            Assert.Throws<System.ArgumentException>(() => new SceneObjectTypeId(value));
        }

        [Test]
        public void SceneComponentData_ClonePreservesOpaquePayloadExactly()
        {
            const string payload = "{\"future\":true,\"order\":[3,2,1]}";
            var source = new SceneComponentData(
                "com.vendor.product.component",
                7,
                payload);

            var clone = source.Clone();

            Assert.AreEqual(source.TypeId, clone.TypeId);
            Assert.AreEqual(source.SchemaVersion, clone.SchemaVersion);
            Assert.AreEqual(payload, clone.PayloadJson);
        }
    }
}
